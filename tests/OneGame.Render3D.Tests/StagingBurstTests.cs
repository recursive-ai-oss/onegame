using System.Runtime.InteropServices;
using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

/// <summary>
/// A burst of mesh creation (a streamed world spawning chunks) must not leave upload memory reserved: staging is bounded by a constant, and
/// what a burst needed goes back once it has settled.
/// </summary>
public class StagingBurstTests
{
    private const int Meshes = 2500;

    // Vertex counts with a median near 370 and a tail to 3.7k, like streamed chunk meshes. Deterministic.
    private static int[] Sizes()
    {
        var rng = new Random(1234);
        var sizes = new int[Meshes];
        for (var i = 0; i < Meshes; i++)
        {
            var z = Math.Sqrt(-2 * Math.Log(1 - rng.NextDouble())) * Math.Cos(2 * Math.PI * rng.NextDouble());
            sizes[i] = Math.Clamp((int)Math.Round(372 * Math.Exp(0.9 * z)), 12, 3700);
        }

        return sizes;
    }

    private static (MeshVertex3D[] Vertices, uint[] Indices) Geometry(int vertices)
    {
        var v = new MeshVertex3D[vertices];
        for (var i = 0; i < vertices; i++)
        {
            v[i] = new MeshVertex3D(new Vector3(i % 16, (i / 16) % 16, i / 256), Vector3.UnitY, new Vector2(0, 0));
        }

        var idx = new uint[(vertices - 2) / 3 * 3 * 3];
        for (var i = 0; i < idx.Length; i++)
        {
            idx[i] = (uint)(i % vertices);
        }

        return (v, idx);
    }

    // Size in KiB of the driver's mappings (NVIDIA maps its memory through /dev/nvidia*), and the resident set. Linux only.
    private static (long MappedKiB, long RssKiB) ProcessMemory()
    {
        long mapped = 0;
        var inDriver = false;
        foreach (var line in File.ReadLines("/proc/self/smaps"))
        {
            if (line.Length > 0 && char.IsAsciiHexDigit(line[0]) && line.Contains('-') && line.Contains(' '))
            {
                inDriver = line.Contains("/dev/nvidia", StringComparison.Ordinal);
            }
            else if (inDriver && line.StartsWith("Size:", StringComparison.Ordinal))
            {
                mapped += long.Parse(line.AsSpan(5, line.Length - 8).Trim());
            }
        }

        long rss = 0;
        foreach (var line in File.ReadLines("/proc/self/status"))
        {
            if (line.StartsWith("VmRSS:", StringComparison.Ordinal))
            {
                rss = long.Parse(line.AsSpan(6, line.Length - 9).Trim());
            }
        }

        return (mapped, rss);
    }

    [Fact]
    public void ABurstOfMeshesKeepsStagingBoundedAndGivesItBack()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var validation = Environment.GetEnvironmentVariable("ONEGAME_BURST_NOVALIDATION") != "1";
        using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = validation, RequireValidation = validation });
        using var renderer = new Renderer3D(device);
        using var target = new Offscreen3D(renderer, 160, 120);
        var material = renderer.CreateMaterial("burst");
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(8, 8, 40), new Vector3(8, 8, 0), Vector3.UnitY), Samples = 1 };
        scene.Lights.Add(Light3D.Directional(new Vector3(0, -1, -1), Vector3.One, 2f));
        scene.Post.Bloom = false;

        for (var i = 0; i < 12; i++)
        {
            target.Render(scene);
        }

        device.WaitIdle();
        var baseStats = device.MemoryStats;
        var baseMem = ProcessMemory();

        var sizes = Sizes();
        var meshes = new List<DynamicMesh3D>(Meshes);
        long peakStaging = 0;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        foreach (var n in sizes)
        {
            var (v, idx) = Geometry(n);
            meshes.Add(renderer.CreateMesh(material, v, idx, "chunk"));
            peakStaging = Math.Max(peakStaging, (long)device.MemoryStats.StagingBytes);
        }

        foreach (var m in meshes)
        {
            scene.Add(m.Model);
        }

        var burstMs = stopwatch.Elapsed.TotalMilliseconds;
        target.Render(scene);
        device.WaitIdle();
        var peakMem = ProcessMemory();
        var burstStats = device.MemoryStats;
        peakStaging = Math.Max(peakStaging, (long)burstStats.StagingBytes);

        for (var i = 0; i < 90; i++)
        {
            target.Render(scene);
        }

        device.WaitIdle();
        for (var i = 0; i < 10; i++)
        {
            target.Render(scene);
        }

        device.WaitIdle();
        var settledStats = device.MemoryStats;
        var settledMem = ProcessMemory();
        Console.WriteLine($"BURST meshes={Meshes} burstMs={burstMs:F0} stagingPeak={peakStaging >> 20}MiB stagingBase={baseStats.StagingBytes >> 20}MiB stagingSettled={settledStats.StagingBytes >> 20}MiB " +
            $"reservedBase={baseStats.ReservedBytes >> 20}MiB reservedPeak={burstStats.ReservedBytes >> 20}MiB reservedSettled={settledStats.ReservedBytes >> 20}MiB " +
            $"mappedBase={baseMem.MappedKiB >> 10}MiB mappedPeak={peakMem.MappedKiB >> 10}MiB mappedSettled={settledMem.MappedKiB >> 10}MiB " +
            $"rssBase={baseMem.RssKiB >> 10}MiB rssPeak={peakMem.RssKiB >> 10}MiB rssSettled={settledMem.RssKiB >> 10}MiB");

        Assert.Equal(Meshes, renderer.LastStats.Objects);

        // Staging is bounded by a constant, whatever the burst: about 300 MB of mesh data passed through it.
        Assert.True(peakStaging <= (long)GpuDevice.StagingBudgetBytes + (8 << 20), $"staging peaked at {peakStaging >> 20} MiB");
        Assert.True(settledStats.StagingBytes <= baseStats.StagingBytes + (16UL << 20), $"staging settled at {settledStats.StagingBytes >> 20} MiB, base {baseStats.StagingBytes >> 20} MiB");

        foreach (var m in meshes)
        {
            m.Dispose();
        }

        scene.Models.Clear();
        for (var i = 0; i < 200; i++)
        {
            target.Render(scene);
        }

        device.WaitIdle();
        target.Render(scene);
        device.WaitIdle();
        var after = device.MemoryStats;
        Console.WriteLine($"BURST after dispose reserved={after.ReservedBytes >> 20}MiB (base {baseStats.ReservedBytes >> 20}) used={after.UsedBytes >> 20}MiB (base {baseStats.UsedBytes >> 20}) staging={after.StagingBytes >> 20}MiB mapped={ProcessMemory().MappedKiB >> 10}MiB");
        Assert.True(after.ReservedBytes <= baseStats.ReservedBytes + (80UL << 20), $"reserved {after.ReservedBytes >> 20} MiB after the meshes were freed, base {baseStats.ReservedBytes >> 20} MiB");
        if (validation)
        {
            Assert.Empty(device.TakeValidationMessages());
        }
    }
}
