using OneGame.Host;

namespace OneGame.Control;

/// <summary>Result of the <c>overlay</c> operation.</summary>
/// <param name="Visible">Whether the debug panel is shown.</param>
/// <param name="Key">The toggle key, or null for none.</param>
/// <param name="Chord">The toggle buttons on a gamepad, or empty for none.</param>
/// <param name="Masked">Whether machine-dependent figures show as <c>--</c>.</param>
/// <param name="Lines">The panel's text now.</param>
public sealed record OverlayResult(bool Visible, string? Key, string[] Chord, bool Masked, string[] Lines)
{
    /// <summary>Gets the frame timing figures the panel shows, as numbers (never masked). Null when no frame has been timed.</summary>
    public TimingReport? Timing { get; init; }
}

/// <summary>One pass of the render graph and its GPU time.</summary>
/// <param name="Name">Pass name.</param>
/// <param name="Microseconds">GPU time in microseconds, from the newest finished frame.</param>
public sealed record GpuPassReport(string Name, double Microseconds);

/// <summary>
/// Frame timing over the last <see cref="FrameTimings.Capacity"/> frames. Timing runs while the debug panel is visible (<c>overlay</c> with
/// <c>visible=true</c>); it costs nothing otherwise.
/// </summary>
/// <param name="Samples">Frames in the history.</param>
/// <param name="Cpu">Newest frame, whole CPU time in milliseconds.</param>
/// <param name="CpuAvg">Average CPU frame time over the history.</param>
/// <param name="CpuMax">Longest CPU frame time over the history.</param>
/// <param name="Sim">Newest frame's time stepping the simulation, in milliseconds.</param>
/// <param name="SimAvg">Average simulation time over the history.</param>
/// <param name="SimMax">Longest simulation time over the history.</param>
/// <param name="Draw">Newest frame's time drawing and submitting, in milliseconds (0 when headless).</param>
/// <param name="Gpu"><c>ready</c> (pass times follow), <c>unsupported</c> (the backend has no timestamps: Metal, WebGPU) or <c>none</c> (no device or no frame drawn yet).</param>
/// <param name="GpuMs">Sum of the pass times in milliseconds; null unless <paramref name="Gpu"/> is <c>ready</c>.</param>
/// <param name="GpuPasses">GPU time per pass of the newest finished frame.</param>
/// <param name="CpuHistory">CPU frame times in milliseconds, oldest first.</param>
public sealed record TimingReport(int Samples, double Cpu, double CpuAvg, double CpuMax, double Sim, double SimAvg, double SimMax, double Draw, string Gpu, double? GpuMs, GpuPassReport[] GpuPasses, double[] CpuHistory);

public sealed partial class ControlSurface
{
    /// <summary>The debug panel of this game.</summary>
    public DebugOverlay Overlay { get; }

    private OverlayResult OverlayOp(Args a)
    {
        var o = Overlay;
        if (a.Has("visible"))
        {
            o.Visible = a.Bool("visible", false);
        }

        if (a.Bool("toggle", false))
        {
            o.Visible = !o.Visible;
        }

        if (a.Has("masked"))
        {
            o.Masked = a.Bool("masked", true);
        }

        if (a.Str("key") is { } key)
        {
            o.ToggleKey = key.Equals("none", StringComparison.OrdinalIgnoreCase)
                ? null
                : OneGame.Host.KeyNames.TryParse(key, out var k) ? k : throw new ControlException("bad_args", $"'{key}' is not a key name (F4, Backquote, none).");
        }

        if (a.Str("chord") is { } chord)
        {
            try
            {
                o.ToggleChord = DebugOverlay.ParseChord(chord);
            }
            catch (FormatException ex)
            {
                throw new ControlException("bad_args", ex.Message, ex);
            }
        }

        var lines = o.Refresh();
        return new OverlayResult(o.Visible, o.ToggleKey?.ToString(), [.. o.ToggleChord.Select(b => b.ToString())], o.IsMasked, [.. lines]) { Timing = TimingNow() };
    }

    private TimingReport? TimingNow()
    {
        var t = Game.Timings;
        if (!t.Enabled || t.Count == 0)
        {
            return null;
        }

        var frames = new FrameTiming[t.Count];
        t.CopyTo(frames);
        double cpuSum = 0, simSum = 0, cpuMax = 0, simMax = 0;
        var history = new double[frames.Length];
        for (var i = 0; i < frames.Length; i++)
        {
            cpuSum += frames[i].CpuMs;
            simSum += frames[i].SimMs;
            cpuMax = Math.Max(cpuMax, frames[i].CpuMs);
            simMax = Math.Max(simMax, frames[i].SimMs);
            history[i] = Math.Round(frames[i].CpuMs, 3);
        }

        var passes = new List<GpuPassTime>();
        var status = Game.ReadGpuTimings(passes);
        var gpu = status == GpuTimingStatus.Ready ? passes.Select(p => new GpuPassReport(p.Name, Math.Round(p.Microseconds, 1))).ToArray() : [];
        var last = t.Latest;
        return new TimingReport(frames.Length, Math.Round(last.CpuMs, 3), Math.Round(cpuSum / frames.Length, 3), Math.Round(cpuMax, 3), Math.Round(last.SimMs, 3), Math.Round(simSum / frames.Length, 3), Math.Round(simMax, 3), Math.Round(last.DrawMs, 3),
            status.ToString().ToLowerInvariant(), status == GpuTimingStatus.Ready ? Math.Round(passes.Sum(p => p.Microseconds) / 1000.0, 3) : null, gpu, history);
    }
}
