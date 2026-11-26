using OneGame.Atlas;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.Text;
using OneGame.UI;
using PhysicsPourExample;
using RecursiveAI.OneGpu;

namespace OneGame.Physics.Tests;

/// <summary>The pour example as a whole game: the same state hashes however it is run, split, restored or replayed, and the view draws without allocating.</summary>
public class PourGameTests
{
    private static readonly TestOptions Options = new() { Gpu = GpuMode.Off };

    // Open the gate on frame 50, with the liquid still settling, and again later (ignored: it is open).
    private static void Drive(TestClient client, int frame)
    {
        if (frame is 50 or 200)
        {
            client.Send("Jug", "Pour");
        }
    }

    [Fact]
    public void TwoRunsGiveTheSameHashEveryTick()
    {
        var report = Determinism.RunTwice(() => new PourGame(), Drive, 400, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Theory]
    [InlineData(30)]  // liquid settling, gate shut
    [InlineData(70)]  // the stream leaving the jug
    [InlineData(150)] // hitting the basin
    [InlineData(300)] // pooling
    public void SnapshotAndRestoreGivesTheSameHashes(int split)
    {
        var report = Determinism.SnapshotRestore(() => new PourGame(), Drive, 400, split, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Fact]
    public void ReplayOfTheRecordingReproducesTheHashes()
    {
        var report = Determinism.Replay(() => new PourGame(), Drive, 400, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Fact]
    public void SteadyFramesDrawingEveryParticleAllocateNothing()
    {
        var definition = new PourGame();
        var registry = new EntityRegistry();
        definition.RegisterEntities(registry);
        var world = new World(1, registry);
        definition.RegisterSystems(world);
        definition.Setup(world);
        var fonts = new FontLibrary();
        definition.DefineFonts(fonts);

        using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        using var stage = new Render2DStage(new HostResources(fonts, new FontTextMeasurer(fonts), new ArtAtlas(), GeneratedShaders.Instance));
        using var target = device.CreateTexture(new TextureDesc(320, 180, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
        var list = new DisplayList();
        var jug = world.Entities.OfType<Jug>().Single();
        jug.Send.Pour();

        void Frame()
        {
            world.Step();
            stage.Render(device, target, new FrameInfo(320, 180, 1, world.Tick, list, world, Fixed.Half));
        }

        for (var i = 0; i < 20; i++)
        {
            Frame();
        }

        device.WaitIdle();
        Frame();

        long total = 0;
        var particles = 0;
        for (var i = 0; i < 60; i++)
        {
            world.Step();
            var before = GC.GetAllocatedBytesForCurrentThread();
            stage.Render(device, target, new FrameInfo(320, 180, 1, world.Tick, list, world, Fixed.Half));
            total += GC.GetAllocatedBytesForCurrentThread() - before;
            particles = Math.Max(particles, PhysicsSpace2D.Of(world)!.FluidParticleCount);
        }

        device.WaitIdle();
        var messages = device.TakeValidationMessages();
        Assert.True(messages.Length == 0, string.Join("\n", messages));
        TestContext.Current.SendDiagnosticMessage($"60 frames drawing up to {particles} particles: {total} B allocated");
        Assert.True(particles > 900);
        Assert.True(total == 0, $"60 steady frames allocated {total} bytes");
    }
}
