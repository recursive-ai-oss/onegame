using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

/// <summary>
/// Shadows against geometry with known answers. A thin slab floats above a ground plane; the camera looks straight down through an
/// orthographic lens with no environment light and no tone curve, so each ground pixel is either lit (a known value) or in shadow
/// (zero). The tests locate the shadow edges and compare them with the edge positions projected from the slab along the light.
/// </summary>
public class ShadowTests
{
    private const int Size = 128;
    private const float OrthoHeight = 16f;
    private const float PixelsPerUnit = Size / OrthoHeight;

    private static (Rig Rig, Scene3D Scene, Model3D Model) Make(float slabHalf)
    {
        var rig = new Rig(Size, Size, GpuFormat.Rgba16Float);
        var shapes = new Shapes()
            .Ground(0f, 30f, Shapes.Matte(0.8f, 0.8f, 0.8f))
            .Box(new Vector3(0, 4f, 0), new Vector3(slabHalf * 2, 0.2f, slabHalf * 2), Shapes.Matte(0.5f, 0.5f, 0.5f));
        var model = shapes.Upload(rig);
        var camera = Camera3D.LookAt(new Vector3(0, 30, 0), Vector3.Zero, -Vector3.UnitZ);
        camera.Orthographic = true;
        camera.OrthoHeight = OrthoHeight;
        camera.Near = 0.5f;
        camera.Far = 40f;
        var scene = new Scene3D { Camera = camera, Samples = 1 };
        scene.Add(model);
        scene.Environment.Intensity = 0f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Vector3.Zero;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        return (rig, scene, model);
    }

    private static float Lum(float[] px, int x, int y) => px[((y * Size) + x) * 4 + 1];

    // Pixel column of world x, and pixel row of world z, for the top-down camera (screen right is +X, screen down is +Z).
    private static float Col(float x) => (Size / 2f) + (x * PixelsPerUnit);

    private static float Row(float z) => (Size / 2f) + (z * PixelsPerUnit);

    // Where along a row the value first falls below half of 'lit', scanning from 'from' towards 'to' with sub-pixel interpolation.
    private static float EdgeAlongRow(float[] px, int y, int from, int to, float lit)
    {
        var step = Math.Sign(to - from);
        for (var x = from; x != to; x += step)
        {
            var a = Lum(px, x, y);
            var b = Lum(px, x + step, y);
            if (a >= lit * 0.5f && b < lit * 0.5f)
            {
                // The sample centres sit at x + 0.5; interpolate where the value crosses the midpoint.
                var t = (a - (lit * 0.5f)) / (a - b);
                return x + 0.5f + (step * t);
            }
        }

        return float.NaN;
    }

    private static float EdgeAlongColumn(float[] px, int x, int from, int to, float lit)
    {
        var step = Math.Sign(to - from);
        for (var y = from; y != to; y += step)
        {
            var a = Lum(px, x, y);
            var b = Lum(px, x, y + step);
            if (a >= lit * 0.5f && b < lit * 0.5f)
            {
                var t = (a - (lit * 0.5f)) / (a - b);
                return y + 0.5f + (step * t);
            }
        }

        return float.NaN;
    }

    // Lambert: albedo / pi * lux * cos(theta)
    private static float LitValue(float albedo, float lux, float cosTheta) => albedo / MathF.PI * lux * cosTheta;

    [Fact]
    public void DirectionalShadowEdgesLandWhereTheSlabProjects()
    {
        var (rig, scene, _) = Make(2f);
        using (rig)
        {
            var dir = Vector3.Normalize(new Vector3(1f, -1f, 0f));
            scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = dir, Intensity = 3f, CastShadows = true });
            rig.Target.Render(scene);
            var px = Plane.ReadLinear(rig);
            var lit = LitValue(0.8f, 3f, dir.Y * -1f);

            // Open ground far from the slab is fully lit.
            Assert.InRange(Lum(px, 8, 8), lit * 0.97f, lit * 1.03f);
            Assert.True(rig.Renderer.LastStats.ShadowTiles >= 1);

            // The slab spans x, z in [-2, 2] at y 3.9..4.1. Along (1, -1, 0) each point lands (y) units further in +x, so the shadow covers
            // x from -2 + 3.9 = 1.9 to 2 + 4.1 = 6.1 and z from -2 to 2. The slab hides the left edge from above; check the other three.
            var right = EdgeAlongRow(px, (int)Row(0f), (int)Col(7.5f), (int)Col(5f), lit);
            var top = EdgeAlongColumn(px, (int)Col(4f), (int)Row(-3.5f), (int)Row(0f), lit);
            var bottom = EdgeAlongColumn(px, (int)Col(4f), (int)Row(3.5f), (int)Row(0f), lit);

            // Tolerance: 1.5 pixels. A cascade texel is under half a pixel here; the filter adds about one pixel of blur.
            Assert.InRange(right, Col(6.1f) - 1.5f, Col(6.1f) + 1.5f);
            Assert.InRange(top, Row(-2f) - 1.5f, Row(-2f) + 1.5f);
            Assert.InRange(bottom, Row(2f) - 1.5f, Row(2f) + 1.5f);

            // Inside the umbra nothing is lit, and the lit slab top shows no acne (a flat surface has one value).
            Assert.InRange(Lum(px, (int)Col(4f), (int)Row(0f)), 0f, lit * 0.02f);
            var slabTop = LitValue(0.5f, 3f, dir.Y * -1f);
            Assert.InRange(Lum(px, (int)Col(0f), (int)Row(0f)), slabTop * 0.97f, slabTop * 1.03f);
            Assert.InRange(Lum(px, (int)Col(1f), (int)Row(1f)), slabTop * 0.97f, slabTop * 1.03f);
        }
    }

    [Fact]
    public void NoShadowWithoutCastShadows()
    {
        var (rig, scene, _) = Make(2f);
        using (rig)
        {
            var dir = Vector3.Normalize(new Vector3(1f, -1f, 0f));
            scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = dir, Intensity = 3f });
            rig.Target.Render(scene);
            var px = Plane.ReadLinear(rig);
            Assert.Equal(0, rig.Renderer.LastStats.ShadowTiles);
            Assert.InRange(Lum(px, (int)Col(4f), (int)Row(0f)), LitValue(0.8f, 3f, dir.Y * -1f) * 0.97f, LitValue(0.8f, 3f, dir.Y * -1f) * 1.03f);
        }
    }

    [Fact]
    public void SpotShadowEdgesLandWhereTheSlabProjects()
    {
        var (rig, scene, _) = Make(1f);
        using (rig)
        {
            scene.Lights.Add(Light3D.Spot(new Vector3(0, 8, 0), -Vector3.UnitY, Vector3.One, 400f, 0.5f, 0.9f));
            scene.Lights[0].CastShadows = true;
            rig.Target.Render(scene);
            var px = Plane.ReadLinear(rig);
            var lit = Lum(px, (int)Col(2.6f), (int)Row(0f));
            Assert.True(lit > 0.05f, "the spot lights the ground outside the shadow");

            // From 8 units up, a point at height 4.1 (slab top) scales its offset by 8 / (8 - 4.1) = 2.051: the slab's half size of 1 casts to 2.051.
            var s = 8f / (8f - 4.1f);
            var edge = EdgeAlongRow(px, (int)Row(0f), (int)Col(3.5f), (int)Col(1.0f), lit);
            Assert.InRange(edge, Col(1f * s) - 1.5f, Col(1f * s) + 1.5f);
            Assert.InRange(Lum(px, (int)Col(1.5f), (int)Row(0f)), 0f, lit * 0.03f);
        }
    }

    [Fact]
    public void PointShadowEdgesLandWhereTheSlabProjects()
    {
        var (rig, scene, _) = Make(1f);
        using (rig)
        {
            var light = Light3D.Point(new Vector3(0, 6, 0), Vector3.One, 300f);
            light.CastShadows = true;
            scene.Lights.Add(light);
            rig.Target.Render(scene);
            var px = Plane.ReadLinear(rig);
            Assert.Equal(6, rig.Renderer.LastStats.ShadowTiles);
            var lit = Lum(px, (int)Col(5f), (int)Row(0f));
            Assert.True(lit > 0.02f);

            var s = 6f / (6f - 4.1f);
            var edgeX = EdgeAlongRow(px, (int)Row(0f), (int)Col(5f), (int)Col(1.5f), lit);
            Assert.InRange(edgeX, Col(1f * s) - 1.5f, Col(1f * s) + 1.5f);
            var edgeZ = EdgeAlongColumn(px, (int)Col(0f), (int)Row(5f), (int)Row(1.5f), lit);
            Assert.InRange(edgeZ, Row(1f * s) - 1.5f, Row(1f * s) + 1.5f);
            Assert.InRange(Lum(px, (int)Col(2f), (int)Row(0f)), 0f, lit * 0.15f);
        }
    }

    [Fact]
    public void CascadesCoverNearAndFarWithTheirOwnResolution()
    {
        var (rig, scene, _) = Make(2f);
        using (rig)
        {
            scene.Quality = QualityLevel.High;
            scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = Vector3.Normalize(new Vector3(1f, -1f, 0f)), Intensity = 3f, CastShadows = true });
            rig.Target.Render(scene);
            Assert.Equal(4, rig.Renderer.LastStats.ShadowTiles);
            Assert.Equal(1, rig.Renderer.LastStats.ShadowLights);
        }
    }

    [Fact]
    public void ShadowReferenceMatches()
    {
        using var rig = new Rig(256, 192);
        var shapes = new Shapes()
            .Ground(0f, 30f, Shapes.Matte(0.75f, 0.78f, 0.72f))
            .Box(new Vector3(-2f, 1f, 0f), new Vector3(2f, 2f, 2f), Shapes.Matte(0.8f, 0.3f, 0.25f))
            .Box(new Vector3(2.2f, 0.75f, 1f), new Vector3(1.5f, 1.5f, 1.5f), Shapes.Matte(0.25f, 0.45f, 0.8f))
            .Box(new Vector3(0.5f, 2.5f, -2.5f), new Vector3(0.6f, 5f, 0.6f), Shapes.Matte(0.85f, 0.8f, 0.3f));
        var model = shapes.Upload(rig);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(8, 8, -9), new Vector3(-1, 0.5f, 0), Vector3.UnitY) };
        scene.Add(model);
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = new Vector3(0.03f, 0.04f, 0.06f);
        scene.Environment.Intensity = 0.25f;
        scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = Vector3.Normalize(new Vector3(-0.6f, -1f, -0.5f)), Intensity = 1.8f, CastShadows = true });
        var spot = Light3D.Spot(new Vector3(4, 5, -1), Vector3.Normalize(new Vector3(-1.2f, -1.5f, 1.4f)), new Vector3(1f, 0.85f, 0.6f), 90f, 0.2f, 0.5f);
        spot.CastShadows = true;
        scene.Lights.Add(spot);
        var pixels = rig.Capture(scene, "shadows");
        Reference.Compare("shadows", pixels, 256, 192, meanTolerance: 1.0, maxTolerance: 24);
    }
}
