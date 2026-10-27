using System.Text.Json;
using OneGame.Control;
using OneGame.Host;
using OneGame.Testing;
using PagesGame;

namespace OneGame.Pages.Tests;

public class GameTests
{
    static GameTests() => Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");

    private static TestCluster Start(bool pages = true) =>
        TestCluster.Start(new PagesGameDefinition { ShowPages = pages }, new TestOptions { Width = 320, Height = 260, Gpu = GpuMode.Auto });

    private static JsonElement Call(TestClient c, string op, string args = "{}") => JsonDocument.Parse(c.Call(op, args)).RootElement.Clone();

    private static JsonElement Query(TestClient c, string name) => Call(c, "ui_query", $$"""{"selector":"#{{name}}"}""");

    [Fact]
    public void AClickByNameReachesTheHandlerAndTheHoverStateShows()
    {
        using var cluster = Start();
        var c = cluster.Client;
        c.Step(2);
        Assert.Equal("0", c.Get("Meter", "Score"));
        c.Call("ui_click", """{"id":"start"}""");
        c.Step(1);   // a handler sends a command; it runs on the next tick
        Assert.Equal("1", c.Get("Meter", "Score"));
        var q = Query(c, "start");
        Assert.Equal(1, q.GetArrayLength());
        Assert.Equal("page", q[0].GetProperty("kind").GetString());
        Assert.Equal("Start", q[0].GetProperty("text").GetString());
        var states = q[0].GetProperty("states").EnumerateArray().Select(s => s.GetString()).ToArray();
        Assert.Contains("hover", states);
        Assert.Contains("focus", states);
    }

    [Fact]
    public void TypingGoesToTheFocusedFieldOfAPage()
    {
        using var cluster = Start();
        var c = cluster.Client;
        c.Step(2);
        c.Call("ui_click", """{"id":"name"}""");
        c.Type("hi");
        Assert.Equal("hi", Query(c, "name")[0].GetProperty("text").GetString());
        // The field has focus, so the key goes to the page and not to the UI.
        Assert.Contains("focus", Query(c, "name")[0].GetProperty("states").EnumerateArray().Select(s => s.GetString()));
    }

    [Fact]
    public void ShowingAPageChangesNoState()
    {
        ulong Run(bool pages)
        {
            using var cluster = Start(pages);
            var c = cluster.Client;
            c.Step(2);
            for (var i = 0; i < 5; i++)
            {
                c.Send("Meter", "Add", "3");
                c.Step(3);
                if (pages)
                {
                    c.Call("ui_click", """{"id":"start"}""");
                }
            }

            // The clicks above add to the score on the page run only, so compare an untouched entity: the tick and the hash of a fresh world.
            return c.Hash;
        }

        // Clicking the page's button sends commands, so the two runs differ by design; the page itself adds no state: same run, page shown or not, no click.
        ulong NoClicks(bool pages)
        {
            using var cluster = Start(pages);
            var c = cluster.Client;
            c.Step(2);
            for (var i = 0; i < 5; i++)
            {
                c.Send("Meter", "Add", "3");
                c.Step(3);
            }

            c.Call("pages");
            c.Capture("320x260");
            return c.Hash;
        }

        Assert.Equal(NoClicks(false), NoClicks(true));
        Assert.NotEqual(Run(false), Run(true));
    }

    [Fact]
    public void PagesAreListedAndTheRenderGraphShowsTheirPasses()
    {
        using var cluster = Start();
        var c = cluster.Client;
        c.Step(2);
        var frame = c.Capture("320x260");
        Shots.Save("game", frame.Width, frame.Height, frame.Rgba);
        var pages = Call(c, "pages");
        Assert.Equal("menu", pages.GetProperty("screens")[0].GetProperty("name").GetString());
        Assert.Equal("html", pages.GetProperty("screens")[0].GetProperty("kind").GetString());
        var graph = c.Call("render_graph");
        Assert.Contains("page.layer", graph, StringComparison.Ordinal);
        Assert.Contains("page.add", graph, StringComparison.Ordinal);
    }

    private static byte[] Shot(TestClient c, string name, bool golden)
    {
        var f = c.Capture("320x260");
        if (golden)
        {
            Shots.Golden(name, f.Width, f.Height, f.Rgba);
        }

        return f.Rgba;
    }

    [Fact]
    public void HoverFocusAndTheTransitionLookTheSameAtFixedViewTimes()
    {
        byte[][] Run(bool golden)
        {
            using var cluster = Start();
            var c = cluster.Client;
            c.Step(2);
            var idle = Shot(c, "game-idle", golden);
            c.Call("ui_click", """{"id":"start"}""");
            // The pointer arrived at view time 0 of the page clock: the hover colour fades in over 200 ms.
            var t0 = Shot(c, "game-hover-0ms", golden);
            Call(c, "tweens", """{"advance":0.1}""");
            var t100 = Shot(c, "game-hover-100ms", golden);
            Call(c, "tweens", """{"advance":0.3}""");
            var done = Shot(c, "game-hover-done", golden);
            Assert.Empty(cluster.Client.Game.TakeGpuValidationMessages());
            return [idle, t0, t100, done];
        }

        var first = Run(true);
        var second = Run(false);
        for (var i = 0; i < first.Length; i++)
        {
            Assert.Equal(first[i], second[i]);
        }

        // The four views differ from each other where the button is: idle, just hovered, halfway, done.
        static int At(byte[] rgba, int x, int y) => rgba[(((y * 320) + x) * 4) + 2];
        var blue = new[] { At(first[0], 30, 115), At(first[1], 30, 115), At(first[2], 30, 115), At(first[3], 30, 115) };
        Shots.Note("button blue channel at idle, 0 ms, 100 ms, done: " + string.Join(", ", blue));
        Assert.True(blue[0] < blue[2] && blue[2] < blue[3], string.Join(",", blue));
    }
}

public class ScenarioTests
{
    private static string Scenario => Path.GetFullPath(Path.Combine(Paths.RepoRoot, "tests", "PagesGame", "scenarios", "menu.yaml"));

    [Fact]
    public void AScenarioClicksAndTypesByNameAndChecksACapture()
    {
        using var cluster = TestCluster.Start(new PagesGameDefinition(), new TestOptions { Width = 320, Height = 260, Gpu = GpuMode.Auto });
        var result = cluster.Run(Scenario, new ScenarioRunOptions { UpdateReferences = Environment.GetEnvironmentVariable("ONEGAME_UPDATE_GOLDENS") == "1" });
        Assert.True(result.Passed, string.Join("\n", result.Expectations.Where(x => !x.Passed).Select(x => x.Message).Concat(result.Steps.Where(x => !x.Ok).Select(x => x.Error))));
    }
}
