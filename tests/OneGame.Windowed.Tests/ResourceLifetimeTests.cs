using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.Render3D;
using OneGame.UI;
using RecursiveAI.OneGpu;
using WaterExample;

namespace OneGame.Windowed.Tests;

/// <summary>
/// A game stage that makes every kind of GPU resource its renderers hand out and disposes none of them: the engine must free them when the
/// instance goes, because the device now outlives the instance (a host keeps it between runs).
/// </summary>
internal sealed class HoardingGame(bool disposeThemItself) : GameDefinition
{
    private readonly WaterGame _inner = new();

    public override string Name => "Hoarder";

    public override void RegisterEntities(EntityRegistry registry) => _inner.RegisterEntities(registry);

    public override void Setup(World world) => _inner.Setup(world);

    public override void DefineScreens(ScreenSet screens) => _inner.DefineScreens(screens);

    public override void DefineFonts(OneGame.Text.FontLibrary fonts) => _inner.DefineFonts(fonts);

    public override IAssetSource CreateAssetSource() => new MemorySource();

    public override IRenderStage CreateRenderStage(HostResources resources) => new HoardingStage(resources, _inner.View, disposeThemItself);

    private sealed class MemorySource : IAssetSource
    {
        private static readonly byte[] Png = OneGame.Host.Png.Encode(new CapturedFrame(64, 64, MakePixels(), "test"));

        private static byte[] MakePixels()
        {
            var p = new byte[64 * 64 * 4];
            for (var i = 0; i < p.Length; i++)
            {
                p[i] = (byte)(i * 7);
            }

            return p;
        }

        public byte[] Read(string path, long maxBytes) => Png;

        public string? PhysicalPath(string path) => null;
    }

    private sealed class HoardingStage : Render3DStage
    {
        private readonly HostResources _host;
        private readonly bool _disposeThemItself;
        private readonly List<IDisposable> _made = [];
        private Offscreen3D? _offscreen;
        private EnvironmentMap? _map;
        private int _frames;

        public HoardingStage(HostResources resources, SceneView3D view, bool disposeThemItself)
            : base(resources, view)
        {
            _host = resources;
            _disposeThemItself = disposeThemItself;
        }

        protected override void RendererCreated(Renderer2D renderer)
        {
            var rgba = new byte[128 * 128 * 4];
            Array.Fill(rgba, (byte)200);
            _made.Add(renderer.CreateTexture(128, 128, rgba));
            _made.Add(renderer.CreateRenderTexture(256, 256));
            _made.Add(renderer.CreateTileSet(16, 16, 8, 8, rgba));
            _made.Add(renderer.CreatePostChain());
            if (_host.Assets is { } assets)
            {
                _ = assets.LoadTexture("art/leak.png");
            }
        }

        protected override void RendererCreated(Renderer3D renderer)
        {
            _made.Add(renderer.Upload(PondModels.Buoy));
            _made.Add(renderer.Upload(PondModels.Scenery));
            _map = renderer.CreateEnvironment(HdrImage.CreateSky(64, 32, new Vector3(0.2f, 0.4f, 1f), new Vector3(0.9f, 0.9f, 1f), new Vector3(0.2f)), 32);
            _made.Add(_map);
            _ = renderer.CreateEnvironmentAsync(HdrImage.CreateSky(64, 32, new Vector3(0.1f), new Vector3(0.5f), new Vector3(0.1f)), new EnvironmentLoadOptions { Size = 32 });
            var material = renderer.CreateMaterial("hoard");
            Span<MeshVertex3D> quad =
            [
                new(new Vector3(-1, 0, 0), Vector3.UnitZ, new Vector2(0, 0), Vector4.One, 0),
                new(new Vector3(1, 0, 0), Vector3.UnitZ, new Vector2(1, 0), Vector4.One, 0),
                new(new Vector3(0, 1, 0), Vector3.UnitZ, new Vector2(0, 1), Vector4.One, 0),
            ];
            for (var i = 0; i < 4; i++)
            {
                _made.Add(renderer.CreateMesh(material, quad, [0u, 1u, 2u], "hoard" + i));
            }

            var layers = new ReadOnlyMemory<byte>[] { new byte[16 * 16 * 4], new byte[16 * 16 * 4] };
            renderer.CreateLayeredMaterial("layers", 16, 16, layers);
            _made.Add(renderer.CreateBillboardTexture(32, 32, new byte[32 * 32 * 4]));
            _offscreen = new Offscreen3D(renderer, 96, 64);
            _made.Add(_offscreen);
        }

        protected override void UpdateScene(Scene3D scene, in FrameInfo frame)
        {
            scene.Samples = 1;
            if (_map is not null && _map.IsBuilt)
            {
                scene.Environment.Map = _map;
            }

            if (++_frames == 3 && _offscreen is not null)
            {
                _offscreen.Render(new Scene3D { Samples = 1 });
            }
        }

        public override void Dispose()
        {
            if (_disposeThemItself)
            {
                // A tidy game frees its own things before the stage goes; the engine must cope with that (and free the rest).
                scene3dCleanup();
            }

            base.Dispose();
        }

        private void scene3dCleanup()
        {
            for (var i = _made.Count - 1; i >= 0; i--)
            {
                _made[i].Dispose();
            }

            _made.Clear();
        }
    }
}

/// <summary>An instance that never frees what its renderers made must still leave the device as it found it.</summary>
[Collection("run loops")]
public sealed class ResourceLifetimeTests
{
    private static GpuMemoryStats Run(GpuDevice device, HoardingGame game)
    {
        using (var instance = GameInstance.Create(game, new GameOptions { Headless = true, Width = 320, Height = 200, Gpu = GpuMode.Required, Device = device }))
        {
            instance.Step(6);
            instance.Capture(320, 200);
            instance.Capture(160, 100);
            instance.Step(3);
            Assert.Empty(instance.TakeGpuValidationMessages());
        }

        device.WaitIdle();
        return device.MemoryStats;
    }

    private static GpuDevice Device()
    {
        Environment.SetEnvironmentVariable("ONEGPU_ADAPTER", Environment.GetEnvironmentVariable("ONEGAME_WINDOWED_ADAPTER") is { Length: > 0 } chosen ? chosen : "software");
        return GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, ApplicationName = "ResourceLifetime" });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EightInstancesThatDisposeNothingLeaveTheDeviceAtItsBaseline(bool gameDisposesItself)
    {
        using var device = Device();
        var baseline = device.MemoryStats;
        var after = new List<GpuMemoryStats>();
        for (var i = 0; i < 8; i++)
        {
            after.Add(Run(device, new HoardingGame(gameDisposesItself)));
        }

        Console.WriteLine($"lifetime (game disposes itself: {gameDisposesItself}), device before any run {baseline.UsedBytes} B / {baseline.Allocations}; after each run:");
        for (var i = 0; i < after.Count; i++)
        {
            Console.WriteLine($"  run {i + 1}: {after[i].UsedBytes} B, {after[i].Allocations} allocations, reserved {after[i].ReservedBytes} B, staging {after[i].StagingBytes} B");
        }

        // Flat after the first run: whatever the first run leaves (pooled blocks, one-time caches) must not grow with the next seven.
        var first = after[0];
        for (var i = 1; i < after.Count; i++)
        {
            Assert.True(after[i].UsedBytes <= first.UsedBytes + 65536, $"run {i + 1} holds {after[i].UsedBytes} B, run 1 held {first.UsedBytes} B");
            Assert.True(after[i].Allocations <= first.Allocations + 4, $"run {i + 1} holds {after[i].Allocations} allocations, run 1 held {first.Allocations}");
        }
    }
}
