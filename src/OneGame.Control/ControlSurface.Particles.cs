using OneGame.Host;
using OneGame.Particles;

namespace OneGame.Control;

public sealed partial class ControlSurface
{
    private ParticlesReport ParticlesOp(Args a)
    {
        var stage = Game.Stage as Render2DStage
            ?? throw new ControlException("no_particles", "This game's render stage is not the 2D renderer's, so nothing shows particles. Use the default stage, or derive from Render2DStage.");
        var filter = a.Str("name");

        // Particles exist only in drawn frames. When none has been drawn at this tick (a headless run draws on capture), draw one offscreen at the window's size.
        if (!stage.Draw.HasDrawn || stage.Draw.Tick != Game.World.Tick)
        {
            Game.Capture(Math.Max(Game.Window.Width, 1), Math.Max(Game.Window.Height, 1), Game.UiScale);
        }

        var items = new List<ParticleEmitterInfo>();
        var live = 0;
        void Add(IReadOnlyList<ParticleEmitter> emitters)
        {
            for (var i = 0; i < emitters.Count; i++)
            {
                var e = emitters[i];
                if (filter is not null && e.Name != filter)
                {
                    continue;
                }

                live += e.Count;
                items.Add(new ParticleEmitterInfo(e.Name, e.Count, e.Capacity, e.Emitting, e.Spawned, e.Dropped,
                    e.Settings.Blend == ParticleBlend.Additive ? "additive" : "alpha", e.Settings.Space == ParticleSpace.Plane ? "plane" : "volume",
                    [Math.Round(e.Position.X, 3) + 0.0, Math.Round(e.Position.Y, 3) + 0.0, Math.Round(e.Position.Z, 3) + 0.0]));
            }
        }

        if ((stage as IScene3DStage)?.View is { } view)
        {
            Add(view.ParticleEmitters);
        }

        Add(stage.Draw.ParticleEmitters);
        return new ParticlesReport(stage.Name, items.Count, live, [.. items]);
    }
}
