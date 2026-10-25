using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace OneGame.Net;

/// <summary>What a bad network does to traffic. Applied to what a conditioned endpoint sends, so wrapping both ends doubles the latency.</summary>
public sealed class NetworkConditions
{
    /// <summary>Delay added to every datagram or message, in milliseconds.</summary>
    public int LatencyMs { get; init; }

    /// <summary>Each delay varies uniformly by up to this much either way.</summary>
    public int JitterMs { get; init; }

    /// <summary>Chance (0 to 1) that a datagram vanishes. On a message stream, which is reliable, a "lost" message instead waits for a retransmission timeout, as TCP would.</summary>
    public double LossRate { get; init; }

    /// <summary>Chance (0 to 1) that a datagram arrives twice. Streams ignore it: TCP discards duplicates.</summary>
    public double DuplicateRate { get; init; }

    /// <summary>Chance (0 to 1) that a datagram is held back by <see cref="ReorderDelayMs"/> so later ones overtake it. Streams ignore it: TCP keeps order.</summary>
    public double ReorderRate { get; init; }

    /// <summary>Extra delay of a reordered datagram, in milliseconds.</summary>
    public int ReorderDelayMs { get; init; } = 40;

    /// <summary>Link speed in bytes per second; 0 is unlimited. Traffic queues behind it, and datagrams that would wait longer than <see cref="QueueLimitMs"/> are dropped.</summary>
    public int BandwidthBytesPerSecond { get; init; }

    /// <summary>The most a datagram may queue behind the bandwidth cap before it is dropped.</summary>
    public int QueueLimitMs { get; init; } = 1000;

    /// <summary>Seed of the random choices, so a run can be repeated.</summary>
    public int Seed { get; init; } = 1;
}

/// <summary>Wraps a datagram socket and applies <see cref="NetworkConditions"/> to what it sends. Delayed datagrams go out when the socket is next used, so keep pumping.</summary>
public sealed class ConditionedDatagramSocket : IDatagramSocket
{
    private readonly IDatagramSocket _inner;
    private readonly NetworkConditions _c;
    private readonly NetClock _clock;
    private readonly bool _ownsInner;
    private readonly Random _rng;
    private readonly PriorityQueue<Pending, (long Due, long Order)> _queue = new();
    private double _busyUntil;
    private long _order;

    /// <summary>Wraps <paramref name="inner"/>.</summary>
    /// <param name="inner">The real socket.</param>
    /// <param name="conditions">What to do to traffic.</param>
    /// <param name="clock">The clock the delays follow; share it with the transports above.</param>
    /// <param name="ownsInner">Dispose <paramref name="inner"/> with this socket.</param>
    public ConditionedDatagramSocket(IDatagramSocket inner, NetworkConditions conditions, NetClock clock, bool ownsInner = true)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(clock);
        _inner = inner;
        _c = conditions;
        _clock = clock;
        _ownsInner = ownsInner;
        _rng = new Random(conditions.Seed);
    }

    /// <summary>Datagrams dropped on purpose (loss or a full link).</summary>
    public long Dropped { get; private set; }

    /// <summary>Datagrams delivered twice on purpose.</summary>
    public long Duplicated { get; private set; }

    /// <summary>Datagrams held back on purpose.</summary>
    public long Reordered { get; private set; }

    /// <inheritdoc />
    public SocketAddress LocalAddress => _inner.LocalAddress;

    /// <inheritdoc />
    public void Send(ReadOnlySpan<byte> datagram, SocketAddress destination)
    {
        var now = _clock.NowMs;
        Release(now);
        if (_rng.NextDouble() < _c.LossRate)
        {
            Dropped++;
            return;
        }

        var copies = _rng.NextDouble() < _c.DuplicateRate ? 2 : 1;
        if (copies == 2)
        {
            Duplicated++;
        }

        for (var i = 0; i < copies; i++)
        {
            var depart = (double)now;
            if (_c.BandwidthBytesPerSecond > 0)
            {
                var start = Math.Max(now, _busyUntil);
                if (start - now > _c.QueueLimitMs)
                {
                    Dropped++;
                    return;
                }

                _busyUntil = start + (datagram.Length * 1000.0 / _c.BandwidthBytesPerSecond);
                depart = _busyUntil;
            }

            var delay = _c.LatencyMs + (_c.JitterMs == 0 ? 0 : _rng.Next(-_c.JitterMs, _c.JitterMs + 1));
            if (_rng.NextDouble() < _c.ReorderRate)
            {
                delay += _c.ReorderDelayMs;
                Reordered++;
            }

            var due = (long)Math.Ceiling(depart) + Math.Max(0, delay);
            var copy = NetBuffers.Rent(Math.Max(1, datagram.Length));
            datagram.CopyTo(copy);
            _queue.Enqueue(new Pending(copy, datagram.Length, Wire.Clone(destination)), (due, _order++));
        }

        Release(now);
    }

    /// <inheritdoc />
    public int Receive(Span<byte> buffer, SocketAddress from)
    {
        Release(_clock.NowMs);
        return _inner.Receive(buffer, from);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        while (_queue.TryDequeue(out var p, out _))
        {
            NetBuffers.Return(p.Data);
        }

        if (_ownsInner)
        {
            _inner.Dispose();
        }
    }

    private void Release(long now)
    {
        while (_queue.TryPeek(out var p, out var key) && key.Due <= now)
        {
            _ = _queue.Dequeue();
            _inner.Send(p.Data.AsSpan(0, p.Length), p.Destination);
            NetBuffers.Return(p.Data);
        }
    }

    private readonly record struct Pending(byte[] Data, int Length, SocketAddress Destination);
}

/// <summary>
/// Wraps any <see cref="INetConnection"/> and applies <see cref="NetworkConditions"/> to what it sends, for transports that are
/// already reliable (WebSocket over TCP). Latency, jitter and bandwidth act as they would on the wire; loss shows up as a
/// retransmission delay, because a lost TCP segment is resent, not lost; order and duplicates are untouched.
/// </summary>
public sealed class ConditionedConnection : INetConnection
{
    private readonly INetConnection _inner;
    private readonly NetworkConditions _c;
    private readonly NetClock _clock;
    private readonly Random _rng;
    private readonly Queue<(long Due, byte[] Data, int Length)> _queue = new();
    private long _lastDue;
    private double _busyUntil;

    /// <summary>Wraps <paramref name="inner"/>.</summary>
    /// <param name="inner">The real connection.</param>
    /// <param name="conditions">What to do to traffic.</param>
    /// <param name="clock">The clock the delays follow.</param>
    public ConditionedConnection(INetConnection inner, NetworkConditions conditions, NetClock clock)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(clock);
        _inner = inner;
        _c = conditions;
        _clock = clock;
        _rng = new Random(conditions.Seed);
        inner.Connected += _ => Connected?.Invoke(this);
        inner.Disconnected += (_, info) => Disconnected?.Invoke(this, info);
    }

    /// <inheritdoc />
    public event Action<INetConnection>? Connected;

    /// <inheritdoc />
    public event Action<INetConnection, DisconnectInfo>? Disconnected;

    /// <inheritdoc />
    public ConnectionState State => _inner.State;

    /// <inheritdoc />
    public DisconnectInfo CloseInfo => _inner.CloseInfo;

    /// <inheritdoc />
    public NetStats Stats => _inner.Stats;

    /// <inheritdoc />
    public string RemoteName => _inner.RemoteName;

    /// <inheritdoc />
    public void Send(ReadOnlySpan<byte> message)
    {
        var now = _clock.NowMs;
        var depart = (double)now;
        if (_c.BandwidthBytesPerSecond > 0)
        {
            var start = Math.Max(now, _busyUntil);
            _busyUntil = start + (message.Length * 1000.0 / _c.BandwidthBytesPerSecond);
            depart = _busyUntil;
        }

        var delay = _c.LatencyMs + (_c.JitterMs == 0 ? 0 : _rng.Next(-_c.JitterMs, _c.JitterMs + 1));
        if (_rng.NextDouble() < _c.LossRate)
        {
            delay += Math.Max(200, 2 * (_c.LatencyMs + _c.JitterMs)); // one retransmission timeout
        }

        // A byte stream never reorders, so a message waits for the one before it.
        var due = Math.Max(_lastDue, (long)Math.Ceiling(depart) + Math.Max(0, delay));
        _lastDue = due;
        var copy = NetBuffers.Rent(Math.Max(1, message.Length));
        message.CopyTo(copy);
        _queue.Enqueue((due, copy, message.Length));
        Release(now);
    }

    /// <inheritdoc />
    public bool TryReceive(out ReadOnlySpan<byte> message) => _inner.TryReceive(out message);

    /// <inheritdoc />
    public void Pump()
    {
        Release(_clock.NowMs);
        _inner.Pump();
    }

    /// <inheritdoc />
    public void Disconnect(DisconnectReason reason = DisconnectReason.Normal, bool flush = false)
    {
        Release(long.MaxValue);
        _inner.Disconnect(reason, flush);
    }

    private void Release(long now)
    {
        while (_queue.TryPeek(out var head) && head.Due <= now)
        {
            _ = _queue.Dequeue();
            _inner.Send(head.Data.AsSpan(0, head.Length));
            NetBuffers.Return(head.Data);
        }
    }
}

/// <summary>Wraps a listener so every connection it accepts is a <see cref="ConditionedConnection"/>.</summary>
public sealed class ConditionedListener : INetListener
{
    private readonly INetListener _inner;
    private readonly NetworkConditions _c;
    private readonly NetClock _clock;
    private readonly List<ConditionedConnection> _wrapped = [];
    private int _seed;

    /// <summary>Wraps <paramref name="inner"/>.</summary>
    /// <param name="inner">The real listener.</param>
    /// <param name="conditions">What to do to each accepted connection's traffic.</param>
    /// <param name="clock">The clock the delays follow.</param>
    public ConditionedListener(INetListener inner, NetworkConditions conditions, NetClock clock)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(clock);
        _inner = inner;
        _c = conditions;
        _clock = clock;
    }

    /// <inheritdoc />
    public EndPoint LocalEndPoint => _inner.LocalEndPoint;

    /// <inheritdoc />
    public int ConnectionCount => _inner.ConnectionCount;

    /// <inheritdoc />
    public int PendingHandshakes => _inner.PendingHandshakes;

    /// <inheritdoc />
    public bool TryAccept(out INetConnection connection)
    {
        if (_inner.TryAccept(out var c))
        {
            var conditions = new NetworkConditions
            {
                LatencyMs = _c.LatencyMs,
                JitterMs = _c.JitterMs,
                LossRate = _c.LossRate,
                DuplicateRate = _c.DuplicateRate,
                ReorderRate = _c.ReorderRate,
                ReorderDelayMs = _c.ReorderDelayMs,
                BandwidthBytesPerSecond = _c.BandwidthBytesPerSecond,
                QueueLimitMs = _c.QueueLimitMs,
                Seed = _c.Seed + (++_seed),
            };
            var w = new ConditionedConnection(c, conditions, _clock);
            _wrapped.Add(w);
            connection = w;
            return true;
        }

        connection = null!;
        return false;
    }

    /// <inheritdoc />
    public void Pump()
    {
        _inner.Pump();
        for (var i = 0; i < _wrapped.Count; i++)
        {
            _wrapped[i].Pump();
            if (_wrapped[i].State == ConnectionState.Closed)
            {
                _wrapped.RemoveAt(i--);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose() => _inner.Dispose();
}
