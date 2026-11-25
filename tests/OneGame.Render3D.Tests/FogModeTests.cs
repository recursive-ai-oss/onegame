using OneGame.Gltf;
using RecursiveAI.OneGpu;

namespace OneGame.Render3D.Tests;

/// <summary>Exponential and height fog against their formulas, the per-material opt-out, and per-instance shadow casting.</summary>
public class FogModeTests
{
    private const int Size = 64;
    private static readonly Vector3 FogColour = new(0.2f, 0.4f, 0.8f);
    private static readonly Vector3 Base = new(0.8f, 0.2f, 0.1f);

    private static (Rig Rig, Scene3D Scene) Make(Vector3 camera)
    {
        var rig = new Rig(Size, Size, GpuFormat.Rgba16Float);
        var scene = new Scene3D { Camera = Camera3D.LookAt(camera, Vector3.Zero, Vector3.UnitY), Samples = 1 };
        scene.Environment.Intensity = 0f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Vector3.Zero;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        scene.Fog.Enabled = true;
        scene.Fog.Color = FogColour;
        return (rig, scene);
    }

    private static Vector3 Centre(float[] px)
    {
        var i = (((Size / 2) * Size) + (Size / 2)) * 4;
        return new Vector3(px[i], px[i + 1], px[i + 2]);
    }

    private static void AssertAmount(float amount, Vector3 actual, string what)
    {
        var expected = (Base * (1 - amount)) + (FogColour * amount);
        Assert.True((expected - actual).Length() <= 0.004f, $"{what}: expected {expected} (amount {amount}), got {actual}");
    }

    [Theory]
    [InlineData(0.05f, 10f)]
    [InlineData(0.1f, 6f)]
    public void ExponentialFogIsOneMinusExpOfDensityTimesDistance(float density, float distance)
    {
        var (rig, scene) = Make(new Vector3(0, 0, distance));
        using (rig)
        {
            scene.Add(rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Unlit(Base.X, Base.Y, Base.Z)))));
            scene.Fog.Mode = FogMode.Exponential;
            scene.Fog.Density = density;
            AssertAmount(1f - MathF.Exp(-density * distance), Centre(Plane.ReadAfterRender(rig, scene)), "exp");
        }
    }

    [Fact]
    public void ExponentialSquaredFogIsOneMinusExpOfTheSquare()
    {
        var (rig, scene) = Make(new Vector3(0, 0, 10));
        using (rig)
        {
            scene.Add(rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Unlit(Base.X, Base.Y, Base.Z)))));
            scene.Fog.Mode = FogMode.ExponentialSquared;
            scene.Fog.Density = 0.1f;
            AssertAmount(1f - MathF.Exp(-1f), Centre(Plane.ReadAfterRender(rig, scene)), "exp2");
            scene.Fog.MaxDensity = 0.5f;
            AssertAmount(0.5f * (1f - MathF.Exp(-1f)), Centre(Plane.ReadAfterRender(rig, scene)), "exp2 capped");
        }
    }

    [Fact]
    public void ZeroDensityDrawsNoFogInTheExponentialModes()
    {
        var (rig, scene) = Make(new Vector3(0, 0, 10));
        using (rig)
        {
            scene.Add(rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Unlit(Base.X, Base.Y, Base.Z)))));
            scene.Fog.Mode = FogMode.Height;
            scene.Fog.Density = 0f;
            AssertAmount(0f, Centre(Plane.ReadAfterRender(rig, scene)), "zero density");
        }
    }

    [Theory]
    [InlineData(0f, 10f, 0.1f, 0.2f, 0f)]      // level ray at the base height: plain exponential
    [InlineData(0f, 10f, 0.1f, 0.2f, -5f)]     // level ray 5 units above the base: density shrinks by exp(-1)
    [InlineData(10f, 10f, 0.03f, 0.25f, 0f)]   // looking down through the thinning layer
    [InlineData(-4f, 10f, 0.05f, 0.3f, 2f)]    // looking up from below the base, into thinner fog
    public void HeightFogIntegratesTheFallingDensityAlongTheRay(float cameraY, float cameraZ, float density, float falloff, float baseHeight)
    {
        var (rig, scene) = Make(new Vector3(0, cameraY, cameraZ));
        using (rig)
        {
            scene.Add(rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Unlit(Base.X, Base.Y, Base.Z)))));
            scene.Fog.Mode = FogMode.Height;
            scene.Fog.Density = density;
            scene.Fog.HeightFalloff = falloff;
            scene.Fog.HeightBase = baseHeight;

            // The ray ends at the plane's centre (0, 0, 0): its length and the heights at both ends give the optical depth in closed form.
            double length = Math.Sqrt((cameraY * cameraY) + (cameraZ * cameraZ));
            double a = -falloff * (cameraY - baseHeight);
            double b = -falloff * (0 - baseHeight);
            double k = a - b;
            var average = Math.Abs(k) < 1e-9 ? Math.Exp(a) : (Math.Exp(a) - Math.Exp(b)) / k;
            var tau = density * length * average;
            AssertAmount((float)(1 - Math.Exp(-tau)), Centre(Plane.ReadAfterRender(rig, scene)), $"height fog from y {cameraY}");
        }
    }

    [Fact]
    public void ZeroFalloffHeightFogMatchesExponentialFog()
    {
        var (rig, scene) = Make(new Vector3(0, 3, 10));
        using (rig)
        {
            scene.Add(rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Unlit(Base.X, Base.Y, Base.Z)))));
            scene.Fog.Density = 0.07f;
            scene.Fog.Mode = FogMode.Exponential;
            var flat = Plane.ReadAfterRender(rig, scene);
            scene.Fog.Mode = FogMode.Height;
            scene.Fog.HeightFalloff = 0f;
            Assert.Equal(flat, Plane.ReadAfterRender(rig, scene));
        }
    }

    [Fact]
    public void ExemptMaterialsKeepTheirColourAndOthersAreFogged()
    {
        var (rig, scene) = Make(new Vector3(0, 0, 10));
        using (rig)
        {
            // A backdrop layer (a sky plane far behind) and a foreground plane in front of it, both unlit, in a thick fog.
            var sky = scene.Add(rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Unlit(0.1f, 0.9f, 0.1f, half: 50f)))), Matrix4x4.CreateTranslation(0, 0, -40));
            var near = scene.Add(rig.Own(rig.Renderer.Upload(GltfLoader.Load(Plane.Unlit(Base.X, Base.Y, Base.Z, half: 1f)))));
            scene.Fog.Mode = FogMode.Exponential;
            scene.Fog.Density = 0.2f;

            var fogged = Plane.ReadAfterRender(rig, scene);
            var corner = ((4 * Size) + 4) * 4;
            var skyFogged = new Vector3(fogged[corner], fogged[corner + 1], fogged[corner + 2]);
            Assert.True((skyFogged - FogColour).Length() < 0.01f, $"the far plane is fogged away: {skyFogged}");

            sky.Model.Materials[0].FogExempt = true;
            var exempt = Plane.ReadAfterRender(rig, scene);
            var skyClear = new Vector3(exempt[corner], exempt[corner + 1], exempt[corner + 2]);
            Assert.True((skyClear - new Vector3(0.1f, 0.9f, 0.1f)).Length() < 0.004f, $"the exempt plane keeps its colour: {skyClear}");
            AssertAmount(1f - MathF.Exp(-2f), Centre(exempt), "the foreground still fogs");

            sky.Model.Materials[0].FogExempt = false;
            Assert.Equal(fogged, Plane.ReadAfterRender(rig, scene));
            Assert.NotNull(near);
        }
    }

    [Fact]
    public void AnInstanceThatDoesNotCastShadowsIsStillDrawnAndLeavesNoShadow()
    {
        const float OrthoHeight = 16f;
        using var rig = new Rig(128, 128, GpuFormat.Rgba16Float);
        var ground = new Shapes().Ground(0f, 30f, Shapes.Matte(0.8f, 0.8f, 0.8f)).Upload(rig);
        var slab = new Shapes().Box(new Vector3(0, 4f, 0), new Vector3(4f, 0.2f, 4f), Shapes.Matte(0.5f, 0.5f, 0.5f)).Upload(rig);
        var camera = Camera3D.LookAt(new Vector3(0, 30, 0), Vector3.Zero, -Vector3.UnitZ);
        camera.Orthographic = true;
        camera.OrthoHeight = OrthoHeight;
        camera.Near = 0.5f;
        camera.Far = 40f;
        var scene = new Scene3D { Camera = camera, Samples = 1 };
        scene.Add(ground);
        var slabInstance = scene.Add(slab);
        scene.Environment.Intensity = 0f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = Vector3.Zero;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        var dir = Vector3.Normalize(new Vector3(1f, -1f, 0f));
        scene.Lights.Add(new Light3D { Kind = LightKind.Directional, Direction = dir, Intensity = 3f, CastShadows = true });

        // Shadow of the slab lands at x 1.9..6.1; pixel columns are 8 per world unit around 64.
        var shadowed = 64 + (int)(4f * 8f);
        var slabCentre = 64;
        var withShadow = Plane.ReadAfterRender(rig, scene);
        Assert.InRange(withShadow[((64 * 128) + shadowed) * 4 + 1], 0f, 0.02f);

        slabInstance.CastShadows = false;
        var without = Plane.ReadAfterRender(rig, scene);
        var lit = 0.8f / MathF.PI * 3f * dir.Y * -1f;
        Assert.InRange(without[((64 * 128) + shadowed) * 4 + 1], lit * 0.97f, lit * 1.03f);
        var slabTop = 0.5f / MathF.PI * 3f * dir.Y * -1f;
        Assert.InRange(without[((64 * 128) + slabCentre) * 4 + 1], slabTop * 0.97f, slabTop * 1.03f);

        slabInstance.CastShadows = true;
        Assert.Equal(withShadow, Plane.ReadAfterRender(rig, scene));
    }
}
