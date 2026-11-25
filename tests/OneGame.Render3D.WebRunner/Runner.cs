using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using OneGame.Gltf;
using RecursiveAI.OneGpu;
using RecursiveAI.OneGpu.WebGpu;

namespace OneGame.Render3D.Tests;

internal static class Paths
{
    // The page copies the files it needs into the in-memory file system under this folder.
    public static string RepoRoot => "/repo";
}

/// <summary>Called by the page: put files into the file system, then draw a case and compare it with its reference.</summary>
[SupportedOSPlatform("browser")]
public static partial class Runner
{
    private static GpuDevice? _device;

    /// <summary>Writes a file into the in-memory file system.</summary>
    [JSExport]
    public static void Put(string path, byte[] data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, data);
    }

    /// <summary>
    /// Makes an environment both ways (synchronously, and asynchronously over many frames on the page's one thread), draws the same scene with each
    /// and returns a JSON result with the largest per-channel difference, the number of frames the load took and the validation messages.
    /// </summary>
    [JSExport]
    public static async Task<string> RunEnvironmentLoad()
    {
        try
        {
            _device ??= await WebGpu.CreateDeviceAsync(new GpuDeviceOptions { ApplicationName = "render3d web runner" }).ConfigureAwait(true);
            using var renderer = new Renderer3D(_device);
            using var target = new Offscreen3D(renderer, 160, 100, GpuFormat.Rgba8UnormSrgb);
            var sky = HdrImage.CreateSky(256, 128, new Vector3(0.25f, 0.5f, 1.2f), new Vector3(0.9f, 0.95f, 1.1f), new Vector3(0.25f, 0.23f, 0.2f), new Vector3(0.3f, 1f, 0.2f), new Vector3(30f));
            var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 0, 4), Vector3.Zero, Vector3.UnitY), Samples = 1 };
            using var rig = new Rig(renderer);
            var sphere = new Shapes().Sphere(Vector3.Zero, 1.2f, "{\"pbrMetallicRoughness\":{\"baseColorFactor\":[0.95,0.9,0.8,1],\"metallicFactor\":1,\"roughnessFactor\":0.3}}", 32).Upload(rig);
            scene.Add(sphere);
            scene.Environment.Background = BackgroundMode.Environment;
            scene.Post.Bloom = false;
            using var sync = renderer.CreateEnvironment(sky, 64);
            renderer.PrepareEnvironment(sync);
            scene.Environment.Map = sync;
            var expected = await target.CaptureAsync(scene).ConfigureAwait(true);
            using var load = renderer.CreateEnvironmentAsync(sky, new EnvironmentLoadOptions { Size = 64, GpuBudget = 1_000_000, UploadBytesPerFrame = 20_000, ConvertPixelsPerFrame = 8192 });
            var frames = 0;
            while (load.IsRunning && frames < 1000)
            {
                await target.CaptureAsync(scene).ConfigureAwait(true);
                frames++;
            }

            using var map = load.Take();
            scene.Environment.Map = map;
            var actual = await target.CaptureAsync(scene).ConfigureAwait(true);
            await WebGpu.SettleAsync(_device).ConfigureAwait(true);
            var worst = 0;
            for (var i = 0; i < expected.Length; i++)
            {
                worst = Math.Max(worst, Math.Abs(expected[i] - actual[i]));
            }

            var messages = _device.TakeValidationMessages();
            return JsonSerializer.Serialize(new Dictionary<string, object?> { ["name"] = "envload", ["ok"] = worst <= 1 && frames > 8 && messages.Length == 0, ["maxDiff"] = worst, ["frames"] = frames, ["validation"] = messages }, RunnerJson.Options);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object?> { ["name"] = "envload", ["ok"] = false, ["error"] = ex.ToString() }, RunnerJson.Options);
        }
    }

    /// <summary>
    /// Draws the light shaft scene (a wall with three slits, the sun behind it) on WebGPU, shafts on and off, and compares the shaft frame with
    /// the software rasterizer's reference. Reports the largest differences, the brightness the shafts add and the validation messages.
    /// </summary>
    [JSExport]
    public static async Task<string> RunShafts()
    {
        try
        {
            _device ??= await WebGpu.CreateDeviceAsync(new GpuDeviceOptions { ApplicationName = "render3d web runner" }).ConfigureAwait(true);
            const int w = 320;
            const int h = 180;
            var (rw, rh, reference) = Png.Read("/ref/shafts_slit.png");
            if ((rw, rh) != (w, h))
            {
                throw new InvalidOperationException($"reference is {rw}x{rh}, expected {w}x{h}");
            }

            using var renderer = new Renderer3D(_device);
            using var target = new Offscreen3D(renderer, w, h, GpuFormat.Rgba8UnormSrgb);
            using var rig = new Rig(renderer);
            var scene = ShaftScene.Slit(ShaftScene.SlitShapes().Upload(rig));
            var on = await target.CaptureAsync(scene).ConfigureAwait(true);
            var ran = renderer.LastStats.Volumetric;
            scene.Volumetric.Enabled = false;
            var off = await target.CaptureAsync(scene).ConfigureAwait(true);
            await WebGpu.SettleAsync(_device).ConfigureAwait(true);
            long added = 0;
            for (var i = 0; i < on.Length; i += 4)
            {
                added += (on[i] - off[i]) + (on[i + 1] - off[i + 1]) + (on[i + 2] - off[i + 2]);
            }

            var diff = new byte[w * h * 4];
            var d = Compare.Measure(w, h, reference, on, 2, diff);
            var messages = _device.TakeValidationMessages();
            Directory.CreateDirectory("/out");
            Png.Write("/out/shafts.png", w, h, on);
            return JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["name"] = "shafts", ["ok"] = ran && added > w * h * 3 * 8 && d.MeanAbs <= 11 && d.P95 <= 45 && messages.Length == 0,
                ["ran"] = ran, ["meanAdded"] = Math.Round(added / (w * h * 3.0), 2), ["meanAbs"] = Math.Round(d.MeanAbs, 3), ["p95"] = d.P95,
                ["validation"] = messages, ["png"] = Convert.ToBase64String(File.ReadAllBytes("/out/shafts.png")),
            }, RunnerJson.Options);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object?> { ["name"] = "shafts", ["ok"] = false, ["error"] = ex.ToString() }, RunnerJson.Options);
        }
    }

    /// <summary>
    /// Draws one case of <c>cases.json</c> (the model is at <c>/models/&lt;model path&gt;</c>, the panorama at <c>/env/&lt;name&gt;_1k.hdr</c>) and returns
    /// a JSON result: the measured difference to the reference, the limits, and the captured frame as a PNG in base64.
    /// </summary>
    [JSExport]
    public static async Task<string> Run(string name)
    {
        try
        {
            _device ??= await WebGpu.CreateDeviceAsync(new GpuDeviceOptions { ApplicationName = "render3d web runner" }).ConfigureAwait(true);
            var (w, h, background, cases) = ReferenceCase.LoadAll();
            var c = cases.Single(x => x.Name == name);
            var (rw, rh, reference) = Png.Read($"/ref/{name}.png");
            if ((rw, rh) != (w, h))
            {
                throw new InvalidOperationException($"reference is {rw}x{rh}, expected {w}x{h}");
            }

            using var renderer = new Renderer3D(_device);
            using var target = new Offscreen3D(renderer, w, h, GpuFormat.Rgba8UnormSrgb);
            var model = renderer.Upload(GltfLoader.LoadFile("/models/" + c.Model));
            var scene = new Scene3D { Camera = c.Camera };
            var instance = scene.Add(model);
            if (c.PoseAnimation is { } anim)
            {
                instance.Animate(Array.FindIndex(model.Source.Animations, a => a.Name == anim), c.PoseTime, loop: false);
            }

            scene.Lights.AddRange(c.Lights);
            scene.Environment.Background = BackgroundMode.Color;
            scene.Environment.BackgroundColor = background;
            scene.Environment.Intensity = c.Ibl ? 1f : 0f;
            using var env = c.Env is null ? null : renderer.CreateEnvironment(HdrImage.Decode(File.ReadAllBytes($"/env/{c.Env}_1k.hdr")));
            if (env is not null)
            {
                scene.Environment.Map = env;
            }

            scene.Post.Bloom = false;
            var actual = await target.CaptureAsync(scene).ConfigureAwait(true);
            await WebGpu.SettleAsync(_device).ConfigureAwait(true);
            var diff = new byte[w * h * 4];
            var d = Compare.Measure(w, h, reference, actual, 2, diff);
            var (maxMean, maxP95) = c.Ibl ? (11.0, 45) : (6.0, 28);
            var messages = _device.TakeValidationMessages();
            Directory.CreateDirectory("/out");
            Png.Write("/out/actual.png", w, h, actual);
            Png.Write("/out/triptych.png", w * 3, h, Compare.Triptych(w, h, reference, actual, diff));
            model.Dispose();
            return JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["name"] = name, ["ok"] = d.Pixels > 0 && d.MeanAbs <= maxMean && d.P95 <= maxP95 && messages.Length == 0,
                ["pixels"] = d.Pixels, ["meanAbs"] = Math.Round(d.MeanAbs, 3), ["p95"] = d.P95, ["limitMeanAbs"] = maxMean, ["limitP95"] = maxP95,
                ["validation"] = messages, ["triptych"] = Convert.ToBase64String(File.ReadAllBytes("/out/triptych.png")),
            }, RunnerJson.Options);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object?> { ["name"] = name, ["ok"] = false, ["error"] = ex.ToString() }, RunnerJson.Options);
        }
    }
}

public static partial class Runner
{
    private static byte Srgb(double linear)
    {
        var c = Math.Clamp(linear, 0, 1);
        var e = c <= 0.0031308 ? c * 12.92 : (1.055 * Math.Pow(c, 1 / 2.4)) - 0.055;
        return (byte)Math.Round(e * 255);
    }

    /// <summary>
    /// Draws a flat-shaded quad under each fog mode and compares its centre pixel with the formula, then with the material opted out of fog.
    /// Returns a JSON result like <see cref="Run"/>.
    /// </summary>
    [JSExport]
    public static async Task<string> RunFog()
    {
        try
        {
            _device ??= await WebGpu.CreateDeviceAsync(new GpuDeviceOptions { ApplicationName = "render3d web runner" }).ConfigureAwait(true);
            using var renderer = new Renderer3D(_device);
            using var target = new Offscreen3D(renderer, 64, 64, GpuFormat.Rgba8UnormSrgb);
            var baseColour = new Vector3(0.8f, 0.2f, 0.1f);
            var fogColour = new Vector3(0.2f, 0.4f, 0.8f);
            var material = renderer.CreateMaterial("flat", baseColor: new Vector4(baseColour, 1f));
            material.Style = MaterialStyle3D.Flat;
            var n = Vector3.UnitZ;
            MeshVertex3D[] quad = [new(new Vector3(-5, -5, 0), n, default), new(new Vector3(5, -5, 0), n, default), new(new Vector3(5, 5, 0), n, default), new(new Vector3(-5, 5, 0), n, default)];
            using var mesh = renderer.CreateMesh(material, quad, [0, 1, 2, 0, 2, 3], "quad");
            var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 6, 10), Vector3.Zero, Vector3.UnitY), Samples = 1 };
            scene.Add(mesh.Model);
            scene.Environment.Intensity = 0f;
            scene.Environment.Background = BackgroundMode.Color;
            scene.Environment.BackgroundColor = Vector3.Zero;
            scene.Post.Bloom = false;
            scene.Post.ToneMapper = ToneMapper.None;
            scene.Fog.Enabled = true;
            scene.Fog.Color = fogColour;
            var length = Math.Sqrt((6 * 6) + (10 * 10));
            var rows = new List<Dictionary<string, object?>>();
            var allOk = true;

            async Task Check(string name, double amount)
            {
                var px = await target.CaptureAsync(scene).ConfigureAwait(true);
                var o = ((32 * 64) + 32) * 4;
                var expected = new[] { Srgb((baseColour.X * (1 - amount)) + (fogColour.X * amount)), Srgb((baseColour.Y * (1 - amount)) + (fogColour.Y * amount)), Srgb((baseColour.Z * (1 - amount)) + (fogColour.Z * amount)) };
                var diff = Math.Max(Math.Abs(px[o] - expected[0]), Math.Max(Math.Abs(px[o + 1] - expected[1]), Math.Abs(px[o + 2] - expected[2])));
                var ok = diff <= 3;
                allOk &= ok;
                rows.Add(new() { ["case"] = name, ["amount"] = Math.Round(amount, 4), ["got"] = new[] { px[o], px[o + 1], px[o + 2] }, ["expected"] = expected, ["ok"] = ok });
            }

            scene.Fog.Mode = FogMode.Linear;
            scene.Fog.Start = 4f;
            scene.Fog.End = 14f;
            await Check("linear", Math.Clamp((length - 4) / 10, 0, 1)).ConfigureAwait(true);
            scene.Fog.Mode = FogMode.Exponential;
            scene.Fog.Density = 0.1f;
            await Check("exp", 1 - Math.Exp(-0.1 * length)).ConfigureAwait(true);
            scene.Fog.Mode = FogMode.ExponentialSquared;
            scene.Fog.Density = 0.07f;
            await Check("exp2", 1 - Math.Exp(-Math.Pow(0.07 * length, 2))).ConfigureAwait(true);
            scene.Fog.Mode = FogMode.Height;
            scene.Fog.Density = 0.1f;
            scene.Fog.HeightBase = 0f;
            scene.Fog.HeightFalloff = 0.2f;
            double a = -0.2 * 6, b = 0, k = a - b;
            await Check("height", 1 - Math.Exp(-0.1 * length * ((Math.Exp(a) - Math.Exp(b)) / k))).ConfigureAwait(true);
            material.FogExempt = true;
            await Check("exempt", 0).ConfigureAwait(true);
            await WebGpu.SettleAsync(_device).ConfigureAwait(true);
            var messages = _device.TakeValidationMessages();
            return JsonSerializer.Serialize(new Dictionary<string, object?> { ["name"] = "fog", ["ok"] = allOk && messages.Length == 0, ["rows"] = rows, ["validation"] = messages }, RunnerJson.Options);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object?> { ["name"] = "fog", ["ok"] = false, ["error"] = ex.ToString() }, RunnerJson.Options);
        }
    }
}

/// <summary>Stands in for the native tests' rig where <see cref="Shapes"/> uploads a model.</summary>
internal sealed class Rig(Renderer3D renderer) : IDisposable
{
    private readonly List<IDisposable> _owned = [];

    public Renderer3D Renderer { get; } = renderer;

    public T Own<T>(T disposable)
        where T : IDisposable
    {
        _owned.Add(disposable);
        return disposable;
    }

    public void Dispose()
    {
        foreach (var d in _owned)
        {
            d.Dispose();
        }
    }
}

internal static class RunnerJson
{
    public static readonly JsonSerializerOptions Options = new() { TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver() };
}
