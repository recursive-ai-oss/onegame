using System.Diagnostics;
using System.Numerics;
using OneGame.Core;
using RecursiveAI.OneGpu;
using RecursiveAI.OneGpu.Vulkan;
using OneGame.Host;
using OneGame.Render3D;
using Scene = OneGame.Render3D.Scene3D;

namespace OneGame.Scene3D.Tests;

/// <summary>
/// A textured, vertex-coloured material shared by many meshes (always run), and an indicative load measurement for streamed content
/// (set ONEGAME_MESH_LOAD=1; it prints timings and is meant for a quiet machine with the validation layer off).
/// </summary>
public class MeshLoadTests
{
    private const int W = 64;
    private const int H = 64;

    private static (MeshVertex3D[], uint[]) Quad(Vector3 at, Vector4 color)
    {
        var n = Vector3.UnitY;
        return ([new(at + new Vector3(-1, 0, -1), n, new(0, 0), color), new(at + new Vector3(1, 0, -1), n, new(1, 0), color), new(at + new Vector3(1, 0, 1), n, new(1, 1), color), new(at + new Vector3(-1, 0, 1), n, new(0, 1), color)], [0, 2, 1, 0, 3, 2]);
    }

    [Fact]
    public void ATextureOnASharedMaterialAndVertexColourBothMultiplyTheBaseColour()
    {
        using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        var renderer = new Renderer3D(device);
        var target = new Offscreen3D(renderer, W, H);
        var view = new SceneView3D();
        // A 2x2 texture, all white except nothing: solid (0.5, 1, 1) in sRGB-encoded bytes, so only red is dimmed.
        using var texture = device.CreateTexture(new TextureDesc(2, 2, GpuFormat.Rgba8UnormSrgb, TextureUsage.Sampled | TextureUsage.CopyDestination));
        byte[] texels = [188, 255, 255, 255, 188, 255, 255, 255, 188, 255, 255, 255, 188, 255, 255, 255];
        device.WriteTexture(texture, 0, 0, texels);
        var shared = renderer.CreateMaterial("atlas");
        shared.SetTexture(MaterialSlot3D.BaseColor, texture);
        shared.Emissive = Vector3.Zero;
        var white = Quad(new Vector3(-3, 0, 0), Vector4.One);
        var tinted = Quad(new Vector3(3, 0, 0), new Vector4(1, 0.25f, 1, 1));
        view.Models.SetMesh("a", shared, white.Item1, white.Item2);
        view.Models.SetMesh("b", shared, tinted.Item1, tinted.Item2);
        view.CameraOverride = new ViewCamera(Matrix4x4.Invert(Matrix4x4.CreateLookAt(new Vector3(0, 9, 0.01f), Vector3.Zero, -Vector3.UnitZ), out var w) ? w : Matrix4x4.Identity, 50f * MathF.PI / 180f, 0f, 0.1f, 100f, default);
        view.Describer = (_, wr) =>
        {
            wr.DirectionalLight(new Vector3(0, -1, 0), Vector3.One, 3f);
            wr.Model("a", Matrix4x4.Identity, new ModelOptions());
            wr.Model("b", Matrix4x4.Identity, new ModelOptions());
        };
        var scene = new Scene();
        scene.Samples = 1;
        scene.Environment.Intensity = 0f;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Vector3.Zero;
        view.Prepare(new World(1, new EntityRegistry()), 0, default);
        view.Build(scene, renderer);
        var px = target.Capture(scene);
        (int R, int G, int B) At(int x, int y) => (px[((y * W) + x) * 4], px[((y * W) + x) * 4 + 1], px[((y * W) + x) * 4 + 2]);
        var left = At(W / 2 - 23, H / 2);
        var right = At(W / 2 + 23, H / 2);

        // Texture (red dimmed) shows on the left; vertex colour (green x0.25) multiplies it on the right.
        Assert.True(left.G > 120 && left.R < left.G - 20 && left.B > 120, $"textured {left}");
        Assert.True(right.G < left.G - 40 && Math.Abs(right.R - left.R) <= 2, $"vertex colour should dim green: {right} vs {left}");
        // Update on a live mesh changes what the next frame shows.
        var moved = Quad(new Vector3(-3, 0, 0), new Vector4(0, 0, 1, 1));
        view.Models.SetMesh("a", shared, moved.Item1, moved.Item2);
        view.Prepare(new World(1, new EntityRegistry()), 0, default);
        view.Build(scene, renderer);
        var after = At(W / 2 - 23, H / 2);
        px = target.Capture(scene);
        after = At(W / 2 - 23, H / 2);
        Assert.True(after.B > 100 && after.G < 40, $"updated mesh should be blue: {after}");
        device.WaitIdle();
        view.Dispose();
        target.Dispose();
        renderer.Dispose();
        var messages = device.TakeValidationMessages();
        Assert.True(messages.Length == 0, string.Join("\n", messages));
    }

    private static int Vertices(Random r)
    {
        var p = r.NextDouble();
        return p < 0.5 ? 80 + r.Next(300) : p < 0.95 ? 380 + r.Next(880) : 1260 + r.Next(2440);
    }

    private static (MeshVertex3D[], uint[]) Chunk(Random r, int vertexCount, Vector4 colour)
    {
        var quads = Math.Max(vertexCount / 4, 1);
        var v = new MeshVertex3D[quads * 4];
        var ix = new uint[quads * 6];
        for (var q = 0; q < quads; q++)
        {
            var c = new Vector3((float)r.NextDouble() * 3f, (float)r.NextDouble() * 3f, (float)r.NextDouble() * 3f);
            var n = Vector3.UnitY;
            v[q * 4] = new(c, n, new(0, 0), colour);
            v[(q * 4) + 1] = new(c + new Vector3(0.3f, 0, 0), n, new(1, 0), colour);
            v[(q * 4) + 2] = new(c + new Vector3(0.3f, 0, 0.3f), n, new(1, 1), colour);
            v[(q * 4) + 3] = new(c + new Vector3(0, 0, 0.3f), n, new(0, 1), colour);
            var b = (uint)(q * 4);
            ix[q * 6] = b;
            ix[(q * 6) + 1] = b + 2;
            ix[(q * 6) + 2] = b + 1;
            ix[(q * 6) + 3] = b;
            ix[(q * 6) + 4] = b + 3;
            ix[(q * 6) + 5] = b + 2;
        }

        return (v, ix);
    }

    private static string Pct(List<double> ms)
    {
        ms.Sort();
        return $"p50 {ms[ms.Count / 2]:F2} p99 {ms[(int)(ms.Count * 0.99)]:F2} max {ms[^1]:F2}";
    }

    [Fact]
    public void IndicativeLoadAtStreamedWorldNumbers()
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_MESH_LOAD") is not { Length: > 0 })
        {
            return;
        }

        var seconds = int.TryParse(Environment.GetEnvironmentVariable("ONEGAME_MESH_SECONDS"), out var s) ? s : 120;
        using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = false });
        var renderer = new Renderer3D(device);
        var target = new Offscreen3D(renderer, 1280, 720);
        var view = new SceneView3D();
        using var atlas = device.CreateTexture(new TextureDesc(256, 256, GpuFormat.Rgba8UnormSrgb, TextureUsage.Sampled | TextureUsage.CopyDestination));
        var texels = new byte[256 * 256 * 4];
        for (var i = 0; i < texels.Length; i += 4)
        {
            texels[i] = texels[i + 1] = texels[i + 2] = (byte)(((i / 4 % 256 / 16) + (i / 4 / 256 / 16)) % 2 == 0 ? 220 : 120);
            texels[i + 3] = 255;
        }

        device.WriteTexture(atlas, 0, 0, texels);
        var material = renderer.CreateMaterial("atlas");
        material.SetTexture(MaterialSlot3D.BaseColor, atlas);
        var rng = new Random(7);
        var live = new List<(string Id, Matrix4x4 At)>();
        var next = 0;
        void Add()
        {
            var id = "c" + next++;
            var k = next;
            var (v, i) = Chunk(rng, Vertices(rng), new Vector4(1, 1, 1, 1));
            view.Models.SetMesh(id, material, v, i);
            live.Add((id, Matrix4x4.CreateTranslation(((k % 80) - 40) * 3.2f, 0, (((k / 80) % 80) - 40) * 3.2f)));
        }

        view.CameraOverride = new ViewCamera(Matrix4x4.Invert(Matrix4x4.CreateLookAt(new Vector3(0, 40, 70), new Vector3(0, 0, 0), Vector3.UnitY), out var cw) ? cw : Matrix4x4.Identity, 60f * MathF.PI / 180f, 0f, 0.1f, 400f, default);
        view.Describer = (_, w) =>
        {
            w.DirectionalLight(new Vector3(0.3f, -1, -0.4f), Vector3.One, 3f);
            for (var i = 0; i < live.Count; i++)
            {
                w.Model(live[i].Id, live[i].At);
            }
        };
        var scene = new Scene();
        scene.Samples = 1;
        var world = new World(1, new EntityRegistry());
        var sw = new Stopwatch();

        // One frame; returns (prepare, build, render) in ms.
        (double, double, double) Frame()
        {
            sw.Restart();
            view.Prepare(world, 0, default);
            var a = sw.Elapsed.TotalMilliseconds;
            view.Build(scene, renderer);
            var b = sw.Elapsed.TotalMilliseconds;
            target.Render(scene);
            return (a, b - a, sw.Elapsed.TotalMilliseconds - b);
        }

        void Phase(string name, int liveTarget, bool swaps)
        {
            while (live.Count < liveTarget)
            {
                Add();
                if (live.Count % 200 == 0)
                {
                    Frame();
                }
            }

            device.WaitIdle();
            Frame();
            device.WaitIdle();
            for (var f = 0; f < 60; f++)
            {
                Frame();
            }

            var total = new List<double>();
            var prep = new List<double>();
            var build = new List<double>();
            var render = new List<double>();
            var set = new List<double>();
            var buildNo = new List<double>();
            var mem0 = device.MemoryStats;
            ulong memMin = ulong.MaxValue, memMax = 0;
            var end = Stopwatch.GetTimestamp() + (swaps ? seconds : 10) * Stopwatch.Frequency;
            var nextSample = 0L;
            long alloc0 = GC.GetAllocatedBytesForCurrentThread();
            while (Stopwatch.GetTimestamp() < end)
            {
                var t0 = Stopwatch.GetTimestamp();
                var setMs = 0.0;
                if (swaps)
                {
                    for (var k = 0; k < 80; k++)
                    {
                        var s0 = Stopwatch.GetTimestamp();
                        if (k < 40)
                        {
                            var e = live[rng.Next(live.Count)];
                            var (v, i) = Chunk(rng, Vertices(rng), new Vector4(1, 1, 1, 1));
                            view.Models.SetMesh(e.Id, material, v, i);
                        }
                        else if (k < 60)
                        {
                            var at = rng.Next(live.Count);
                            view.Models.Remove(live[at].Id);
                            live[at] = live[^1];
                            live.RemoveAt(live.Count - 1);
                        }
                        else
                        {
                            Add();
                        }

                        setMs += (Stopwatch.GetTimestamp() - s0) * 1000.0 / Stopwatch.Frequency;
                    }
                }

                var (a, b, c) = Frame();
                total.Add((Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency - setMs);
                prep.Add(a);
                build.Add(b);
                render.Add(c);
                set.Add(setMs);
                var now = Stopwatch.GetTimestamp();
                if (now >= nextSample)
                {
                    nextSample = now + Stopwatch.Frequency;
                    var m = device.MemoryStats.ReservedBytes;
                    memMin = Math.Min(memMin, m);
                    memMax = Math.Max(memMax, m);
                }
            }

            var allocPerFrame = (GC.GetAllocatedBytesForCurrentThread() - alloc0) / total.Count;
            Console.WriteLine($"LOAD {name}: live {live.Count}, frames {total.Count}, swaps {(swaps ? "80/frame" : "none")}");
            Console.WriteLine($"  frame (prepare+build+render, excl. SetMesh) ms: {Pct(total)}");
            Console.WriteLine($"  prepare {Pct(prep)}; build {Pct(build)}; render/submit {Pct(render)}");
            Console.WriteLine($"  SetMesh x80 (copy into pool, incl. test's own mesh generation) ms: {Pct(set)}");
            Console.WriteLine($"  reserved MiB at start {mem0.ReservedBytes >> 20}, over run min {memMin >> 20} max {memMax >> 20}; used now {device.MemoryStats.UsedBytes >> 20}; managed B/frame {allocPerFrame}");
            _ = buildNo;
        }

        Phase("4100 steady", 4100, swaps: false);
        Phase("4100 + 80 swaps/frame", 4100, swaps: true);
        Phase("6000 steady", 6000, swaps: false);
        Phase("6000 + 80 swaps/frame", 6000, swaps: true);
        var stats = renderer.LastStats;
        Console.WriteLine($"LOAD last frame stats: {stats}");
        device.WaitIdle();
        view.Dispose();
        target.Dispose();
        renderer.Dispose();
    }
}
