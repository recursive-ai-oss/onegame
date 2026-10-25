using System.Runtime.Versioning;
using OneGame.Net;
using OneGame.Platform;
using OneGame.Sync;

namespace OneGame.Host;

/// <summary>
/// A client connection over the browser's WebSocket, behind the same interface as the native transports, so a web build subscribes to
/// a server's scopes exactly like a desktop client does. Messages are binary and whole; the browser handles framing and ping/pong.
/// </summary>
[SupportedOSPlatform("browser")]
public sealed class BrowserWebSocketConnection : INetConnection
{
    private readonly BrowserWebSocket _socket;
    private ConnectionState _state = ConnectionState.Connecting;
    private NetStats _stats;

    /// <summary>Starts connecting to a <c>ws://</c> or <c>wss://</c> address.</summary>
    public BrowserWebSocketConnection(string url)
    {
        RemoteName = url;
        _socket = new BrowserWebSocket(url);
    }

    /// <inheritdoc />
    public ConnectionState State => _state;

    /// <inheritdoc />
    public DisconnectInfo CloseInfo { get; private set; }

    /// <inheritdoc />
    public NetStats Stats => _stats;

    /// <inheritdoc />
    public string RemoteName { get; }

    /// <inheritdoc />
    public event Action<INetConnection>? Connected;

    /// <inheritdoc />
    public event Action<INetConnection, DisconnectInfo>? Disconnected;

    /// <inheritdoc />
    public void Send(ReadOnlySpan<byte> message)
    {
        if (_state != ConnectionState.Connected)
        {
            return;
        }

        if (message.Length > Loopback.MaxMessageBytes)
        {
            throw new ArgumentException($"A message of {message.Length} bytes exceeds the {Loopback.MaxMessageBytes} byte limit.", nameof(message));
        }

        _socket.Send(message);
        _stats.MessagesSent++;
        _stats.BytesSent += message.Length;
    }

    /// <inheritdoc />
    public bool TryReceive(out ReadOnlySpan<byte> message)
    {
        if (_state == ConnectionState.Connecting)
        {
            Pump();
        }

        if (_socket.TryReceive(out message))
        {
            _stats.MessagesReceived++;
            _stats.BytesReceived += message.Length;
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void Pump()
    {
        var s = _socket.State;
        if (_state == ConnectionState.Connecting && s == 1)
        {
            _state = ConnectionState.Connected;
            Connected?.Invoke(this);
        }
        else if (_state != ConnectionState.Closed && s == 3)
        {
            var wasOpen = _state != ConnectionState.Connecting;
            var closing = _state == ConnectionState.Closing;
            _state = ConnectionState.Closed;
            CloseInfo = new DisconnectInfo(closing ? DisconnectReason.Normal : wasOpen ? DisconnectReason.Timeout : DisconnectReason.ConnectTimeout, !closing, (ushort)_socket.CloseCode);
            Disconnected?.Invoke(this, CloseInfo);
        }
    }

    /// <inheritdoc />
    public void Disconnect(DisconnectReason reason = DisconnectReason.Normal, bool flush = false)
    {
        if (_state is ConnectionState.Closed or ConnectionState.Closing)
        {
            return;
        }

        _state = ConnectionState.Closing;
        _socket.Close();
    }
}
