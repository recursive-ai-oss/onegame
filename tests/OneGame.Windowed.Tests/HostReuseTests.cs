using OneGame.Host;
using OneGame.Platform;
using OneGame.Platform.Tests;
using OneGame.UI;

namespace OneGame.Windowed.Tests;

/// <summary>A <see cref="GameHost"/> keeps the window and device between instances and nothing else.</summary>
[Collection("run loops")]
public sealed class HostReuseTests : IClassFixture<XvfbFixture>
{
    private readonly XvfbFixture _xvfb;

    public HostReuseTests(XvfbFixture xvfb)
    {
        _xvfb = xvfb;
        Environment.SetEnvironmentVariable("ONEGPU_ADAPTER", Environment.GetEnvironmentVariable("ONEGAME_WINDOWED_ADAPTER") is { Length: > 0 } chosen ? chosen : "software");
        Environment.SetEnvironmentVariable("ONEGPU_VALIDATION", "1");
    }

    // A real X server (a vendor driver needs one) can be named in ONEGAME_RUNLOOP_DISPLAY; Xvfb otherwise.
    private string? Display => Environment.GetEnvironmentVariable("ONEGAME_RUNLOOP_DISPLAY") is { Length: > 0 } named ? named : _xvfb.Display;

    private GameOptions Options(GameHost? host) =>
        new() { Width = 320, Height = 200, Gpu = GpuMode.Required, Backend = PlatformBackendKind.X11, DisplayName = Display, Host = host };

    private static void Present(GameInstance game)
    {
        for (var i = 0; i < 300 && game.PresentedFrames < 3; i++)
        {
            game.Step();
            Thread.Sleep(2);
        }

        Assert.True(game.PresentedFrames >= 3, $"only {game.PresentedFrames} frames were presented");
    }

    // The state after exactly 40 frames, however many it took to get the first frames on screen.
    private static ulong HashAt40(GameInstance game)
    {
        Present(game);
        while (game.Frame < 40)
        {
            game.Step();
        }

        Assert.Equal(40, game.Frame);
        return game.World.ComputeHash();
    }

    [Fact]
    public void TheWindowAndDeviceSurviveAnInstanceAndTheNextOneStartsFresh() => MainThread.Run(() =>
    {
        Assert.SkipUnless(Display is not null, "Xvfb is not installed");
        using var host = new GameHost();
        ulong first;
        PlatformApp app;
        Window window;
        string? adapter;
        using (var game = GameInstance.Create(new WindowedGame(), Options(host)))
        {
            first = HashAt40(game);
            app = game.Platform;
            window = game.Window;
            adapter = game.AdapterName;
            Assert.Empty(game.TakeGpuValidationMessages());
        }

        Assert.True(host.HasWindow);
        Assert.True(host.HasDevice);
        Assert.False(window.IsClosed);

        // A second game on the host: same window and connection, but its own world, frame counter and presented count.
        using (var game = GameInstance.Create(new WindowedGame(), Options(host)))
        {
            Assert.Same(app, game.Platform);
            Assert.Same(window, game.Window);
            Assert.Equal(0, game.World.Tick);
            Assert.Equal(0, game.Frame);
            Assert.Equal(0, game.PresentedFrames);
            Assert.Equal(first, HashAt40(game));
            Assert.Equal(adapter, game.AdapterName);
            Assert.Empty(game.TakeGpuValidationMessages());
        }

        // An instance with a window of its own reaches the same state: the hash does not depend on the host.
        using (var alone = GameInstance.Create(new WindowedGame(), Options(null)))
        {
            Assert.Equal(first, HashAt40(alone));
            Assert.Empty(alone.TakeGpuValidationMessages());
        }

        Assert.Equal(2, host.Runs);
    });

    [Fact]
    public void AStaleQuitAndHeldKeysDoNotCarryOver() => MainThread.Run(() =>
    {
        Assert.SkipUnless(Display is not null, "Xvfb is not installed");
        using var host = new GameHost();
        using (var game = GameInstance.Create(new WindowedGame(), Options(host)))
        {
            Present(game);
            game.Platform.Injector.Key(game.Window.Id, Key.W, true);
            game.Platform.RequestQuit();
            game.Step();
            Assert.True(game.QuitRequested);
        }

        using (var game = GameInstance.Create(new WindowedGame(), Options(host)))
        {
            Assert.False(game.QuitRequested);
            Assert.False(game.Platform.Input.IsKeyDown(Key.W));
            Present(game);
            Assert.False(game.QuitRequested);
        }
    });

    [Fact]
    public void AWindowThePlayerClosedIsReplacedAndAFailedStartLeavesTheHostFree() => MainThread.Run(() =>
    {
        Assert.SkipUnless(Display is not null, "Xvfb is not installed");
        using var host = new GameHost();
        Window closed;
        using (var game = GameInstance.Create(new WindowedGame(), Options(host)))
        {
            Present(game);
            closed = game.Window;
            game.Window.Close();
        }

        Assert.False(host.HasWindow);
        Assert.False(host.HasDevice);

        // A game that throws while it starts must not leave the host marked busy.
        Assert.Throws<InvalidOperationException>(() => GameInstance.Create(new NoScreensGame(), Options(host)));
        using var next = GameInstance.Create(new WindowedGame(), Options(host));
        Assert.NotSame(closed, next.Window);
        Present(next);

        // One game at a time.
        Assert.Throws<InvalidOperationException>(() => GameInstance.Create(new WindowedGame(), Options(host)));
        Assert.Throws<InvalidOperationException>(host.Dispose);
    });

    private sealed class NoScreensGame : GameDefinition
    {
        private readonly WindowedGame _inner = new();

        public override string Name => "NoScreens";

        public override void RegisterEntities(OneGame.Core.EntityRegistry registry) => _inner.RegisterEntities(registry);

        public override void Setup(OneGame.Core.World world) => _inner.Setup(world);

        public override void DefineScreens(ScreenSet screens)
        {
        }
    }
}

/// <summary>The same reuse on Windows, where the Windows CI runner checks it: one window and device serve two instances, and the second starts fresh.</summary>
[Collection("run loops")]
public sealed class Win32HostReuseTests
{
    [Fact]
    public void TwoInstancesShareOneWindowAndDeviceAndTheSecondStartsFresh() => MainThread.Run(() =>
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "needs Windows");
        Environment.SetEnvironmentVariable("ONEGPU_ADAPTER", Environment.GetEnvironmentVariable("ONEGAME_WINDOWED_ADAPTER") is { Length: > 0 } chosen ? chosen : "software");
        Environment.SetEnvironmentVariable("ONEGPU_VALIDATION", "1");
        using var host = new GameHost();
        var options = new GameOptions { Width = 320, Height = 200, Gpu = GpuMode.Required, Backend = PlatformBackendKind.Win32, Host = host };
        Window window;
        using (var game = GameInstance.Create(new WindowedGame(), options))
        {
            for (var i = 0; i < 300 && game.PresentedFrames < 3; i++)
            {
                game.Step();
                Thread.Sleep(2);
            }

            Assert.True(game.PresentedFrames >= 3);
            window = game.Window;
        }

        Assert.True(host.HasWindow);
        Assert.True(host.HasDevice);
        using (var game = GameInstance.Create(new WindowedGame(), options))
        {
            Assert.Same(window, game.Window);
            Assert.Equal(0, game.World.Tick);
            for (var i = 0; i < 300 && game.PresentedFrames < 3; i++)
            {
                game.Step();
                Thread.Sleep(2);
            }

            Assert.True(game.PresentedFrames >= 3);
            Assert.Empty(game.TakeGpuValidationMessages());
        }
    });
}
