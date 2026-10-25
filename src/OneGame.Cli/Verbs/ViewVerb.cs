using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using OneGame.Gltf;
using OneGame.Gpu;
using OneGame.Gpu.Vulkan;
using OneGame.Host;
using OneGame.Render3D;

namespace OneGame.Cli.Verbs;

/// <summary>Result of <c>onegame view</c>.</summary>
internal sealed record ViewResult(
    bool Ok, string Model, string File, int Width, int Height, string Device, string Sha256, int Objects, int DrawCalls, long Triangles, int Lights,
    string[] Passes, double Seconds);

/// <summary>
/// <c>onegame view &lt;model&gt;</c> renders a glTF model offscreen with the 3D renderer and saves a PNG, so a model can be looked at without a game around it.
/// </summary>
internal sealed class ViewVerb : IVerb
{
    private const string Usage = "onegame view <model.gltf|model.glb> [--out view.png] [--size 1280x720] [--yaw D] [--pitch D] [--camera N] [--env panorama.hdr] "
        + "[--env-yaw D] [--exposure X] [--tonemap neutral|aces|none] [--bloom X] [--samples N] [--animation NAME|INDEX] [--time S] [--light-dir x,y,z --light-lux X] [--no-ibl] [--background env|#rrggbb]";

    public string Name => "view";

    public string Summary => "Render a glTF model offscreen with the PBR renderer to a PNG: " + Usage;

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        string? file = null;
        var options = new Dictionary<string, string>();
        var flags = new HashSet<string>();
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--no-ibl")
            {
                flags.Add(args[i]);
            }
            else if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                if (i + 1 >= args.Length)
                {
                    throw new CliException(ExitCodes.Usage, "bad_args", $"{args[i]} needs a value. Usage: {Usage}");
                }

                options[args[i]] = args[++i];
            }
            else if (file is null)
            {
                file = args[i];
            }
            else
            {
                throw new CliException(ExitCodes.Usage, "bad_args", $"Unexpected argument '{args[i]}'. Usage: {Usage}");
            }
        }

        if (file is null)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "Missing the model file. Usage: " + Usage);
        }

        if (!File.Exists(file))
        {
            throw new CliException(ExitCodes.RuntimeError, "file_not_found", $"'{file}' does not exist.");
        }

        var clock = System.Diagnostics.Stopwatch.StartNew();
        var (width, height) = Resolution.Parse(options.GetValueOrDefault("--size", "1280x720"));
        var outPath = options.GetValueOrDefault("--out", "view.png");
        float Num(string key, float fallback) => options.TryGetValue(key, out var v) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)
            ? f
            : options.ContainsKey(key) ? throw new CliException(ExitCodes.Usage, "bad_args", $"{key} must be a number.") : fallback;

        GltfModel model;
        try
        {
            model = GltfLoader.LoadFile(file);
        }
        catch (GltfException ex)
        {
            throw new CliException(ExitCodes.Failed, "gltf_" + ex.Code, ex.Message);
        }

        HdrImage? panorama = null;
        if (options.TryGetValue("--env", out var envFile))
        {
            try
            {
                panorama = HdrImage.Decode(File.ReadAllBytes(envFile));
            }
            catch (Exception ex) when (ex is HdrException or IOException)
            {
                throw new CliException(ExitCodes.Failed, "env_unreadable", ex.Message);
            }
        }

        GpuDevice device;
        try
        {
            if (!VulkanGpu.IsAvailable)
            {
                throw new GpuUnsupportedException("No Vulkan loader library was found.");
            }

            device = VulkanGpu.CreateDevice(new GpuDeviceOptions { Validation = Environment.GetEnvironmentVariable("ONEGAME_GPU_VALIDATION") == "1", ApplicationName = "onegame view" });
        }
        catch (GpuException ex)
        {
            throw new CliException(ExitCodes.RuntimeError, "no_gpu", ex.Message);
        }

        using (device)
        {
            using var renderer = new Renderer3D(device);
            using var gpuModel = renderer.Upload(model);
            var scene = new Scene3D { Samples = (int)Num("--samples", 4) };
            var instance = scene.Add(gpuModel);
            if (options.TryGetValue("--animation", out var anim))
            {
                var index = int.TryParse(anim, NumberStyles.None, CultureInfo.InvariantCulture, out var n) ? n : Array.FindIndex(model.Animations, a => a.Name == anim);
                if (index < 0 || index >= model.Animations.Length)
                {
                    throw new CliException(ExitCodes.Usage, "bad_args", $"The model has no animation '{anim}' (it has {model.Animations.Length}).");
                }

                instance.Animate(index, Num("--time", 0f), loop: false);
            }

            var aspect = width / (float)height;
            if (options.ContainsKey("--camera"))
            {
                scene.Camera = Scene3D.CameraOf(instance, (int)Num("--camera", 0)) ?? throw new CliException(ExitCodes.Usage, "bad_args", "The model has no such camera.");
            }
            else
            {
                var bounds = model.ComputeWorldBounds() ?? throw new CliException(ExitCodes.Failed, "nothing_to_draw", "The model has nothing to draw.");
                scene.Camera = Camera3D.Frame(bounds.Min, bounds.Max, Num("--yaw", 25f), Num("--pitch", 12f), aspect);
            }

            scene.Post.Exposure = Num("--exposure", 1f);
            scene.Post.BloomStrength = Num("--bloom", 0.04f);
            scene.Post.Bloom = scene.Post.BloomStrength > 0f;
            scene.Post.ToneMapper = options.GetValueOrDefault("--tonemap", "neutral") switch
            {
                "neutral" => ToneMapper.PbrNeutral,
                "aces" => ToneMapper.Aces,
                "none" => ToneMapper.None,
                var t => throw new CliException(ExitCodes.Usage, "bad_args", $"Unknown tone mapper '{t}': use neutral, aces or none."),
            };
            scene.Environment.Yaw = Num("--env-yaw", 0f) * MathF.PI / 180f;
            if (flags.Contains("--no-ibl"))
            {
                scene.Environment.Intensity = 0f;
            }

            if (options.GetValueOrDefault("--background", "env") is { } bg && bg != "env")
            {
                if (bg.Length != 7 || bg[0] != '#' || !int.TryParse(bg.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
                {
                    throw new CliException(ExitCodes.Usage, "bad_args", "--background is 'env' or a colour like #303542.");
                }

                static float Lin(int v) => v / 255f <= 0.04045f ? v / 255f / 12.92f : MathF.Pow(((v / 255f) + 0.055f) / 1.055f, 2.4f);
                scene.Environment.Background = BackgroundMode.Color;
                scene.Environment.BackgroundColor = new Vector3(Lin((rgb >> 16) & 255), Lin((rgb >> 8) & 255), Lin(rgb & 255));
            }

            if (options.TryGetValue("--light-dir", out var dir))
            {
                var p = dir.Split(',');
                if (p.Length != 3 || !p.All(x => float.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
                {
                    throw new CliException(ExitCodes.Usage, "bad_args", "--light-dir is x,y,z.");
                }

                scene.Lights.Add(Light3D.Directional(new Vector3(float.Parse(p[0], CultureInfo.InvariantCulture), float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture)), Vector3.One, Num("--light-lux", 3f)));
            }

            scene.AddModelLights(instance);

            using var environment = panorama is null ? null : renderer.CreateEnvironment(panorama);
            scene.Environment.Map = environment;
            using var target = new Offscreen3D(renderer, width, height);
            var pixels = target.Capture(scene);
            var passes = target.Graph.Describe().Passes.Where(p => !p.Culled).Select(p => p.Name).ToArray();
            Png.Save(new CapturedFrame(width, height, pixels, device.Adapter.Name), outPath);
            var stats = renderer.LastStats;
            var result = new ViewResult(true, Path.GetFullPath(file), Path.GetFullPath(outPath), width, height, device.Adapter.Name, Convert.ToHexString(SHA256.HashData(pixels)).ToLowerInvariant(),
                stats.Objects, stats.DrawCalls, stats.Triangles, stats.Lights, passes, clock.Elapsed.TotalSeconds);
            if (json)
            {
                output.WriteLine(JsonSerializer.Serialize(result, CliJson.Default.ViewResult));
            }
            else
            {
                output.WriteLine($"Rendered {Path.GetFileName(file)} to {result.File} ({width}x{height} on {result.Device}): {stats.DrawCalls} draws, {stats.Triangles} triangles, {stats.Lights} lights, {passes.Length} passes.");
            }

            return ExitCodes.Ok;
        }
    }
}
