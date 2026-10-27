using System.Numerics;
using OneGame.Host;

namespace OneGame.Control;

public sealed partial class ControlSurface
{
    private Draw2DReport Draw2DOp(Args a)
    {
        var stage = Game.Stage as Render2DStage
            ?? throw new ControlException("no_draw2d", "This game's render stage is not the 2D renderer's. Use the default stage, or derive from Render2DStage.");
        var d = stage.Draw;
        var camera = d.Camera.Position;
        double[]? world = null;
        double[]? screen = null;
        if (a.Numbers("screen", 2) is { } s)
        {
            var w = d.ScreenToWorld(new Vector2((float)s[0], (float)s[1]));
            world = [Math.Round(w.X, 3) + 0.0, Math.Round(w.Y, 3) + 0.0];
        }

        if (a.Numbers("world", 2) is { } p)
        {
            var px = d.WorldToScreen(new Vector2((float)p[0], (float)p[1]));
            screen = [Math.Round(px.X, 3) + 0.0, Math.Round(px.Y, 3) + 0.0];
        }

        return new Draw2DReport(stage.Name, d.SpriteCount, d.TextCount, d.Tick, Math.Round(d.Time, 4), d.Width, d.Height,
            [Math.Round(camera.X, 3) + 0.0, Math.Round(camera.Y, 3) + 0.0], Math.Round(d.Camera.Zoom, 4), Math.Round(d.Camera.Rotation, 5), world, screen);
    }
}
