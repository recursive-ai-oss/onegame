using System.Text.Json;
using OneGame.Control;
using OneGame.Host;
using OneGame.Testing;
using PagesGame;

namespace OneGame.Pages.Tests;

public class GameTests
{
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
}
