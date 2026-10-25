using System.Runtime.CompilerServices;
using OneGame.Gltf;
using OneGame.Gpu;
using OneGame.Gpu.Vulkan;

// Each test makes its own GPU device; real drivers are not happy with several being created and destroyed at once.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace OneGame.Render3D.Tests;

internal static class Paths
{
    private static string SourceDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    public static string RepoRoot => Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));

    public static string Reference(string name) => Path.Combine(RepoRoot, "testdata", "render3d", name);

    /// <summary>A sample model: from the folder named by ONEGAME_GLTF_SAMPLES, else from the small committed set. Null when neither has it.</summary>
    public static string? Model(string relative)
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_GLTF_SAMPLES") is { Length: > 0 } dir && File.Exists(Path.Combine(dir, relative)))
        {
            return Path.Combine(dir, relative);
        }

        var committed = Path.Combine(RepoRoot, "testdata", "gltf", relative);
        return File.Exists(committed) ? committed : null;
    }

    /// <summary>A directory for captures a person wants to look at, or null.</summary>
    public static string? CaptureDir => Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { Length: > 0 } d ? d : null;
}

/// <summary>A device with the validation layer required, a renderer and an offscreen target; fails the test if the layer reported anything.</summary>
internal sealed class Rig : IDisposable
{
    private readonly List<IDisposable> _owned = [];

    public Rig(int width = 320, int height = 240, GpuFormat format = GpuFormat.Rgba8UnormSrgb)
    {
        Device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        Renderer = new Renderer3D(Device);
        Target = new Offscreen3D(Renderer, width, height, format);
    }

    public GpuDevice Device { get; }

    public Renderer3D Renderer { get; }

    public Offscreen3D Target { get; }

    public T Own<T>(T disposable)
        where T : IDisposable
    {
        _owned.Add(disposable);
        return disposable;
    }

    public Model3D Load(string path)
    {
        var model = Own(Renderer.Upload(GltfLoader.LoadFile(path)));
        return model;
    }

    public byte[] Capture(Scene3D scene, string? name = null)
    {
        var pixels = Target.Capture(scene);
        if (name is not null && Paths.CaptureDir is { } dir)
        {
            Directory.CreateDirectory(dir);
            Png.Write(Path.Combine(dir, name + ".png"), Target.Width, Target.Height, pixels);
        }

        return pixels;
    }

    public void Dispose()
    {
        Device.WaitIdle();
        for (var i = _owned.Count - 1; i >= 0; i--)
        {
            _owned[i].Dispose();
        }

        Target.Dispose();
        Renderer.Dispose();
        Device.WaitIdle();
        var messages = Device.TakeValidationMessages();
        Device.Dispose();
        Assert.True(messages.Length == 0, "validation layer reported:\n" + string.Join("\n", messages));
    }
}
