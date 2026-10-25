using System.Net;

namespace OneGame.Net;

/// <summary>Where a connection is in its life.</summary>
public enum ConnectionState : byte
{
    /// <summary>The handshake is running. Messages may be queued; they go out once connected.</summary>
    Connecting = 0,

    /// <summary>Messages flow both ways.</summary>
    Connected = 1,

    /// <summary>A flushing disconnect is draining queued messages. New sends are ignored.</summary>
    Closing = 2,

    /// <summary>Over. <see cref="INetConnection.CloseInfo"/> says why. Sends are ignored.</summary>
    Closed = 3,
}

/// <summary>Why a connection ended. Values travel on the wire, so they never change meaning.</summary>
public enum DisconnectReason : byte
{
    /// <summary>Not closed yet.</summary>
    None = 0,

    /// <summary>One side closed on purpose.</summary>
    Normal = 1,

    /// <summary>Nothing was heard from the peer for the timeout.</summary>
    Timeout = 2,

    /// <summary>The handshake did not finish in time.</summary>
    ConnectTimeout = 3,

    /// <summary>The peer sent something the protocol forbids (an oversize message, a bad frame sequence).</summary>
    ProtocolViolation = 4,

    /// <summary>More was queued to send than the configured limit; the peer is too slow or gone.</summary>
    SendQueueFull = 5,

    /// <summary>The server has no room for another connection.</summary>
    ServerFull = 6,

    /// <summary>The peer speaks a protocol version this side does not.</summary>
    VersionMismatch = 7,

    /// <summary>The server refused the handshake.</summary>
    Rejected = 8,

    /// <summary>The server removed this client.</summary>
    Kicked = 9,

    /// <summary>The operating system reported a socket failure.</summary>
    TransportError = 10,

    /// <summary>The peer is shutting down.</summary>
    Shutdown = 11,

    /// <summary>A message was larger than the limit.</summary>
    MessageTooLarge = 12,
}

/// <summary>How and why a connection closed.</summary>
/// <param name="Reason">The reason. For a remote close this is what the peer said.</param>
/// <param name="ByRemote">True if the peer closed it, false if this side did (including timeouts detected here).</param>
/// <param name="Code">Transport detail: the application byte of a UDP disconnect, or the WebSocket close code.</param>
public readonly record struct DisconnectInfo(DisconnectReason Reason, bool ByRemote, ushort Code = 0);

/// <summary>Counters and estimates for one connection. All values are since the connection was created.</summary>
public struct NetStats
{
    /// <summary>Smoothed round-trip time in milliseconds; 0 until measured.</summary>
    public double RttMs { get; set; }

    /// <summary>Round-trip time variation in milliseconds.</summary>
    public double RttVarMs { get; set; }

    /// <summary>Datagrams (UDP) or frames (WebSocket) sent.</summary>
    public long PacketsSent { get; set; }

    /// <summary>Datagrams or frames accepted.</summary>
    public long PacketsReceived { get; set; }

    /// <summary>Datagrams dropped as malformed, duplicate or stale.</summary>
    public long PacketsDropped { get; set; }

    /// <summary>Bytes written to the socket.</summary>
    public long BytesSent { get; set; }

    /// <summary>Bytes read from the socket and accepted.</summary>
    public long BytesReceived { get; set; }

    /// <summary>Whole messages queued by Send.</summary>
    public long MessagesSent { get; set; }

    /// <summary>Whole messages handed out by TryReceive or waiting for it.</summary>
    public long MessagesReceived { get; set; }

    /// <summary>Reliable fragments sent again (timeout or fast retransmit).</summary>
    public long Retransmits { get; set; }

    /// <summary>Estimated share of the peer's datagrams that never arrived (0 to 1), from gaps in packet numbers.</summary>
    public double InboundLoss { get; set; }

    /// <summary>Share of reliable fragment transmissions that were repeats (0 to 1): a proxy for loss of our own datagrams.</summary>
    public double OutboundLoss { get; set; }

    /// <summary>Bytes of messages queued and not yet acknowledged.</summary>
    public int SendQueueBytes { get; set; }

    /// <summary>Bytes sent and awaiting acknowledgement.</summary>
    public int InFlightBytes { get; set; }

    /// <summary>Current congestion window in bytes (UDP only).</summary>
    public int CongestionWindowBytes { get; set; }
}

/// <summary>
/// One live connection to a peer. Messages are whole, ordered and reliable (<see cref="IMessageChannel"/>). Nothing happens on its
/// own: <see cref="Pump"/> does the socket work, on the caller's thread. The events fire inside <see cref="Pump"/> (or inside
/// <see cref="Disconnect"/>).
/// </summary>
public interface INetConnection : IMessageChannel
{
    /// <summary>Current state.</summary>
    ConnectionState State { get; }

    /// <summary>Why the connection closed; <see cref="DisconnectReason.None"/> until then.</summary>
    DisconnectInfo CloseInfo { get; }

    /// <summary>A snapshot of the counters.</summary>
    NetStats Stats { get; }

    /// <summary>The peer's address, as text, for logs.</summary>
    string RemoteName { get; }

    /// <summary>Raised once when the handshake completes.</summary>
    event Action<INetConnection>? Connected;

    /// <summary>Raised once when the connection closes, with the reason.</summary>
    event Action<INetConnection, DisconnectInfo>? Disconnected;

    /// <summary>
    /// Reads and writes whatever the socket allows right now. Connections made by <c>Connect</c> must be pumped by their owner;
    /// connections handed out by a listener are pumped by the listener (calling this on them is harmless).
    /// </summary>
    void Pump();

    /// <summary>Closes the connection and tells the peer why.</summary>
    /// <param name="reason">The reason the peer will see.</param>
    /// <param name="flush">Keep pumping until queued messages are acknowledged (or a short linger ends) before closing.</param>
    void Disconnect(DisconnectReason reason = DisconnectReason.Normal, bool flush = false);
}

/// <summary>A server endpoint that turns incoming handshakes into <see cref="INetConnection"/>s.</summary>
public interface INetListener : IDisposable
{
    /// <summary>The address the listener is bound to.</summary>
    EndPoint LocalEndPoint { get; }

    /// <summary>Connections currently alive (connected or closing).</summary>
    int ConnectionCount { get; }

    /// <summary>Handshakes the server holds state for right now. Always 0 for UDP, whose handshake is stateless until the client proves its address.</summary>
    int PendingHandshakes { get; }

    /// <summary>Takes the next newly connected peer, or returns false.</summary>
    bool TryAccept(out INetConnection connection);

    /// <summary>Processes incoming datagrams or connections and pumps every accepted connection.</summary>
    void Pump();
}

/// <summary>A message was larger than the configured maximum, so it was refused before anything was queued.</summary>
public sealed class MessageTooLargeException : ArgumentException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="size">The size that was offered.</param>
    /// <param name="limit">The configured maximum.</param>
    public MessageTooLargeException(int size, int limit)
        : base($"A message of {size} bytes is larger than the configured maximum of {limit} bytes. Split it, or raise MaxMessageBytes on both ends.", "message")
    {
        Size = size;
        Limit = limit;
    }

    /// <summary>The size that was offered.</summary>
    public int Size { get; }

    /// <summary>The configured maximum.</summary>
    public int Limit { get; }
}
