namespace OneGame.Render3D.Tests;

/// <summary>
/// Punctual lights against the analytic answer. A Lambert plane (white albedo 0.8, no specular) lit by one light, rendered to a 16-bit float
/// target with no tone curve, no environment light and no bloom: the value of each pixel is albedo / pi * (light radiometry) * cos(theta),
/// which the test recomputes from the KHR_lights_punctual formulas in double precision for every pixel.
/// </summary>
public class LightTests
{
    private const float Albedo = 0.8f;
    private const int Size = 64;
    private const float OrthoHeight = 8f;

    private static double Expected(Light3D light, Vector3 p)
    {
        double cosTheta;
        double irradiance;
        if (light.Kind == LightKind.Directional)
        {
            var toLight = -Vector3.Normalize(light.Direction);
            cosTheta = toLight.Z;
            irradiance = light.Intensity;
        }
        else
        {
            var d = light.Position - p;
            var dist = d.Length();
            cosTheta = d.Z / dist;
            irradiance = light.Intensity / ((double)dist * dist);
            if (light.Range > 0)
            {
                irradiance *= Math.Clamp(1.0 - Math.Pow(dist / light.Range, 4), 0, 1);
            }

            if (light.Kind == LightKind.Spot)
            {
                var cosOuter = Math.Cos(light.OuterConeAngle);
                var cosInner = Math.Cos(light.InnerConeAngle);
                var scale = 1.0 / Math.Max(0.001, cosInner - cosOuter);
                var cd = Vector3.Dot(Vector3.Normalize(light.Direction), -d / dist);
                var a = Math.Clamp((cd * scale) - (cosOuter * scale), 0, 1);
                irradiance *= a * a;
            }
        }

        return cosTheta <= 0 ? 0 : Albedo / Math.PI * irradiance * cosTheta;
    }

    private static (double MaxError, double MaxValue) Check(Light3D light, string name)
    {
        var (rig, scene) = Plane.Setup(Plane.Lambert(Albedo), Size, OrthoHeight);
        using (rig)
        {
            scene.Lights.Add(light);
            rig.Target.Render(scene);
            var pixels = Plane.ReadLinear(rig);
            double maxErr = 0;
            double maxVal = 0;
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var p = Plane.PixelToWorld(x, y, Size, Size, OrthoHeight);
                    var want = Expected(light, p);
                    var got = pixels[((y * Size) + x) * 4];
                    // Half-float storage and the single-precision shader give about 1e-3 relative accuracy.
                    var err = Math.Abs(got - want) - (0.003 * want);
                    maxErr = Math.Max(maxErr, err);
                    maxVal = Math.Max(maxVal, want);
                    Assert.True(err < 0.002, $"{name}: pixel ({x},{y}) at {p}: rendered {got:F4}, analytic {want:F4}");
                    Assert.Equal(got, pixels[(((y * Size) + x) * 4) + 1], 3);
                }
            }

            return (maxErr, maxVal);
        }
    }

    [Fact]
    public void DirectionalLightIsUniformAndFollowsCosine()
    {
        var (_, v1) = Check(Light3D.Directional(new Vector3(0, 0, -1), Vector3.One, 2f), "directional straight on");
        Assert.InRange(v1, 0.5, 0.52);
        var (_, v2) = Check(Light3D.Directional(new Vector3(1, 0, -1), Vector3.One, 2f), "directional at 45 degrees");
        Assert.InRange(v2, 0.35, 0.37);
    }

    [Fact]
    public void PointLightFallsOffWithTheSquareOfDistance()
    {
        var (_, v) = Check(Light3D.Point(new Vector3(0.5f, -0.5f, 2f), Vector3.One, 10f), "point");
        Assert.InRange(v, 0.5, 0.7);
    }

    [Fact]
    public void RangeWindowFadesLightToZeroAtTheRange()
    {
        var light = Light3D.Point(new Vector3(0, 0, 2f), Vector3.One, 12f, 4f);
        Check(light, "ranged point");

        // At and beyond the range there is no light at all.
        var (rig, scene) = Plane.Setup(Plane.Lambert(Albedo), Size, OrthoHeight);
        using (rig)
        {
            scene.Lights.Add(light);
            rig.Target.Render(scene);
            var pixels = Plane.ReadLinear(rig);
            for (var x = 0; x < Size; x++)
            {
                var p = Plane.PixelToWorld(x, 0, Size, Size, OrthoHeight);
                if ((light.Position - p).Length() >= 4f)
                {
                    Assert.Equal(0f, pixels[x * 4]);
                }
            }
        }
    }

    [Fact]
    public void SpotLightFollowsTheCone()
    {
        var spot = Light3D.Spot(new Vector3(0, 0, 3f), new Vector3(0, 0, -1), Vector3.One, 20f, 15f * MathF.PI / 180f, 35f * MathF.PI / 180f);
        var (_, v) = Check(spot, "spot");
        Assert.InRange(v, 0.5, 0.6);

        // Outside the outer cone (35 degrees from the axis, so beyond radius 3 tan 35 = 2.1) nothing is lit.
        var (rig, scene) = Plane.Setup(Plane.Lambert(Albedo), Size, OrthoHeight);
        using (rig)
        {
            scene.Lights.Add(spot);
            rig.Target.Render(scene);
            var pixels = Plane.ReadLinear(rig);
            Assert.Equal(0f, pixels[0]);
            Assert.True(pixels[((Size / 2) * Size + (Size / 2)) * 4] > 0.4f);
        }
    }

    [Fact]
    public void LightColourAndSeveralLightsAdd()
    {
        var (rig, scene) = Plane.Setup(Plane.Lambert(Albedo), Size, OrthoHeight);
        using (rig)
        {
            scene.Lights.Add(Light3D.Directional(new Vector3(0, 0, -1), new Vector3(1, 0.5f, 0.25f), 1f));
            scene.Lights.Add(Light3D.Point(new Vector3(0, 0, 4f), new Vector3(0, 1, 0), 8f));
            rig.Target.Render(scene);
            var pixels = Plane.ReadLinear(rig);
            var i = ((Size / 2 * Size) + (Size / 2)) * 4;
            var p = Plane.PixelToWorld(Size / 2, Size / 2, Size, Size, OrthoHeight);
            var d = new Vector3(0, 0, 4f) - p;
            var point = Albedo / Math.PI * 8 / d.LengthSquared() * (d.Z / d.Length());
            var sun = Albedo / Math.PI;
            Assert.Equal(sun * 1.0, pixels[i], 2);
            Assert.Equal((sun * 0.5) + point, pixels[i + 1], 2);
            Assert.Equal(sun * 0.25, pixels[i + 2], 2);
        }
    }
}
