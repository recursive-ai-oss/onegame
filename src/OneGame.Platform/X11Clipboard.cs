using System.Text;

namespace OneGame.Platform;

// Clipboard on X11, following the ICCCM selection protocol (section 2): the owner of a selection answers SelectionRequest events by
// writing the data into a property of the requestor's window and sending SelectionNotify; a reader asks with XConvertSelection and
// waits for SelectionNotify. Large transfers use the INCR protocol (section 2.7.2); only the reading side supports it.
internal sealed unsafe partial class X11Backend
{
    private const int ReadTimeoutMilliseconds = 1000;
    private const int XaAtom = 4, XaString = 31;

    private readonly string?[] _owned = new string?[2];
    private nuint _selWindow, _aClipboard, _aPrimary, _aTargets, _aIncr, _aTextMime, _aText, _aTransfer;
    private bool _gotNotify, _gotPropertyChange;
    private nuint _notifyProperty;

    private void InitClipboard()
    {
        // Selections need a window to own them and to receive replies on. It is never mapped.
        _selWindow = Xlib.CreateSimpleWindow(_display, _root, 0, 0, 1, 1, 0, 0, 0);
        Xlib.SelectInput(_display, _selWindow, (nint)Xlib.PropertyChangeMask);
        _aClipboard = Atom("CLIPBOARD");
        _aPrimary = Atom("PRIMARY");
        _aTargets = Atom("TARGETS");
        _aIncr = Atom("INCR");
        _aTextMime = Atom("text/plain;charset=utf-8");
        _aText = Atom("TEXT");
        _aTransfer = Atom("ONEGAME_SELECTION");
    }

    public override bool ClipboardAvailable => _selWindow != 0;

    public override bool ClipboardSupports(ClipboardSelection selection) => ClipboardAvailable;

    private nuint SelectionAtom(ClipboardSelection s) => s == ClipboardSelection.Primary ? _aPrimary : _aClipboard;

    // Properties travel in one protocol request, so the text cannot be bigger than the server accepts.
    private int MaxPropertyBytes()
    {
        var words = Xlib.ExtendedMaxRequestSize(_display);
        if (words <= 0)
        {
            words = Xlib.MaxRequestSize(_display);
        }

        return (int)Math.Clamp((words * 4) - 64, 0, PlatformClipboard.MaxTextBytes);
    }

    public override bool ClipboardSet(ClipboardSelection selection, string text)
    {
        if (Encoding.UTF8.GetByteCount(text) > MaxPropertyBytes())
        {
            return false;
        }

        var atom = SelectionAtom(selection);
        _owned[(int)selection] = text;
        Xlib.SetSelectionOwner(_display, atom, _selWindow, 0);
        Xlib.Flush(_display);

        // The server does not report a refused claim; asking who owns it afterwards is the check.
        if (Xlib.GetSelectionOwner(_display, atom) != _selWindow)
        {
            _owned[(int)selection] = null;
            return false;
        }

        return true;
    }

    public override string? ClipboardGet(ClipboardSelection selection)
    {
        if (_owned[(int)selection] is { } own)
        {
            return own;
        }

        var atom = SelectionAtom(selection);
        if (Xlib.GetSelectionOwner(_display, atom) == 0)
        {
            return null;
        }

        foreach (var target in new[] { _utf8String, (nuint)XaString })
        {
            var text = Fetch(atom, target);
            if (text is not null)
            {
                return text;
            }
        }

        return null;
    }

    private string? Fetch(nuint selection, nuint target)
    {
        // Settle anything already in flight so a stale reply is not mistaken for this one's.
        DrainEvents();
        Xlib.DeleteProperty(_display, _selWindow, _aTransfer);
        _gotNotify = false;
        _notifyProperty = 0;
        Xlib.ConvertSelection(_display, selection, target, _aTransfer, _selWindow, 0);
        Xlib.Flush(_display);
        if (!WaitFor(() => _gotNotify) || _notifyProperty == 0)
        {
            return null;
        }

        var limit = PlatformClipboard.MaxTextBytes;
        if (!ReadProperty(limit, out var type, out var first))
        {
            return null;
        }

        byte[] bytes;
        if (type == _aIncr)
        {
            // The owner sends the data in chunks: each time we delete the property it writes the next one; an empty chunk ends it.
            using var all = new MemoryStream();
            while (true)
            {
                _gotPropertyChange = false;
                if (!WaitFor(() => _gotPropertyChange))
                {
                    return null;
                }

                if (!ReadProperty(limit, out _, out var chunk))
                {
                    return null;
                }

                if (chunk.Length == 0)
                {
                    break;
                }

                if (all.Length + chunk.Length > limit)
                {
                    return null;
                }

                all.Write(chunk);
            }

            bytes = all.ToArray();
        }
        else if (type == target || (target == _utf8String && type == _aTextMime))
        {
            bytes = first;
        }
        else
        {
            return null;
        }

        return target == _utf8String ? ClipboardText.FromUtf8(bytes) : ClipboardText.FromLatin1(bytes);
    }

    // Reads and deletes the transfer property. Rejects other than byte data and anything over the limit (nothing is allocated for it).
    private bool ReadProperty(int limit, out nuint type, out byte[] data)
    {
        nuint actualType;
        int format;
        nuint count, after;
        byte* raw;
        data = [];
        type = 0;
        var status = Xlib.GetWindowProperty(_display, _selWindow, _aTransfer, 0, (limit / 4) + 1, 1, 0, &actualType, &format, &count, &after, &raw);
        if (status != 0)
        {
            return false;
        }

        try
        {
            type = actualType;
            if (actualType == 0)
            {
                return false;
            }

            if (after != 0)
            {
                // More than the limit; a property is deleted only when it was read completely, so do it ourselves.
                Xlib.DeleteProperty(_display, _selWindow, _aTransfer);
                return false;
            }

            if (format != 8 && !(actualType == _aIncr && format == 32))
            {
                return false;
            }

            if (actualType == _aIncr)
            {
                return true;
            }

            if (count > (nuint)limit)
            {
                return false;
            }

            data = new ReadOnlySpan<byte>(raw, (int)count).ToArray();
            return true;
        }
        finally
        {
            if (raw != null)
            {
                Xlib.Free(raw);
            }
        }
    }

    private void DrainEvents()
    {
        Xlib.XEvent e;
        while (Xlib.Pending(_display) > 0)
        {
            Xlib.NextEvent(_display, &e);
            Handle(&e);
        }
    }

    // Processes events until the condition holds or the owner took too long.
    private bool WaitFor(Func<bool> condition)
    {
        var end = Environment.TickCount64 + ReadTimeoutMilliseconds;
        while (true)
        {
            DrainEvents();
            if (condition())
            {
                return true;
            }

            var left = end - Environment.TickCount64;
            if (left <= 0)
            {
                return false;
            }

            var fd = new Libc.PollFd { Fd = Xlib.ConnectionNumber(_display), Events = Libc.POLLIN };
            Libc.Poll(&fd, 1, (int)Math.Min(left, 20));
        }
    }

    // Returns true when the event belonged to the selection machinery.
    private bool HandleSelectionEvent(Xlib.XEvent* e)
    {
        switch (e->Type)
        {
            case Xlib.SelectionRequest:
                AnswerRequest(e);
                return true;
            case Xlib.SelectionClear:
                var cleared = Xlib.At<nuint>(e, 40);
                if (cleared == _aClipboard)
                {
                    _owned[(int)ClipboardSelection.Clipboard] = null;
                }
                else if (cleared == _aPrimary)
                {
                    _owned[(int)ClipboardSelection.Primary] = null;
                }

                return true;
            case Xlib.SelectionNotify:
                _notifyProperty = Xlib.At<nuint>(e, 56);
                _gotNotify = true;
                return true;
            case Xlib.PropertyNotify when Xlib.At<nuint>(e, 32) == _selWindow:
                if (Xlib.At<nuint>(e, 40) == _aTransfer && Xlib.At<int>(e, 56) == Xlib.PropertyNewValue)
                {
                    _gotPropertyChange = true;
                }

                return true;
            default:
                return false;
        }
    }

    private void AnswerRequest(Xlib.XEvent* e)
    {
        var requestor = Xlib.At<nuint>(e, 40);
        var selection = Xlib.At<nuint>(e, 48);
        var target = Xlib.At<nuint>(e, 56);
        var property = Xlib.At<nuint>(e, 64);
        var time = Xlib.At<nuint>(e, 72);
        var index = selection == _aClipboard ? 0 : selection == _aPrimary ? 1 : -1;

        // Clients written for the older protocol send no property; ICCCM says to use the target atom then.
        var reply = property == 0 ? target : property;
        var ok = false;
        if (index >= 0 && _owned[index] is { } text)
        {
            if (target == _aTargets)
            {
                var atoms = stackalloc nint[] { (nint)_aTargets, (nint)_utf8String, (nint)XaString, (nint)_aText, (nint)_aTextMime };
                Xlib.ChangeProperty(_display, requestor, reply, XaAtom, 32, Xlib.PropModeReplace, (byte*)atoms, 5);
                ok = true;
            }
            else if (target == _utf8String || target == _aTextMime || target == _aText)
            {
                ok = Write(requestor, reply, _utf8String, Encoding.UTF8.GetBytes(text));
            }
            else if (target == XaString)
            {
                ok = Write(requestor, reply, XaString, ClipboardText.ToLatin1(text));
            }
        }

        // The reply names the property that holds the data, or None (0) when the request was refused.
        Xlib.XEvent ev = default;
        ev.Type = Xlib.SelectionNotify;
        *(int*)((byte*)&ev + 16) = 1;
        *(nint*)((byte*)&ev + 24) = _display;
        *(nuint*)((byte*)&ev + 32) = requestor;
        *(nuint*)((byte*)&ev + 40) = selection;
        *(nuint*)((byte*)&ev + 48) = target;
        *(nuint*)((byte*)&ev + 56) = ok ? reply : 0;
        *(nuint*)((byte*)&ev + 64) = time;
        Xlib.SendEvent(_display, requestor, 0, 0, &ev);
        Xlib.Flush(_display);
    }

    private bool Write(nuint window, nuint property, nuint type, byte[] bytes)
    {
        if (bytes.Length > MaxPropertyBytes())
        {
            return false;
        }

        fixed (byte* p = bytes)
        {
            Xlib.ChangeProperty(_display, window, property, type, 8, Xlib.PropModeReplace, p, bytes.Length);
        }

        return true;
    }
}
