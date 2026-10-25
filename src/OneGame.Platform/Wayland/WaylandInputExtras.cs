using System.Runtime.InteropServices;
using System.Text;

namespace OneGame.Platform;

// The clipboard (wl_data_device, core protocol) and input-method text (text-input-v3) on Wayland.
internal sealed unsafe partial class WaylandBackend
{
    private const int ReceiveTimeoutMilliseconds = 1000;

    private static readonly string[] TextMimeTypes = ["text/plain;charset=utf-8", "UTF8_STRING", "text/plain", "TEXT", "STRING"];

    private WlObject? _dataDeviceManager, _textInputManager, _dataDevice, _textInput;

    // Clipboard: the offer the compositor last announced, and the source we own while our text is on the clipboard.
    private readonly Dictionary<nint, (WlObject Offer, List<string> Mimes)> _offers = [];
    private (WlObject Offer, List<string> Mimes)? _selectionOffer;
    private WlObject? _ownSource;
    private string? _ownText;

    // Text input: the window that has the keyboard focus on the text-input object, and the double-buffered state of one `done` batch.
    private WaylandWindow? _textInputWindow;
    private string _preedit = "";
    private string? _pendingPreedit, _pendingCommit;
    private int _pendingBegin = -1, _pendingEnd = -1;
    private bool _textInputEnabled;

    private void CreateSeatExtras()
    {
        if (_seat is null)
        {
            return;
        }

        if (_dataDeviceManager is not null && _dataDevice is null)
        {
            _dataDevice = _dataDeviceManager.Create(WlDataDeviceManager.Request.GetDataDevice, WlDataDevice.Info, _dataDeviceManager.Version, [0, WlObject.Arg(_seat)]);
            _dataDevice.Handler = OnDataDevice;
        }

        if (_textInputManager is not null && _textInput is null)
        {
            _textInput = _textInputManager.Create(ZwpTextInputManagerV3.Request.GetTextInput, ZwpTextInputV3.Info, _textInputManager.Version, [0, WlObject.Arg(_seat)]);
            _textInput.Handler = OnTextInput;
        }
    }

    // ---- clipboard ----

    public override bool ClipboardAvailable => _dataDevice is not null;

    /// <summary>Whether the compositor offers input-method support (text-input-v3).</summary>
    internal bool HasTextInput => _textInput is not null;

    public override string? ClipboardGet(ClipboardSelection selection)
    {
        // Take in what the compositor has announced since the last pump, so the offer is the current one.
        Pump(0);
        if (_ownSource is not null)
        {
            return _ownText;
        }

        if (_selectionOffer is not { } sel)
        {
            return null;
        }

        string? mime = null;
        foreach (var wanted in TextMimeTypes)
        {
            if (sel.Mimes.Contains(wanted))
            {
                mime = wanted;
                break;
            }
        }

        if (mime is null)
        {
            return null;
        }

        var fds = stackalloc int[2];
        if (Libc.Pipe2(fds, Libc.O_CLOEXEC) != 0)
        {
            return null;
        }

        var bytes = Encoding.UTF8.GetBytes(mime + "\0");
        fixed (byte* m = bytes)
        {
            sel.Offer.Send(WlDataOffer.Request.Receive, [(ulong)m, (ulong)fds[1]]);
        }

        // The compositor passes the write end to the owner, which fills it and closes it. Ours must be closed first or the read never ends.
        _ = WlClient.DisplayFlush(_display);
        _ = Libc.Close(fds[1]);
        var data = ReadAll(fds[0], PlatformClipboard.MaxTextBytes);
        _ = Libc.Close(fds[0]);
        return data is null ? null : ClipboardText.FromUtf8(data);
    }

    // Reads until end of stream; null when the owner stalls or sends more than the limit.
    private static byte[]? ReadAll(int fd, int limit)
    {
        using var all = new MemoryStream();
        var buffer = new byte[16 * 1024];
        var end = Environment.TickCount64 + ReceiveTimeoutMilliseconds;
        while (true)
        {
            var left = end - Environment.TickCount64;
            if (left <= 0)
            {
                return null;
            }

            var p = new Libc.PollFd { Fd = fd, Events = Libc.POLLIN };
            if (Libc.Poll(&p, 1, (int)left) <= 0)
            {
                return null;
            }

            nint n;
            fixed (byte* b = buffer)
            {
                n = Libc.Read(fd, b, (nuint)buffer.Length);
            }

            if (n < 0)
            {
                return null;
            }

            if (n == 0)
            {
                return all.ToArray();
            }

            if (all.Length + n > limit)
            {
                return null;
            }

            all.Write(buffer, 0, (int)n);
        }
    }

    public override bool ClipboardSet(ClipboardSelection selection, string text)
    {
        if (_dataDeviceManager is null || _dataDevice is null)
        {
            return false;
        }

        var source = _dataDeviceManager.Create(WlDataDeviceManager.Request.CreateDataSource, WlDataSource.Info, _dataDeviceManager.Version, [0]);
        source.Handler = OnDataSource;
        foreach (var mime in TextMimeTypes)
        {
            var bytes = Encoding.UTF8.GetBytes(mime + "\0");
            fixed (byte* m = bytes)
            {
                source.Send(WlDataSource.Request.Offer, [(ulong)m]);
            }
        }

        _dataDevice.Send(WlDataDevice.Request.SetSelection, [WlObject.Arg(source), _inputSerial]);
        var old = _ownSource;
        _ownSource = source;
        _ownText = text;
        old?.Destroy(WlDataSource.Request.Destroy);
        _ = WlClient.DisplayFlush(_display);
        return true;
    }

    private void OnDataDevice(WlObject device, int opcode, WlArgs a)
    {
        switch (opcode)
        {
            case WlDataDevice.Event.DataOffer:
                var offer = WlObject.Wrap(a.Object(0), WlDataOffer.Info);
                var mimes = new List<string>();
                offer.Handler = (_, op, args) =>
                {
                    if (op == WlDataOffer.Event.Offer && args.String(0) is { } mime && mimes.Count < 64)
                    {
                        mimes.Add(mime);
                    }
                };
                _offers[offer.Proxy] = (offer, mimes);
                break;
            case WlDataDevice.Event.Enter:
                // Drag and drop is not supported; drop the offer that came with it.
                DiscardOffer(a.Object(4));
                break;
            case WlDataDevice.Event.Selection:
                var previous = _selectionOffer;
                var id = a.Object(0);
                _selectionOffer = id != 0 && _offers.Remove(id, out var found) ? found : null;
                if (previous is { } old)
                {
                    old.Offer.Destroy(WlDataOffer.Request.Destroy);
                }

                break;
        }
    }

    private void DiscardOffer(nint id)
    {
        if (id != 0 && _offers.Remove(id, out var found))
        {
            found.Offer.Destroy(WlDataOffer.Request.Destroy);
        }
    }

    private void OnDataSource(WlObject source, int opcode, WlArgs a)
    {
        switch (opcode)
        {
            case WlDataSource.Event.Send:
                var fd = a.Fd(1);
                if (source != _ownSource || _ownText is null || a.String(0) is not { } mime || Array.IndexOf(TextMimeTypes, mime) < 0)
                {
                    _ = Libc.Close(fd);
                    break;
                }

                // The reader may be slow, and the game thread must not wait for it, so the bytes are written from the thread pool.
                var data = Encoding.UTF8.GetBytes(_ownText);
                System.Threading.ThreadPool.UnsafeQueueUserWorkItem(static s => WriteAndClose(s.Fd, s.Data), (Fd: fd, Data: data), preferLocal: false);
                break;
            case WlDataSource.Event.Cancelled:
                // Another program took the clipboard.
                if (source == _ownSource)
                {
                    _ownSource = null;
                    _ownText = null;
                }

                source.Destroy(WlDataSource.Request.Destroy);
                break;
        }
    }

    private static void WriteAndClose(int fd, byte[] data)
    {
        try
        {
            var done = 0;
            while (done < data.Length)
            {
                var p = new Libc.PollFd { Fd = fd, Events = Libc.POLLOUT };
                if (Libc.Poll(&p, 1, 5000) <= 0)
                {
                    return; // The reader stopped reading.
                }

                nint n;
                fixed (byte* b = data)
                {
                    n = Libc.Write(fd, b + done, (nuint)(data.Length - done));
                }

                if (n <= 0)
                {
                    return;
                }

                done += (int)n;
            }
        }
        finally
        {
            _ = Libc.Close(fd);
        }
    }

    // ---- input method ----

    internal void SetTextInput(WaylandWindow window)
    {
        if (_textInput is null || _textInputWindow != window)
        {
            return; // Without the protocol only plain text input exists; with it, the window enables once the compositor says it has focus.
        }

        PushTextInputState(window);
    }

    private void PushTextInputState(WaylandWindow window)
    {
        if (_textInput is not { } ti)
        {
            return;
        }

        if (window.TextInputActive)
        {
            // Coordinates are in the surface's logical units, not buffer pixels.
            var (x, y, w, h) = window.TextInputArea;
            var scale = window.Scale <= 0f ? 1f : window.Scale;
            if (!_textInputEnabled)
            {
                ti.Send(ZwpTextInputV3.Request.Enable);
                ti.Send(ZwpTextInputV3.Request.SetContentType, [ZwpTextInputV3.ContentHintEnum.None, ZwpTextInputV3.ContentPurposeEnum.Normal]);
                _textInputEnabled = true;
            }

            ti.Send(
                ZwpTextInputV3.Request.SetCursorRectangle,
                [WlObject.Arg((int)Math.Round(x / scale)), WlObject.Arg((int)Math.Round(y / scale)), WlObject.Arg(Math.Max(1, (int)Math.Round(w / scale))), WlObject.Arg(Math.Max(1, (int)Math.Round(h / scale)))]);
        }
        else if (_textInputEnabled)
        {
            ti.Send(ZwpTextInputV3.Request.Disable);
            _textInputEnabled = false;
            EndPreedit(window);
        }
        else
        {
            return;
        }

        ti.Send(ZwpTextInputV3.Request.Commit);
        _ = WlClient.DisplayFlush(_display);
    }

    private void ForgetTextInputWindow(WaylandWindow window)
    {
        if (_textInputWindow == window)
        {
            _textInputWindow = null;
            _textInputEnabled = false;
            _preedit = "";
        }
    }

    private void EndPreedit(WaylandWindow window)
    {
        if (_preedit.Length > 0)
        {
            _preedit = "";
            App.Post(new PlatformEvent { Kind = PlatformEventKind.TextComposition, WindowId = window.Id, Text = "", CursorStart = -1, CursorEnd = -1 });
        }
    }

    private void OnTextInput(WlObject sender, int opcode, WlArgs a)
    {
        switch (opcode)
        {
            case ZwpTextInputV3.Event.Enter:
                _textInputWindow = _bySurface.GetValueOrDefault(a.Object(0));
                _textInputEnabled = false; // A new focus starts disabled; the state is sent again if the window wants input.
                if (_textInputWindow is { TextInputActive: true } w)
                {
                    PushTextInputState(w);
                }

                break;
            case ZwpTextInputV3.Event.Leave:
                if (_textInputWindow is { } left)
                {
                    EndPreedit(left);
                }

                _textInputWindow = null;
                _textInputEnabled = false;
                _pendingPreedit = _pendingCommit = null;
                break;
            case ZwpTextInputV3.Event.PreeditString:
                _pendingPreedit = a.String(0) ?? "";
                _pendingBegin = a.Int(1);
                _pendingEnd = a.Int(2);
                break;
            case ZwpTextInputV3.Event.CommitString:
                _pendingCommit = a.String(0);
                break;
            case ZwpTextInputV3.Event.Done:
                ApplyTextInputBatch();
                break;
        }
    }

    // Each `done` closes a batch that describes the whole state: no preedit_string in it means the preedit is empty. The input method's
    // order of effects (protocol text, "done"): replace the old preedit with the commit string, then insert the new preedit.
    private void ApplyTextInputBatch()
    {
        var window = _textInputWindow;
        var commit = _pendingCommit;
        var preedit = _pendingPreedit ?? "";
        var begin = _pendingBegin;
        var end = _pendingEnd;
        _pendingPreedit = _pendingCommit = null;
        _pendingBegin = _pendingEnd = -1;
        if (window is null)
        {
            return;
        }

        var committed = !string.IsNullOrEmpty(commit);
        if (committed && _preedit.Length > 0)
        {
            App.Post(new PlatformEvent { Kind = PlatformEventKind.TextComposition, WindowId = window.Id, Text = "", CursorStart = -1, CursorEnd = -1 });
            _preedit = "";
        }

        if (committed)
        {
            App.Post(new PlatformEvent { Kind = PlatformEventKind.TextInput, WindowId = window.Id, Text = commit });
        }

        if (preedit != _preedit)
        {
            _preedit = preedit;
            App.Post(new PlatformEvent
            {
                Kind = PlatformEventKind.TextComposition,
                WindowId = window.Id,
                Text = preedit,
                CursorStart = ImeOffsets.Utf8ToUtf16(preedit, begin),
                CursorEnd = ImeOffsets.Utf8ToUtf16(preedit, end),
            });
        }
    }
}

/// <summary>Converts the byte offsets input-method protocols use into the UTF-16 indexes .NET strings use.</summary>
internal static class ImeOffsets
{
    /// <summary>The UTF-16 index of a UTF-8 byte offset into <paramref name="text"/>. Negative offsets (no cursor) give -1; offsets past the end or inside a character are clamped to the nearest character boundary before them.</summary>
    public static int Utf8ToUtf16(string text, int byteOffset)
    {
        if (byteOffset < 0)
        {
            return -1;
        }

        var bytes = 0;
        var i = 0;
        while (i < text.Length)
        {
            var width = char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]) ? 2 : 1;
            var size = Encoding.UTF8.GetByteCount(text.AsSpan(i, width));
            if (bytes + size > byteOffset)
            {
                break;
            }

            bytes += size;
            i += width;
        }

        return i;
    }
}
