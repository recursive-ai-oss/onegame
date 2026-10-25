using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace OneGame.Platform;

/// <summary>
/// A binary WebSocket made with the browser's own <c>WebSocket</c>. A page cannot open sockets, so this is how a web build reaches a
/// server. Messages are queued by the browser and read here by polling.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed partial class BrowserWebSocket
{
    private const string Module = "onegame-platform";
    private readonly int _id;
    private byte[] _buffer = new byte[4096];
    private byte[] _send = new byte[4096];

    /// <summary>Starts connecting to <paramref name="url"/> (<c>ws://</c> or <c>wss://</c>).</summary>
    public BrowserWebSocket(string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        _id = WsOpen(url);
    }

    /// <summary>0 connecting, 1 open, 2 closing, 3 closed.</summary>
    public int State => WsState(_id);

    /// <summary>The WebSocket close code once <see cref="State"/> is 3 (1006 when the connection never opened).</summary>
    public int CloseCode => WsCode(_id);

    /// <summary>Sends one binary message. Dropped silently unless the socket is open.</summary>
    public void Send(ReadOnlySpan<byte> message)
    {
        if (_send.Length < message.Length)
        {
            _send = new byte[Math.Max(message.Length, _send.Length * 2)];
        }

        message.CopyTo(_send);
        WsSend(_id, _send.AsSpan(0, message.Length));
    }

    /// <summary>Takes the next received message, if one has arrived. The span is valid until the next call.</summary>
    public bool TryReceive(out ReadOnlySpan<byte> message)
    {
        var n = WsPending(_id);
        if (n < 0)
        {
            message = default;
            return false;
        }

        if (_buffer.Length < n)
        {
            _buffer = new byte[Math.Max(n, _buffer.Length * 2)];
        }

        WsReceive(_id, _buffer.AsSpan(0, n));
        message = _buffer.AsSpan(0, n);
        return true;
    }

    /// <summary>Closes the socket with the normal closure code.</summary>
    public void Close() => WsClose(_id);

    [JSImport("wsOpen", Module)]
    private static partial int WsOpen(string url);

    [JSImport("wsState", Module)]
    private static partial int WsState(int id);

    [JSImport("wsCode", Module)]
    private static partial int WsCode(int id);

    [JSImport("wsPending", Module)]
    private static partial int WsPending(int id);

    [JSImport("wsReceive", Module)]
    private static partial int WsReceive(int id, [JSMarshalAs<JSType.MemoryView>] Span<byte> destination);

    [JSImport("wsSend", Module)]
    private static partial void WsSend(int id, [JSMarshalAs<JSType.MemoryView>] Span<byte> data);

    [JSImport("wsClose", Module)]
    private static partial void WsClose(int id);
}
