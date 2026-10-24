using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace OneGame.Platform.Tests;

internal static unsafe partial class WlServer
{
    private const string Lib = "libwayland-server.so.0";

    [LibraryImport(Lib, EntryPoint = "wl_display_create")]
    public static partial nint DisplayCreate();

    [LibraryImport(Lib, EntryPoint = "wl_display_destroy")]
    public static partial void DisplayDestroy(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_display_destroy_clients")]
    public static partial void DisplayDestroyClients(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_display_get_event_loop")]
    public static partial nint DisplayGetEventLoop(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_display_add_socket_fd")]
    public static partial int DisplayAddSocketFd(nint display, int fd);

    [LibraryImport(Lib, EntryPoint = "wl_display_flush_clients")]
    public static partial void DisplayFlushClients(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_event_loop_dispatch")]
    public static partial int EventLoopDispatch(nint loop, int timeout);

    [LibraryImport(Lib, EntryPoint = "wl_global_create")]
    public static partial nint GlobalCreate(nint display, nint @interface, int version, void* data, delegate* unmanaged<nint, void*, uint, uint, void> bind);

    [LibraryImport(Lib, EntryPoint = "wl_resource_create")]
    public static partial nint ResourceCreate(nint client, nint @interface, int version, uint id);

    [LibraryImport(Lib, EntryPoint = "wl_resource_set_dispatcher")]
    public static partial void ResourceSetDispatcher(nint resource, delegate* unmanaged<void*, void*, uint, void*, ulong*, int> dispatcher, void* implementation, void* data, void* destroy);

    [LibraryImport(Lib, EntryPoint = "wl_resource_destroy")]
    public static partial void ResourceDestroy(nint resource);

    [LibraryImport(Lib, EntryPoint = "wl_resource_get_client")]
    public static partial nint ResourceGetClient(nint resource);

    [LibraryImport(Lib, EntryPoint = "wl_resource_get_version")]
    public static partial int ResourceGetVersion(nint resource);

    [LibraryImport(Lib, EntryPoint = "wl_resource_post_event_array")]
    public static partial void ResourcePostEventArray(nint resource, uint opcode, ulong* args);
}

internal static unsafe partial class XkbText
{
    [LibraryImport("libxkbcommon.so.0", EntryPoint = "xkb_keymap_get_as_string")]
    public static partial byte* KeymapGetAsString(nint keymap, int format);
}

/// <summary>
/// A minimal Wayland compositor for tests, built on libwayland-server: just enough of wl_compositor, xdg-shell and wl_seat to host one
/// window and send it exact pointer and keyboard events on the real wire protocol. Weston's headless backend has no input devices.
/// </summary>
internal sealed unsafe class FakeCompositor : IDisposable
{
    private readonly nint _display;
    private readonly nint _loop;
    private readonly Socket _listener;
    private readonly Thread _thread;
    private readonly ConcurrentQueue<Action> _work = new();
    private volatile bool _stop;
    private uint _serial = 100;

    private nint _surface, _pointer, _keyboard, _toplevel;
    private bool _toplevelReady;

    public FakeCompositor(string socketPath)
    {
        SocketPath = socketPath;
        _display = WlServer.DisplayCreate();
        _loop = WlServer.DisplayGetEventLoop(_display);
        File.Delete(socketPath);
        _listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _listener.Bind(new UnixDomainSocketEndPoint(socketPath));
        _listener.Listen(8);
        _ = WlServer.DisplayAddSocketFd(_display, (int)_listener.SafeHandle.DangerousGetHandle());

        Global(WlCompositor.Name, 4);
        Global(XdgWmBase.Name, 3);
        Global(WlSeat.Name, 7);
        _thread = new Thread(Loop) { IsBackground = true, Name = "fake-compositor" };
        _thread.Start();
    }

    public string SocketPath { get; }

    /// <summary>Runs <paramref name="action"/> on the compositor thread and waits for it.</summary>
    public void Run(Action action)
    {
        using var done = new ManualResetEventSlim();
        Exception? error = null;
        _work.Enqueue(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                done.Set();
            }
        });
        done.Wait(5000);
        if (error is not null)
        {
            throw new InvalidOperationException("compositor action failed", error);
        }
    }

    public bool WaitForWindow(int milliseconds)
    {
        var end = Environment.TickCount64 + milliseconds;
        while (Environment.TickCount64 < end)
        {
            if (_toplevelReady && _pointer != 0 && _keyboard != 0)
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return false;
    }

    // Event senders: call them through Run.
    public void KeyboardEnter() => Post(_keyboard, WlKeyboard.Event.Enter, [Serial(), (ulong)_surface, (ulong)EmptyArray()]);

    public void PointerEnter(double x, double y) => Post(_pointer, WlPointer.Event.Enter, [Serial(), (ulong)_surface, Fixed(x), Fixed(y)]);

    public void PointerMotion(double x, double y) => Post(_pointer, WlPointer.Event.Motion, [Tick(), Fixed(x), Fixed(y)]);

    public void PointerButton(uint button, bool down) => Post(_pointer, WlPointer.Event.Button, [Serial(), Tick(), button, down ? 1UL : 0UL]);

    public void PointerAxisDiscrete(bool vertical, int notches) =>
        Post(_pointer, WlPointer.Event.AxisDiscrete, [vertical ? 0UL : 1UL, (uint)notches]);

    public void Key(uint evdevCode, bool down) => Post(_keyboard, WlKeyboard.Event.Key, [Serial(), Tick(), evdevCode, down ? 1UL : 0UL]);

    public void Modifiers(uint depressed) => Post(_keyboard, WlKeyboard.Event.Modifiers, [Serial(), depressed, 0, 0, 0]);

    public void CloseToplevel() => Post(_toplevel, XdgToplevel.Event.Close, []);

    public void Dispose()
    {
        _stop = true;
        _thread.Join(2000);
        WlServer.DisplayDestroyClients(_display);
        WlServer.DisplayDestroy(_display);
        _listener.Dispose();
        File.Delete(SocketPath);
    }

    private static ulong Fixed(double v) => (uint)(int)Math.Round(v * 256);

    private ulong Serial() => ++_serial;

    private static ulong Tick() => (uint)Environment.TickCount;

    // An empty wl_array { size, alloc, data } that stays valid for the process lifetime.
    private static nint EmptyArray() => s_emptyArray;

    private static readonly nint s_emptyArray = (nint)NativeMemory.AllocZeroed(24);

    private void Loop()
    {
        while (!_stop)
        {
            while (_work.TryDequeue(out var a))
            {
                a();
            }

            _ = WlServer.EventLoopDispatch(_loop, 10);
            WlServer.DisplayFlushClients(_display);
        }
    }

    private static void Post(nint resource, int opcode, ReadOnlySpan<ulong> args)
    {
        Span<ulong> copy = stackalloc ulong[Math.Max(1, args.Length)];
        args.CopyTo(copy);
        fixed (ulong* p = copy)
        {
            WlServer.ResourcePostEventArray(resource, (uint)opcode, p);
        }
    }

    private sealed class Obj(FakeCompositor owner, string iface, nint resource)
    {
        public FakeCompositor Owner { get; } = owner;

        public string Iface { get; } = iface;

        public nint Resource { get; set; } = resource;
    }

    private sealed class GlobalData(FakeCompositor owner, string iface)
    {
        public FakeCompositor Owner { get; } = owner;

        public string Iface { get; } = iface;
    }

    private void Global(string iface, int version)
    {
        var handle = GCHandle.Alloc(new GlobalData(this, iface));
        WlServer.GlobalCreate(_display, WlInterfaces.Get(iface), version, (void*)GCHandle.ToIntPtr(handle), &OnBind);
    }

    [UnmanagedCallersOnly]
    private static void OnBind(nint client, void* data, uint version, uint id)
    {
        var g = (GlobalData)GCHandle.FromIntPtr((nint)data).Target!;
        var o = g.Owner.NewResource(client, g.Iface, (int)version, id);
        if (g.Iface == WlSeat.Name)
        {
            // Pointer and keyboard.
            Post(o.Resource, WlSeat.Event.Capabilities, [3]);
        }
    }

    private Obj NewResource(nint client, string iface, int version, uint id)
    {
        var resource = WlServer.ResourceCreate(client, WlInterfaces.Get(iface), version, id);
        var obj = new Obj(this, iface, resource);
        var handle = GCHandle.Alloc(obj);
        WlServer.ResourceSetDispatcher(resource, &OnRequest, (void*)GCHandle.ToIntPtr(handle), null, null);
        return obj;
    }

    [UnmanagedCallersOnly]
    private static int OnRequest(void* implementation, void* resource, uint opcode, void* message, ulong* args)
    {
        var o = (Obj)GCHandle.FromIntPtr((nint)implementation).Target!;
        o.Owner.Handle(o, (int)opcode, args);
        return 0;
    }

    private void Handle(Obj o, int opcode, ulong* args)
    {
        var client = WlServer.ResourceGetClient(o.Resource);
        var version = WlServer.ResourceGetVersion(o.Resource);
        switch (o.Iface)
        {
            case WlCompositor.Name when opcode == WlCompositor.Request.CreateSurface:
                var surface = NewResource(client, WlSurface.Name, version, (uint)args[0]);
                _surface = surface.Resource;
                break;
            case XdgWmBase.Name when opcode == XdgWmBase.Request.GetXdgSurface:
                NewResource(client, XdgSurface.Name, version, (uint)args[0]);
                break;
            case XdgSurface.Name when opcode == XdgSurface.Request.GetToplevel:
                var top = NewResource(client, XdgToplevel.Name, version, (uint)args[0]);
                _toplevel = top.Resource;
                Post(top.Resource, XdgToplevel.Event.Configure, [0, 0, (ulong)EmptyArray()]);
                Post(o.Resource, XdgSurface.Event.Configure, [Serial()]);
                _toplevelReady = true;
                break;
            case WlSeat.Name when opcode == WlSeat.Request.GetPointer:
                _pointer = NewResource(client, WlPointer.Name, version, (uint)args[0]).Resource;
                break;
            case WlSeat.Name when opcode == WlSeat.Request.GetKeyboard:
                _keyboard = NewResource(client, WlKeyboard.Name, version, (uint)args[0]).Resource;
                SendKeymap(_keyboard);
                break;
            case WlSurface.Name when opcode == WlSurface.Request.Destroy:
            case XdgSurface.Name when opcode == XdgSurface.Request.Destroy:
            case XdgToplevel.Name when opcode == XdgToplevel.Request.Destroy:
                WlServer.ResourceDestroy(o.Resource);
                break;
        }
    }

    private static void SendKeymap(nint keyboard)
    {
        // The compositor sends its layout as keymap text in a shared-memory file; here it is the default US layout.
        var context = XkbNative.ContextNew(0);
        var keymap = XkbNative.KeymapNewFromNames(context, null, 0);
        var text = Marshal.PtrToStringUTF8((nint)XkbText.KeymapGetAsString(keymap, 1))!;
        var bytes = Encoding.UTF8.GetBytes(text + "\0");

        int fd;
        fixed (byte* name = "keymap\0"u8)
        {
            fd = Libc.MemfdCreate(name, 1);
        }

        _ = Libc.Ftruncate(fd, bytes.Length);
        var map = Libc.Mmap(null, (nuint)bytes.Length, Libc.PROT_READ | Libc.PROT_WRITE, Libc.MAP_SHARED, fd, 0);
        bytes.CopyTo(new Span<byte>(map, bytes.Length));
        _ = Libc.Munmap(map, (nuint)bytes.Length);

        Post(keyboard, WlKeyboard.Event.Keymap, [1, (ulong)fd, (ulong)bytes.Length]);
        Post(keyboard, WlKeyboard.Event.RepeatInfo, [0, 0]);
        _ = Libc.Close(fd);
        XkbNative.KeymapUnref(keymap);
        XkbNative.ContextUnref(context);
    }
}
