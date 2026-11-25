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

internal static class RunnerJson
{
    public static readonly JsonSerializerOptions Options = new() { TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver() };
}
