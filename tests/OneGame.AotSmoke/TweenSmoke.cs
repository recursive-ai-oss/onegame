using System.Numerics;
using OneGame.Tweens;
using OneGame.UI;

namespace OneGame.AotSmoke;

/// <summary>Runs tweens, every easing family and a style transition, so the native publish compiles the tween path.</summary>
internal static class TweenSmoke
{
    public static string? Run()
    {
        var set = new TweenSet();
        var v = 0f;
        var vec = Vector2.Zero;
        var color = Color.Black;
        var done = 0;
        var seq = set.Sequence(
            set.Number(0, 10, 0.5f, x => v = x).Ease(Easing.OutBack),
            set.Parallel(set.Vector2(Vector2.Zero, Vector2.One, 0.25f, x => vec = x), set.Color(Color.White, Color.Black, 0.25f, c => color = c)),
            set.Call(() => done++));
        for (var i = 0; i < 40; i++)
        {
            set.Advance(0.025);
        }

        if (seq.IsActive || done != 1 || v != 10f || vec != Vector2.One || color != Color.Black)
        {
            return $"sequence ended wrongly: done={done} v={v} vec={vec} color={color}";
        }

        var sum = 0f;
        EasingMode[] modes = [EasingMode.In, EasingMode.Out, EasingMode.InOut];
        for (var f = EasingFamily.Sine; f <= EasingFamily.Bounce; f++)
        {
            foreach (var m in modes)
            {
                sum += Easing.Of(f, m).Evaluate(0.3f);
            }
        }

        if (!Easing.TryParse("cubic-bezier(0.2, 0, 0, 1)", out var bezier) || Math.Abs(bezier.Evaluate(0.5f) - Easing.CubicBezier(0.2f, 0, 0, 1).Evaluate(0.5f)) > 1e-6f || !Easing.TryParse("steps(3, jump-both)", out _) || sum == 0)
        {
            return "easing parse or evaluation failed";
        }

        var ui = new UiTree();
        ui.AddStyleSheet(new StyleSheet()
            .Rule(".b", s => s.Size(100, 40).Opacity(1).Background(Color.Black).Transition("opacity 200ms ease-out, background-color 200ms"))
            .Rule(".on", s => s.Opacity(0.5f).Background(Color.White)));
        var box = new Element("Box").Class("b");
        ui.Root.Add(box);
        ui.Frame();
        box.SetClass("on", true);
        ui.Frame();
        ui.Tweens.Advance(0.1);
        ui.Frame();
        var mid = box.Computed.Get("opacity");
        ui.Tweens.Advance(0.2);
        ui.Frame();
        return mid is null or "1" or "0.5" || box.Computed.Get("opacity") != "0.5" || ui.Tweens.ActiveCount != 0 ? $"transition did not run: mid={mid}" : null;
    }
}
