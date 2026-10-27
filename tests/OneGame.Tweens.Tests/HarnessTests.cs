using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using OneGame.Control;
using OneGame.Host;
using TweenGame;

// Each GPU test makes its own device; real drivers are not happy with several at once.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace OneGame.Tweens.Tests;

public class HarnessTests
{
    private static string SourceDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    private static string Scenarios => Path.GetFullPath(Path.Combine(SourceDir(), "..", "TweenGame", "scenarios"));

    private static TestCluster Start(bool animate = true, GpuMode gpu = GpuMode.Off) =>
        TestCluster.Start(new TweenGameDefinition { Animate = animate }, new TestOptions { Width = 480, Height = 270, Gpu = gpu });

    private static JsonElement Call(TestClient c, string op, string args = "{}") => JsonDocument.Parse(c.Call(op, args)).RootElement.Clone();

    [Fact]
    public void TheTweensOpListsStyleTransitionsAndMovesViewTime()
    {
        using var cluster = Start();
        var c = cluster.Client;
        c.Step(2);
        Assert.Equal(0, Call(c, "tweens").GetProperty("active").GetInt32());

        c.Click("toggle");
        var listed = Call(c, "tweens");
        Assert.Equal(3, listed.GetProperty("active").GetInt32());
        var labels = listed.GetProperty("tweens").EnumerateArray().Select(t => t.GetProperty("label").GetString()).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(["Panel#panel background-color", "Panel#panel opacity", "Panel#panel width"], labels);
        Assert.All(listed.GetProperty("tweens").EnumerateArray(), t => Assert.Equal(0.2, t.GetProperty("duration").GetDouble(), 1e-9));

        var panel = c.Find("panel")!;
        var before = Call(c, "tweens").GetProperty("time").GetDouble();
        var half = Call(c, "tweens", "{\"advance\":0.1}");
        Assert.Equal(0.1, half.GetProperty("time").GetDouble() - before, 1e-6);
        Assert.InRange(float.Parse(panel.Computed.Get("opacity")!, System.Globalization.CultureInfo.InvariantCulture), 0.5f, 0.76f);

        var settled = Call(c, "tweens", "{\"settle\":true}");
        Assert.Equal(0, settled.GetProperty("active").GetInt32());
        Assert.Equal("0.5", panel.Computed.Get("opacity"));
        Assert.Equal(320f, panel.Bounds.Width);

        var bad = Assert.Throws<ControlException>(() => c.Call("tweens", "{\"advance\":700}"));
        Assert.Equal("bad_args", bad.Code);
    }

    [Fact]
    public void PausingAndCancellingGoThroughTheOp()
    {
        using var cluster = Start();
        var c = cluster.Client;
        c.Step(1);
        c.Click("toggle");
        Call(c, "tweens", "{\"paused\":true}");
        Call(c, "tweens", "{\"advance\":5}");
        Assert.Equal(3, Call(c, "tweens").GetProperty("active").GetInt32());
        c.Step(30);
        Assert.Equal(3, Call(c, "tweens").GetProperty("active").GetInt32());
        Call(c, "tweens", "{\"paused\":false,\"time_scale\":0.5}");
        var r = Call(c, "tweens", "{\"advance\":0.1}");
        Assert.Equal(0.5, r.GetProperty("timeScale").GetDouble());
        Assert.Equal(3, r.GetProperty("active").GetInt32());
        Assert.Equal(0, Call(c, "tweens", "{\"cancel_all\":true}").GetProperty("active").GetInt32());
    }

    [Fact]
    public void ASteppedRunMovesTheViewClockOneTickAtATime()
    {
        using var cluster = Start();
        var c = cluster.Client;
        c.Step(1);
        var t0 = Call(c, "tweens").GetProperty("time").GetDouble();
        c.Step(60);
        Assert.Equal(1.0, Call(c, "tweens").GetProperty("time").GetDouble() - t0, 1e-6);
    }

    [Fact]
    public void ACommandThatChangesTheScoreStartsANamedPulseFromViewCode()
    {
        using var cluster = Start();
        var c = cluster.Client;
        c.Step(2);
        c.Send("Meter", "Add", "5");
        c.Step(2);
        var t = Assert.Single(Call(c, "tweens").GetProperty("tweens").EnumerateArray());
        Assert.Equal("pulse", t.GetProperty("name").GetString());
        Assert.Equal("Score 5", c.Find("score")!.Text);
    }

    [Fact]
    public void TweensNeverChangeTheSimulation()
    {
        // The same commands and clicks, with every animation on and with every animation off, plus extra view-time jumps in the first run:
        // the state hash is the same at every tick.
        static List<ulong> Run(bool animate, bool jumpTime, out int maxActive)
        {
            using var cluster = Start(animate);
            var c = cluster.Client;
            var hashes = new List<ulong>();
            var peak = 0;
            for (var tick = 0; tick < 240; tick++)
            {
                if (tick % 50 == 10)
                {
                    c.Click("toggle");
                }

                if (tick % 37 == 5)
                {
                    c.Send("Meter", "Add", (tick % 7 + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                if (jumpTime && tick % 11 == 0)
                {
                    c.Call("tweens", "{\"advance\":0.07}");
                }

                c.Step(1);
                peak = Math.Max(peak, c.Game.Tweens.ActiveCount);
                hashes.Add(c.Hash);
            }

            maxActive = peak;
            return hashes;
        }

        var animated = Run(true, true, out var peakA);
        var plain = Run(false, false, out var peakB);
        Assert.True(peakA >= 3, $"expected tweens to be running, peak was {peakA}");
        Assert.Equal(0, peakB);
        Assert.Equal(plain, animated);
    }

    [Fact]
    public void ReplayingTheRecordingWithTweensRunningKeepsTheHashes()
    {
        var report = Determinism.Replay(() => new TweenGameDefinition(), (client, frame) =>
        {
            if (frame % 40 == 3)
            {
                client.Click("toggle");
            }

            if (frame % 30 == 7)
            {
                client.Send("Meter", "Add", "2");
            }
        }, 200, new TestOptions { Gpu = GpuMode.Off });
        Assert.True(report.Identical, report.ToString());
    }

    [Fact]
    public void TheScenarioStepsAndCapturesAtFixedPointsPass()
    {
        Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
        using var cluster = Start(gpu: GpuMode.Required);
        var update = Environment.GetEnvironmentVariable("ONEGAME_UPDATE_REFERENCES") == "1";
        var result = cluster.Run(Path.Combine(Scenarios, "01-toggle.yaml"), new ScenarioRunOptions { BaseDirectory = Scenarios, ArtifactsDirectory = Path.Combine(cluster.OutputDirectory, "art"), UpdateReferences = update });
        Assert.True(result.Passed, string.Join("\n", result.Expectations.Where(e => !e.Passed).Select(e => e.Message)) + string.Join("\n", result.Steps.Where(st => !st.Ok).Select(st => st.Error)));
        Assert.Empty(cluster.Client.Game.TakeGpuValidationMessages());
    }

    [Fact]
    public void CapturesAlongATransitionAreIdenticalAcrossRunsAndShowTheMotion()
    {
        static List<string> Series()
        {
            using var cluster = Start(gpu: GpuMode.Required);
            var c = cluster.Client;
            c.Step(2);
            var hashes = new List<string> { Hash(c) };
            c.Click("toggle");
            foreach (var dt in new[] { 0.04, 0.05, 0.05, 0.1 })
            {
                c.Call("tweens", "{\"advance\":" + dt.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
                hashes.Add(Hash(c));
            }

            return hashes;
        }

        static string Hash(TestClient c) => Convert.ToHexString(SHA256.HashData(c.Capture("480x270").Rgba));

        var first = Series();
        var second = Series();
        Assert.Equal(first, second);
        // Before, three points mid-way and the end are five different frames.
        Assert.Equal(first.Count, first.Distinct().Count());
    }
}
