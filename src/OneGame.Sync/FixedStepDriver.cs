namespace OneGame.Sync;

/// <summary>
/// Turns variable frame times into a fixed number of simulation ticks. It does exact integer arithmetic on the time it is given, so N
/// ticks come out of N/rate seconds however the frames are cut, and it never reads a clock itself: the host passes the elapsed time.
/// </summary>
public sealed class FixedStepDriver
{
    private const long MaxFrameTicks = TimeSpan.TicksPerHour;
    private const long ScaleUnit = 1000;

    private long _accumulator;   // elapsed ticks x rate x scale units
    private long _scaleMilli = ScaleUnit;
    private int _pendingSteps;

    /// <summary>Creates a driver.</summary>
    /// <param name="tickRate">Simulation ticks per second, 1 to 1000.</param>
    /// <param name="maxStepsPerFrame">Most ticks one <see cref="Advance"/> returns; time beyond that is dropped so a stall cannot snowball.</param>
    public FixedStepDriver(int tickRate = 60, int maxStepsPerFrame = 5)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tickRate, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(tickRate, 1000);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxStepsPerFrame, 1);
        TickRate = tickRate;
        MaxStepsPerFrame = maxStepsPerFrame;
    }

    /// <summary>Ticks per second.</summary>
    public int TickRate { get; }

    /// <summary>The catch-up cap.</summary>
    public int MaxStepsPerFrame { get; }

    /// <summary>While true, <see cref="Advance"/> returns 0 (or 1 after <see cref="RequestStep"/>) and discards the time.</summary>
    public bool Paused { get; set; }

    /// <summary>Speed multiplier, 0 to 100, in steps of 0.001. 1 is real time.</summary>
    public double TimeScale
    {
        get => _scaleMilli / (double)ScaleUnit;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100.0);
            _scaleMilli = (long)Math.Round(value * ScaleUnit);
        }
    }

    /// <summary>Ticks returned so far.</summary>
    public long TotalSteps { get; private set; }

    /// <summary>Ticks of time discarded by the catch-up cap.</summary>
    public long DroppedTicks { get; private set; }

    /// <summary>How far between the last tick and the next the frame is, from 0 to just under 1. Views interpolate with it.</summary>
    public double Alpha => (double)_accumulator / ((double)TimeSpan.TicksPerSecond * ScaleUnit);

    /// <summary>While paused, lets the next <see cref="Advance"/> return one tick.</summary>
    public void RequestStep() => _pendingSteps++;

    /// <summary>Takes the time since the last call and returns how many ticks to run now.</summary>
    /// <param name="frameTime">The elapsed time; negative values count as zero.</param>
    public int Advance(TimeSpan frameTime)
    {
        if (Paused)
        {
            _accumulator = 0;
            var requested = Math.Min(_pendingSteps, MaxStepsPerFrame);
            _pendingSteps = 0;
            TotalSteps += requested;
            return requested;
        }

        _pendingSteps = 0;
        var elapsed = Math.Clamp(frameTime.Ticks, 0, MaxFrameTicks);
        var stepCost = TimeSpan.TicksPerSecond * ScaleUnit;
        _accumulator += elapsed * TickRate * _scaleMilli;
        var steps = _accumulator / stepCost;
        _accumulator -= steps * stepCost;
        if (steps > MaxStepsPerFrame)
        {
            DroppedTicks += steps - MaxStepsPerFrame;
            steps = MaxStepsPerFrame;
        }

        TotalSteps += steps;
        return (int)steps;
    }
}
