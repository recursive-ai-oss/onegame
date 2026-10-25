using System.Runtime.InteropServices;
using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

/// <summary>
/// Image-based lighting against CPU references. The environment is two flat tones, bright above the horizon and dark below, for which the
/// diffuse convolution has a closed form and the GGX prefilter can be integrated by brute force in double precision.
/// </summary>
public class IblTests
{
    private static readonly Vector3 Up = new(2.0f, 1.5f, 1.0f);
    private static readonly Vector3 Down = new(0.25f, 0.2f, 0.1f);

    private static HdrImage TwoTone()
    {
        const int w = 256;
        const int h = 128;
        var rgb = new float[w * h * 3];
        for (var y = 0; y < h; y++)
        {
            var c = y < h / 2 ? Up : Down;
            for (var x = 0; x < w; x++)
            {
                var o = ((y * w) + x) * 3;
                rgb[o] = c.X;
                rgb[o + 1] = c.Y;
                rgb[o + 2] = c.Z;
            }
        }

        return new HdrImage(w, h, rgb);
    }

    // The cube face selection table of the Vulkan specification (16.5.4), inverted: the direction of texel (u, v in -1..1).
    internal static Vector3 CubeDir(int face, float u, float v) => Vector3.Normalize(face switch
    {
        0 => new Vector3(1, -v, -u),
        1 => new Vector3(-1, -v, u),
        2 => new Vector3(u, 1, v),
        3 => new Vector3(u, -1, -v),
        4 => new Vector3(u, -v, 1),
        _ => new Vector3(-u, -v, -1),
    });

    private static Vector3[] ReadFace(GpuDevice device, GpuTexture cube, uint mip, uint face)
    {
        var size = Math.Max(cube.Width >> (int)mip, 1);
        var bytes = new byte[size * size * 8];
        device.ReadTexture(cube, mip, face, bytes);
        var half = MemoryMarshal.Cast<byte, Half>(bytes.AsSpan());
        var result = new Vector3[size * size];
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = new Vector3((float)half[i * 4], (float)half[(i * 4) + 1], (float)half[(i * 4) + 2]);
        }

        return result;
    }

    private static Vector3 Radiance(Vector3 dir) => dir.Y >= 0 ? Up : Down;

    [Fact]
    public void IrradianceMatchesTheClosedForm()
    {
        using var rig = new Rig(16, 16);
        var env = rig.Own(rig.Renderer.CreateEnvironment(TwoTone()));
        rig.Renderer.PrepareEnvironment(env);
        Assert.True(env.IsBuilt);
        var worst = 0.0;
        for (var face = 0; face < 6; face++)
        {
            var texels = ReadFace(rig.Device, env.Irradiance, 0, (uint)face);
            for (var y = 0; y < 32; y++)
            {
                for (var x = 0; x < 32; x++)
                {
                    var n = CubeDir(face, ((x + 0.5f) / 32 * 2) - 1, ((y + 0.5f) / 32 * 2) - 1);

                    // E / pi for a normal with n.y = c is Lup (1 + c) / 2 + Ldown (1 - c) / 2.
                    var want = (Up * (1 + n.Y) / 2) + (Down * (1 - n.Y) / 2);
                    var got = texels[(y * 32) + x];
                    var err = Math.Abs((got - want).X) / Up.X;
                    worst = Math.Max(worst, err);
                    Assert.True(err < 0.03, $"face {face} texel ({x},{y}) normal {n}: {got} against {want}");
                }
            }
        }

        Assert.True(worst > 0, "the irradiance cube was never written");
    }

    // Brute-force GGX prefilter for N = V = R: sum of L(l) n.l over GGX-distributed half vectors, exactly as the shader defines it, in double precision.
    private static Vector3 ReferencePrefilter(Vector3 n, double roughness, int samples)
    {
        var alpha = roughness * roughness;
        var up = Math.Abs(n.Y) < 0.999f ? Vector3.UnitY : Vector3.UnitX;
        var t = Vector3.Normalize(Vector3.Cross(up, n));
        var b = Vector3.Cross(n, t);
        double sx = 0, sy = 0, sz = 0, wsum = 0;
        for (var i = 0; i < samples; i++)
        {
            var xi1 = (i + 0.5) / samples;
            var xi2 = RadicalInverse((uint)i);
            var phi = 2 * Math.PI * xi1;
            var cosT = Math.Sqrt((1 - xi2) / (1 + (((alpha * alpha) - 1) * xi2)));
            var sinT = Math.Sqrt(Math.Max(1 - (cosT * cosT), 0));
            var h = (t * (float)(sinT * Math.Cos(phi))) + (b * (float)(sinT * Math.Sin(phi))) + (n * (float)cosT);
            var l = (2 * Vector3.Dot(n, h) * h) - n;
            var ndl = Vector3.Dot(n, l);
            if (ndl > 0)
            {
                var c = Radiance(l);
                sx += c.X * ndl;
                sy += c.Y * ndl;
                sz += c.Z * ndl;
                wsum += ndl;
            }
        }

        return new Vector3((float)(sx / wsum), (float)(sy / wsum), (float)(sz / wsum));
    }

    private static double RadicalInverse(uint bits)
    {
        bits = (bits << 16) | (bits >> 16);
        bits = ((bits & 0x55555555) << 1) | ((bits & 0xAAAAAAAA) >> 1);
        bits = ((bits & 0x33333333) << 2) | ((bits & 0xCCCCCCCC) >> 2);
        bits = ((bits & 0x0F0F0F0F) << 4) | ((bits & 0xF0F0F0F0) >> 4);
        bits = ((bits & 0x00FF00FF) << 8) | ((bits & 0xFF00FF00) >> 8);
        return bits * 2.3283064365386963e-10;
    }

    [Fact]
    public void PrefilteredMipsMatchABruteForceGgxIntegral()
    {
        using var rig = new Rig(16, 16);
        var env = rig.Own(rig.Renderer.CreateEnvironment(TwoTone(), 128));
        rig.Renderer.PrepareEnvironment(env);
        var mips = env.SpecularMipCount;
        Assert.Equal(8, mips);

        // Mip 0 is the sharp environment; the others are roughness mip / (count - 1).
        var rows = new List<string>();
        foreach (var mip in new[] { 0u, 2u, 4u, 6u })
        {
            var roughness = mip / (double)(mips - 1);
            var size = Math.Max(128 >> (int)mip, 1);
            var tested = 0;
            for (var face = 0; face < 6; face++)
            {
                var texels = ReadFace(rig.Device, env.Specular, mip, (uint)face);
                for (var y = 0; y < size; y += Math.Max(size / 4, 1))
                {
                    for (var x = 0; x < size; x += Math.Max(size / 4, 1))
                    {
                        var dir = CubeDir(face, ((x + 0.5f) / size * 2) - 1, ((y + 0.5f) / size * 2) - 1);
                        if (mip == 0 && Math.Abs(dir.Y) < 0.05f)
                        {
                            continue;   // a texel straddling the horizon is a blend of the two tones
                        }

                        var want = mip == 0 ? Radiance(dir) : ReferencePrefilter(dir, roughness, 20000);
                        var got = texels[(y * size) + x];
                        var err = Math.Abs(got.X - want.X) / Up.X;
                        rows.Add($"mip {mip} face {face} ({x},{y}) {dir}: {got.X:F3} vs {want.X:F3}");
                        Assert.True(err < 0.045, $"mip {mip} (roughness {roughness:F2}) face {face} texel ({x},{y}) direction {dir}: {got} against {want}");
                        tested++;
                    }
                }
            }

            Assert.True(tested > 0);
        }
    }

    [Fact]
    public void EnvironmentBrdfTableMatchesTheIntegral()
    {
        using var rig = new Rig(16, 16);
        var env = rig.Own(rig.Renderer.CreateEnvironment(TwoTone(), 32));
        rig.Renderer.PrepareEnvironment(env);
        var lut = rig.Renderer.BrdfLut;
        var bytes = new byte[128 * 128 * 8];
        rig.Device.ReadTexture(lut, 0, 0, bytes);
        var half = MemoryMarshal.Cast<byte, Half>(bytes.AsSpan());
        foreach (var (x, y) in new[] { (10, 10), (64, 64), (120, 20), (30, 100), (100, 120), (5, 60) })
        {
            var nv = (x + 0.5) / 128;
            var rough = (y + 0.5) / 128;
            var (a, b) = ReferenceLut(nv, rough);
            var ga = (float)half[((y * 128) + x) * 4];
            var gb = (float)half[(((y * 128) + x) * 4) + 1];
            Assert.True(Math.Abs(ga - a) < 0.02 && Math.Abs(gb - b) < 0.02, $"n.v {nv:F2} roughness {rough:F2}: ({ga:F3}, {gb:F3}) against ({a:F3}, {b:F3})");
        }
    }

    private static (double A, double B) ReferenceLut(double nv, double roughness)
    {
        var alpha = roughness * roughness;
        var a2 = alpha * alpha;
        var v = new[] { Math.Sqrt(1 - (nv * nv)), 0, nv };
        double sa = 0, sb = 0;
        const int n = 20000;
        for (var i = 0; i < n; i++)
        {
            var xi1 = (i + 0.5) / n;
            var xi2 = RadicalInverse((uint)i);
            var phi = 2 * Math.PI * xi1;
            var cosT = Math.Sqrt((1 - xi2) / (1 + ((a2 - 1) * xi2)));
            var sinT = Math.Sqrt(Math.Max(1 - (cosT * cosT), 0));
            var h = new[] { sinT * Math.Cos(phi), sinT * Math.Sin(phi), cosT };
            var vh = (v[0] * h[0]) + (v[1] * h[1]) + (v[2] * h[2]);
            var l = new[] { (2 * vh * h[0]) - v[0], (2 * vh * h[1]) - v[1], (2 * vh * h[2]) - v[2] };
            var nl = Math.Clamp(l[2], 0, 1);
            var nh = Math.Clamp(h[2], 0, 1);
            var vhc = Math.Clamp(vh, 0, 1);
            if (nl > 0)
            {
                var gv = nl * Math.Sqrt((nv * nv * (1 - a2)) + a2);
                var gl = nv * Math.Sqrt((nl * nl * (1 - a2)) + a2);
                var vis = 0.5 / Math.Max(gv + gl, 1e-8);
                var gVis = vis * 4 * nl * vhc / Math.Max(nh, 1e-8);
                var fc = Math.Pow(1 - vhc, 5);
                sa += (1 - fc) * gVis;
                sb += fc * gVis;
            }
        }

        return (sa / n, sb / n);
    }
}
