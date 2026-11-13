using System.Numerics;
using OneGame.Core;
using RecursiveAI.OneGpu;
using RecursiveAI.OneGpu.Vulkan;
using OneGame.Host;
using OneGame.Render3D;
using TestPng = OneGame.Render3D.Tests.Png;
using Scene = OneGame.Render3D.Scene3D;

namespace OneGame.Scene3D.Tests;

/// <summary>Meshes added, replaced and freed at run time through <see cref="ModelLibrary"/> and the scene view.</summary>
public class DynamicMeshTests
{
    private const int W = 96;
    private const int H = 64;

    private sealed class Rig : IDisposable
    {
        private readonly Dictionary<string, (Matrix4x4 World, ModelOptions Options)> _shown = new(StringComparer.Ordinal);
        private readonly World _world = new(1, new EntityRegistry());

        public Rig(int width = W, int height = H)
        {
            Device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
            Renderer = new Renderer3D(Device);
            Target = new Offscreen3D(Renderer, width, height);
            View = new SceneView3D();
            View.Describer = Describe;
            View.CameraOverride = new ViewCamera(
                Matrix4x4.Invert(Matrix4x4.CreateLookAt(new Vector3(0, 6, 0.01f), Vector3.Zero, -Vector3.UnitZ), out var w) ? w : Matrix4x4.Identity,
                50f * MathF.PI / 180f, 0f, 0.1f, 100f, default);
            Scene.Environment.Intensity = 0f;
            Scene.Environment.Background = BackgroundMode.Color;
            Scene.Environment.BackgroundColor = new Vector3(0.05f, 0.1f, 0.2f);
            Scene.Post.Bloom = false;
            Scene.Samples = 1;
        }

        public GpuDevice Device { get; }

        public Renderer3D Renderer { get; }

        public Offscreen3D Target { get; }

        public SceneView3D View { get; }

        public Scene Scene { get; } = new();

        public ModelLibrary Models => View.Models;

        public void Show(string id, Vector3 at, ModelOptions? options = null) => _shown[id] = (Matrix4x4.CreateTranslation(at), options ?? new ModelOptions());

        public void Hide(string id) => _shown.Remove(id);

        private void Describe(World world, SceneWriter writer)
        {
            writer.DirectionalLight(new Vector3(0.2f, -1f, -0.3f), Vector3.One, 3f);
            foreach (var (id, s) in _shown)
            {
                writer.Model(id, s.World, s.Options);
            }
        }

        public void Frame()
        {
            View.Prepare(_world, 0, default);
            View.Build(Scene, Renderer);
            Target.Render(Scene);
        }

        public byte[] Capture()
        {
            View.Prepare(_world, 0, default);
            View.Build(Scene, Renderer);
            return Target.Capture(Scene);
        }

        public void Dispose()
        {
            Device.WaitIdle();
            View.Dispose();
            Target.Dispose();
            Renderer.Dispose();
            var messages = Device.TakeValidationMessages();
            Device.Dispose();
            Assert.True(messages.Length == 0, string.Join("\n", messages));
        }
    }

    // A flat grid of cells x cells quads at a height, facing up, one unit wide and centred on the origin (separate vertices per quad).
    private static (MeshVertex3D[] Vertices, uint[] Indices) Tile(int cells, float height, float half = 0.5f)
    {
        var vertices = new MeshVertex3D[cells * cells * 4];
        var indices = new uint[cells * cells * 6];
        var step = 2f * half / cells;
        for (var z = 0; z < cells; z++)
        {
            for (var x = 0; x < cells; x++)
            {
                var q = (z * cells) + x;
                var x0 = -half + (x * step);
                var z0 = -half + (z * step);
                var n = Vector3.UnitY;
                var v = q * 4;
                vertices[v] = new MeshVertex3D(new Vector3(x0, height, z0), n, new Vector2(0, 0));
                vertices[v + 1] = new MeshVertex3D(new Vector3(x0 + step, height, z0), n, new Vector2(1, 0));
                vertices[v + 2] = new MeshVertex3D(new Vector3(x0 + step, height, z0 + step), n, new Vector2(1, 1));
                vertices[v + 3] = new MeshVertex3D(new Vector3(x0, height, z0 + step), n, new Vector2(0, 1));
                var i = q * 6;
                indices[i] = (uint)v;
                indices[i + 1] = (uint)(v + 2);
                indices[i + 2] = (uint)(v + 1);
                indices[i + 3] = (uint)v;
                indices[i + 4] = (uint)(v + 3);
                indices[i + 5] = (uint)(v + 2);
            }
        }

        return (vertices, indices);
    }

    private static (int R, int G, int B) Px(byte[] rgba, int x, int y)
    {
        var i = ((y * W) + x) * 4;
        return (rgba[i], rgba[i + 1], rgba[i + 2]);
    }

    private static string RefPath(string name, [System.Runtime.CompilerServices.CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "testdata", "dynamicmesh", name));

    // Compares a capture with a committed PNG (written when ONEGAME_UPDATE_REFS names a directory); the tolerance is per channel.
    private static void MatchesReference(string name, byte[] rgba, int maxDiff, double meanDiff)
    {
        var path = RefPath(name);
        if (Environment.GetEnvironmentVariable("ONEGAME_UPDATE_REFS") is { Length: > 0 } dir)
        {
            Directory.CreateDirectory(dir);
            TestPng.Write(Path.Combine(dir, name), W, H, rgba);
        }

        var (w, h, expected) = TestPng.Read(path);
        Assert.Equal((W, H), (w, h));
        long sum = 0;
        var max = 0;
        for (var i = 0; i < rgba.Length; i++)
        {
            var d = Math.Abs(rgba[i] - expected[i]);
            sum += d;
            max = Math.Max(max, d);
        }

        var mean = (double)sum / rgba.Length;
        Assert.True(max <= maxDiff && mean <= meanDiff, $"{name}: max diff {max} (limit {maxDiff}), mean {mean:F3} (limit {meanDiff})");
    }

    [Fact]
    public void ReplacingAMeshShowsTheNewGeometryAndRemovingItHidesIt()
    {
        using var rig = new Rig();
        var red = rig.Renderer.CreateMaterial("red", baseColor: new Vector4(0.8f, 0.05f, 0.05f, 1));
        var green = rig.Renderer.CreateMaterial("green", baseColor: new Vector4(0.05f, 0.7f, 0.1f, 1));
        var (v, i) = Tile(2, 0f, 1.2f);
        rig.Models.SetMesh("left", red, v, i);
        rig.Models.SetMesh("right", green, v, i);
        rig.Show("left", new Vector3(-2.6f, 0, 0));
        rig.Show("right", new Vector3(2.6f, 0, 0));

        var before = rig.Capture();
        MatchesReference("dynamic-two.png", before, 4, 0.6);
        var (lx, ly, rx, ry) = (W / 2 - 24, H / 2, W / 2 + 24, H / 2);
        var left0 = Px(before, lx, ly);
        var right0 = Px(before, rx, ry);
        Assert.True(left0.R > 100 && left0.G < 60, $"left tile should be red, got {left0}");
        Assert.True(right0.G > 100 && right0.R < 60, $"right tile should be green, got {right0}");

        // Replace the left mesh with a much bigger one: it now covers the middle too.
        var (v2, i2) = Tile(6, 0f, 3.6f);
        rig.Models.SetMesh("left", red, v2, i2);
        var after = rig.Capture();
        MatchesReference("dynamic-replaced.png", after, 4, 0.6);
        var mid = Px(after, W / 2, H / 2);
        Assert.True(mid.R > 100 && mid.G < 60, $"the replacement should cover the middle, got {mid}");

        // Remove the right mesh: the background is back where it was.
        Assert.True(rig.Models.Remove("right"));
        rig.Hide("right");
        var removed = Px(rig.Capture(), W - 6, 6);
        Assert.True(removed.B > removed.G && removed.G > removed.R && removed.R < 40, $"background expected, got {removed}");
        Assert.False(rig.Models.Contains("right"));
        rig.Show("right", Vector3.Zero);
        Assert.Throws<UnknownModelException>(() => rig.Frame());
        rig.Hide("right");
    }

    [Fact]
    public void ALabelAndHalfOpacityReachTheInstanceAndTheFrame()
    {
        using var rig = new Rig();
        var white = rig.Renderer.CreateMaterial("white", baseColor: new Vector4(0.9f, 0.9f, 0.9f, 1));
        var (v, i) = Tile(2, 0f, 2f);
        rig.Models.SetMesh("island", white, v, i);
        rig.Show("island", Vector3.Zero, new ModelOptions { Opacity = 1f, Label = "island 3" });
        var solid = Px(rig.Capture(), W / 2, H / 2);
        Assert.Equal("island 3", rig.Scene.Models[0].Label);

        rig.Show("island", Vector3.Zero, new ModelOptions { Opacity = 0.5f, Label = "island 3" });
        var half = rig.Capture();
        Assert.Equal(0.5f, rig.Scene.Models[0].Opacity);
        var mix = Px(half, W / 2, H / 2);
        var back = Px(half, 2, 2);
        Assert.True(mix.R > back.R && mix.R < solid.R && mix.G > back.G && mix.G < solid.G, $"half opacity should sit between {back} and {solid}, got {mix}");
        MatchesReference("dynamic-half.png", half, 4, 0.6);

        // A fade back to solid restores the pooled instance completely, and an unlabelled draw clears the label.
        rig.Show("island", Vector3.Zero);
        var again = Px(rig.Capture(), W / 2, H / 2);
        Assert.Equal(solid, again);
        Assert.Null(rig.Scene.Models[0].Label);
    }

    [Fact]
    public void ABadMeshIsRefusedWithTheReason()
    {
        using var rig = new Rig();
        var m = rig.Renderer.CreateMaterial("m");
        var (v, i) = Tile(1, 0f);
        i[2] = 99;
        Assert.Contains("vertex count", Assert.Throws<ArgumentException>(() => rig.Models.SetMesh("bad", m, v, i)).Message);
        Assert.Contains("multiple of 3", Assert.Throws<ArgumentException>(() => rig.Models.SetMesh("bad", m, v, i.AsSpan(0, 4))).Message);
        Assert.False(rig.Models.Contains("bad"));

        // Empty geometry keeps the name and draws nothing.
        rig.Models.SetMesh("empty", m, default, default);
        rig.Show("empty", Vector3.Zero);
        rig.Frame();
        Assert.Empty(rig.Scene.Models);
    }

    [Fact]
    public void ThousandsOfSwapsKeepGpuMemoryFlatAndSteadyFramesAllocateNothing()
    {
        using var rig = new Rig(160, 90);
        var materials = new[]
        {
            rig.Renderer.CreateMaterial("opaque"),
            rig.Renderer.CreateMaterial("cutout", MaterialAlpha3D.Mask),
            rig.Renderer.CreateMaterial("water", MaterialAlpha3D.Blend, baseColor: new Vector4(0.2f, 0.4f, 0.9f, 0.6f)),
        };
        var rng = new Random(12345);
        const int Live = 1000;
        var ids = new string[Live];
        var isLive = new bool[Live];
        for (var k = 0; k < Live; k++)
        {
            ids[k] = "chunk " + k;
        }

        var liveCount = 0;

        void Churn()
        {
            var k = rng.Next(Live);
            if (isLive[k] && rng.Next(4) == 0)
            {
                rig.Models.Remove(ids[k]);
                rig.Hide(ids[k]);
                isLive[k] = false;
                liveCount--;
                return;
            }

            // 5..30 cells per side: 100 to 3,600 vertices, so buffers cross size classes now and then.
            var (v, i) = Tile(5 + rng.Next(26), 0f, 0.45f);
            rig.Models.SetMesh(ids[k], materials[k % 3], v, i);
            rig.Show(ids[k], new Vector3((k % 40) - 20, 0, (k / 40) - 12), new ModelOptions { Opacity = k % 7 == 0 ? 0.5f : 1f });
            if (!isLive[k])
            {
                isLive[k] = true;
                liveCount++;
            }
        }

        // Fill the world, then churn for a warm-up so pools and size classes have settled.
        for (var k = 0; k < Live; k++)
        {
            var (v, i) = Tile(5 + rng.Next(26), 0f, 0.45f);
            rig.Models.SetMesh(ids[k], materials[k % 3], v, i);
            rig.Show(ids[k], new Vector3((k % 40) - 20, 0, (k / 40) - 12), new ModelOptions { Opacity = k % 7 == 0 ? 0.5f : 1f });
            isLive[k] = true;
            liveCount++;
        }

        rig.Frame();
        for (var c = 0; c < 2000; c++)
        {
            Churn();
            if (c % 25 == 24)
            {
                rig.Frame();
            }
        }

        rig.Device.WaitIdle();
        rig.Frame();
        rig.Device.WaitIdle();
        var warm = rig.Device.MemoryStats;
        var peak = warm.ReservedBytes;
        var peakUsed = warm.UsedBytes;

        long churnAlloc = 0;
        for (var c = 0; c < 5000; c++)
        {
            var a0 = GC.GetAllocatedBytesForCurrentThread();
            Churn();
            churnAlloc += GC.GetAllocatedBytesForCurrentThread() - a0;
            if (c % 25 == 24)
            {
                rig.Frame();
                var now = rig.Device.MemoryStats;
                peak = Math.Max(peak, now.ReservedBytes);
                peakUsed = Math.Max(peakUsed, now.UsedBytes);
            }
        }

        rig.Device.WaitIdle();
        rig.Frame();
        rig.Device.WaitIdle();
        var end = rig.Device.MemoryStats;
        Console.WriteLine($"dynamic mesh soak: live {liveCount}; warm reserved {warm.ReservedBytes >> 20} MiB used {warm.UsedBytes >> 20} MiB allocs {warm.Allocations}; end reserved {end.ReservedBytes >> 20} MiB used {end.UsedBytes >> 20} MiB allocs {end.Allocations}; peak reserved {peak >> 20} MiB used {peakUsed >> 20} MiB; managed bytes per churn step {churnAlloc / 5000}");

        // Flat: the memory held from the driver may not grow past one more 64 MiB block, and what live meshes use stays within 25% of the warm level.
        Assert.True(end.ReservedBytes <= warm.ReservedBytes + (64UL << 20), $"reserved {warm.ReservedBytes} -> {end.ReservedBytes}");
        Assert.True(peak <= warm.ReservedBytes + (128UL << 20), $"peak reserved {peak} vs warm {warm.ReservedBytes}");
        Assert.True(end.UsedBytes <= warm.UsedBytes + (warm.UsedBytes / 4), $"used {warm.UsedBytes} -> {end.UsedBytes}");
        Assert.True(end.Allocations <= warm.Allocations + (warm.Allocations / 4), $"allocations {warm.Allocations} -> {end.Allocations}");

        // Steady frames: nothing changes, nothing is allocated.
        for (var f = 0; f < 5; f++)
        {
            rig.Frame();
        }

        rig.Device.WaitIdle();
        rig.Frame();
        long frames = 0;
        for (var f = 0; f < 30; f++)
        {
            var a0 = GC.GetAllocatedBytesForCurrentThread();
            rig.Frame();
            frames += GC.GetAllocatedBytesForCurrentThread() - a0;
        }

        Assert.True(frames == 0, $"30 steady frames allocated {frames} bytes");
    }
}
