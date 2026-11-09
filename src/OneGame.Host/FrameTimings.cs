using System.Diagnostics;

namespace OneGame.Host;

/// <summary>How long one frame took on the CPU.</summary>
/// <param name="Frame">The frame number (<see cref="GameInstance.Frame"/>).</param>
/// <param name="CpuMs">The whole frame: input, simulation, view update and drawing, in milliseconds.</param>
/// <param name="SimMs">The part spent stepping the simulation (or taking in the server's ticks), in milliseconds.</param>
/// <param name="DrawMs">The part spent building and submitting the picture, in milliseconds; 0 in a headless run.</param>
public readonly record struct FrameTiming(long Frame, double CpuMs, double SimMs, double DrawMs);

/// <summary>One pass of the render graph and how long the GPU spent on it.</summary>
/// <param name="Name">The pass name, as <c>render_graph</c> shows it.</param>
/// <param name="Microseconds">GPU time in microseconds, from the newest finished frame.</param>
public readonly record struct GpuPassTime(string Name, double Microseconds);

/// <summary>Whether GPU pass times can be read.</summary>
public enum GpuTimingStatus
{
    /// <summary>No graphics device, or the stage has not drawn a frame yet.</summary>
    None,

    /// <summary>The backend cannot time passes (Metal and WebGPU).</summary>
    Unsupported,

    /// <summary>Times are available (a frame or two behind).</summary>
    Ready,
}

/// <summary>
/// A short history of CPU frame times, filled by <see cref="GameInstance"/> only while <see cref="Enabled"/> is set; the debug panel sets it
/// while it is shown. With it off a frame takes no timestamps and stores nothing. The history is a fixed ring: recording allocates nothing.
/// </summary>
public sealed class FrameTimings
{
    /// <summary>How many frames are kept.</summary>
    public const int Capacity = 60;

    private readonly FrameTiming[] _ring = new FrameTiming[Capacity];
    private int _next;

    /// <summary>Whether frames are being timed. Off by default.</summary>
    public bool Enabled { get; set; }

    /// <summary>Frames held, up to <see cref="Capacity"/>.</summary>
    public int Count { get; private set; }

    /// <summary>The newest frame, or a zeroed value when none was timed.</summary>
    public FrameTiming Latest => Count == 0 ? default : _ring[(_next - 1 + Capacity) % Capacity];

    /// <summary>Copies the history, oldest first.</summary>
    /// <param name="destination">At least <see cref="Count"/> long.</param>
    /// <returns>How many were written.</returns>
    public int CopyTo(Span<FrameTiming> destination)
    {
        var n = Math.Min(Count, destination.Length);
        for (var i = 0; i < n; i++)
        {
            destination[i] = _ring[(_next - Count + i + Capacity) % Capacity];
        }

        return n;
    }

    /// <summary>Forgets the history.</summary>
    public void Clear()
    {
        Count = 0;
        _next = 0;
    }

    internal void Record(long frame, long start, long simStart, long simEnd, long drawStart, long end)
    {
        var toMs = 1000.0 / Stopwatch.Frequency;
        _ring[_next] = new FrameTiming(frame, (end - start) * toMs, (simEnd - simStart) * toMs, drawStart == 0 ? 0 : (end - drawStart) * toMs);
        _next = (_next + 1) % Capacity;
        Count = Math.Min(Count + 1, Capacity);
    }
}
