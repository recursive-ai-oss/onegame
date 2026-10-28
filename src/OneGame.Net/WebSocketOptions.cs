namespace OneGame.Net;

/// <summary>Settings for the WebSocket transport (RFC 6455 over plain TCP).</summary>
public sealed class WebSocketOptions
{
    /// <summary>How links are encrypted and authenticated. Required: a transport opened without it throws <see cref="SecurityNotConfiguredException"/>. Use <see cref="NetSecurity.ForServer"/>, <see cref="NetSecurity.ForClient"/> or, for tests and local tools, <see cref="NetSecurity.Insecure"/>.</summary>
    public NetSecurity? Security { get; init; }

    /// <summary>Largest message accepted by Send and from the wire. A longer incoming message closes the connection with code 1009.</summary>
    public int MaxMessageBytes { get; init; } = 1 << 20;

    /// <summary>Most message bytes queued for sending. Past it the connection closes with <see cref="DisconnectReason.SendQueueFull"/>.</summary>
    public int MaxSendQueueBytes { get; init; } = 8 << 20;

    /// <summary>Most completed-message bytes waiting for TryReceive. Past it the socket is not read, so TCP flow control slows the sender.</summary>
    public int MaxReceiveQueueBytes { get; init; } = 4 << 20;

    /// <summary>Largest frame payload this side writes. Longer messages are split into continuation frames so pings and closes can go between them.</summary>
    public int SendFragmentBytes { get; init; } = 16 * 1024;

    /// <summary>Size of the read buffer.</summary>
    public int ReceiveBufferBytes { get; init; } = 64 * 1024;

    /// <summary>Largest HTTP header block accepted during the opening handshake.</summary>
    public int MaxHeaderBytes { get; init; } = 8 * 1024;

    /// <summary>Listener only: connections allowed at once.</summary>
    public int MaxConnections { get; init; } = 64;

    /// <summary>Listener only: sockets allowed to sit in the opening handshake at once. More are closed on accept.</summary>
    public int MaxPendingHandshakes { get; init; } = 32;

    /// <summary>Listener with security: handshakes started per second (counted when the client's first message arrives, before any key agreement). Sources that confirmed a session recently have a separate allowance of the same size. More are closed unanswered.</summary>
    public int MaxHandshakesPerSecond { get; init; } = 200;

    /// <summary>
    /// Listener with security: handshakes one source (an IPv4 address or an IPv6 /64) may start per second, so one host cannot use up the
    /// shared budget. Behind a TLS-terminating proxy every client has the proxy's address, so the per-source limits cap all players
    /// together: raise this and <see cref="MaxPendingHandshakesPerSource"/> to the totals you want (up to <see cref="MaxHandshakesPerSecond"/> and <see cref="MaxPendingHandshakes"/>) and keep the proxy's own connection limits.
    /// </summary>
    public int MaxHandshakesPerSourcePerSecond { get; init; } = 10;

    /// <summary>
    /// Listener with security: a socket must send its first handshake message within this long of connecting, or it is closed. Sockets that
    /// have not spoken yet are also the first to be dropped when the pending queue is full.
    /// </summary>
    public int FirstMessageTimeoutMs { get; init; } = 2000;

    /// <summary>Listener with security: sockets from one source (an IPv4 address or an IPv6 /64) allowed to sit in the handshake at once.</summary>
    public int MaxPendingHandshakesPerSource { get; init; } = 4;

    /// <summary>The TCP connect and the opening handshake must finish within this long.</summary>
    public int HandshakeTimeoutMs { get; init; } = 5000;

    /// <summary>Send a ping after this long without hearing anything.</summary>
    public int KeepAliveMs { get; init; } = 5000;

    /// <summary>Close with <see cref="DisconnectReason.Timeout"/> after this long without hearing anything.</summary>
    public int TimeoutMs { get; init; } = 20_000;

    /// <summary>How long to wait for the peer's close frame before closing the socket anyway.</summary>
    public int CloseTimeoutMs { get; init; } = 2000;

    /// <summary>Client only: the request path.</summary>
    public string Path { get; init; } = "/";

    /// <summary>Client only: the Host header; the remote address if null.</summary>
    public string? Host { get; init; }

    /// <summary>Listener only: if set, a browser's Origin header must be one of these (compared ignoring case). Requests without an Origin header (non-browser clients) are always accepted.</summary>
    public IReadOnlyList<string>? AllowedOrigins { get; init; }

    // The options of the link underneath a secure one: records are at most 65535 bytes, so that bounds its messages.
    internal WebSocketOptions ForInner() => new()
    {
        Security = NetSecurity.Insecure,
        MaxMessageBytes = Math.Min(SecureConnection.MaxRecord, MaxMessageBytes + 17),
        MaxSendQueueBytes = MaxSendQueueBytes,
        MaxReceiveQueueBytes = MaxReceiveQueueBytes,
        SendFragmentBytes = SendFragmentBytes,
        ReceiveBufferBytes = ReceiveBufferBytes,
        MaxHeaderBytes = MaxHeaderBytes,
        MaxConnections = MaxConnections,
        MaxPendingHandshakes = MaxPendingHandshakes,
        HandshakeTimeoutMs = HandshakeTimeoutMs,
        FirstMessageTimeoutMs = FirstMessageTimeoutMs,
        KeepAliveMs = KeepAliveMs,
        TimeoutMs = TimeoutMs,
        CloseTimeoutMs = CloseTimeoutMs,
        Path = Path,
        Host = Host,
        AllowedOrigins = AllowedOrigins,
    };

    internal void Validate()
    {
        Check(MaxMessageBytes is >= 1 and <= (64 << 20), nameof(MaxMessageBytes), "must be 1 to 64 MiB");
        Check(MaxSendQueueBytes >= MaxMessageBytes, nameof(MaxSendQueueBytes), "must be at least MaxMessageBytes");
        Check(MaxReceiveQueueBytes >= 1, nameof(MaxReceiveQueueBytes), "must be positive");
        Check(SendFragmentBytes is >= 125 and <= (1 << 20), nameof(SendFragmentBytes), "must be 125 to 1 MiB");
        Check(ReceiveBufferBytes >= 1024, nameof(ReceiveBufferBytes), "must be at least 1024");
        Check(MaxHeaderBytes >= 512 && MaxHeaderBytes <= ReceiveBufferBytes, nameof(MaxHeaderBytes), "must be 512 up to ReceiveBufferBytes");
        Check(MaxConnections >= 1 && MaxPendingHandshakes >= 1, nameof(MaxConnections), "and MaxPendingHandshakes must be positive");
        Check(MaxHandshakesPerSecond >= 1 && MaxPendingHandshakesPerSource >= 1, nameof(MaxHandshakesPerSecond), "and MaxPendingHandshakesPerSource must be positive");
        Check(MaxHandshakesPerSourcePerSecond >= 1 && FirstMessageTimeoutMs >= 1, nameof(MaxHandshakesPerSourcePerSecond), "and FirstMessageTimeoutMs must be positive");
        Check(HandshakeTimeoutMs >= 1 && CloseTimeoutMs >= 1, nameof(HandshakeTimeoutMs), "and CloseTimeoutMs must be positive");
        Check(KeepAliveMs >= 10 && TimeoutMs > KeepAliveMs, nameof(TimeoutMs), "must exceed KeepAliveMs, which must be at least 10");
        Check(Path.Length > 0 && Path[0] == '/' && !Path.Contains(' ', StringComparison.Ordinal) && !Path.Contains('\r', StringComparison.Ordinal) && !Path.Contains('\n', StringComparison.Ordinal), nameof(Path), "must start with '/' and contain no spaces or line breaks");
        Check(Host is null || (!Host.Contains('\r', StringComparison.Ordinal) && !Host.Contains('\n', StringComparison.Ordinal)), nameof(Host), "must not contain line breaks");
    }

    private static void Check(bool ok, string name, string rule)
    {
        if (!ok)
        {
            throw new ArgumentException($"WebSocketOptions.{name} {rule}.", name);
        }
    }
}
