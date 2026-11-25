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
    /// <summary>
    /// Draws a layered material (four 2x2 layers, one quad each, flat style) and checks the centre of every texel against the layer's colour,
    /// with no WebGPU validation messages. Returns JSON with <c>ok</c>.
    /// </summary>
    [JSExport]
    public static async Task<string> RunLayers()
    {
        try
        {
            _device ??= await WebGpu.CreateDeviceAsync(new GpuDeviceOptions { ApplicationName = "render3d web runner" }).ConfigureAwait(true);
            const int cell = 16, quads = 4, w = quads * 2 * cell, h = 2 * cell;
            using var renderer = new Renderer3D(_device);
            using var target = new Offscreen3D(renderer, w, h, GpuFormat.Rgba8UnormSrgb);
            var layers = new ReadOnlyMemory<byte>[quads];
            for (var l = 0; l < quads; l++)
            {
                var px = new byte[16];
                for (var t = 0; t < 4; t++)
                {
                    px[t * 4] = (byte)(40 + (l * 50) + (t * 7));
                    px[(t * 4) + 1] = (byte)(220 - (l * 40) + (t * 5));
                    px[(t * 4) + 2] = (byte)(30 + (t * 60));
                    px[(t * 4) + 3] = 255;
                }

                layers[l] = px;
            }

            var material = renderer.CreateLayeredMaterial("blocks", 2, 2, layers);
            material.Style = MaterialStyle3D.Flat;
            var vertices = new List<MeshVertex3D>();
            var indices = new List<uint>();
            for (var q = 0; q < quads; q++)
            {
                var x = (q * 2) - 3f;
                var b = (uint)vertices.Count;
                vertices.Add(new MeshVertex3D(new Vector3(x - 1, 1, 0), Vector3.UnitZ, new Vector2(0, 0), Vector4.One, q));
                vertices.Add(new MeshVertex3D(new Vector3(x + 1, 1, 0), Vector3.UnitZ, new Vector2(2, 0), Vector4.One, q));
                vertices.Add(new MeshVertex3D(new Vector3(x + 1, -1, 0), Vector3.UnitZ, new Vector2(2, 2), Vector4.One, q));
                vertices.Add(new MeshVertex3D(new Vector3(x - 1, -1, 0), Vector3.UnitZ, new Vector2(0, 2), Vector4.One, q));
                indices.AddRange([b, b + 3, b + 2, b, b + 2, b + 1]);
            }

            using var mesh = renderer.CreateMesh(material, vertices.ToArray(), indices.ToArray(), "row");
            var scene = new Scene3D { Camera = new Camera3D { World = Matrix4x4.CreateTranslation(0, 0, 10), Orthographic = true, OrthoHeight = 2, Near = 0.1f, Far = 15f }, Samples = 1 };
            scene.Environment.Intensity = 0f;
            scene.Environment.Background = BackgroundMode.Color;
            scene.Environment.BackgroundColor = Vector3.Zero;
            scene.Post.Bloom = false;
            scene.Post.ToneMapper = ToneMapper.None;
            scene.Add(mesh.Model);
            var actual = await target.CaptureAsync(scene).ConfigureAwait(true);
            await WebGpu.SettleAsync(_device).ConfigureAwait(true);
            var worst = 0;
            for (var q = 0; q < quads; q++)
            {
                for (var cy = 0; cy < 4; cy++)
                {
                    for (var cx = 0; cx < 4; cx++)
                    {
                        var t = ((cy % 2) * 2) + (cx % 2);
                        var o = ((((cy * (cell / 2)) + (cell / 4)) * w) + (q * 2 * cell) + (cx * (cell / 2)) + (cell / 4)) * 4;
                        var l = layers[q].Span;
                        for (var c = 0; c < 3; c++)
                        {
                            worst = Math.Max(worst, Math.Abs(actual[o + c] - l[(t * 4) + c]));
                        }
                    }
                }
            }

            var messages = _device.TakeValidationMessages();
            return JsonSerializer.Serialize(new Dictionary<string, object?> { ["name"] = "layers", ["ok"] = worst <= 2 && messages.Length == 0, ["worstTexelError"] = worst, ["validation"] = messages }, RunnerJson.Options);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object?> { ["name"] = "layers", ["ok"] = false, ["error"] = ex.ToString() }, RunnerJson.Options);
        }
    }
}

internal static class RunnerJson
{
    public static readonly JsonSerializerOptions Options = new() { TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver() };
}
