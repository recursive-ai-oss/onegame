using OneGame.Tweens;

namespace OneGame.Control;

/// <summary>Result of the <c>tweens</c> operation.</summary>
/// <param name="Time">Seconds of view time the tween clock has advanced.</param>
/// <param name="Paused">Whether the whole set is paused.</param>
/// <param name="TimeScale">The clock's speed factor.</param>
/// <param name="Active">How many top-level tweens are running or waiting.</param>
/// <param name="Tweens">The running tweens, oldest first.</param>
public sealed record TweensResult(double Time, bool Paused, float TimeScale, int Active, TweenInfo[] Tweens);

public sealed partial class ControlSurface
{
    private TweensResult TweensOp(Args a)
    {
        var set = Game.Tweens;
        if (a.Has("paused"))
        {
            set.Paused = a.Bool("paused", false);
        }

        if (a.Has("time_scale"))
        {
            var scale = a.Double("time_scale", 1);
            if (!(scale >= 0 && scale <= 100))
            {
                throw new ControlException("bad_args", "time_scale must be between 0 and 100.");
            }

            set.TimeScale = (float)scale;
        }

        if (a.Bool("cancel_all", false))
        {
            set.CancelAll();
        }

        var advance = a.Double("advance", 0);
        if (!(advance >= 0 && advance <= 600))
        {
            throw new ControlException("bad_args", "advance must be between 0 and 600 seconds.");
        }

        if (a.Has("advance"))
        {
            Game.AdvanceView(advance);
        }

        if (a.Bool("settle", false))
        {
            // Everything that ends ends within the longest finite tween's remaining time; endless ones stay.
            var list = new List<TweenInfo>();
            set.Snapshot(list);
            var remaining = 0.0;
            foreach (var t in list)
            {
                if (t.Duration >= 0)
                {
                    remaining = Math.Max(remaining, t.Duration - t.Elapsed);
                }
            }

            Game.AdvanceView(Math.Min(600, remaining / Math.Max(set.TimeScale, 0.01) + 0.001));
        }
        else
        {
            Game.Ui.Update();
        }

        var infos = new List<TweenInfo>();
        set.Snapshot(infos);
        return new TweensResult(set.Time, set.Paused, set.TimeScale, set.ActiveCount, [.. infos]);
    }
}
