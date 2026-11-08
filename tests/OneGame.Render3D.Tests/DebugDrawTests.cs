using OneGame.Gpu;

namespace OneGame.Render3D.Tests;

public class DebugDrawTests
{
    private const int W = 240;
    private const int H = 180;

    private static Scene3D Scene()
    {
        var scene = new Scene3D { Camera = Camera3D.LookAt(new Vector3(0, 0, 6), Vector3.Zero, Vector3.UnitY), Samples = 4 };
        scene.Environment.Background = BackgroundMode.Color;
        scene.Environment.BackgroundColor = new Vector3(0.04f);
        scene.Environment.Intensity = 0;
        scene.Post.Bloom = false;
        scene.Post.ToneMapper = ToneMapper.None;
        return scene;
    }

    private static void Fill(DebugDraw3D d, DebugDepth3D mode)
    {
        d.Clear();
        d.Line(new Vector3(-2.5f, 0, -1), new Vector3(2.5f, 0, -1), new Vector4(1, 0.1f, 0, 1), 5, mode: mode);
        d.Line(new Vector3(-2.5f, 0.55f, -1), new Vector3(2.5f, 0.55f, -1), new Vector4(0, 0.5f, 1, 0.5f), 0.12f, DebugSize3D.World, mode);
        d.Arrow(new Vector3(-2.2f, -1.5f, 0), new Vector3(2, 1.6f, -1), new Vector4(0, 1, 0.1f, 1), 3, mode: mode);
        d.Arrow(new Vector3(2, -1.5f, 0), new Vector3(1.3f, -0.8f, 0), new Vector4(1, 0.3f, 1, 0.7f), 0.06f, DebugSize3D.World, mode);
        d.Point(new Vector3(-1.8f, 1.5f, 0), new Vector4(1, 1, 0, 1), 12, mode: mode);
        d.Point(new Vector3(-1.2f, -1.3f, 0), new Vector4(0, 1, 1, 0.5f), 0.3f, DebugSize3D.World, mode);
    }

    [Fact]
    public void DepthTopAndAlphaCaptures()
    {
        using var rig = new Rig(W, H);
        var scene = Scene();
        scene.Add(new Shapes().Box(Vector3.Zero, new Vector3(2), Shapes.Matte(0.4f, 0.5f, 0.6f)).Upload(rig));
        scene.Lights.Add(Light3D.Directional(new Vector3(-0.3f, -0.5f, -1), Vector3.One, 3));
        foreach (var mode in new[] { DebugDepth3D.DepthTested, DebugDepth3D.AlwaysOnTop })
        {
            Fill(scene.Debug, mode);
            var name = mode == DebugDepth3D.DepthTested ? "debug3d-depth" : "debug3d-top";
            var pixels = rig.Capture(scene, name);
            // MSAA edges can choose a different coverage sample on hardware. Interior pixels should agree.
            Reference.Compare(name, pixels, W, H, 0.25, 4, 0.01);
            Assert.Equal(1, rig.Renderer.DebugDrawCalls);
            Assert.Contains(rig.Target.Graph.Describe().Passes, p => p.Name == "forward + debug3d");
        }
    }

    [Fact]
    public void DepthOccludesButTopIgnoresItAndAlphaBlendsInLinearLight()
    {
        using var rig = new Rig(W, H, GpuFormat.Rgba16Float);
        var scene = Scene();
        scene.Samples = 1;
        scene.Add(new Shapes().Box(Vector3.Zero, new Vector3(2), Shapes.Matte(0.4f, 0.5f, 0.6f)).Upload(rig));
        scene.Lights.Add(Light3D.Directional(new Vector3(0, 0, -1), Vector3.One, 3));
        var bare = Plane.ReadAfterRender(rig, scene);
        var center = ((H / 2 * W) + W / 2) * 4;
        scene.Debug.Line(new Vector3(-3, 0, -1), new Vector3(3, 0, -1), new Vector4(1, 0, 0, 0.5f), 8);
        var tested = Plane.ReadAfterRender(rig, scene);
        for (var c = 0; c < 3; c++) Assert.Equal(bare[center + c], tested[center + c]);
        scene.Debug.Clear();
        scene.Debug.Line(new Vector3(-3, 0, -1), new Vector3(3, 0, -1), new Vector4(1, 0, 0, 0.5f), 8, mode: DebugDepth3D.AlwaysOnTop);
        var top = Plane.ReadAfterRender(rig, scene);
        Assert.InRange(top[center], bare[center] * 0.5f + 0.495f, bare[center] * 0.5f + 0.505f);
        Assert.InRange(top[center + 1], bare[center + 1] * 0.5f - 0.005f, bare[center + 1] * 0.5f + 0.005f);
    }

    [Fact]
    public void TenThousandLinesUseTwoDrawsAndNoSteadyFrameAllocations()
    {
        using var rig = new Rig(64, 48);
        var scene = Scene();
        scene.Samples = 1;
        void FillLines()
        {
            scene.Debug.Clear();
            for (var i = 0; i < 10000; i++)
            {
                var y = (i % 100) * 0.03f - 1.5f;
                scene.Debug.Line(new Vector3(-2, y, 0), new Vector3(2, y, 0), new Vector4(1, 0, 0, 0.01f), mode: i < 5000 ? DebugDepth3D.DepthTested : DebugDepth3D.AlwaysOnTop);
            }
        }
        for (var i = 0; i < 12; i++) { FillLines(); rig.Target.Render(scene); }
        rig.Device.WaitIdle();
        rig.Target.Render(scene);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 20; i++) { FillLines(); rig.Target.Render(scene); }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Equal(10000, scene.Debug.Count);
        Assert.Equal(2, rig.Renderer.DebugDrawCalls);
        scene.Debug.Clear();
        rig.Target.Render(scene);
        Assert.Equal(0, rig.Renderer.DebugDrawCalls);
    }

    [Fact]
    public void InvalidPrimitivesAreRejectedAndClearReusesTheList()
    {
        var d = new DebugDraw3D();
        Assert.Throws<ArgumentOutOfRangeException>(() => d.Line(Vector3.Zero, Vector3.One, Vector4.One, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => d.Point(new Vector3(float.NaN), Vector4.One));
        Assert.Throws<ArgumentOutOfRangeException>(() => d.Arrow(Vector3.Zero, Vector3.One, new Vector4(1, 1, 1, 2)));
        d.Point(Vector3.Zero, Vector4.One);
        var copy = new DebugDraw3D();
        copy.Append(d);
        d.Clear();
        Assert.Equal(1, copy.Count);
        Assert.Equal(0, d.Count);
    }
}
