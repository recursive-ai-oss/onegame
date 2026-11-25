using OneGame.Gpu.Graph;
using RecursiveAI.OneGpu;

namespace OneGame.Render3D.Tests;

/// <summary>
/// Light shafts: off means no pass and the same pixels, the shaft is where the light gets through and dark where a wall blocks it, spot
/// cones and render scale work, the quality ladder bounds the cost, and steady frames allocate nothing.
/// </summary>
public class ShaftTests
{
    private const int W = 320;
    private const int H = 180;

    /// <summary>A wall with three slits at z = 0 and the sun behind it: the light comes through as slabs and crosses the air in front.</summary>
    internal static (Rig Rig, Scene3D Scene) SunThroughSlit(int width = W, int height = H, bool shafts = true)
    {
        var rig = new Rig(width, height);
        var scene = ShaftScene.Slit(ShaftScene.SlitShapes().Upload(rig), shafts);
        return (rig, scene);
    }

    private static string[] LivePasses(Rig rig) => [.. rig.Target.Graph.Describe().Passes.Where(p => !p.Culled).Select(p => p.Name)];

    /// <summary>A dark floor with a pillar and a spot light high above, pointing down at it: a cone of air, cut by the pillar's shadow.</summary>
    internal static (Rig Rig, Scene3D Scene) SpotOverFloor(bool shadows = true)
    {
        var rig = new Rig(W, H);
        var model = new Shapes()
            .Ground(0f, 40f, Shapes.Matte(0.5f, 0.5f, 0.5f))
            .Box(new Vector3(0.6f, 1.5f, 0f), new Vector3(0.8f, 3f, 0.8f), Shapes.Matte(0.6f, 0.3f, 0.25f))
            .Upload(rig);
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(4.5f, 2f, 6f), new Vector3(0.5f, 1.8f, 0f), Vector3.UnitY) };
        scene.Add(model);
        scene.Environment.Intensity = 0.1f;
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = new Vector3(0.01f, 0.012f, 0.02f);
        scene.Post.Bloom = false;
        var spot = Light3D.Spot(new Vector3(-4f, 5f, 3f), Vector3.Normalize(new Vector3(4.6f, -3.5f, -3f)), new Vector3(1f, 0.95f, 0.85f), 250f, 0.12f, 0.3f);
        spot.CastShadows = shadows;
        spot.Volumetric = true;
        scene.Lights.Add(spot);
        scene.Volumetric.Enabled = true;
        scene.Volumetric.Density = 0.05f;
        scene.Volumetric.Intensity = 6f;
        return (rig, scene);
    }

    private static (int X, int Y) Pixel(Scene3D scene, Vector3 world, int width = W, int height = H)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), scene.Camera.View * scene.Camera.Projection(width / (float)height));
        return ((int)((clip.X / clip.W * 0.5f + 0.5f) * width), (int)((0.5f - clip.Y / clip.W * 0.5f) * height));
    }

    /// <summary>Mean over a small patch of the sum of the three colour channels (0..765).</summary>
    private static double Patch(byte[] px, (int X, int Y) at, int width = W, int height = H)
    {
        double sum = 0;
        var n = 0;
        for (var y = Math.Max(at.Y - 2, 0); y <= Math.Min(at.Y + 2, height - 1); y++)
        {
            for (var x = Math.Max(at.X - 2, 0); x <= Math.Min(at.X + 2, width - 1); x++)
            {
                var i = ((y * width) + x) * 4;
                sum += px[i] + px[i + 1] + px[i + 2];
                n++;
            }
        }

        return sum / n;
    }

    // Wall pixels whose view ray crosses the slab (left wall) and ones whose ray stays in the wall's shadow (right wall).
    private static readonly Vector3 CrossesBeam = new(-5f, 2f, 0.2f);
    private static readonly Vector3 MissesBeam = new(5f, 2f, 0.2f);

    // From the right of the wall, looking across it: rays to the left part of the wall cross the slabs, rays to the right part do not.
    private static void SideView(Scene3D scene) => scene.Camera = Camera3D.LookAt(new Vector3(8f, 2.2f, 3.5f), new Vector3(0.5f, 1.8f, 1.5f), Vector3.UnitY);

    [Fact]
    public void OffAddsNoPassAndKeepsThePixels()
    {
        var (rig, scene) = SunThroughSlit(shafts: false);
        using (rig)
        {
            var plain = rig.Capture(scene, "shafts_off");
            var passes = LivePasses(rig);
            Assert.DoesNotContain("light_shafts", passes);
            Assert.DoesNotContain("light_shafts_composite", passes);
            Assert.DoesNotContain("depth_normal_prepass", passes);
            Assert.False(rig.Renderer.LastStats.Volumetric);
            Reference.Compare("shafts_off", plain, W, H, meanTolerance: 0.6, maxTolerance: 16);

            // Enabled with no light opted in, a light with no intensity, a level that has none, or no density: still the same frame.
            scene.Volumetric.Enabled = true;
            Assert.Equal(plain, rig.Capture(scene));
            scene.Lights[0].Volumetric = true;
            scene.Lights[0].VolumetricIntensity = 0f;
            Assert.Equal(plain, rig.Capture(scene));
            scene.Lights[0].VolumetricIntensity = 1f;
            scene.Volumetric.Density = 0f;
            Assert.Equal(plain, rig.Capture(scene));
            scene.Volumetric.Density = 0.03f;
            scene.Quality = QualityLevel.Low;
            var low = rig.Capture(scene);
            Assert.DoesNotContain("light_shafts", LivePasses(rig));
            scene.Lights[0].Volumetric = false;
            Assert.Equal(low, rig.Capture(scene));
            Assert.Equal(0, QualitySettings.For(QualityLevel.Low).VolumetricSteps);
        }
    }

    [Fact]
    public void LightComesThroughTheSlitAndTheWallLeavesItsShadowDark()
    {
        var (rig, scene) = SunThroughSlit();
        using (rig)
        {
            var on = rig.Capture(scene, "shafts_slit");
            var passes = LivePasses(rig);
            Assert.Contains("light_shafts", passes);
            Assert.True(passes.ToList().IndexOf("depth_normal_prepass") < passes.ToList().IndexOf("light_shafts"));
            Assert.True(passes.ToList().IndexOf("forward") < passes.ToList().IndexOf("light_shafts_composite"));
            Assert.True(passes.ToList().IndexOf("light_shafts_composite") < passes.ToList().IndexOf("tonemap"));
            Assert.True(rig.Renderer.LastStats.Volumetric);
            Reference.Compare("shafts_slit", on, W, H, meanTolerance: 0.6, maxTolerance: 16);

            SideView(scene);
            on = rig.Capture(scene, "shafts_side");
            scene.Volumetric.Enabled = false;
            var off = rig.Capture(scene);
            var crossed = Pixel(scene, CrossesBeam);
            var missed = Pixel(scene, MissesBeam);
            var gain = Patch(on, crossed) - Patch(off, crossed);
            var leak = Patch(on, missed) - Patch(off, missed);
            Assert.True(gain > 40, $"air in the beam adds {gain} (sum of channels)");
            Assert.True(leak < 2, $"air in the wall's shadow adds {leak}");
        }
    }

    [Fact]
    public void FogLinkFadesTheShaftWhereTheFogHasTakenOver()
    {
        var (rig, scene) = SunThroughSlit();
        using (rig)
        {
            SideView(scene);
            scene.Fog.Enabled = true;
            scene.Fog.Color = new Vector3(0.02f, 0.025f, 0.03f);
            scene.Fog.Start = 1f;
            scene.Fog.End = 3f;
            var crossed = Pixel(scene, CrossesBeam);
            var unlinked = Patch(rig.Capture(scene), crossed);
            scene.Volumetric.LinkToFog = true;
            var linked = Patch(rig.Capture(scene), crossed);
            scene.Volumetric.Enabled = false;
            var off = Patch(rig.Capture(scene), crossed);
            Assert.True(unlinked - off > 40, $"without the link the beam adds {unlinked - off}");
            Assert.True(linked - off < 4, $"with the link the fogged-out beam adds {linked - off}");
        }
    }

    [Fact]
    public void ASpotLightShowsItsConeAndThePillarCutsAShaftOutOfIt()
    {
        var (rig, scene) = SpotOverFloor();
        using (rig)
        {
            var on = rig.Capture(scene, "shafts_spot");
            Reference.Compare("shafts_spot", on, W, H, meanTolerance: 0.6, maxTolerance: 16);
            scene.Volumetric.Enabled = false;
            var off = rig.Capture(scene);

            // Inside the cone near the lamp; far outside it, top right.
            var inside = (X: 70, Y: 40);
            var outside = (X: 290, Y: 30);
            Assert.True(Patch(on, inside) - Patch(off, inside) > 30, $"cone adds {Patch(on, inside) - Patch(off, inside)}");
            Assert.True(Patch(on, outside) - Patch(off, outside) < 2, $"outside the cone adds {Patch(on, outside) - Patch(off, outside)}");
        }
    }

    [Fact]
    public void AnUnshadowedSpotFillsItsWholeConeEvenBehindTheBlocker()
    {
        var (rig, scene) = SpotOverFloor();
        using (rig)
        {
            var shadowed = rig.Capture(scene);
            scene.Lights[0].CastShadows = false;
            var open = rig.Capture(scene, "shafts_spot_open");
            Assert.NotEqual(shadowed, open);

            // In the pillar's shadow (beside it, seen from the camera) the shadowed cone is darker than the open one.
            var behind = (X: 205, Y: 85);
            Assert.True(Patch(open, behind) > Patch(shadowed, behind), $"open {Patch(open, behind)} shadowed {Patch(shadowed, behind)}");
        }
    }

    [Fact]
    public void HalfRenderScaleStillShowsTheBeamAtHalfSize()
    {
        var (rig, scene) = SunThroughSlit();
        using (rig)
        {
            var full = rig.Capture(scene);
            scene.RenderScale = 0.5f;
            var half = rig.Capture(scene, "shafts_slit_half");
            Reference.Compare("shafts_slit_half", half, W, H, meanTolerance: 0.6, maxTolerance: 16);
            var graph = rig.Target.Graph.Describe();
            Assert.Contains(graph.Passes, p => p.Name == "light_shafts" && !p.Culled);
            Assert.Equal("80x45", graph.Resources.First(r => r.Name == "shafts_scatter").Size);   // half of 160x90, at the High level
            SideView(scene);
            full = rig.Capture(scene);
            scene.RenderScale = 0.5f;
            half = rig.Capture(scene);
            var crossed = Pixel(scene, CrossesBeam);
            Assert.True(Patch(half, crossed) > 0.7 * Patch(full, crossed) - 20, $"half {Patch(half, crossed)} full {Patch(full, crossed)}");
            var missed = Pixel(scene, MissesBeam);
            Assert.True(Math.Abs(Patch(half, missed) - Patch(full, missed)) < 30);
        }
    }

    [Fact]
    public void TheLevelsBoundTheWorkAndLowHasNone()
    {
        var (rig, scene) = SunThroughSlit();
        using (rig)
        {
            var sizes = new Dictionary<QualityLevel, string?>();
            foreach (var level in new[] { QualityLevel.Low, QualityLevel.Medium, QualityLevel.High, QualityLevel.Ultra })
            {
                scene.Quality = level;
                rig.Target.Render(scene);
                sizes[level] = rig.Target.Graph.Describe().Resources.FirstOrDefault(r => r.Name == "shafts_scatter")?.Size;
                Assert.Equal(level != QualityLevel.Low, rig.Renderer.LastStats.Volumetric);
            }

            Assert.Null(sizes[QualityLevel.Low]);
            Assert.Equal("80x45", sizes[QualityLevel.Medium]);    // a quarter of 320x180
            Assert.Equal("160x90", sizes[QualityLevel.High]);
            Assert.Equal("160x90", sizes[QualityLevel.Ultra]);
            var steps = new[] { QualityLevel.Medium, QualityLevel.High, QualityLevel.Ultra }.Select(l => QualitySettings.For(l).VolumetricSteps).ToArray();
            Assert.True(steps[0] < steps[1] && steps[1] < steps[2]);
        }
    }

    [Fact]
    public void MoreOptedInLightsThanTheBudgetAreIgnoredAndNothingBreaks()
    {
        var (rig, scene) = SunThroughSlit();
        using (rig)
        {
            for (var i = 0; i < Renderer3D.MaxVolumetricLights + 3; i++)
            {
                scene.Lights.Add(new Light3D { Kind = LightKind.Point, Position = new Vector3(i, 3f, 3f), Intensity = 20f, Range = 8f, Volumetric = true });
            }

            rig.Target.Render(scene);
            Assert.True(rig.Renderer.LastStats.Volumetric);
        }
    }

    [Fact]
    public void ShaftFramesAllocateNothingWhileTheSettingsChange()
    {
        var (rig, scene) = SunThroughSlit();
        using (rig)
        {
            for (var i = 0; i < 12; i++)
            {
                rig.Target.Render(scene);
            }

            rig.Device.WaitIdle();
            rig.Target.Render(scene);
            long allocated = 0;
            for (var i = 0; i < 40; i++)
            {
                scene.Volumetric.Density = 0.02f + (i * 0.0005f);
                scene.Lights[0].VolumetricIntensity = 1f + (i * 0.01f);
                var a0 = GC.GetAllocatedBytesForCurrentThread();
                rig.Target.Render(scene);
                allocated += GC.GetAllocatedBytesForCurrentThread() - a0;
            }

            rig.Device.WaitIdle();
            Assert.True(allocated == 0, $"40 steady shaft frames allocated {allocated} bytes");
        }
    }

    /// <summary>
    /// GPU time of the shaft passes at 1080p per quality level (four cascades, 4x MSAA, SSAO), printed for the record. Runs only with
    /// ONEGAME_SHAFT_TIMING=1: a timing run is for the 4070, not for every test pass.
    /// </summary>
    [Fact]
    public void GpuCostPerLevel()
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_SHAFT_TIMING") != "1")
        {
            return;
        }

        var (rig, scene) = SunThroughSlit(1920, 1080);
        using (rig)
        {
            if (!rig.Device.Capabilities.Timestamps)
            {
                return;
            }

            scene.Ssao.Enabled = true;
            var line = new System.Text.StringBuilder(rig.Device.Adapter.Name + " 1920x1080 (median over frames, ms):");
            var passes = new List<PassTime>(64);
            foreach (var level in new[] { QualityLevel.Low, QualityLevel.Medium, QualityLevel.High, QualityLevel.Ultra })
            {
                scene.Quality = level;
                var seen = new HashSet<int>();
                var march = new List<double>();
                var composite = new List<double>();
                var frame = new List<double>();
                for (var i = 0; i < 260; i++)
                {
                    rig.Target.Render(scene);
                    var index = rig.Renderer.ReadGpuPassTimes(passes);
                    if (i >= 20 && index > 0 && seen.Add(index))
                    {
                        march.Add(passes.FirstOrDefault(p => p.Name == "light_shafts").Microseconds / 1000.0);
                        composite.Add(passes.FirstOrDefault(p => p.Name == "light_shafts_composite").Microseconds / 1000.0);
                        if (rig.Renderer.LastStats.GpuFrameMs is { } ms)
                        {
                            frame.Add(ms);
                        }
                    }
                }

                rig.Device.WaitIdle();
                static double Median(List<double> v) => v.Count == 0 ? double.NaN : v.Order().ElementAt(v.Count / 2);
                line.Append(System.Globalization.CultureInfo.InvariantCulture, $" {level}: march {Median(march):F3} + composite {Median(composite):F3} (frame {Median(frame):F2});");
            }

            Console.WriteLine(line);
            if (Paths.CaptureDir is { } dir)
            {
                Directory.CreateDirectory(dir);
                File.AppendAllText(Path.Combine(dir, "shafttimes.txt"), line + "\n");
            }
        }
    }

    [Fact]
    public void SettingsClampBadValues()
    {
        var v = new VolumetricSettings { Density = float.NaN, Intensity = -3f, Anisotropy = 7f, MaxDistance = float.PositiveInfinity };
        Assert.Equal(0f, v.Density);
        Assert.Equal(0f, v.Intensity);
        Assert.Equal(0.95f, v.Anisotropy);
        Assert.Equal(60f, v.MaxDistance);
        Assert.Equal(0f, new Light3D { VolumetricIntensity = float.NaN }.VolumetricIntensity);
    }
}
