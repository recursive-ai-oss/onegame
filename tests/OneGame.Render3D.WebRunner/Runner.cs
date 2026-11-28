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
    /// Draws twelve posed copies of the Fox once as separate animated models and once as one skinned instance batch (shadows on) and returns the
    /// largest per-channel difference, the draw counts and the validation messages.
    /// </summary>
    [JSExport]
    public static async Task<string> RunSkinnedInstances()
    {
        try
        {
            _device ??= await WebGpu.CreateDeviceAsync(new GpuDeviceOptions { ApplicationName = "render3d web runner" }).ConfigureAwait(true);
            using var renderer = new Renderer3D(_device);
            using var target = new Offscreen3D(renderer, 320, 200, GpuFormat.Rgba8UnormSrgb);
            using var rig = new Rig(renderer);
            var fox = rig.Own(renderer.Upload(GltfLoader.LoadFile("/models/Fox/glTF/Fox.gltf")));
            var ground = rig.Own(new Shapes().Ground(0f, 200f, Shapes.Matte(0.45f, 0.5f, 0.35f)).Upload(rig));

            Scene3D Stage()
            {
                var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 7, 14), new Vector3(0, 1.2f, -4.4f), Vector3.UnitY), Samples = 1 };
                scene.Camera.Far = 300f;
                scene.Add(ground);
                scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = Vector3.Normalize(new Vector3(-0.5f, -1f, -0.4f)), Intensity = 3f, CastShadows = true });
                scene.Shadows.MaxDistance = 80f;
                scene.Environment.Intensity = 0.6f;
                return scene;
            }

            var separate = Stage();
            var crowd = Stage();
            var batch = crowd.AddSkinnedInstanced(fox, 12);
            for (var i = 0; i < 12; i++)
            {
                var pose = fox.Source.CreateRestPose();
                var clip = fox.Source.Animations[1 + (i % 2)];
                clip.Apply(pose, (i * 0.137f) % clip.Duration);
                var place = Matrix4x4.CreateScale(0.04f) * Matrix4x4.CreateRotationY(i * 0.7f) * Matrix4x4.CreateTranslation(((i % 4) - 1.5f) * 4.5f, 0, -(i / 4) * 4.5f);
                var instance = separate.Add(fox, place);
                instance.Pose.CopyFrom(pose);
                instance.UpdateTransforms();
                batch.Add(place, pose);
            }

            var expected = await target.CaptureAsync(separate).ConfigureAwait(true);
            var separateDraws = renderer.LastStats.DrawCalls;
            var actual = await target.CaptureAsync(crowd).ConfigureAwait(true);
            var stats = renderer.LastStats;
            await WebGpu.SettleAsync(_device).ConfigureAwait(true);
            var worst = 0;
            for (var i = 0; i < expected.Length; i++)
            {
                worst = Math.Max(worst, Math.Abs(expected[i] - actual[i]));
            }

            var messages = _device.TakeValidationMessages();
            Directory.CreateDirectory("/out");
            Png.Write("/out/skinned.png", 320, 200, actual);
            return JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["name"] = "skinned", ["ok"] = worst <= 3 && stats.Instances == 12 && stats.DrawCalls < separateDraws && messages.Length == 0,
                ["maxDiff"] = worst, ["draws"] = stats.DrawCalls, ["separateDraws"] = separateDraws, ["validation"] = messages,
                ["png"] = Convert.ToBase64String(File.ReadAllBytes("/out/skinned.png")),
            }, RunnerJson.Options);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object?> { ["name"] = "skinned", ["ok"] = false, ["error"] = ex.ToString() }, RunnerJson.Options);
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
    /// <summary>
    /// A lit wall seen from close by, so the facing term falls from 1 at the middle to about 0.58 at the corners: the view-angle tint must leave
    /// the middle alone, turn the corners red past its edge, and leave the picture unchanged while its strength is 0. Returns a JSON result like <see cref="Run"/>.
    /// </summary>
    [JSExport]
    public static async Task<string> RunViewTint()
    {
        try
        {
            _device ??= await WebGpu.CreateDeviceAsync(new GpuDeviceOptions { ApplicationName = "render3d web runner" }).ConfigureAwait(true);
            using var renderer = new Renderer3D(_device);
            using var target = new Offscreen3D(renderer, 64, 64, GpuFormat.Rgba8UnormSrgb);
            var material = renderer.CreateMaterial("wall", baseColor: new Vector4(0.8f, 0.8f, 0.8f, 1f));
            var n = Vector3.UnitZ;
            MeshVertex3D[] quad = [new(new Vector3(-5, -5, 0), n, default), new(new Vector3(5, -5, 0), n, default), new(new Vector3(5, 5, 0), n, default), new(new Vector3(-5, 5, 0), n, default)];
            using var mesh = renderer.CreateMesh(material, quad, [0, 1, 2, 0, 2, 3], "quad");
            var camera = Camera3D.LookAt(new Vector3(0, 0, 3), Vector3.Zero, Vector3.UnitY);
            camera.YFov = MathF.PI / 2f;
            var scene = new Scene3D { Camera = camera, Samples = 1 };
            scene.Add(mesh.Model);
            scene.Environment.Intensity = 0f;
            scene.Environment.Background = BackgroundMode.Color;
            scene.Environment.BackgroundColor = Vector3.Zero;
            scene.Post.Bloom = false;
            scene.Post.ToneMapper = ToneMapper.None;
            scene.Lights.Add(Light3D.Directional(new Vector3(0, 0, -1), Vector3.One, 3f));
            var rows = new List<Dictionary<string, object?>>();
            var allOk = true;

            async Task<byte[]> Shot() => await target.CaptureAsync(scene).ConfigureAwait(true);
            static int[] At(byte[] px, int x, int y) { var o = ((y * 64) + x) * 4; return [px[o], px[o + 1], px[o + 2]]; }
            void Row(string name, bool ok, object got) { allOk &= ok; rows.Add(new() { ["case"] = name, ["got"] = got, ["ok"] = ok }); }

            var plain = await Shot().ConfigureAwait(true);
            material.ViewTintColor = new Vector3(1, 0, 0);
            material.ViewTintEdge = 0.66f;
            material.ViewTintSoftness = 0f;
            var unused = await Shot().ConfigureAwait(true);
            Row("strength 0 is identical", plain.AsSpan().SequenceEqual(unused), 0);
            material.ViewTintStrength = 1f;
            var tinted = await Shot().ConfigureAwait(true);
            Row("middle untouched", At(plain, 32, 32).AsSpan().SequenceEqual(At(tinted, 32, 32)), At(tinted, 32, 32));
            var corner = At(tinted, 4, 4);
            Row("corner red", corner[0] > 200 && corner[1] < 60 && corner[2] < 60, corner);
            var soft = At(plain, 4, 4);
            Row("corner was grey", soft[1] > 20, soft);
            await WebGpu.SettleAsync(_device).ConfigureAwait(true);
            var messages = _device.TakeValidationMessages();
            return JsonSerializer.Serialize(new Dictionary<string, object?> { ["name"] = "viewtint", ["ok"] = allOk && messages.Length == 0, ["rows"] = rows, ["validation"] = messages }, RunnerJson.Options);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object?> { ["name"] = "viewtint", ["ok"] = false, ["error"] = ex.ToString() }, RunnerJson.Options);
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
