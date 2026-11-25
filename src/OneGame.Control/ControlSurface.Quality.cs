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

    private RenderScaleResult RenderScaleOp(Args a)
    {
        var stage = Game.Stage as Render3DStage
            ?? throw new ControlException("no_scene3d", "This game does not draw a 3D scene. Return a Render3DStage from GameDefinition.CreateRenderStage.");
        if (a.Has("scale"))
        {
            var scale = a.Double("scale", 1);
            if (!(scale >= Renderer3D.MinRenderScale && scale <= 1))
            {
                throw new ControlException("bad_args", "scale must be between 0.25 and 1.");
            }

            if (stage.View is { } view)
            {
                view.RenderScale = (float)scale;
            }

            stage.Scene.RenderScale = (float)scale;
        }

        var stats = stage.Renderer?.LastStats;
        return new RenderScaleResult(stage.Scene.RenderScale, stats?.RenderWidth ?? 0, stats?.RenderHeight ?? 0);
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

        if (a.Str("fogMode") is { } mode)
        {
            fog.Mode = mode.ToLowerInvariant() switch
            {
                "linear" => FogMode.Linear,
                "exp" or "exponential" => FogMode.Exponential,
                "exp2" or "exponentialsquared" => FogMode.ExponentialSquared,
                "height" => FogMode.Height,
                _ => throw new ControlException("bad_args", "fogMode must be linear, exp, exp2 or height."),
            };
        }

        if (a.Has("fogDensity"))
        {
            var d = a.Double("fogDensity", fog.Density);
            if (!(d >= 0 && d < double.PositiveInfinity))
            {
                throw new ControlException("bad_args", "fogDensity must be zero or more.");
            }

            fog.Density = (float)d;
        }

        if (a.Has("fogHeightBase"))
        {
            fog.HeightBase = (float)a.Double("fogHeightBase", fog.HeightBase);
        }

        if (a.Has("fogHeightFalloff"))
        {
            fog.HeightFalloff = (float)Math.Max(0, a.Double("fogHeightFalloff", fog.HeightFalloff));
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

        EnvironmentLoadCommands(stage, a);
        var latest = stage.LatestEnvironmentLoad;
        var load = latest is null ? null : new EnvironmentLoadResult(latest.State.ToString().ToLowerInvariant(), Math.Round(latest.Progress, 3), stage.LatestEnvironmentSlot == EnvironmentSlot.Map ? "map" : "next", latest.Error?.Message);
        return new EnvironmentResult(fog.Enabled, [fog.Color.X, fog.Color.Y, fog.Color.Z], fog.Start, fog.End, fog.MaxDensity, env.NextMap is not null, env.Blend, env.Intensity, env.Yaw) { Load = load,
            FogMode = fog.Mode switch { FogMode.Exponential => "exp", FogMode.ExponentialSquared => "exp2", FogMode.Height => "height", _ => "linear" },
            FogDensity = fog.Density,
            FogHeightBase = fog.HeightBase,
            FogHeightFalloff = fog.HeightFalloff,
        };
    }

    private void EnvironmentLoadCommands(Render3DStage stage, Args a)
    {
        if (a.Bool("cancel", false))
        {
            stage.LatestEnvironmentLoad?.Cancel();
        }

        var path = a.Str("load");
        var sky = a.Has("sky") ? a.Int("sky", 2048) : 0;
        if (path is null && sky == 0)
        {
            return;
        }

        var renderer = stage.Renderer ?? throw new ControlException("not_ready", "The 3D renderer is created by the first frame; step a frame and try again.");
        var into = a.Str("into") ?? "next";
        if (into is not ("next" or "map"))
        {
            throw new ControlException("bad_args", "into must be next or map.");
        }

        HdrImage image;
        if (path is not null)
        {
            var full = ResolveOutput(path);
            var info = new FileInfo(full);
            if (!info.Exists)
            {
                throw new ControlException("not_found", $"No panorama file {full}.");
            }

            if (info.Length > 256L * 1024 * 1024)
            {
                throw new ControlException("too_large", "The panorama file is larger than 256 MiB.");
            }

            try
            {
                image = HdrImage.Decode(File.ReadAllBytes(full));
            }
            catch (HdrException e)
            {
                throw new ControlException("bad_args", e.Message);
            }
        }
        else
        {
            if (sky is < 64 or > 8192)
            {
                throw new ControlException("bad_args", "sky must be a width from 64 to 8192.");
            }

            image = HdrImage.CreateSky(sky, Math.Max(sky / 2, 1), new System.Numerics.Vector3(0.25f, 0.5f, 1.2f), new System.Numerics.Vector3(0.9f, 0.95f, 1.1f), new System.Numerics.Vector3(0.25f, 0.23f, 0.2f), new System.Numerics.Vector3(0.3f, 1f, 0.2f), new System.Numerics.Vector3(30f));
        }

        EnvironmentLoad load;
        try
        {
            load = renderer.CreateEnvironmentAsync(image, new EnvironmentLoadOptions { Size = a.Int("size", 0) });
        }
        catch (ArgumentException e)
        {
            throw new ControlException("bad_args", e.Message);
        }

        stage.AdoptEnvironment(load, into == "map" ? EnvironmentSlot.Map : EnvironmentSlot.NextMap);
    }
}
