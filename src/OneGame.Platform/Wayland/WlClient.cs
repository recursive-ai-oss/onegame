using System.Runtime.InteropServices;
using System.Text;

namespace OneGame.Platform;

/// <summary>One interface's messages as the generator found them in the protocol XML.</summary>
/// <param name="Name">Interface name.</param>
/// <param name="Version">Highest version in the XML.</param>
/// <param name="Requests">Client to server messages, in opcode order.</param>
/// <param name="Events">Server to client messages, in opcode order.</param>
internal sealed record WlInterfaceInfo(string Name, int Version, WlMessageInfo[] Requests, WlMessageInfo[] Events);

/// <summary>One protocol message: name, libwayland signature string, and the interface each argument names (null when none).</summary>
internal sealed record WlMessageInfo(string Name, string Signature, string?[] ArgInterfaces);

/// <summary>libwayland-client entry points. Marshalling of requests goes through the array form, which is not variadic.</summary>
internal static unsafe partial class WlClient
{
    private const string Lib = "libwayland-client.so.0";

    public const uint MarshalFlagDestroy = 1;

    [LibraryImport(Lib, EntryPoint = "wl_display_connect")]
    public static partial nint DisplayConnect(byte* name);

    [LibraryImport(Lib, EntryPoint = "wl_display_disconnect")]
    public static partial void DisplayDisconnect(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_display_get_fd")]
    public static partial int DisplayGetFd(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_display_dispatch_pending")]
    public static partial int DisplayDispatchPending(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_display_flush")]
    public static partial int DisplayFlush(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_display_roundtrip")]
    public static partial int DisplayRoundtrip(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_display_prepare_read")]
    public static partial int DisplayPrepareRead(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_display_read_events")]
    public static partial int DisplayReadEvents(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_display_cancel_read")]
    public static partial void DisplayCancelRead(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_display_get_error")]
    public static partial int DisplayGetError(nint display);

    [LibraryImport(Lib, EntryPoint = "wl_proxy_marshal_array_flags")]
    public static partial nint ProxyMarshalArrayFlags(nint proxy, uint opcode, nint @interface, uint version, uint flags, ulong* args);

    [LibraryImport(Lib, EntryPoint = "wl_proxy_add_dispatcher")]
    public static partial int ProxyAddDispatcher(nint proxy, delegate* unmanaged<void*, void*, uint, void*, ulong*, int> dispatcher, void* implementation, void* data);

    [LibraryImport(Lib, EntryPoint = "wl_proxy_get_version")]
    public static partial uint ProxyGetVersion(nint proxy);

    [LibraryImport(Lib, EntryPoint = "wl_proxy_destroy")]
    public static partial void ProxyDestroy(nint proxy);
}

/// <summary>The native <c>wl_interface</c> tables libwayland needs to marshal messages, built once from the generated descriptions.</summary>
internal static unsafe class WlInterfaces
{
    // struct wl_interface { const char* name; int version; int method_count; const wl_message* methods; int event_count; const wl_message* events; } = 40 bytes.
    // struct wl_message { const char* name; const char* signature; const wl_interface** types; } = 24 bytes.
    private const int InterfaceSize = 40, MessageSize = 24;

    private static readonly Dictionary<string, nint> s_table = Build();

    /// <summary>The native interface for a name; throws when the generated tables do not contain it.</summary>
    public static nint Get(string name) =>
        s_table.TryGetValue(name, out var p) ? p : throw new PlatformException($"The Wayland interface {name} is not in the generated tables.");

    private static nint Utf8(string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        var p = (byte*)NativeMemory.Alloc((nuint)bytes.Length + 1);
        bytes.CopyTo(new Span<byte>(p, bytes.Length));
        p[bytes.Length] = 0;
        return (nint)p;
    }

    private static Dictionary<string, nint> Build()
    {
        var table = new Dictionary<string, nint>();
        foreach (var info in WaylandProtocols.All)
        {
            table[info.Name] = (nint)NativeMemory.AllocZeroed(InterfaceSize);
        }

        // These tables live for the whole process, as libwayland keeps pointers into them.
        foreach (var info in WaylandProtocols.All)
        {
            var p = (byte*)table[info.Name];
            *(nint*)p = Utf8(info.Name);
            *(int*)(p + 8) = info.Version;
            *(int*)(p + 12) = info.Requests.Length;
            *(nint*)(p + 16) = Messages(info.Requests, table);
            *(int*)(p + 24) = info.Events.Length;
            *(nint*)(p + 32) = Messages(info.Events, table);
        }

        return table;
    }

    private static nint Messages(WlMessageInfo[] messages, Dictionary<string, nint> table)
    {
        if (messages.Length == 0)
        {
            return 0;
        }

        var array = (byte*)NativeMemory.AllocZeroed((nuint)(messages.Length * MessageSize));
        for (var i = 0; i < messages.Length; i++)
        {
            var m = messages[i];
            var types = (nint*)NativeMemory.AllocZeroed((nuint)(Math.Max(1, m.ArgInterfaces.Length) * sizeof(nint)));
            for (var a = 0; a < m.ArgInterfaces.Length; a++)
            {
                if (m.ArgInterfaces[a] is { } name && table.TryGetValue(name, out var target))
                {
                    types[a] = target;
                }
            }

            var e = array + (i * MessageSize);
            *(nint*)e = Utf8(m.Name);
            *(nint*)(e + 8) = Utf8(m.Signature);
            *(nint*)(e + 16) = (nint)types;
        }

        return (nint)array;
    }
}

/// <summary>Read access to the arguments of a received event, indexed in message order.</summary>
internal readonly unsafe ref struct WlArgs
{
    private readonly ulong* _args;

    public WlArgs(ulong* args) => _args = args;

    public int Int(int i) => (int)(uint)_args[i];

    public uint Uint(int i) => (uint)_args[i];

    /// <summary>A wl_fixed_t (24.8 fixed point) as a double.</summary>
    public double Fixed(int i) => Int(i) / 256.0;

    public nint Object(int i) => (nint)_args[i];

    public int Fd(int i) => Int(i);

    public string? String(int i)
    {
        var p = (byte*)_args[i];
        return p == null ? null : Marshal.PtrToStringUTF8((nint)p);
    }

    /// <summary>The bytes of a wl_array argument; valid only during the event callback.</summary>
    public ReadOnlySpan<byte> Array(int i)
    {
        var array = (nint*)_args[i];
        return array == null ? default : new ReadOnlySpan<byte>((void*)array[2], (int)array[0]);
    }
}

/// <summary>A client-side proxy for one Wayland object: sends requests and routes its events to a handler.</summary>
internal sealed unsafe class WlObject
{
    private GCHandle _handle;

    private WlObject(nint proxy, WlInterfaceInfo info, bool listen)
    {
        Proxy = proxy;
        Info = info;
        if (listen)
        {
            _handle = GCHandle.Alloc(this);
            _ = WlClient.ProxyAddDispatcher(proxy, &Dispatch, (void*)GCHandle.ToIntPtr(_handle), null);
        }
    }

    /// <summary>The handler for events: object, event opcode, arguments.</summary>
    public delegate void EventHandler(WlObject sender, int opcode, WlArgs args);

    public nint Proxy { get; private set; }

    public WlInterfaceInfo Info { get; }

    public EventHandler? Handler { get; set; }

    /// <summary>Set by the dispatcher when a handler threw; the backend rethrows it outside native code.</summary>
    public static Exception? PendingException { get; set; }

    public uint Version => Proxy == 0 ? 0u : WlClient.ProxyGetVersion(Proxy);

    /// <summary>Wraps a proxy returned by libwayland (the display itself, or an object made by a constructor request).</summary>
    /// <remarks>The display proxy must be wrapped with <paramref name="listen"/> false: libwayland handles its events itself.</remarks>
    public static WlObject Wrap(nint proxy, WlInterfaceInfo info, bool listen = true) => new(proxy, info, listen);

    /// <summary>Sends a request that creates no object.</summary>
    public void Send(int opcode, ReadOnlySpan<ulong> args = default)
    {
        // libwayland writes the new object's pointer back into the argument array, so it must be writable memory.
        Span<ulong> copy = stackalloc ulong[Math.Max(1, args.Length)];
        args.CopyTo(copy);
        fixed (ulong* a = copy)
        {
            WlClient.ProxyMarshalArrayFlags(Proxy, (uint)opcode, 0, Version, 0, a);
        }
    }

    /// <summary>Sends a request that creates an object of <paramref name="child"/>; <paramref name="args"/> holds a 0 in the new_id slot.</summary>
    public WlObject Create(int opcode, WlInterfaceInfo child, uint version, ReadOnlySpan<ulong> args)
    {
        nint made;
        Span<ulong> copy = stackalloc ulong[Math.Max(1, args.Length)];
        args.CopyTo(copy);
        fixed (ulong* a = copy)
        {
            made = WlClient.ProxyMarshalArrayFlags(Proxy, (uint)opcode, WlInterfaces.Get(child.Name), version, 0, a);
        }

        return made == 0 ? throw new PlatformException($"The compositor could not create a {child.Name}.") : Wrap(made, child);
    }

    /// <summary>Sends a destructor request and drops the proxy.</summary>
    public void Destroy(int opcode)
    {
        if (Proxy == 0)
        {
            return;
        }

        WlClient.ProxyMarshalArrayFlags(Proxy, (uint)opcode, 0, Version, WlClient.MarshalFlagDestroy, null);
        Forget();
    }

    /// <summary>Drops the proxy of an object that has no destructor request.</summary>
    public void Release()
    {
        if (Proxy != 0)
        {
            WlClient.ProxyDestroy(Proxy);
            Forget();
        }
    }

    // libwayland discards events still queued for a destroyed proxy, so nothing can call back into this object afterwards.
    private void Forget()
    {
        Proxy = 0;
        Handler = null;
        if (_handle.IsAllocated)
        {
            _handle.Free();
        }
    }

    public static ulong Arg(int v) => (uint)v;

    public static ulong Arg(uint v) => v;

    public static ulong Arg(WlObject? o) => o is null ? 0 : (ulong)o.Proxy;

    public static ulong Fixed(double v) => (uint)(int)Math.Round(v * 256.0);

    [UnmanagedCallersOnly]
    private static int Dispatch(void* implementation, void* proxy, uint opcode, void* message, ulong* args)
    {
        try
        {
            if (GCHandle.FromIntPtr((nint)implementation).Target is WlObject o)
            {
                o.Handler?.Invoke(o, (int)opcode, new WlArgs(args));
            }
        }
        catch (Exception e)
        {
            PendingException = e;
        }

        return 0;
    }
}
