namespace OneGame.Net;

/// <summary>Settings for the reliable-UDP transport. Both ends should use the same <see cref="MaxMessageBytes"/>; a peer that sends more is disconnected.</summary>
public sealed class UdpOptions
{
    /// <summary>How links are encrypted and authenticated. Required: a transport opened without it throws <see cref="SecurityNotConfiguredException"/>. Use <see cref="NetSecurity.ForServer"/>, <see cref="NetSecurity.ForClient"/> or, for tests and local tools, <see cref="NetSecurity.Insecure"/>.</summary>
    public NetSecurity? Security { get; init; }

    /// <summary>Largest datagram written to the socket, in bytes. 1200 fits every IPv6 path (minimum link MTU 1280). Range 576 to 1472.</summary>
    public int Mtu { get; init; } = 1200;

    /// <summary>Largest message accepted by Send and from the wire.</summary>
    public int MaxMessageBytes { get; init; } = 1 << 20;

    /// <summary>Most message bytes queued for sending (including unacknowledged). Past it the connection closes with <see cref="DisconnectReason.SendQueueFull"/>.</summary>
    public int MaxSendQueueBytes { get; init; } = 8 << 20;

    /// <summary>Most completed-message bytes waiting for TryReceive. Past it the receiver stops acknowledging new data, which stalls the sender instead of growing memory.</summary>
    public int MaxReceiveQueueBytes { get; init; } = 4 << 20;

    /// <summary>How many fragments past the delivered point the receiver will hold out of order. A power of two from 16 to 4096.</summary>
    public int ReceiveWindow { get; init; } = 256;

    /// <summary>Listener only: connections allowed at once.</summary>
    public int MaxConnections { get; init; } = 64;

    /// <summary>Send an empty packet after this long without sending anything.</summary>
    public int KeepAliveMs { get; init; } = 1000;

    /// <summary>Close with <see cref="DisconnectReason.Timeout"/> after this long without a valid packet.</summary>
    public int TimeoutMs { get; init; } = 10_000;

    /// <summary>Give up on the handshake after this long.</summary>
    public int ConnectTimeoutMs { get; init; } = 5000;

    /// <summary>Client handshake packets repeat at this interval.</summary>
    public int ConnectRetryMs { get; init; } = 250;

    /// <summary>A flushing disconnect waits at most this long for the peer to acknowledge queued data.</summary>
    public int LingerMs { get; init; } = 1000;

    /// <summary>Lower bound of the retransmission timeout.</summary>
    public int MinRtoMs { get; init; } = 40;

    /// <summary>Upper bound of the retransmission timeout before backoff.</summary>
    public int MaxRtoMs { get; init; } = 3000;

    /// <summary>
    /// The congestion window never shrinks below this many datagrams, and starts there. Game traffic is small and steady, and random
    /// loss on a wireless link is not congestion, so the usual collapse to one or two packets after a loss would starve a sync stream.
    /// </summary>
    public int MinCongestionWindowPackets { get; init; } = 12;

    /// <summary>
    /// A secure connection closes with <see cref="DisconnectReason.PacketLimit"/> after sending this many packets in one direction,
    /// because the 32-bit packet number is the encryption nonce and must never repeat. The client then connects again (new keys).
    /// </summary>
    public uint MaxPacketsPerKey { get; init; } = uint.MaxValue - 65_535;

    /// <summary>
    /// Listener only: secure handshakes the server will start per second for sources it has not seen confirm a session. Each costs five X25519
    /// operations (the server's own public key is computed once), so excess requests are dropped (clients retry) instead of queued.
    /// </summary>
    public int MaxHandshakesPerSecond { get; init; } = 200;

    /// <summary>Listener only: handshakes per second one source may start (an IPv4 address, or an IPv6 /64), so one host cannot use up the shared budget.</summary>
    public int MaxHandshakesPerSourcePerSecond { get; init; } = 10;

    /// <summary>Listener only: an extra per-second budget kept for sources that recently completed a session, so a flood from strangers cannot lock out returning players.</summary>
    public int ReservedHandshakesPerSecond { get; init; } = 100;

    /// <summary>Listener only: handshakes that have been answered but not yet confirmed by an authenticated packet. They do not count against <see cref="MaxConnections"/>.</summary>
    public int MaxUnconfirmed { get; init; } = 256;

    /// <summary>Listener only: unconfirmed handshakes one source may hold.</summary>
    public int MaxUnconfirmedPerSource { get; init; } = 4;

    /// <summary>Listener only: an unconfirmed handshake is dropped after this long.</summary>
    public int UnconfirmedTimeoutMs { get; init; } = 5000;

    /// <summary>Most datagrams read from one socket per pump, so a flood cannot starve the game loop.</summary>
    public int MaxDatagramsPerPump { get; init; } = 4096;

    internal void Validate()
    {
        Check(Mtu is >= 576 and <= 1472, nameof(Mtu), "must be 576 to 1472");
        Check(MaxMessageBytes is >= 1 and <= (64 << 20), nameof(MaxMessageBytes), "must be 1 to 64 MiB");
        Check(MaxSendQueueBytes >= MaxMessageBytes, nameof(MaxSendQueueBytes), "must be at least MaxMessageBytes");
        Check(MaxReceiveQueueBytes >= 1, nameof(MaxReceiveQueueBytes), "must be positive");
        Check(ReceiveWindow is >= 16 and <= 4096 && (ReceiveWindow & (ReceiveWindow - 1)) == 0, nameof(ReceiveWindow), "must be a power of two from 16 to 4096");
        Check(MaxConnections >= 1, nameof(MaxConnections), "must be positive");
        Check(KeepAliveMs >= 10 && TimeoutMs > KeepAliveMs, nameof(TimeoutMs), "must exceed KeepAliveMs, which must be at least 10");
        Check(ConnectRetryMs >= 10 && ConnectTimeoutMs >= ConnectRetryMs, nameof(ConnectTimeoutMs), "must be at least ConnectRetryMs, which must be at least 10");
        Check(MinRtoMs >= 1 && MaxRtoMs >= MinRtoMs, nameof(MaxRtoMs), "must be at least MinRtoMs");
        Check(MinCongestionWindowPackets is >= 2 and <= 1024, nameof(MinCongestionWindowPackets), "must be 2 to 1024");
        Check(MaxHandshakesPerSourcePerSecond >= 1 && ReservedHandshakesPerSecond >= 0 && MaxUnconfirmed >= 1 && MaxUnconfirmedPerSource >= 1 && UnconfirmedTimeoutMs >= 100, nameof(MaxHandshakesPerSourcePerSecond), "handshake budgets must be positive (reserved may be 0, the unconfirmed timeout at least 100 ms)");
        Check(MaxHandshakesPerSecond >= 1, nameof(MaxHandshakesPerSecond), "must be positive");
        Check(MaxPacketsPerKey >= 1024, nameof(MaxPacketsPerKey), "must be at least 1024");
        Check(MaxDatagramsPerPump >= 1, nameof(MaxDatagramsPerPump), "must be positive");
    }

    private static void Check(bool ok, string name, string rule)
    {
        if (!ok)
        {
            throw new ArgumentException($"UdpOptions.{name} {rule}.", name);
        }
    }
}
