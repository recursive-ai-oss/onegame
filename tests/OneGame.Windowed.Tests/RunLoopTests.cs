using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.Platform.Tests;
using OneGame.UI;
using WaterExample;

namespace OneGame.Windowed.Tests;

/// <summary>
/// Wraps the water example (a 3D scene with a planar reflection) and records what a game loop that enters and leaves several times cares
/// about: when the first frame of each run was presented, how long the frames of its first second took, and what the graphics layer said.
/// </summary>
internal sealed class ProbeGame(WaterGame inner, Stopwatch clock) : GameDefinition
{
    private long _lastFrameAt = -1;
    private long _firstPresentedAt = -1;
    private long _lastCallbackAt;

    public long FirstPresentedMs => _firstPresentedAt;

    public double MaxFrameMs { get; private set; }

    public long LastCallbackMs => _lastCallbackAt;

    public int ValidationMessages { get; private set; }

    public string? FirstValidation { get; private set; }

    public string? Adapter { get; private set; }

    public override string Name => inner.Name;

    public override ulong Seed => inner.Seed;

    public override void RegisterEntities(EntityRegistry registry) => inner.RegisterEntities(registry);

    public override void Setup(World world) => inner.Setup(world);

    public override void DefineScreens(ScreenSet screens) => inner.DefineScreens(screens);

    public override IRenderStage CreateRenderStage(HostResources resources) => inner.CreateRenderStage(resources);

    public override void OnEachFrame(GameInstance game)
    {
        var now = clock.ElapsedMilliseconds;
        _lastCallbackAt = now;
        if (_firstPresentedAt < 0 && game.PresentedFrames >= 1)
        {
            _firstPresentedAt = now;
            _lastFrameAt = now;
        }
        else if (_firstPresentedAt >= 0)
        {
            // The first second after the first frame: a frame that stalls shows here.
            if (now - _firstPresentedAt <= 1000)
            {
                MaxFrameMs = Math.Max(MaxFrameMs, now - _lastFrameAt);
            }

            _lastFrameAt = now;
        }

        foreach (var message in game.TakeGpuValidationMessages())
        {
            ValidationMessages++;
            FirstValidation ??= message;
        }

        Adapter = game.AdapterName;
    }
}

/// <summary>
/// A game that calls <see cref="GameApp.Run(GameDefinition, string[])"/> several times in one process, as a game with a menu loop and a
/// match loop does, must get its next window up quickly and must leave quickly. The measurements run in a child process because the
/// exit itself is one of them.
/// </summary>
[Collection("run loops")]
public sealed class RunLoopTests : IClassFixture<XvfbFixture>, IClassFixture<WestonFixture>
{
    private readonly XvfbFixture _xvfb;
    private readonly WestonFixture _weston;

    public RunLoopTests(XvfbFixture xvfb, WestonFixture weston)
    {
        _xvfb = xvfb;
        _weston = weston;
    }

    /// <summary>The child half. Prints one JSON object per run and a final line, then returns (the parent times the process exit).</summary>
    internal static int RunChild(string backend, string display, int runs, bool sameDefinition, string extra)
    {
        // Cocoa windows belong to the main thread, so the runs happen there.
        var code = 0;
        MainThread.Run(() => code = RunChildOnThisThread(backend, display, runs, sameDefinition, extra));
        return code;
    }

    private static int RunChildOnThisThread(string backend, string display, int runs, bool sameDefinition, string extra)
    {
        var clock = Stopwatch.StartNew();
        var shared = new WaterGame();
        for (var i = 0; i < runs; i++)
        {
            var probe = new ProbeGame(sameDefinition ? shared : new WaterGame(), clock);
            var args = new List<string> { "--backend", backend, "--frames", "90" };
            if (display != "-")
            {
                args.Add("--display");
                args.Add(display);
            }

            args.AddRange(extra.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            var start = clock.ElapsedMilliseconds;
            var code = GameApp.Run(probe, [.. args], Console.Out, Console.Error);
            var end = clock.ElapsedMilliseconds;
            var line = string.Create(CultureInfo.InvariantCulture,
                $$"""{"run":{{i + 1}},"code":{{code}},"firstFrameMs":{{probe.FirstPresentedMs - start}},"maxFrameMs":{{probe.MaxFrameMs:F1}},"teardownMs":{{end - probe.LastCallbackMs}},"adapter":"{{probe.Adapter}}","validation":{{probe.ValidationMessages}}}""");
            Console.WriteLine(line);
            if (probe.FirstValidation is { } message)
            {
                Console.Error.WriteLine("validation: " + message);
            }

        }

        // A Mac game releases the host itself: the process-exit handler leaves macOS windows alone. The test can leave that call out to see what exit does then.
        if (Environment.GetEnvironmentVariable("ONEGAME_CHILD_NO_RELEASE") is not { Length: > 0 })
        {
            GameHost.Release();
        }

        Console.WriteLine("done");
        return 0;
    }

    private static async Task<(JsonElement[] Runs, long ExitMs, string Errors)> RunChildAsync(string backend, string display, int runs, bool sameDefinition, string extra = "", string? adapter = null)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var a in new[] { "--child-runs", backend, display, runs.ToString(CultureInfo.InvariantCulture), sameDefinition ? "same" : "new", extra.Length == 0 ? "-" : extra })
        {
            start.ArgumentList.Add(a);
        }

        start.Environment["ONEGPU_ADAPTER"] = adapter ?? Environment.GetEnvironmentVariable("ONEGAME_WINDOWED_ADAPTER") ?? "software";
        start.Environment["ONEGPU_VALIDATION"] = "1";
        if (backend == "x11")
        {
            start.Environment.Remove("WAYLAND_DISPLAY");
        }

        using var child = Process.Start(start)!;
        var cancel = TestContext.Current.CancellationToken;
        var error = child.StandardError.ReadToEndAsync(cancel);

        // The parent notes when "done" arrives, so the time the process then takes to leave is its own measurement.
        var clock = Stopwatch.StartNew();
        var lines = new List<string>();
        var doneAt = -1L;
        var reader = Task.Run(() =>
        {
            while (child.StandardOutput.ReadLine() is { } line)
            {
                lines.Add(line);
                if (line == "done")
                {
                    doneAt = clock.ElapsedMilliseconds;
                }
            }
        }, CancellationToken.None);
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        limit.CancelAfter(TimeSpan.FromSeconds(180));
        try
        {
            await child.WaitForExitAsync(limit.Token);
        }
        catch (OperationCanceledException)
        {
            child.Kill(true);
            Assert.Fail($"the child did not exit\n{string.Join('\n', lines)}\n{await error}");
        }

        var exitedAt = clock.ElapsedMilliseconds;
        await reader;
        var text = string.Join('\n', lines);
        var errors = await error;
        Assert.True(child.ExitCode == 0, $"exit code {child.ExitCode}\n{text}\n{errors}");
        Assert.Contains("done", text, StringComparison.Ordinal);
        var runsJson = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(l => l.StartsWith('{')).Select(l => JsonDocument.Parse(l).RootElement).ToArray();

        return (runsJson, exitedAt - doneAt, errors);
    }

    private static void Report(string label, JsonElement[] runs, long exitMs)
    {
        foreach (var r in runs)
        {
            Console.WriteLine($"{label}: {r}");
        }

        Console.WriteLine($"{label}: process exit after the last run {exitMs} ms");
    }

    private static void AssertArrivals(JsonElement[] runs, int count, int firstFrameLimit, int maxFrameLimit, int teardownLimit)
    {
        Assert.Equal(count, runs.Length);
        for (var i = 0; i < runs.Length; i++)
        {
            var r = runs[i];

            // The bars are for real graphics hardware; the software rasterizer is several times slower per frame and shares a loaded machine.
            var software = r.GetProperty("adapter").GetString()!.Contains("llvmpipe", StringComparison.OrdinalIgnoreCase);
            var firstLimit = software ? firstFrameLimit * 2 : firstFrameLimit;
            var frameLimit = software ? maxFrameLimit * 4 : maxFrameLimit;

            Assert.Equal(0, r.GetProperty("code").GetInt32());
            Assert.Equal(0, r.GetProperty("validation").GetInt32());
            Assert.True(r.GetProperty("teardownMs").GetInt64() < teardownLimit, $"run {i + 1} teardown: {r}");
            if (i > 0)
            {
                Assert.True(r.GetProperty("firstFrameMs").GetInt64() <= firstLimit, $"run {i + 1} first frame: {r}");
                Assert.True(r.GetProperty("maxFrameMs").GetDouble() <= frameLimit, $"run {i + 1} frame times: {r}");
            }
        }
    }

    /// <summary>Three runs on X11: the later ones present within the arrival bar, and the process leaves promptly.</summary>
    [Fact]
    public async Task ThreeRunsOnX11ArriveFastAndTheProcessLeavesPromptly()
    {
        var display = Environment.GetEnvironmentVariable("ONEGAME_RUNLOOP_DISPLAY") is { Length: > 0 } named ? named : _xvfb.Display;
        Assert.SkipUnless(OperatingSystem.IsLinux() && display is not null, "Xvfb is not installed");
        var (runs, exitMs, errors) = await RunChildAsync("x11", display!, 3, sameDefinition: false);
        Report("x11", runs, exitMs);
        AssertArrivals(runs, 3, 250, 50, 1000);
        Assert.True(exitMs < 1000, $"the process took {exitMs} ms to exit");
        Assert.DoesNotContain("validation:", errors, StringComparison.Ordinal);
    }

    /// <summary>The same on Wayland, so the reuse is not an X11 special case.</summary>
    [Fact]
    public async Task ThreeRunsOnWaylandArriveFastAndTheProcessLeavesPromptly()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() && _weston.Available, "weston is not installed");
        var (runs, exitMs, errors) = await RunChildAsync("wayland", _weston.SocketPath!, 3, sameDefinition: false);
        Report("wayland", runs, exitMs);
        AssertArrivals(runs, 3, 250, 50, 1000);
        Assert.True(exitMs < 1000, $"the process took {exitMs} ms to exit");
        Assert.DoesNotContain("validation:", errors, StringComparison.Ordinal);
    }

    /// <summary>One definition object used for every run arrives as fast as a new one each time.</summary>
    [Fact]
    public async Task OneDefinitionRunThreeTimesArrivesAsFastAsNewOnes()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() && _xvfb.Available, "Xvfb is not installed");
        var (runs, exitMs, _) = await RunChildAsync("x11", _xvfb.Display!, 3, sameDefinition: true);
        Report("x11-same", runs, exitMs);
        AssertArrivals(runs, 3, 250, 50, 1000);
        Assert.True(exitMs < 1000, $"the process took {exitMs} ms to exit");
    }

    /// <summary>The same on Cocoa: three runs with reuse, and the main thread releasing the host before the process leaves.</summary>
    [Fact]
    public async Task ThreeRunsOnCocoaArriveFastAndTheProcessLeavesPromptly()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        var (runs, exitMs, errors) = await RunChildAsync("cocoa", "-", 3, sameDefinition: false);
        Report("cocoa", runs, exitMs);
        AssertArrivals(runs, 3, 250, 50, 1000);
        Assert.True(exitMs < 1000, $"the process took {exitMs} ms to exit");
        Assert.DoesNotContain("validation:", errors, StringComparison.Ordinal);
    }

    /// <summary>Cocoa with no <see cref="GameHost.Release"/> call: the process must still leave, whatever the exit path does on macOS.</summary>
    [Fact]
    public async Task TheProcessLeavesAfterCocoaRunsWithoutAReleaseCall()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "needs macOS");
        Environment.SetEnvironmentVariable("ONEGAME_CHILD_NO_RELEASE", "1");
        try
        {
            var (runs, exitMs, errors) = await RunChildAsync("cocoa", "-", 3, sameDefinition: false);
            Report("cocoa-no-release", runs, exitMs);
            AssertArrivals(runs, 3, 250, 50, 1000);
            Assert.True(exitMs < 1000, $"the process took {exitMs} ms to exit");
            Assert.DoesNotContain("validation:", errors, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ONEGAME_CHILD_NO_RELEASE", null);
        }
    }
}
