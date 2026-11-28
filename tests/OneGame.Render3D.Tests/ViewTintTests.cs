using RecursiveAI.OneGpu;

namespace OneGame.Render3D.Tests;

/// <summary>View-angle tint: off means identical output, the cone edge sits where its cosine says, soft edges fade, and both zones work.</summary>
public class ViewTintTests
{
    private const int N = 128;

    // A white sphere of radius 1 seen head on from far away (narrow field of view, so n.v is close to sqrt(1 - r^2) at screen radius r).
    private static (Rig Rig, Scene3D Scene, Material3D Material) Make()
    {
        var rig = new Rig(N, N, GpuFormat.Rgba16Float);
        var model = new Shapes().Sphere(Vector3.Zero, 1f, Shapes.Matte(0.8f, 0.8f, 0.8f), 96).Upload(rig);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 0, 40), Vector3.Zero, Vector3.UnitY), Samples = 1 };
        scene.Camera.YFov = 0.06f;
        scene.Add(model);
        scene.Environment.Intensity = 0f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Vector3.Zero;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        scene.Lights.Add(Light3D.Directional(new Vector3(0, 0, -1), Vector3.One, 3f));
        return (rig, scene, model.Materials[0]);
    }

    private static Vector3 At(float[] px, int x, int y)
    {
        var i = ((y * N) + x) * 4;
        return new Vector3(px[i], px[i + 1], px[i + 2]);
    }

    // Writes a float capture as an sRGB PNG when a capture folder is set.
    private static void Save(float[] linear, string name)
    {
        if (Paths.CaptureDir is not { } dir)
        {
            return;
        }

        var bytes = new byte[N * N * 4];
        for (var i = 0; i < bytes.Length; i++)
        {
            var l = Math.Clamp(linear[i], 0f, 1f);
            var c = i % 4 == 3 ? 1f : (l <= 0.0031308f ? l * 12.92f : (1.055f * MathF.Pow(l, 1f / 2.4f)) - 0.055f);
            bytes[i] = (byte)MathF.Round(c * 255f);
        }

        Directory.CreateDirectory(dir);
        Png.Write(Path.Combine(dir, name + ".png"), N, N, bytes);
    }

    private static float[] Render(Rig rig, Scene3D scene) => Plane.ReadAfterRender(rig, scene);

    [Fact]
    public void UnusedTintChangesNothingEvenWithItsOtherSettingsChanged()
    {
        var (rig, scene, material) = Make();
        using (rig)
        {
            var before = Render(rig, scene);
            material.ViewTintColor = new Vector3(1, 0, 0);
            material.ViewTintEdge = 0.3f;
            material.ViewTintSoftness = 0.5f;
            material.ViewTintPower = 3f;
            material.ViewTintZone = ViewTintZone3D.InsideCone;
            var after = Render(rig, scene);
            Assert.Equal(before, after);
            material.ViewTintStrength = 1f;
            Assert.NotEqual(before, Render(rig, scene));
            material.ViewTintStrength = 0f;
            Assert.Equal(before, Render(rig, scene));
        }
    }

    // Finds the silhouette radius in pixels along the middle row (first pixel from the centre that is black).
    private static int Silhouette(float[] px)
    {
        var r = 0;
        while (N / 2 + r + 1 < N && At(px, (N / 2) + r + 1, N / 2).LengthSquared() > 1e-4f)
        {
            r++;
        }

        return r;
    }

    private static float Mix(float[] plain, float[] tinted, int dx)
    {
        // Fraction of the way the pixel moved from the plain colour toward pure red (red channel gain, green loss).
        var a = At(plain, (N / 2) + dx, N / 2);
        var b = At(tinted, (N / 2) + dx, N / 2);
        return a.Y > 1e-5f ? 1f - (b.Y / a.Y) : 0f;
    }

    [Fact]
    public void SharpConeTintsOutsideTheEdgeOnlyAndInsideZoneTintsTheOtherSide()
    {
        var (rig, scene, material) = Make();
        using (rig)
        {
            var plain = Render(rig, scene);
            var radius = Silhouette(plain);
            Assert.InRange(radius, 40, 64);
            material.ViewTintColor = new Vector3(1, 0, 0);
            material.ViewTintStrength = 1f;
            material.ViewTintEdge = 0.66f;
            material.ViewTintSoftness = 0f;
            var outside = Render(rig, scene);
            Save(outside, "viewtint-sharp-outside");
            // n.v = sqrt(1 - s^2) at screen fraction s: the edge 0.66 is at s = 0.751.
            var inner = (int)(radius * 0.70f);
            var outer = (int)(radius * 0.80f);
            Assert.True(Mix(plain, outside, 0) < 0.001f, "head-on centre is untinted");
            Assert.True(Mix(plain, outside, inner) < 0.001f, "just inside the cone is untinted");
            Assert.True(Mix(plain, outside, outer) > 0.999f, "just outside the cone is fully tinted");

            // The edge is sharp: the change happens within a couple of pixels of s = 0.751.
            var edge = (int)MathF.Round(radius * 0.751f);
            Assert.True(Mix(plain, outside, edge - 3) < 0.01f);
            Assert.True(Mix(plain, outside, edge + 3) > 0.99f);

            material.ViewTintZone = ViewTintZone3D.InsideCone;
            var inside = Render(rig, scene);
            Assert.True(Mix(plain, inside, 0) > 0.999f);
            Assert.True(Mix(plain, inside, outer) < 0.001f);
        }
    }

    [Fact]
    public void SoftEdgeFadesAcrossAWideBandAndPowerShapesTheFalloff()
    {
        var (rig, scene, material) = Make();
        using (rig)
        {
            var plain = Render(rig, scene);
            var radius = Silhouette(plain);
            material.ViewTintColor = new Vector3(1, 0, 0);
            material.ViewTintStrength = 1f;
            material.ViewTintEdge = 0.5f;
            material.ViewTintSoftness = 0.45f;
            var soft = Render(rig, scene);
            Save(soft, "viewtint-soft");
            // Count pixels on the row that are neither untinted nor fully tinted: a soft edge has many, a sharp one almost none.
            var between = 0;
            var previous = 0f;
            for (var dx = 0; dx < radius - 2; dx++)
            {
                var m = Mix(plain, soft, dx);
                Assert.True(m >= previous - 0.002f, "the tint grows steadily toward the rim");
                previous = m;
                if (m is > 0.05f and < 0.95f)
                {
                    between++;
                }
            }

            Assert.True(between > radius / 4, $"soft band is wide: {between} of {radius}");

            material.ViewTintPower = 3f;
            var pulled = Render(rig, scene);
            var mid = (int)(radius * 0.85f);
            Assert.True(Mix(plain, pulled, mid) < Mix(plain, soft, mid), "a higher power weakens the middle of the fade");
        }
    }
}
