using System.Runtime.InteropServices;

namespace OneGame.Platform.Tests;

/// <summary>
/// A just-enough EWMH window manager for tests: it maps windows, honours configure requests, and maximizes on the
/// <c>_NET_WM_STATE</c> request by writing the state property and filling the screen, restoring the old rectangle when the state is removed.
/// Xvfb has no window manager of its own, so without this the maximized state could not be tested.
/// </summary>
internal sealed unsafe partial class FakeWindowManager : IDisposable
{
    private const int MapRequest = 20, ConfigureRequest = 23, ClientMessage = 33;

    private readonly nint _display;
    private readonly nuint _root;
    private readonly nuint _state, _maxH, _maxV;
    private readonly Thread _thread;
    private readonly Dictionary<nuint, (int X, int Y, uint W, uint H)> _saved = [];
    private volatile bool _stop;

    public FakeWindowManager(string display)
    {
        var name = System.Text.Encoding.ASCII.GetBytes(display + "\0");
        fixed (byte* p = name)
        {
            _display = Xlib.OpenDisplay(p);
        }

        if (_display == 0)
        {
            throw new InvalidOperationException("The window manager could not open " + display);
        }

        _root = Xlib.RootWindow(_display, Xlib.DefaultScreen(_display));
        _state = Atom("_NET_WM_STATE");
        _maxH = Atom("_NET_WM_STATE_MAXIMIZED_HORZ");
        _maxV = Atom("_NET_WM_STATE_MAXIMIZED_VERT");

        // Only one client may select SubstructureRedirect on the root, which is what makes it the window manager.
        Xlib.SelectInput(_display, _root, (nint)(Xlib.SubstructureRedirectMask | Xlib.SubstructureNotifyMask));
        var check = Atom("_NET_SUPPORTING_WM_CHECK");
        var self = _root;
        Xlib.ChangeProperty(_display, _root, check, 33 /* XA_WINDOW */, 32, Xlib.PropModeReplace, (byte*)&self, 1);
        Xlib.Flush(_display);
        _thread = new Thread(Loop) { IsBackground = true, Name = "fake-wm" };
        _thread.Start();
    }

    [LibraryImport("libX11.so.6", EntryPoint = "XPending")]
    private static partial int Pending(nint display);

    [LibraryImport("libX11.so.6", EntryPoint = "XGetGeometry")]
    private static partial int GetGeometry(nint display, nuint drawable, nuint* root, int* x, int* y, uint* width, uint* height, uint* border, uint* depth);

    private nuint Atom(string name)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(name + "\0");
        fixed (byte* p = bytes)
        {
            return Xlib.InternAtom(_display, p, 0);
        }
    }

    private void Loop()
    {
        Xlib.XEvent e;
        while (!_stop)
        {
            if (Pending(_display) == 0)
            {
                Thread.Sleep(2);
                continue;
            }

            Xlib.NextEvent(_display, &e);
            switch (e.Type)
            {
                case MapRequest:
                {
                    var window = Xlib.At<nuint>(&e, 40);
                    if (IsMaximized(window))
                    {
                        Maximize(window);
                    }

                    Xlib.MapWindow(_display, window);
                    break;
                }

                case ConfigureRequest:
                {
                    var window = Xlib.At<nuint>(&e, 40);
                    var mask = Xlib.At<nuint>(&e, 88);
                    int x = Xlib.At<int>(&e, 48), y = Xlib.At<int>(&e, 52);
                    uint w = (uint)Xlib.At<int>(&e, 56), h = (uint)Xlib.At<int>(&e, 60);
                    var move = (mask & 3) != 0;
                    var size = (mask & 12) != 0;
                    if (move && size)
                    {
                        Xlib.MoveResizeWindow(_display, window, x, y, w, h);
                    }
                    else if (move)
                    {
                        Xlib.MoveWindow(_display, window, x, y);
                    }
                    else if (size)
                    {
                        Xlib.ResizeWindow(_display, window, w, h);
                    }

                    break;
                }

                case ClientMessage when Xlib.At<nuint>(&e, 40) == _state:
                {
                    var window = Xlib.At<nuint>(&e, 32);
                    var action = Xlib.At<nint>(&e, 56);
                    var asks = Xlib.At<nuint>(&e, 64) == _maxH || Xlib.At<nuint>(&e, 72) == _maxV;
                    if (asks && action == 1)
                    {
                        Maximize(window);
                    }
                    else if (asks && action == 0)
                    {
                        Unmaximize(window);
                    }

                    break;
                }
            }

            Xlib.Flush(_display);
        }
    }

    private bool IsMaximized(nuint window)
    {
        nuint type, count, after;
        int format;
        byte* data;
        var found = false;
        if (Xlib.GetWindowProperty(_display, window, _state, 0, 32, 0, 4, &type, &format, &count, &after, &data) == 0 && data != null)
        {
            for (nuint i = 0; i < count; i++)
            {
                found |= ((nuint*)data)[i] == _maxH;
            }

            Xlib.Free(data);
        }

        return found;
    }

    private void Maximize(nuint window)
    {
        nuint root;
        int x, y;
        uint w, h, border, depth;
        if (GetGeometry(_display, window, &root, &x, &y, &w, &h, &border, &depth) != 0 && !_saved.ContainsKey(window))
        {
            _saved[window] = (x, y, w, h);
        }

        var atoms = stackalloc nuint[2] { _maxH, _maxV };
        Xlib.ChangeProperty(_display, window, _state, 4, 32, Xlib.PropModeReplace, (byte*)atoms, 2);
        Xlib.MoveResizeWindow(_display, window, 0, 0, (uint)Xlib.DisplayWidth(_display, 0), (uint)Xlib.DisplayHeight(_display, 0));
    }

    private void Unmaximize(nuint window)
    {
        Xlib.DeleteProperty(_display, window, _state);
        if (_saved.Remove(window, out var old))
        {
            Xlib.MoveResizeWindow(_display, window, old.X, old.Y, old.W, old.H);
        }
    }

    public void Dispose()
    {
        _stop = true;
        _thread.Join();
        Xlib.CloseDisplay(_display);
    }
}
