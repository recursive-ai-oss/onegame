using System.Numerics;
using OneGame.Gltf;
using OneGame.Gpu;
using OneGame.Render3D;

namespace OneGame.AotSmoke;

/// <summary>The 3D renderer under the native publish: a skinned glTF model with a texture, lit by a sun and the built-in sky, drawn offscreen and read back.</summary>
internal static class Render3DSmoke
{
    public static string? Run(GpuDevice device, byte[] foxGlb)
    {
        using var renderer = new Renderer3D(device);
        using var target = new Offscreen3D(renderer, 96, 72, GpuFormat.Rgba8UnormSrgb);
        using var model = renderer.Upload(GltfLoader.Load(foxGlb));
        var scene = new Scene3D();
        var instance = scene.Add(model);
        if (model.Source.Animations.Length > 0)
        {
            instance.Animate(0, 0.2f, loop: true);
        }

        scene.Lights.Add(Light3D.Directional(new Vector3(-1, -1, -1), Vector3.One, 3f));
        var bounds = model.Source.ComputeWorldBounds()!.Value;
        scene.Camera = Camera3D.Frame(bounds.Min, bounds.Max, 30, 20, 96 / 72f);
        var pixels = target.Capture(scene);
        var different = 0;
        for (var i = 4; i < pixels.Length; i += 4)
        {
            if (pixels[i] != pixels[0] || pixels[i + 1] != pixels[1] || pixels[i + 2] != pixels[2])
            {
                different++;
            }
        }

        device.WaitIdle();
        return different < 300 ? $"only {different} pixels differ from the background" : null;
    }
}
