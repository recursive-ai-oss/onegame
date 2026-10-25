using OneGame.Host;
using OneGame.Render3D;

namespace OneGame.Control;

public sealed partial class ControlSurface
{
    private QualityResult Quality(Args a)
    {
        var stage = Game.Stage as Render3DStage
            ?? throw new ControlException("no_scene3d", "This game does not draw a 3D scene. Return a Render3DStage from GameDefinition.CreateRenderStage.");
        var scene = stage.Scene;
        if (a.Str("level") is { } name)
        {
            if (!QualitySettings.TryParse(name, out var level))
            {
                throw new ControlException("bad_args", "level must be low, medium, high or ultra.");
            }

            scene.Quality = level;
        }

        if (a.Has("ssao"))
        {
            scene.Ssao.Enabled = a.Bool("ssao", false);
        }

        if (a.Has("ssaoRadius"))
        {
            var radius = a.Double("ssaoRadius", 0.5);
            if (!(radius > 0 && radius < 1000))
            {
                throw new ControlException("bad_args", "ssaoRadius must be above 0.");
            }

            scene.Ssao.Radius = (float)radius;
        }

        var q = QualitySettings.For(scene.Quality);
        var stats = stage.Renderer?.LastStats;
        var passes = stage.Graph?.Describe().Passes.Where(p => !p.Culled).Select(p => p.Name).ToArray() ?? [];
        return new QualityResult(
            q.Level.ToString().ToLowerInvariant(), ["low", "medium", "high", "ultra"], q.ShadowTileSize, q.MaxCascades, q.ShadowFilterRadius, q.MaxSamples, q.Bloom,
            q.EnvironmentSize, q.Ssao, q.SsaoSamples, scene.Ssao.Enabled, stats?.ShadowTiles ?? 0, stats?.ShadowLights ?? 0, stats?.Instances ?? 0, stats?.InstancesDrawn ?? 0, passes);
    }
}
