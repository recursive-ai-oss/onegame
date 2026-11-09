using OneGame.Gltf;
using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

/// <summary>A view drawn into a sampled target and shown on a material, and runtime emissive light and opacity.</summary>
public class PortalAndMaterialTests
{
    private const int Size = 64;

    private static string Glow(float r, float g, float b) =>
        FormattableString.Invariant($$"""{"pbrMetallicRoughness":{"baseColorFactor":[0,0,0,1],"metallicFactor":0,"roughnessFactor":1},"emissiveFactor":[{{r}},{{g}},{{b}}]}""");

    private static Vector3 At(float[] px, int x, int y)
    {
        var i = ((y * Size) + x) * 4;
        return new Vector3(px[i], px[i + 1], px[i + 2]);
    }

    private static void Near(Vector3 expected, Vector3 actual, float tolerance = 0.01f) =>
        Assert.True(Vector3.Distance(expected, actual) <= tolerance, $"expected {expected}, got {actual}");

    private static void Plain(Scene3D scene, Vector3 background)
    {
        scene.Environment.Intensity = 0f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = background;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
    }

    [Fact]
    public void EmissiveColourAndStrengthChangeAtRuntime()
    {
        var (rig, scene) = Plane.Setup(Plane.Gltf(Glow(0.2f, 0, 0), half: 4f));
        using (rig)
        {
            var material = scene.Models[0].Model.Materials[0];
            Near(new Vector3(0.2f, 0, 0), At(Plane.ReadAfterRender(rig, scene), 32, 32));
            material.Emissive = new Vector3(0, 0.5f, 0);
            Near(new Vector3(0, 0.5f, 0), At(Plane.ReadAfterRender(rig, scene), 32, 32));
            material.EmissiveStrength = 2f;
            Near(new Vector3(0, 1f, 0), At(Plane.ReadAfterRender(rig, scene), 32, 32));
        }
    }

    [Fact]
    public void MaterialOpacityBlendsOverWhatIsBehindAndRestores()
    {
        var (rig, scene) = Plane.Setup(Plane.Gltf(Glow(1, 1, 1), half: 4f));
        using (rig)
        {
            Plain(scene, new Vector3(0, 0, 1));
            var material = scene.Models[0].Model.Materials[0];
            Near(Vector3.One, At(Plane.ReadAfterRender(rig, scene), 32, 32));
            material.Opacity = 0.25f;
            Near(new Vector3(0.25f, 0.25f, 1f), At(Plane.ReadAfterRender(rig, scene), 32, 32));
            material.Opacity = 1f;
            Near(Vector3.One, At(Plane.ReadAfterRender(rig, scene), 32, 32));
        }
    }

    [Fact]
    public void InstanceOpacityMultipliesTheMaterialsAndIsPerInstance()
    {
        var (rig, scene) = Plane.Setup(Plane.Gltf(Glow(1, 1, 1), half: 4f));
        using (rig)
        {
            Plain(scene, new Vector3(0, 0, 1));
            var first = scene.Models[0];
            var other = scene.Add(first.Model);
            other.Visible = false;
            first.Opacity = 0.5f;
            Near(new Vector3(0.5f, 0.5f, 1f), At(Plane.ReadAfterRender(rig, scene), 32, 32), 0.02f);
            first.Model.Materials[0].Opacity = 0.5f;
            Near(new Vector3(0.25f, 0.25f, 1f), At(Plane.ReadAfterRender(rig, scene), 32, 32), 0.02f);
            Assert.Equal(1f, other.Opacity);
        }
    }

    [Fact]
    public void TranslucentSurfacesBlendFarToNearWithoutWritingDepth()
    {
        // Two translucent boxes in front of a black background: the far (green) one is drawn first, so the near (red) one is composited over it.
        var rig = new Rig(Size, Size, GpuFormat.Rgba16Float);
        using (rig)
        {
            var model = new Shapes()
                .Box(new Vector3(0, 0, -1), new Vector3(6, 6, 0.2f), Glow(0, 1, 0))
                .Box(new Vector3(0, 0, 1), new Vector3(6, 6, 0.2f), Glow(1, 0, 0))
                .Upload(rig);
            var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 0, 10), Vector3.Zero, Vector3.UnitY), Samples = 1 };
            scene.Add(model);
            Plain(scene, Vector3.Zero);
            model.Materials[0].Opacity = 0.5f;
            model.Materials[1].Opacity = 0.5f;
            Near(new Vector3(0.5f, 0.25f, 0f), At(Plane.ReadAfterRender(rig, scene), 32, 32), 0.02f);

            // Opaque near, translucent far: the opaque one still hides it.
            model.Materials[1].Opacity = 1f;
            Near(new Vector3(1f, 0f, 0f), At(Plane.ReadAfterRender(rig, scene), 32, 32), 0.02f);
        }
    }

    [Fact]
    public void ARenderedViewShowsOnAMaterialAndFollowsTheOtherSceneWithoutNewUploads()
    {
        var rig = new Rig(Size, Size, GpuFormat.Rgba16Float);
        using (rig)
        {
            // The other place: a small red square at the top left on a blue background.
            var other = new Scene3D { Camera = Plane.TopDown(8f), Samples = 1 };
            var redModel = rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Gltf(
                """{"pbrMetallicRoughness":{"baseColorFactor":[1,0,0,1]},"extensions":{"KHR_materials_unlit":{}}}""", "\"KHR_materials_unlit\"", half: 1.5f, nodeExtra: ",\"translation\":[-3,3,0]"))));
            var red = other.Add(redModel);
            Plain(other, new Vector3(0, 0, 1));
            using var portal = new Offscreen3D(rig.Renderer, Size, Size, GpuFormat.Rgba8UnormSrgb, sampled: true);

            // The window: a plane that shows the portal as its emissive texture.
            var window = rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Gltf(Glow(1, 1, 1), half: 4f))));
            window.Materials[0].SetTexture(MaterialSlot3D.Emissive, portal.Target);
            var scene = new Scene3D { Camera = Plane.TopDown(8f), Samples = 1 };
            scene.Add(window);
            Plain(scene, Vector3.Zero);

            portal.Render(other);
            var px = Plane.ReadAfterRender(rig, scene);
            Near(new Vector3(1, 0, 0), At(px, 8, 8));
            Near(new Vector3(0, 0, 1), At(px, 56, 56));

            // The other scene moves; the window follows on the next frame.
            red.Transform = Matrix4x4.CreateTranslation(6, -6, 0);
            portal.Render(other);
            px = Plane.ReadAfterRender(rig, scene);
            Near(new Vector3(0, 0, 1), At(px, 8, 8));
            Near(new Vector3(1, 0, 0), At(px, 56, 56));

            // Putting the file's texture back drops the portal.
            window.Materials[0].ResetTexture(MaterialSlot3D.Emissive);
            Near(Vector3.One, At(Plane.ReadAfterRender(rig, scene), 8, 8));
        }
    }

    [Fact]
    public void PortalCapture()
    {
        // A lit room with a framed window onto another place, as a viewer would see it.
        var rig = new Rig(240, 160);
        using (rig)
        {
            var other = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 1.5f, 6), new Vector3(0, 0.5f, 0), Vector3.UnitY), Samples = 1 };
            other.Add(new Shapes().Sphere(new Vector3(0, 0.5f, 0), 1f, Shapes.Matte(0.9f, 0.2f, 0.1f)).Ground(-0.5f, 20, Shapes.Matte(0.2f, 0.5f, 0.2f)).Upload(rig));
            other.Lights.Add(Light3D.Directional(new Vector3(-0.4f, -1f, -0.6f), Vector3.One, 3f));
            other.Environment.Intensity = 0.3f;
            other.Environment.Background = BackgroundMode.Color;
            other.Environment.BackgroundColor = new Vector3(0.1f, 0.2f, 0.5f);
            using var portal = new Offscreen3D(rig.Renderer, 192, 128, GpuFormat.Rgba8UnormSrgb, sampled: true);

            var window = rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Gltf(Glow(1, 1, 1), half: 1f))));
            window.Materials[0].SetTexture(MaterialSlot3D.Emissive, portal.Target);
            var room = new Shapes().Box(new Vector3(0, 0, -1), new Vector3(6, 4, 0.2f), Shapes.Matte(0.6f, 0.55f, 0.5f)).Upload(rig);
            var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0.8f, 0.4f, 4.5f), Vector3.Zero, Vector3.UnitY), Samples = 1 };
            scene.Add(room);
            scene.Add(window).Transform = Matrix4x4.CreateScale(1.5f, 1f, 1f) * Matrix4x4.CreateTranslation(0, 0, -0.85f);
            scene.Lights.Add(Light3D.Directional(new Vector3(-0.3f, -0.5f, -1f), Vector3.One, 2f));
            scene.Environment.Intensity = 0.2f;

            portal.Render(other);
            var pixels = rig.Capture(scene, "portal");
            Reference.Compare("portal", pixels, 240, 160, meanTolerance: 0.8, maxTolerance: 20);
        }
    }

    [Fact]
    public void TranslucentAndGlowingCapture()
    {
        var rig = new Rig(240, 160);
        using (rig)
        {
            var model = new Shapes()
                .Sphere(new Vector3(-2, 0, 0), 0.9f, Shapes.Matte(0.8f, 0.3f, 0.2f))
                .Sphere(new Vector3(0, 0, 0), 0.9f, Shapes.Matte(0.2f, 0.6f, 0.9f))
                .Sphere(new Vector3(2, 0, 0), 0.9f, Glow(0.1f, 0.4f, 0.1f))
                .Ground(-1f, 20, Shapes.Matte(0.5f, 0.5f, 0.5f))
                .Upload(rig);
            var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 1.5f, 7), Vector3.Zero, Vector3.UnitY), Samples = 1 };
            scene.Add(model);
            scene.Lights.Add(Light3D.Directional(new Vector3(-0.4f, -1f, -0.6f), Vector3.One, 3f));
            scene.Environment.Intensity = 0.3f;
            var behind = scene.Add(new Shapes().Box(new Vector3(0, 0, -2.5f), new Vector3(8, 3, 0.2f), Shapes.Matte(0.9f, 0.9f, 0.2f)).Upload(rig));
            model.Materials[0].Opacity = 0.5f;          // the red sphere: translucent through its material
            scene.Models[0].Opacity = 0.6f;             // the instance: the whole model also fades
            model.Materials[2].Emissive = new Vector3(0.1f, 1.2f, 0.2f);
            model.Materials[2].EmissiveStrength = 1.5f; // the green sphere glows
            Assert.NotNull(behind);
            var pixels = rig.Capture(scene, "translucent");
            Reference.Compare("translucent", pixels, 240, 160, meanTolerance: 0.8, maxTolerance: 20);
        }
    }
}
