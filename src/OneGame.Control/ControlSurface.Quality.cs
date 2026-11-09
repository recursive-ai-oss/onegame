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

    private EnvironmentResult EnvironmentOp(Args a)
    {
        var stage = Game.Stage as Render3DStage
            ?? throw new ControlException("no_scene3d", "This game does not draw a 3D scene. Return a Render3DStage from GameDefinition.CreateRenderStage.");
        var scene = stage.Scene;
        var fog = scene.Fog;
        if (a.Has("fog"))
        {
            fog.Enabled = a.Bool("fog", false);
        }

        if (a.Numbers("fogColor", 3) is { } c)
        {
            if (c.Any(v => v < 0))
            {
                throw new ControlException("bad_args", "fogColor components must not be negative.");
            }

            fog.Color = new System.Numerics.Vector3((float)c[0], (float)c[1], (float)c[2]);
        }

        var start = a.Has("fogStart") ? (float)Math.Max(0, a.Double("fogStart", fog.Start)) : fog.Start;
        var end = a.Has("fogEnd") ? (float)a.Double("fogEnd", fog.End) : fog.End;
        if (!(end > start))
        {
            throw new ControlException("bad_args", "fogEnd must be above fogStart.");
        }

        fog.Start = start;
        fog.End = end;

        if (a.Has("fogMaxDensity"))
        {
            var d = a.Double("fogMaxDensity", 1);
            if (!(d >= 0 && d <= 1))
            {
                throw new ControlException("bad_args", "fogMaxDensity must be between 0 and 1.");
            }

            fog.MaxDensity = (float)d;
        }

        var env = scene.Environment;
        if (a.Has("blend"))
        {
            var b = a.Double("blend", 0);
            if (!(b >= 0 && b <= 1))
            {
                throw new ControlException("bad_args", "blend must be between 0 and 1.");
            }

            env.Blend = (float)b;
        }

        if (a.Has("intensity"))
        {
            env.Intensity = (float)Math.Max(0, a.Double("intensity", 1));
        }

        if (a.Has("yaw"))
        {
            env.Yaw = (float)a.Double("yaw", 0);
        }

        return new EnvironmentResult(fog.Enabled, [fog.Color.X, fog.Color.Y, fog.Color.Z], fog.Start, fog.End, fog.MaxDensity, env.NextMap is not null, env.Blend, env.Intensity, env.Yaw);
    }
}
