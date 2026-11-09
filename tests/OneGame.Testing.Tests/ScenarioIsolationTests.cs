using System.Text.Json;
using OneGame.Host;
using OneGame.Live;
using OneGame.UI;

namespace OneGame.Testing.Tests;

public sealed class ScenarioIsolationTests
{
    private static TestOptions Options() => new() { Width = 320, Height = 200, Gpu = GpuMode.Off };

    [Theory]
    [InlineData("flag")]
    [InlineData("binding")]
    [InlineData("display")]
    [InlineData("visibility")]
    public void VisibilityIncludesAncestorsInScenariosAndControl(string mode)
    {
        var definition = new VisibilityGame(mode);
        using var cluster = TestCluster.Start(definition, Options());
        Check(false);
        definition.Shown = true;
        Check(true);
        definition.Shown = false;
        Check(false);

        void Check(bool visible)
        {
            var scenario = ScenarioParser.Parse($"expect:\n  - ui: {{ id: child, visible: {visible.ToString().ToLowerInvariant()} }}\n");
            var result = cluster.Run(scenario);
            Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
            using var query = JsonDocument.Parse(cluster.Client.Call("ui_query", """{"selector":"#child"}"""));
            var child = query.RootElement[0];
            var hidden = child.TryGetProperty("states", out var states) && states.EnumerateArray().Any(s => s.GetString() == "hidden");
            Assert.Equal(!visible, hidden);
            Assert.True(cluster.Client.Game.Ui.Find("child")!.IsVisible);
        }
    }

    [Fact]
    public void ScenarioHookRestoresViewDefaultsBeforeBuildingTheScreen()
    {
        var definition = new SettingsGame();
        using var cluster = TestCluster.Start(definition, Options());
        var first = cluster.Run(ScenarioParser.Parse("""
            steps:
              - invoke: change
              - reset: 1
            expect:
              - ui: {id: setting, text: changed}
            """));
        Assert.True(first.Passed, JsonSerializer.Serialize(first, ScenarioJson.Default.ScenarioResult));
        Assert.Equal(1, definition.Starts);
        var second = cluster.Run(ScenarioParser.Parse("""
            expect:
              - ui: {id: setting, text: default}
              - ui: {id: built-setting, text: default}
            """));
        Assert.True(second.Passed, JsonSerializer.Serialize(second, ScenarioJson.Default.ScenarioResult));
        Assert.Equal(2, definition.Starts);
        Assert.Equal(first.Hash, second.Hash);
    }

    [Fact]
    public void ScenarioDirectoryResetsViewSettingsBetweenFiles()
    {
        var directory = Path.Combine(Path.GetTempPath(), "scenario-isolation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "10-change.yaml"), """
                name: change
                steps:
                  - invoke: change
                expect:
                  - ui: {id: setting, text: changed}
                """);
            File.WriteAllText(Path.Combine(directory, "20-default.yaml"), """
                name: defaults
                expect:
                  - ui: {id: setting, text: default}
                  - ui: {id: built-setting, text: default}
                """);
            var definition = new SettingsGame();
            Dev.Enable();
            var output = new StringWriter();
            var exit = GameApp.Run(definition,
                ["--headless", "--gpu", "off", "--scenario", directory, "--artifacts", directory, "--json"], output, TextWriter.Null);
            Assert.True(exit == 0, output.ToString());
            Assert.Equal(2, definition.Starts);
            using var result = JsonDocument.Parse(output.ToString());
            Assert.Equal(2, result.RootElement.GetProperty("passed").GetInt32());
            Assert.Equal(0, result.RootElement.GetProperty("failed").GetInt32());
            var scenarios = result.RootElement.GetProperty("scenarios");
            Assert.Equal(scenarios[0].GetProperty("hash").GetString(), scenarios[1].GetProperty("hash").GetString());
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private sealed class VisibilityGame(string mode) : ShopGame
    {
        public bool Shown { get; set; }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ =>
        {
            var child = Ui.Label("child").Named("child").Style(s => s.Visibility(VisibilityMode.Visible));
            var parent = Ui.Column(Ui.Column(child)).Named("parent");
            switch (mode)
            {
                case "flag":
                    parent.OnUpdate(e => e.IsVisible = Shown);
                    break;
                case "binding":
                    parent.BindVisible(() => Shown);
                    break;
                case "display":
                    parent.OnUpdate(e => e.Style(s => s.Display(Shown ? DisplayMode.Flex : DisplayMode.None)));
                    break;
                case "visibility":
                    parent.OnUpdate(e => e.Style(s => s.Visibility(Shown ? VisibilityMode.Visible : VisibilityMode.Hidden)));
                    break;
            }

            return parent;
        });
    }

    private sealed class SettingsGame : ShopGame
    {
        private string _setting = "default";
        public int Starts { get; private set; }

        public override void OnScenarioStarting(GameInstance game)
        {
            _setting = "default";
            Starts++;
        }

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(
            Ui.Label(() => _setting).Named("setting"),
            Ui.Label(_setting).Named("built-setting"),
            Ui.Button("change", () => _setting = "changed").Named("change")));
    }
}
