using System;
using System.Globalization;
using System.Linq;
using OneGame.Host;
using OneGame.Physics;

namespace PhysicsPourExample;

/// <summary>
/// `--bench` (with `--dots` to draw one dot per particle instead of the surface): lets the liquid settle for 60 frames, opens the gate, times the next 360 frames and prints one line (step and frame times in
/// milliseconds, frames per second over the window), then quits. The frame time includes the wait for the display when the device paces
/// frames, so read it together with the frame rate: a game that holds 60 fps and a frame p95 near 16.7 ms fits the budget.
/// </summary>
internal sealed class Bench
{
    private const int Settle = 60;
    private const int Window = 360;
    private readonly double[] _sim = new double[Window];
    private readonly double[] _cpu = new double[Window];
    private readonly double[] _draw = new double[Window];
    private int _frame;
    private long _startTicks;

    public void OnFrame(GameInstance game)
    {
        if (_frame == 0)
        {
            game.Timings.Enabled = true;
            if (game.Args.Has("dots"))
            {
                game.World.Entities.OfType<Jug>().Single().Send.ToggleDots();
            }
        }

        if (_frame == Settle)
        {
            game.World.Entities.OfType<Jug>().Single().Send.Pour();
            _startTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        }

        if (_frame > Settle && _frame <= Settle + Window)
        {
            // The newest timing is the previous frame's.
            var t = game.Timings.Latest;
            var i = _frame - Settle - 1;
            _sim[i] = t.SimMs;
            _cpu[i] = t.CpuMs;
            _draw[i] = t.DrawMs;
        }

        if (_frame == Settle + Window)
        {
            var seconds = (System.Diagnostics.Stopwatch.GetTimestamp() - _startTicks) / (double)System.Diagnostics.Stopwatch.Frequency;
            var space = PhysicsSpace2D.Of(game.World)!;
            var line = string.Create(CultureInfo.InvariantCulture,
                $"BENCH particles={space.FluidParticleCount} frames={Window} fps={Window / seconds:F1} sim_p50={P(_sim, 0.5):F2} sim_p95={P(_sim, 0.95):F2} frame_p50={P(_cpu, 0.5):F2} frame_p95={P(_cpu, 0.95):F2} draw_p50={P(_draw, 0.5):F2} draw_p95={P(_draw, 0.95):F2}");
            Console.WriteLine(line);
            Console.Error.WriteLine(line);
            game.Platform.RequestQuit();
        }

        _frame++;
    }

    private static double P(double[] values, double q)
    {
        var sorted = (double[])values.Clone();
        Array.Sort(sorted);
        return sorted[(int)Math.Min(sorted.Length - 1, sorted.Length * q)];
    }
}
