using OneGame.Gltf;
using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

/// <summary>
/// Planar reflection and UV scroll: the mirror image shows nearby geometry where analytic mirroring puts it, costs nothing when no
/// material opts in, follows the quality ladder, and a scrolling texture moves by velocity times time.
/// </summary>
public class WaterReflectionTests
{
    private const int W = 320;
    private const int H = 240;

    // A polished metal sheet: the sharpest mirror the shader gives, so the image is easy to find. Opaque colour, drawn in the blended pass.
    private const string MirrorMaterial = """{"name":"Water","alphaMode":"BLEND","pbrMetallicRoughness":{"baseColorFactor":[0.9,0.9,0.9,1],"metallicFactor":1,"roughnessFactor":0.02}}""";

    private static readonly Vector3[] MirroredFace =
    [
        new(-1, -2.5f, -1), new(1, -2.5f, -1), new(1, -0.5f, -1), new(-1, -0.5f, -1),
    ];

    private static Scene3D Lit(Camera3D camera)
    {
        var scene = new Scene3D { Camera = camera, Samples = 1 };
        scene.Environment.Intensity = 0f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Vector3.Zero;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = Vector3.Normalize(new Vector3(-0.3f, -1f, -0.5f)), Color = Vector3.One, Intensity = 3f });
        return scene;
    }

    private static Camera3D Camera() => Camera3D.LookAt(new Vector3(0, 4, 8), new Vector3(0, 0, -1), Vector3.UnitY);

    /// <summary>A mirror sheet at y = 0 with a red box floating above it (front face at z = -1, y 0.5 to 2.5). The sheet is opted in by name.</summary>
    private static (Rig Rig, Scene3D Scene, Material3D Water) Make(bool boxInBatch = false)
    {
        var rig = new Rig(W, H, GpuFormat.Rgba16Float);
        var scene = Lit(Camera());
        var water = new Shapes().Ground(0f, 10f, MirrorMaterial).Upload(rig);
        scene.Add(water);
        var box = new Shapes().Box(new Vector3(0, 1.5f, -2), new Vector3(2, 2, 2), Shapes.Matte(0.8f, 0.1f, 0.1f)).Upload(rig);
        if (boxInBatch)
        {
            scene.AddInstanced(box, 4).Add(Matrix4x4.Identity, Vector4.One);
        }
        else
        {
            scene.Add(box);
        }

        var material = water.FindMaterial("Water");
        Assert.NotNull(material);
        material.PlanarReflection = true;
        return (rig, scene, material);
    }

    private static (int MinX, int MinY, int MaxX, int MaxY, int Count) MirrorBounds(float[] on, float[] off)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1, count = 0;
        for (var y = 0; y < H; y++)
        {
            for (var x = 0; x < W; x++)
            {
                var i = ((y * W) + x) * 4;
                if (on[i] - off[i] > 0.05f)
                {
                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                    count++;
                }
            }
        }

        return (minX, minY, maxX, maxY, count);
    }

    private static (float MinX, float MinY, float MaxX, float MaxY) Project(Scene3D scene, Vector3[] points)
    {
        var viewProj = scene.Camera.View * scene.Camera.Projection(W / (float)H);
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in points)
        {
            var c = Vector4.Transform(new Vector4(p, 1), viewProj);
            var x = ((c.X / c.W * 0.5f) + 0.5f) * W;
            var y = (0.5f - (c.Y / c.W * 0.5f)) * H;
            minX = MathF.Min(minX, x);
            minY = MathF.Min(minY, y);
            maxX = MathF.Max(maxX, x);
            maxY = MathF.Max(maxY, y);
        }

        return (minX, minY, maxX, maxY);
    }

    private static void AssertMirrorWhereMirroringPutsIt(Rig rig, Scene3D scene, Material3D water)
    {
        water.PlanarReflection = false;
        var off = Plane.ReadAfterRender(rig, scene);
        Assert.False(rig.Renderer.LastStats.PlanarReflection);
        water.PlanarReflection = true;
        var on = Plane.ReadAfterRender(rig, scene);
        Assert.True(rig.Renderer.LastStats.PlanarReflection);

        var found = MirrorBounds(on, off);
        var expected = Project(scene, MirroredFace);
        Assert.True(found.Count > 500, $"only {found.Count} pixels show the mirror image");

        // The reflection is drawn at three quarters of the frame size, so its edges are about a pixel and a half soft.
        const float slack = 3f;
        Assert.InRange(found.MinX, expected.MinX - slack, expected.MinX + slack);
        Assert.InRange(found.MaxX + 1, expected.MaxX - slack, expected.MaxX + slack);
        Assert.InRange(found.MinY, expected.MinY - slack, expected.MinY + slack);
        Assert.InRange(found.MaxY + 1, expected.MaxY - slack, expected.MaxY + slack);

        // Inside the image the light is the box's red, outside nothing changed.
        var cx = (found.MinX + found.MaxX) / 2;
        var cy = (found.MinY + found.MaxY) / 2;
        var c = (((cy * W) + cx) * 4);
        Assert.True(on[c] > on[c + 1] * 4f && on[c] > on[c + 2] * 4f, $"centre of the mirror image is not red: {on[c]}, {on[c + 1]}, {on[c + 2]}");
        var far = ((10 * W) + 10) * 4;
        Assert.Equal(off[far], on[far]);
    }

    [Fact]
    public void TheMirroredBoxAppearsWhereAnalyticMirroringPutsIt()
    {
        var (rig, scene, water) = Make();
        using (rig)
        {
            AssertMirrorWhereMirroringPutsIt(rig, scene, water);
        }
    }

    [Fact]
    public void InstancedBatchesAreMirroredTooWithShadowsAndFogOn()
    {
        var (rig, scene, water) = Make(boxInBatch: true);
        using (rig)
        {
            scene.Lights[0].CastShadows = true;
            scene.Fog.Enabled = true;
            scene.Fog.Start = 500f;
            scene.Fog.End = 900f;
            scene.Camera.Far = 1000f;
            AssertMirrorWhereMirroringPutsIt(rig, scene, water);
        }
    }

    [Fact]
    public void ThePlaneHeightMovesTheMirrorImage()
    {
        var (rig, scene, water) = Make();
        using (rig)
        {
            // Water at y = -1: a point at height y is mirrored to -2 - y, so the box front face (y 0.5 to 2.5) lands at -4.5 to -2.5.
            var model = scene.Models[0];
            model.Transform = Matrix4x4.CreateTranslation(0, -1, 0);
            scene.Reflection.Height = -1f;
            water.PlanarReflection = false;
            var off = Plane.ReadAfterRender(rig, scene);
            water.PlanarReflection = true;
            var on = Plane.ReadAfterRender(rig, scene);
            var found = MirrorBounds(on, off);
            var expected = Project(scene, [new(-1, -4.5f, -1), new(1, -4.5f, -1), new(1, -2.5f, -1), new(-1, -2.5f, -1)]);
            Assert.True(found.Count > 300);
            Assert.InRange(found.MinY, expected.MinY - 3, expected.MinY + 3);
            Assert.InRange(found.MaxY + 1, expected.MaxY - 3, expected.MaxY + 3);
        }
    }

    [Fact]
    public void NothingIsAddedUnlessAMaterialOptsInAndTheLevelAndCameraAllowIt()
    {
        var (rig, scene, water) = Make();
        using (rig)
        {
            bool Pass() => rig.Target.Graph.Describe().Passes.Any(p => p.Name == "planar_reflection" && !p.Culled);
            bool Resource() => rig.Target.Graph.Describe().Resources.Any(r => r.Name == "reflection_hdr");

            water.PlanarReflection = false;
            var plain = Plane.ReadAfterRender(rig, scene);
            Assert.False(Pass());
            Assert.False(Resource());

            water.PlanarReflection = true;
            water.ReflectionStrength = 0f;
            Assert.Equal(plain, Plane.ReadAfterRender(rig, scene));
            Assert.False(Pass());
            water.ReflectionStrength = 1f;

            scene.Reflection.Enabled = false;
            Assert.Equal(plain, Plane.ReadAfterRender(rig, scene));
            Assert.False(Pass());
            scene.Reflection.Enabled = true;

            scene.Quality = QualityLevel.Low;
            Assert.Equal(plain, Plane.ReadAfterRender(rig, scene));
            Assert.False(Pass());
            scene.Quality = QualityLevel.High;

            var camera = scene.Camera;
            scene.Camera = Camera3D.LookAt(new Vector3(0, -3, 8), new Vector3(0, 0, -1), Vector3.UnitY);
            rig.Target.Render(scene);
            Assert.False(Pass());
            Assert.False(rig.Renderer.LastStats.PlanarReflection);
            scene.Camera = camera;

            rig.Target.Render(scene);
            Assert.True(Pass());
            Assert.True(Resource());
            Assert.True(rig.Renderer.LastStats.PlanarReflection);

            // Drawn only while a material needs it: opting out again takes the pass and its image out of the graph.
            water.PlanarReflection = false;
            rig.Target.Render(scene);
            Assert.False(Pass());
        }
    }

    [Fact]
    public void TheReflectionImageFollowsTheQualityLadder()
    {
        var (rig, scene, _) = Make();
        using (rig)
        {
            string Size(QualityLevel level)
            {
                scene.Quality = level;
                rig.Target.Render(scene);
                var found = rig.Target.Graph.Describe().Resources.FirstOrDefault(r => r.Name == "reflection_hdr");
                return found?.Size ?? "none";
            }

            Assert.Equal("none", Size(QualityLevel.Low));
            Assert.Equal("160x120", Size(QualityLevel.Medium));
            Assert.Equal("240x180", Size(QualityLevel.High));
            Assert.Equal("320x240", Size(QualityLevel.Ultra));
        }
    }

    // ---- UV scroll ----

    // An 8x8 texture whose texel (x, y) is red = x * 32, green = y * 32, read by an unlit sheet through a nearest, repeating sampler.
    private static byte[] ScrollingSheet()
    {
        var rgba = new byte[8 * 8 * 4];
        for (var y = 0; y < 8; y++)
        {
            for (var x = 0; x < 8; x++)
            {
                var i = ((y * 8) + x) * 4;
                rgba[i] = (byte)(x * 32);
                rgba[i + 1] = (byte)(y * 32);
                rgba[i + 3] = 255;
            }
        }

        var path = Path.Combine(Path.GetTempPath(), $"scroll-{Environment.ProcessId}.png");
        Png.Write(path, 8, 8, rgba);
        var uri = "data:image/png;base64," + Convert.ToBase64String(File.ReadAllBytes(path));
        File.Delete(path);
        const string material = "{\"name\":\"Scroller\",\"pbrMetallicRoughness\":{\"baseColorTexture\":{\"index\":0}},\"extensions\":{\"KHR_materials_unlit\":{}}}";
        var json = System.Text.Encoding.UTF8.GetString(Plane.Gltf(material, "\"KHR_materials_unlit\""));
        var extra = "\"textures\":[{\"sampler\":0,\"source\":0}],\"samplers\":[{\"magFilter\":9728,\"minFilter\":9728,\"wrapS\":10497,\"wrapT\":10497}],\"images\":[{\"uri\":\"" + uri + "\"}],";
        return System.Text.Encoding.UTF8.GetBytes(json.Replace("\"buffers\":", extra + "\"buffers\":"));
    }

    private static float SrgbToLinear(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);

    [Fact]
    public void ScrolledTexturesMoveByVelocityTimesTime()
    {
        var (rig, scene) = Plane.Setup(ScrollingSheet(), 64);
        using (rig)
        {
            var material = scene.Models[0].Model.FindMaterial("Scroller");
            Assert.NotNull(material);
            material.UvScroll = new Vector2(0.125f, 0.25f);

            // The sheet maps u and v across its width, with v = 0 at the top; the centre pixel samples (0.5, 0.5) = texel (4, 4) at rest.
            (float R, float G) Centre()
            {
                var px = Plane.ReadAfterRender(rig, scene);
                var i = ((32 * 64) + 32) * 4;
                return (px[i], px[i + 1]);
            }

            void Expect(double seconds, int texelX, int texelY)
            {
                scene.Time = seconds;
                var (r, g) = Centre();
                Assert.Equal(SrgbToLinear((texelX * 32) / 255f), r, 0.004f);
                Assert.Equal(SrgbToLinear((texelY * 32) / 255f), g, 0.004f);
            }

            Expect(0, 4, 4);

            // Frames advance the clock in steps; only the clock at the frame being drawn counts.
            for (var frame = 1; frame <= 4; frame++)
            {
                scene.Time = frame * 0.5;
                Plane.ReadAfterRender(rig, scene);
            }

            // t = 2 s: u + 0.25 = texel 6, v + 0.5 = texel 8 -> wraps to 0.
            Expect(2.0, 6, 0);

            // t = 1 s: u + 0.125 = texel 5, v + 0.25 = texel 6.
            Expect(1.0, 5, 6);

            // Back to zero and off: a zero velocity never moves.
            material.UvScroll = Vector2.Zero;
            Expect(37.0, 4, 4);
        }
    }

    // ---- no allocation ----

    [Fact]
    public void SteadyFramesWithReflectionAndScrollAllocateNothing()
    {
        var (rig, scene, water) = Make();
        using (rig)
        {
            scene.Lights[0].CastShadows = true;
            scene.Environment.Map = rig.Own(rig.Renderer.CreateEnvironment(HdrImage.CreateSky(64, 32, new Vector3(0.5f), new Vector3(0.7f), new Vector3(0.2f), new Vector3(0.3f, 1, 0.2f), new Vector3(20f))));
            scene.Environment.Intensity = 1f;
            scene.Environment.Background = BackgroundMode.Environment;
            scene.Fog.Enabled = true;
            scene.Samples = 0;
            water.UvScroll = new Vector2(0.01f, 0.02f);
            for (var i = 0; i < 12; i++)
            {
                scene.Time = i / 60.0;
                rig.Target.Render(scene);
            }

            rig.Device.WaitIdle();
            rig.Target.Render(scene);
            Assert.True(rig.Renderer.LastStats.PlanarReflection);
            long allocated = 0;
            var where = new List<string>();
            for (var i = 0; i < 40; i++)
            {
                scene.Time = (12 + i) / 60.0;
                scene.Reflection.Height = 0.001f * (i % 5);
                water.ReflectionStrength = 0.8f + (0.004f * i);
                var a0 = GC.GetAllocatedBytesForCurrentThread();
                rig.Target.Render(scene);
                var delta = GC.GetAllocatedBytesForCurrentThread() - a0;
                allocated += delta;
                if (delta > 0)
                {
                    where.Add($"frame {i}: {delta}");
                }
            }

            rig.Device.WaitIdle();
            Assert.True(allocated == 0, $"40 steady frames with reflection allocated {allocated} bytes ({string.Join(", ", where)})");
        }
    }

    // ---- a picture to look at ----

    /// <summary>A pond between two banks with a white tower and a red box, a low camera and a sky: a dielectric water sheet that mirrors them at grazing angles.</summary>
    internal static (Rig Rig, Scene3D Scene) Pond(int width = 320, int height = 180)
    {
        var rig = new Rig(width, height);
        const string pond = """{"name":"Water","alphaMode":"BLEND","pbrMetallicRoughness":{"baseColorFactor":[0.02,0.12,0.2,0.75],"metallicFactor":0,"roughnessFactor":0.03}}""";
        var shapes = new Shapes()
            .Ground(0f, 14f, pond)
            .Box(new Vector3(-9f, 0.25f, -2f), new Vector3(8f, 1f, 24f), Shapes.Matte(0.25f, 0.4f, 0.15f))
            .Box(new Vector3(9f, 0.25f, -2f), new Vector3(8f, 1f, 24f), Shapes.Matte(0.25f, 0.4f, 0.15f))
            .Box(new Vector3(-1.5f, 1.8f, -6f), new Vector3(1.2f, 4f, 1.2f), Shapes.Matte(0.9f, 0.9f, 0.85f))
            .Box(new Vector3(1.8f, 0.9f, -3f), new Vector3(1.6f, 1.6f, 1.6f), Shapes.Matte(0.85f, 0.15f, 0.1f));
        var model = shapes.Upload(rig);
        model.FindMaterial("Water")!.PlanarReflection = true;
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 1.6f, 9f), new Vector3(0, 1.2f, -6f), Vector3.UnitY), Samples = 1 };
        scene.Add(model);
        scene.Environment.Map = rig.Own(rig.Renderer.CreateEnvironment(HdrImage.CreateSky(64, 32, new Vector3(0.5f), new Vector3(0.7f), new Vector3(0.2f), new Vector3(0.3f, 1, 0.2f), new Vector3(20f))));
        scene.Environment.Intensity = 0.8f;
        scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = Vector3.Normalize(new Vector3(-0.4f, -0.7f, -0.5f)), Intensity = 3f, CastShadows = true });
        return (rig, scene);
    }

    [Fact]
    public void PondReferenceMatches()
    {
        var (rig, scene) = Pond();
        using (rig)
        {
            var pixels = rig.Capture(scene, "pond_reflection");
            Assert.True(rig.Renderer.LastStats.PlanarReflection);
            Reference.Compare("pond_reflection", pixels, 320, 180, meanTolerance: 0.6, maxTolerance: 16);
            scene.Reflection.Enabled = false;
            Reference.Compare("pond_no_reflection", rig.Capture(scene, "pond_no_reflection"), 320, 180, meanTolerance: 0.6, maxTolerance: 16);
        }
    }
}
