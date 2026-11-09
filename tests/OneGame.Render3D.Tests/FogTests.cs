using OneGame.Gltf;
using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

/// <summary>Distance fog: off by default, linear between start and end, applied by every surface kind and billboards.</summary>
public class FogTests
{
    private const int Size = 64;
    private static readonly Vector3 FogColour = new(0.2f, 0.4f, 0.8f);

    private static (Rig Rig, Scene3D Scene) Make(float distance)
    {
        var rig = new Rig(Size, Size, GpuFormat.Rgba16Float);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 0, distance), Vector3.Zero, Vector3.UnitY), Samples = 1 };
        scene.Environment.Intensity = 0f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Vector3.Zero;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        scene.Lights.Add(Light3D.Directional(new Vector3(0, 0, -1), Vector3.One, 3f));
        return (rig, scene);
    }

    private static void FullFog(Scene3D scene)
    {
        scene.Fog.Enabled = true;
        scene.Fog.Color = FogColour;
        scene.Fog.Start = 1f;
        scene.Fog.End = 2f;
        scene.Fog.MaxDensity = 1f;
    }

    private static Vector3 At(float[] px, int x, int y)
    {
        var i = ((y * Size) + x) * 4;
        return new Vector3(px[i], px[i + 1], px[i + 2]);
    }

    private static void AssertNear(Vector3 expected, Vector3 actual, float tolerance = 0.01f, string? what = null) =>
        Assert.True((expected - actual).Length() <= tolerance, $"{what}: expected {expected}, got {actual}");

    [Fact]
    public void FogIsLinearInDistanceAndCapsAtMaxDensity()
    {
        var baseColour = new Vector3(0.8f, 0.2f, 0.1f);
        foreach (var (distance, expectedAmount) in new[] { (4f, 0f), (9f, 0.25f), (14f, 0.5f), (30f, 0.5f) })
        {
            var (rig, scene) = Make(distance);
            using (rig)
            {
                scene.Add(rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Unlit(baseColour.X, baseColour.Y, baseColour.Z)))));
                scene.Fog.Enabled = true;
                scene.Fog.Color = FogColour;
                scene.Fog.Start = 4f;
                scene.Fog.End = 14f;
                scene.Fog.MaxDensity = 0.5f;
                var px = Plane.ReadAfterRender(rig, scene);
                AssertNear((baseColour * (1 - expectedAmount)) + (FogColour * expectedAmount), At(px, Size / 2, Size / 2), 0.004f, $"distance {distance}");
            }
        }
    }

    [Fact]
    public void FogIsOffUntilEnabledAndDisabledMatchesNoFog()
    {
        var (rig, scene) = Make(10f);
        using (rig)
        {
            scene.Add(rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Lambert(0.6f)))));
            var plain = Plane.ReadAfterRender(rig, scene);
            FullFog(scene);
            scene.Fog.Enabled = false;
            var disabled = Plane.ReadAfterRender(rig, scene);
            Assert.Equal(plain, disabled);
            scene.Fog.Enabled = true;
            AssertNear(FogColour, At(Plane.ReadAfterRender(rig, scene), Size / 2, Size / 2));
        }
    }

    [Theory]
    [InlineData(MaterialStyle3D.Pbr)]
    [InlineData(MaterialStyle3D.Toon)]
    [InlineData(MaterialStyle3D.Flat)]
    public void EveryStyleFogsToTheFogColour(MaterialStyle3D style)
    {
        var (rig, scene) = Make(10f);
        using (rig)
        {
            var model = rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Lambert(0.6f))));
            model.SetStyle(style);
            scene.Add(model);
            FullFog(scene);
            AssertNear(FogColour, At(Plane.ReadAfterRender(rig, scene), Size / 2, Size / 2), what: style.ToString());
        }
    }

    [Fact]
    public void OutlinesAreFoggedToo()
    {
        var (rig, scene) = Make(10f);
        using (rig)
        {
            var sphere = new Shapes().Sphere(Vector3.Zero, 1.5f, Shapes.Matte(0.9f, 0.6f, 0.3f), 32).Upload(rig);
            sphere.SetStyle(MaterialStyle3D.Toon);
            foreach (var m in sphere.Materials)
            {
                m.OutlineWidth = 6f;
                m.OutlineColor = new Vector3(1f, 0f, 0f);
            }

            scene.Add(sphere);
            scene.Environment.BackgroundColor = new Vector3(0.5f);
            FullFog(scene);
            var px = Plane.ReadAfterRender(rig, scene);

            // Nothing red survives: outline and body are both the fog colour, only the backdrop differs.
            var fogged = 0;
            for (var i = 0; i < Size * Size; i++)
            {
                var c = At(px, i % Size, i / Size);
                Assert.True(c.X < 0.3f || MathF.Abs(c.X - 0.5f) < 0.01f, $"unfogged red at pixel {i}: {c}");
                if ((c - FogColour).Length() < 0.01f)
                {
                    fogged++;
                }
            }

            Assert.True(fogged > 100);
        }
    }

    [Fact]
    public void DynamicMeshesAndCutoutsAreFogged()
    {
        var (rig, scene) = Make(10f);
        using (rig)
        {
            var material = rig.Renderer.CreateMaterial("cutout", MaterialAlpha3D.Mask, true, new Vector4(0.9f, 0.5f, 0.2f, 1f), 0.5f);
            var n = Vector3.UnitZ;
            var clear = new Vector4(1, 1, 1, 0);
            MeshVertex3D[] quad =
            [
                new(new Vector3(-5, -5, 0), n, new Vector2(0, 1), clear),
                new(new Vector3(0, -5, 0), n, new Vector2(0.5f, 1), clear),
                new(new Vector3(0, 5, 0), n, new Vector2(0.5f, 0), clear),
                new(new Vector3(-5, 5, 0), n, new Vector2(0, 0), clear),
                new(new Vector3(0.5f, -5, 0), n, new Vector2(0.5f, 1)),
                new(new Vector3(5, -5, 0), n, new Vector2(1, 1)),
                new(new Vector3(5, 5, 0), n, new Vector2(1, 0)),
                new(new Vector3(0.5f, 5, 0), n, new Vector2(0.5f, 0)),
            ];
            var mesh = rig.Own(rig.Renderer.CreateMesh(material, quad, [0, 1, 2, 0, 2, 3, 4, 5, 6, 4, 6, 7], "quad"));
            scene.Add(mesh.Model);
            var plain = Plane.ReadAfterRender(rig, scene);
            Assert.True((At(plain, 16, Size / 2) - Vector3.Zero).Length() < 0.01f, "the clear half is cut out");
            Assert.True(At(plain, 48, Size / 2).X > 0.05f, "the opaque half is lit");
            FullFog(scene);
            var fogged = Plane.ReadAfterRender(rig, scene);
            AssertNear(Vector3.Zero, At(fogged, 16, Size / 2), what: "cut-out half stays the backdrop");
            AssertNear(FogColour, At(fogged, 48, Size / 2), what: "dynamic mesh half");
        }
    }

    [Fact]
    public void TranslucentSurfacesBlendTowardsFogByTheirAlpha()
    {
        var (rig, scene) = Make(10f);
        using (rig)
        {
            var material = rig.Renderer.CreateMaterial("glass", MaterialAlpha3D.Blend, true, new Vector4(1, 1, 1, 0.5f));
            var n = Vector3.UnitZ;
            MeshVertex3D[] quad = [new(new Vector3(-5, -5, 0), n, default), new(new Vector3(5, -5, 0), n, default), new(new Vector3(5, 5, 0), n, default), new(new Vector3(-5, 5, 0), n, default)];
            var mesh = rig.Own(rig.Renderer.CreateMesh(material, quad, [0, 1, 2, 0, 2, 3], "glass"));
            scene.Add(mesh.Model);
            FullFog(scene);
            AssertNear(FogColour * 0.5f, At(Plane.ReadAfterRender(rig, scene), Size / 2, Size / 2));
        }
    }

    [Fact]
    public void InstancedDrawsAreFogged()
    {
        var (rig, scene) = Make(10f);
        using (rig)
        {
            var model = rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Lambert(0.6f))));
            scene.AddInstanced(model).Add(Matrix4x4.Identity);
            FullFog(scene);
            var px = Plane.ReadAfterRender(rig, scene);
            Assert.Equal(1, rig.Renderer.LastStats.Instances);
            AssertNear(FogColour, At(px, Size / 2, Size / 2));
        }
    }

    [Fact]
    public void BillboardsFogByCoverageAndAdditiveLightFadesOut()
    {
        var (rig, scene) = Make(10f);
        using (rig)
        {
            var batch = new BillboardBatch3D();
            scene.Billboards.Add(batch);
            batch.Add(new Billboard3D { Position = new Vector3(-3, 0, 0), Size = new Vector2(3), Color = new Vector4(1, 0, 0, 1) });
            batch.Add(new Billboard3D { Position = new Vector3(3, 0, 0), Size = new Vector2(3), Color = new Vector4(1, 1, 1, 1), Additive = true });
            var clip = Vector4.Transform(new Vector4(3, 0, 0, 1), scene.Camera.View * scene.Camera.Projection(1f));
            var offset = (int)(clip.X / clip.W * 0.5f * Size);
            var left = (Size / 2) - offset;
            var right = (Size / 2) + offset;
            var plain = Plane.ReadAfterRender(rig, scene);
            Assert.True(At(plain, left, Size / 2).X > 0.9f && At(plain, right, Size / 2).X > 0.9f, "both quads are visible without fog");
            FullFog(scene);
            var fogged = Plane.ReadAfterRender(rig, scene);
            AssertNear(FogColour, At(fogged, left, Size / 2), 0.02f, "blended quad");
            AssertNear(Vector3.Zero, At(fogged, right, Size / 2), what: "additive quad");
        }
    }

    [Fact]
    public void NearAndFarRowsFadeIntoTheFogAndStayAllocationFree()
    {
        var (rig, scene) = FogScene(out _);
        using (rig)
        {
            var px = rig.Capture(scene, "fog_on").ToArray();
            scene.Fog.Enabled = false;
            var off = rig.Capture(scene, "fog_off");
            Assert.NotEqual(px, off);
            for (var i = 0; i < 12; i++)
            {
                rig.Target.Render(scene);
            }

            rig.Device.WaitIdle();
            rig.Target.Render(scene);
            scene.Fog.Enabled = true;
            long allocated = 0;
            var where = new List<string>();
            for (var i = 0; i < 30; i++)
            {
                scene.Fog.Start = 10f + i;
                scene.Fog.End = 100f + i;
                scene.Fog.Color = new Vector3(0.5f + (i * 0.01f), 0.6f, 0.7f);
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
            Assert.True(allocated == 0, $"changing fog every frame allocated {allocated} bytes ({string.Join(", ", where)})");
        }
    }

    /// <summary>Rows of boxes receding to the far plane. Fog: start 10, end 90, maximum density 0.9, the backdrop is the fog colour.</summary>
    internal static (Rig Rig, Scene3D Scene) FogScene(out Vector3 fogColour)
    {
        fogColour = new Vector3(0.45f, 0.55f, 0.7f);
        var rig = new Rig(320, 180);
        var shapes = new Shapes().Ground(0f, 300f, Shapes.Matte(0.25f, 0.35f, 0.15f));
        for (var row = 0; row < 12; row++)
        {
            var z = -row * 10f;
            shapes.Box(new Vector3(-3f, 1f, z), new Vector3(2, 2, 2), Shapes.Matte(0.8f, 0.2f, 0.15f));
            shapes.Box(new Vector3(3f, 1.5f, z), new Vector3(2, 3, 2), Shapes.Matte(0.9f, 0.85f, 0.7f));
        }

        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 3, 12), new Vector3(0, 1.5f, -120), Vector3.UnitY), Samples = 1 };
        scene.Camera.Far = 200f;
        scene.Add(shapes.Upload(rig));
        scene.Lights.Add(Light3D.Directional(new Vector3(-0.4f, -0.8f, -0.5f), Vector3.One, 3f));
        scene.Environment.Intensity = 0.3f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = fogColour;
        scene.Fog.Enabled = true;
        scene.Fog.Color = fogColour;
        scene.Fog.Start = 10f;
        scene.Fog.End = 90f;
        scene.Fog.MaxDensity = 0.9f;
        return (rig, scene);
    }

    [Fact]
    public void FogReferencesMatch()
    {
        var (rig, scene) = FogScene(out _);
        using (rig)
        {
            Reference.Compare("fog_on", rig.Capture(scene), 320, 180, meanTolerance: 0.6, maxTolerance: 16);
            scene.Fog.Enabled = false;
            Reference.Compare("fog_off", rig.Capture(scene), 320, 180, meanTolerance: 0.6, maxTolerance: 16);
        }
    }
}
