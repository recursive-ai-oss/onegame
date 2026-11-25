using System.Numerics;
using System.Runtime.CompilerServices;
using OneGame.Core;
using OneGame.Gltf;
using OneGame.Host;
using RecursiveAI.OneGpu;
using RecursiveAI.OneGpu.Vulkan;
using OneGame.Render3D;
using Scene = OneGame.Render3D.Scene3D;

namespace OneGame.Scene3D.Tests;

/// <summary>Posed copies described from view code: one pooled skinned batch per model, refilled each frame, no allocation once warm.</summary>
public class SkinnedInstanceTests
{
    private static string Fox([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "testdata", "gltf", "Fox", "glTF", "Fox.gltf"));

    [Fact]
    public void ViewCodeDrawsPosedCopiesThroughOneBatchWithoutAllocating()
    {
        using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        var renderer = new Renderer3D(device);
        var target = new Offscreen3D(renderer, 160, 120);
        var view = new SceneView3D();
        var source = GltfLoader.LoadFile(Fox());
        view.Models.Add("fox", source);
        var poses = new NodePose[4];
        for (var i = 0; i < poses.Length; i++)
        {
            poses[i] = source.CreateRestPose();
            source.Animations[1].Apply(poses[i], i * 0.2f);
        }

        view.CameraOverride = new ViewCamera(Matrix4x4.Invert(Matrix4x4.CreateLookAt(new Vector3(0, 6, 14), new Vector3(0, 1, 0), Vector3.UnitY), out var w) ? w : Matrix4x4.Identity, 45f * MathF.PI / 180f, 0f, 0.1f, 200f, default);
        view.Describer = (_, wr) =>
        {
            wr.DirectionalLight(new Vector3(-0.5f, -1f, -0.4f), Vector3.One, 3f, castShadows: true);
            for (var i = 0; i < 20; i++)
            {
                var at = new Vector3(((i % 5) - 2) * 3f, 0, -(i / 5) * 3f);
                wr.Instance("fox", Matrix4x4.CreateScale(0.04f) * Matrix4x4.CreateTranslation(at), poses[i % poses.Length]);
            }
        };

        var scene = new Scene { Samples = 1 };
        var world = new World(1, new EntityRegistry());
        view.Prepare(world, 0, Fixed.Zero);
        view.Build(scene, renderer);
        Assert.Single(scene.Batches);
        Assert.True(scene.Batches[0].Posed);
        Assert.Equal(20, scene.Batches[0].Count);
        target.Render(scene);
        Assert.Equal(20, renderer.LastStats.Instances);
        Assert.True(renderer.LastStats.InstancesDrawn > 0);

        for (var i = 0; i < 12; i++)
        {
            view.Prepare(world, i, Fixed.Half);
            view.Build(scene, renderer);
            target.Render(scene);
        }

        device.WaitIdle();
        view.Prepare(world, 20, Fixed.Half);
        view.Build(scene, renderer);
        target.Render(scene);
        long allocated = 0;
        for (var i = 0; i < 30; i++)
        {
            var a0 = GC.GetAllocatedBytesForCurrentThread();
            view.Prepare(world, 30 + i, Fixed.Half);
            view.Build(scene, renderer);
            target.Render(scene);
            allocated += GC.GetAllocatedBytesForCurrentThread() - a0;
        }

        device.WaitIdle();
        view.Dispose();
        target.Dispose();
        renderer.Dispose();
        Assert.True(allocated == 0, $"30 steady frames allocated {allocated} bytes");
        var messages = device.TakeValidationMessages();
        Assert.True(messages.Length == 0, string.Join("\n", messages));
    }
}
