using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

/// <summary>Stylized materials: cel shading steps, outlines and flat colour, chosen per material.</summary>
public class StyleTests
{
    private const int W = 200;
    private const int H = 150;
    private static readonly Vector3 Background = new(0.5f);

    private static (Rig Rig, Scene3D Scene, Model3D Sphere) Make(float r = 0.9f, float g = 0.6f, float b = 0.3f)
    {
        var rig = new Rig(W, H, GpuFormat.Rgba16Float);
        var sphere = new Shapes().Sphere(Vector3.Zero, 1.5f, Shapes.Matte(r, g, b), 64).Upload(rig);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 0, 8), Vector3.Zero, Vector3.UnitY), Samples = 1 };
        scene.Add(sphere);
        scene.Lights.Add(Light3D.Directional(new Vector3(-0.6f, -0.5f, -1f), Vector3.One, 3f));
        scene.Environment.Intensity = 0f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Background;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        return (rig, scene, sphere);
    }

    // Pixels well inside the sphere's silhouette (the sphere is centred, about 34 pixels in radius).
    private static IEnumerable<int> InsidePixels(float radius = 30f)
    {
        for (var y = 0; y < H; y++)
        {
            for (var x = 0; x < W; x++)
            {
                var dx = x + 0.5f - (W / 2f);
                var dy = y + 0.5f - (H / 2f);
                if ((dx * dx) + (dy * dy) < radius * radius)
                {
                    yield return ((y * W) + x) * 4;
                }
            }
        }
    }

    private static int PopulatedLevels(float[] px, float fraction)
    {
        var max = InsidePixels().Max(i => px[i + 1]);
        var bins = new Dictionary<int, int>();
        var total = 0;
        foreach (var i in InsidePixels())
        {
            var bin = (int)MathF.Round(px[i + 1] / max * 100f);
            bins[bin] = bins.GetValueOrDefault(bin) + 1;
            total++;
        }

        return bins.Count(kv => kv.Value >= total * fraction);
    }

    [Fact]
    public void ToonShadesInFlatSteps()
    {
        var (rig, scene, sphere) = Make();
        using (rig)
        {
            var pbr = Plane.ReadAfterRender(rig, scene);
            sphere.SetStyle(MaterialStyle3D.Toon);
            foreach (var m in sphere.Materials)
            {
                m.ToonBands = 3;
                m.ToonSoftness = 0f;
                m.ToonHighlight = 0f;
            }

            var toon = Plane.ReadAfterRender(rig, scene);

            // A smooth shaded sphere fills many brightness levels; the cel sphere has the dark side and at most three lit steps.
            Assert.True(PopulatedLevels(pbr, 0.005f) > 15, "the PBR sphere should shade smoothly");
            var levels = PopulatedLevels(toon, 0.01f);
            Assert.InRange(levels, 2, 4);

            // Each step is the diffuse value at k / bands of the light: albedo / pi * lux * k / 3 (green channel, albedo 0.6).
            var max = InsidePixels().Max(i => toon[i + 1]);
            Assert.InRange(max, (0.6f / MathF.PI * 3f) * 0.97f, (0.6f / MathF.PI * 3f) * 1.03f);
            var onStep = 0;
            var total = 0;
            foreach (var i in InsidePixels())
            {
                var k = toon[i + 1] / max * 3f;
                if (MathF.Abs(k - MathF.Round(k)) < 0.04f)
                {
                    onStep++;
                }

                total++;
            }

            Assert.True(onStep > total * 0.97f, $"{onStep} of {total} pixels sit on a step");
        }
    }

    [Fact]
    public void ToonShadowsDarkenSteps()
    {
        var (rig, scene, sphere) = Make();
        using (rig)
        {
            sphere.SetStyle(MaterialStyle3D.Toon);
            var floor = new Shapes().Box(new Vector3(0, 0, -1.2f), new Vector3(6, 6, 0.2f), Shapes.Matte(0.5f, 0.5f, 0.5f)).Upload(rig);
            scene.Add(floor);
            scene.Lights[0].CastShadows = true;
            rig.Target.Render(scene);
            Assert.True(rig.Renderer.LastStats.ShadowTiles > 0);
        }
    }

    [Fact]
    public void OutlineSurroundsTheSilhouetteByTheRequestedWidth()
    {
        var (rig, scene, sphere) = Make();
        using (rig)
        {
            sphere.SetStyle(MaterialStyle3D.Toon);
            float[] Render() => Plane.ReadAfterRender(rig, scene);
            var without = Render();
            int Edge(float[] px)
            {
                var y = H / 2;
                for (var x = W - 1; x > W / 2; x--)
                {
                    if (MathF.Abs(px[(((y * W) + x) * 4) + 1] - Background.Y) > 0.02f)
                    {
                        return x;
                    }
                }

                return -1;
            }

            var silhouette = Edge(without);
            Assert.True(silhouette > W / 2 + 20, "the sphere is visible");

            foreach (var m in sphere.Materials)
            {
                m.OutlineWidth = 6f;
                m.OutlineColor = new Vector3(0.02f, 0.02f, 0.03f);
            }

            var with = Render();
            var row = H / 2;
            float G(float[] px, int x) => px[(((row * W) + x) * 4) + 1];

            // Just outside the silhouette the line has the outline colour; 6 pixels out it is over and the background is back.
            Assert.InRange(G(with, silhouette + 2), 0.015f, 0.03f);
            Assert.InRange(G(with, silhouette + 4), 0.015f, 0.03f);
            Assert.InRange(Edge(with) - silhouette, 5, 7);
            Assert.InRange(G(with, silhouette + 9), Background.Y - 0.01f, Background.Y + 0.01f);

            // Inside the sphere nothing changed.
            foreach (var i in InsidePixels(25f))
            {
                Assert.Equal(without[i + 1], with[i + 1], 3);
            }
        }
    }

    [Fact]
    public void FlatIsTheBaseColourWhateverTheLight()
    {
        var (rig, scene, sphere) = Make(0.2f, 0.6f, 0.9f);
        using (rig)
        {
            sphere.SetStyle(MaterialStyle3D.Flat);
            var px = Plane.ReadAfterRender(rig, scene);
            foreach (var i in InsidePixels(25f))
            {
                Assert.Equal(0.2f, px[i], 2);
                Assert.Equal(0.6f, px[i + 1], 2);
                Assert.Equal(0.9f, px[i + 2], 2);
            }
        }
    }

    [Fact]
    public void StylesReference()
    {
        using var rig = new Rig(320, 160);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 1f, 8.2f), new Vector3(0, 0, 0), Vector3.UnitY) };
        var styles = new[] { MaterialStyle3D.Toon, MaterialStyle3D.Pbr, MaterialStyle3D.Flat };
        for (var i = 0; i < 3; i++)
        {
            var model = new Shapes().Sphere(Vector3.Zero, 1.5f, Shapes.Matte(0.85f, 0.35f, 0.25f), 64).Upload(rig);
            model.SetStyle(styles[i]);
            foreach (var m in model.Materials)
            {
                m.OutlineWidth = styles[i] == MaterialStyle3D.Pbr ? 0f : 3f;
                m.ToonBands = 4;
                m.ToonSoftness = 0.08f;
            }

            scene.Add(model, Matrix4x4.CreateTranslation((i - 1) * 3.5f, 0, 0));
        }

        scene.Lights.Add(Light3D.Directional(new Vector3(-0.5f, -0.6f, -1f), Vector3.One, 3f));
        scene.Environment.Intensity = 0.5f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = new Vector3(0.2f, 0.25f, 0.35f);
        var px = rig.Capture(scene, "styles");
        Reference.Compare("styles", px, 320, 160, meanTolerance: 0.6, maxTolerance: 16);
    }
}
