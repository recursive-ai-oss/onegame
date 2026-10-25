using OneGame.Gltf;

namespace OneGame.Render3D.Tests;

public class GalleryTests
{
    [Theory]
    [InlineData("DamagedHelmet/glTF-Binary/DamagedHelmet.glb", 20f, 10f)]
    [InlineData("MetalRoughSpheres/glTF/MetalRoughSpheres.gltf", 0f, 0f)]
    [InlineData("NormalTangentMirrorTest/glTF/NormalTangentMirrorTest.gltf", 0f, 0f)]
    [InlineData("TextureTransformTest/glTF/TextureTransformTest.gltf", 0f, 0f)]
    [InlineData("EmissiveStrengthTest/glTF/EmissiveStrengthTest.gltf", 0f, 0f)]
    [InlineData("AlphaBlendModeTest/glTF/AlphaBlendModeTest.gltf", 0f, 0f)]
    [InlineData("Fox/glTF/Fox.gltf", 40f, 15f)]
    public void Gallery(string relative, float yaw, float pitch)
    {
        var path = Paths.Model(relative);
        if (path is null)
        {
            return;
        }

        using var rig = new Rig(640, 480);
        var model = rig.Load(path);
        var scene = new Scene3D();
        var inst = scene.Add(model);
        if (model.Source.Animations.Length > 0 && relative.StartsWith("Fox"))
        {
            inst.Animate(2, 0.5f);
        }

        var b = model.Source.ComputeWorldBounds()!.Value;
        scene.Camera = Camera3D.Frame(b.Min, b.Max, yaw, pitch, 640 / 480f);
        var hdr = Environment.GetEnvironmentVariable("ONEGAME_TEST_HDR");
        if (hdr is not null)
        {
            scene.Environment.Map = rig.Own(rig.Renderer.CreateEnvironment(HdrImage.Decode(File.ReadAllBytes(hdr))));
        }

        var c = scene.Camera; File.AppendAllText(Path.Combine(Paths.CaptureDir ?? ".", "cameras.txt"), $"{relative} eye={c.Position.X:G6},{c.Position.Y:G6},{c.Position.Z:G6} near={c.Near:G5} far={c.Far:G5} bounds={b.Min}-{b.Max}\n");
        rig.Capture(scene, "gallery_" + Path.GetFileNameWithoutExtension(relative));
    }
}
