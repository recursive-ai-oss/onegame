using System.Diagnostics;

namespace OneGame.Net;

/// <summary>The time source transports use for timeouts, retransmission and the conditioner. Tests swap in <see cref="ManualNetClock"/> to run seconds of network time instantly.</summary>
public abstract class NetClock
{
    /// <summary>The real clock.</summary>
    public static NetClock System { get; } = new SystemNetClock();

    /// <summary>Milliseconds since an arbitrary origin; never goes backwards.</summary>
    public abstract long NowMs { get; }
}

internal sealed class SystemNetClock : NetClock
{
    public override long NowMs => Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
}

/// <summary>A clock the caller advances by hand.</summary>
public sealed class ManualNetClock : NetClock
{
    private long _now = 1_000_000;

    /// <inheritdoc />
    public override long NowMs => _now;

    /// <summary>Moves time forward.</summary>
    /// <param name="ms">Milliseconds to add; must not be negative.</param>
    public void Advance(long ms)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ms);
        _now += ms;
    }
}
