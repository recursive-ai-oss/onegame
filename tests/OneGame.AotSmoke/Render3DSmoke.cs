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
        scene.Debug.Line(bounds.Min, bounds.Max, new Vector4(1, 0, 0, 0.5f), 3);
        scene.Debug.Point(bounds.Max, Vector4.One, 8, mode: DebugDepth3D.AlwaysOnTop);
        scene.Debug.Arrow(bounds.Min, bounds.Max, new Vector4(0, 1, 0, 1), 1, DebugSize3D.World);
        var pixels = target.Capture(scene);
        if (renderer.DebugDrawCalls != 2) { return "debug geometry did not draw in both modes"; }
        if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { } captureDir)
        {
            Directory.CreateDirectory(captureDir);
            File.WriteAllBytes(Path.Combine(captureDir, "aot-debug3d.rgba"), pixels);
        }
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

        // A view inside a view, runtime emissive and opacity, and the probe a hand-filled scene answers the control operations with.
        scene.Models.Clear();
        scene.Lights.Clear();
        scene.Ssao.Enabled = false;
        scene.Quality = QualityLevel.Medium;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = new Vector3(0.1f, 0.2f, 0.6f);
        scene.Camera = Camera3D.Frame(bounds.Min, bounds.Max, 30, 20, 96 / 72f);
        var fox = scene.Add(model);
        fox.Label = "fox";
        using var portal = new Offscreen3D(renderer, 96, 72, GpuFormat.Rgba8UnormSrgb, sampled: true);
        portal.Render(scene);
        var emissive = model.Materials[0];
        emissive.SetTexture(MaterialSlot3D.Emissive, portal.Target);
        emissive.Emissive = new Vector3(0.5f);
        emissive.EmissiveStrength = 2f;
        emissive.Opacity = 0.5f;
        fox.Opacity = 0.75f;
        target.Capture(scene);
        emissive.ResetTexture(MaterialSlot3D.Emissive);
        var probe = new OneGame.Host.ManualScene3D(scene);
        if (probe.Cast(probe.Ray(48, 36, 96, 72)) is not { Model: "fox" }) { return "the hand-filled scene probe did not pick the model"; }
        device.WaitIdle();

        // Meshes built at run time: a shared material, a mesh updated in place and replaced by a bigger one, and the same through a scene view with a label and opacity.
        var lime = renderer.CreateMaterial("lime", baseColor: new Vector4(0.2f, 0.9f, 0.1f, 1f));
        MeshVertex3D[] quad = [new(new Vector3(-1, 0, -1), Vector3.UnitY, Vector2.Zero), new(new Vector3(1, 0, -1), Vector3.UnitY, Vector2.UnitX), new(new Vector3(1, 0, 1), Vector3.UnitY, Vector2.One), new(new Vector3(-1, 0, 1), Vector3.UnitY, Vector2.UnitY)];
        uint[] quadIndices = [0, 2, 1, 0, 3, 2];
        using var mesh = renderer.CreateMesh(lime, quad, quadIndices, "smoke");
        mesh.Update(quad, quadIndices);
        var meshScene = new Scene3D();
        meshScene.Add(mesh.Model);
        meshScene.Camera = Camera3D.LookAt(new Vector3(0, 4, 0.01f), Vector3.Zero, -Vector3.UnitZ);
        meshScene.Lights.Add(Light3D.Directional(new Vector3(0.2f, -1, -0.3f), Vector3.One, 3f));
        var drawn = target.Capture(meshScene);
        var view = new OneGame.Host.SceneView3D();
        view.Models.SetMesh("chunk", lime, quad, quadIndices);
        view.Describer = (_, w) => w.Model("chunk", Matrix4x4.Identity, new OneGame.Host.ModelOptions { Label = "chunk", Opacity = 0.5f });
        view.Prepare(new OneGame.Core.World(1, new OneGame.Core.EntityRegistry()), 0, default);
        view.Build(meshScene, renderer);
        target.Capture(meshScene);
        var listed = view.ListModels();
        if (meshScene.Models.Count != 1 || meshScene.Models[0].Label != "chunk" || meshScene.Models[0].Opacity != 0.5f || listed.Count != 1 || !listed[0].Dynamic || mesh.TriangleCount != 2) { return "a mesh built at run time was not drawn with its label and opacity"; }
        view.Models.Remove("chunk");
        view.Describer = null;
        view.Prepare(new OneGame.Core.World(1, new OneGame.Core.EntityRegistry()), 0, default);
        view.Build(meshScene, renderer);
        if (meshScene.Models.Count != 0) { return "a removed mesh is still in the scene"; }
        device.WaitIdle();
        view.Dispose();
        var greenPixels = 0;
        for (var i = 0; i < drawn.Length; i += 4)
        {
            greenPixels += drawn[i + 1] > 100 && drawn[i] < 100 ? 1 : 0;
        }

        if (greenPixels < 100) { return $"the mesh covers {greenPixels} pixels"; }

        // Distance fog and an environment crossfade, both set per frame.
        using var dayMap = renderer.CreateEnvironment(HdrImage.CreateSky(32, 16, new Vector3(0.2f, 0.4f, 1f), new Vector3(0.9f, 0.9f, 1f), new Vector3(0.2f)), 32);
        using var duskMap = renderer.CreateEnvironment(HdrImage.CreateSky(32, 16, new Vector3(0.1f, 0.05f, 0.3f), new Vector3(1.5f, 0.5f, 0.2f), new Vector3(0.1f)), 32);
        meshScene.Add(mesh.Model);
        meshScene.Environment.Background = BackgroundMode.Environment;
        meshScene.Environment.Map = dayMap;
        meshScene.Environment.NextMap = duskMap;
        meshScene.Environment.Blend = 0.5f;
        meshScene.Fog.Enabled = true;
        meshScene.Fog.Color = new Vector3(1f, 0f, 0f);
        meshScene.Fog.Start = 0.5f;
        meshScene.Fog.End = 1f;
        var fogged = target.Capture(meshScene);
        var redPixels = 0;
        for (var i = 0; i < fogged.Length; i += 4)
        {
            redPixels += fogged[i] > 200 && fogged[i + 1] < 60 ? 1 : 0;
        }

        if (redPixels < 100) { return $"fog turned {redPixels} pixels red"; }

        // Planar reflection and texture scrolling on a material, with the mirror plane and the scene clock set on the scene.
        meshScene.Fog.Enabled = false;
        lime.PlanarReflection = true;
        lime.ReflectionStrength = 0.8f;
        lime.UvScroll = new Vector2(0.05f, 0.02f);
        meshScene.Reflection.Height = -0.5f;
        meshScene.Time = 2.5;
        target.Capture(meshScene);
        var mirrored = renderer.LastStats.PlanarReflection;
        lime.PlanarReflection = false;
        device.WaitIdle();
        return mirrored ? null : "the planar reflection pass did not run";
    }
}
