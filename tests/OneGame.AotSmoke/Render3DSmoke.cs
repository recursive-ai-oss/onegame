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

        if (different < 300)
        {
            return $"only {different} pixels differ from the background";
        }

        // Shadows, ambient occlusion, instancing, the quality ladder and stylized materials, all on the native publish.
        scene.Models.Clear();
        scene.Lights.Clear();
        var batch = scene.AddInstanced(model, 64);
        for (var i = 0; i < 25; i++)
        {
            var at = new Vector3(((i % 5) - 2) * 90f, 0, ((i / 5) - 2) * 90f);
            batch.Add(Matrix4x4.CreateRotationY(i * 0.7f) * Matrix4x4.CreateTranslation(at), new Vector4(1f, 0.6f + (i % 3 * 0.2f), 0.6f, 1f));
        }

        scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = new Vector3(-1, -1.5f, -0.6f), Intensity = 3f, CastShadows = true });
        var spot = Light3D.Spot(new Vector3(0, 300, 200), new Vector3(0, -1, -0.6f), Vector3.One, 2e5f, 0.3f, 0.7f);
        spot.CastShadows = true;
        scene.Lights.Add(spot);
        scene.Ssao.Enabled = true;
        scene.Ssao.Radius = 20f;
        foreach (var m in model.Materials)
        {
            m.Style = MaterialStyle3D.Toon;
            m.OutlineWidth = 2f;
        }

        scene.Camera = Camera3D.LookAt(new Vector3(0, 220, 380), new Vector3(0, 20, 0), Vector3.UnitY);
        scene.Camera.Far = 2000f;
        foreach (var level in new[] { QualityLevel.Low, QualityLevel.Medium, QualityLevel.High, QualityLevel.Ultra })
        {
            scene.Quality = level;
            target.Capture(scene);
        }

        var stats = renderer.LastStats;
        device.WaitIdle();
        if (stats.Instances != 25 || stats.InstancesDrawn == 0 || stats.ShadowTiles < 5 || !stats.Ssao)
        {
            return $"instancing or shadows did not run: {stats}";
        }

        return null;
    }
}
