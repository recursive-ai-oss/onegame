using System.Text.Json;
using OneGame.Control;
using OneGame.Host;
using OneGame.Testing;
using OneGame.UI.Kit.Tests.Guide;

namespace OneGame.UI.Kit.Tests;

/// <summary>Controls made of parts keep their label in a child; inspection and scenarios must still read it from the control.</summary>
public sealed class ComposedTextTests
{
    private static string TextOf(Screen s, Element e)
    {
        using var doc = JsonDocument.Parse(s.Ui.DumpJson(e, false, false));
        return doc.RootElement.TryGetProperty("text", out var t) ? t.GetString()! : "";
    }

    [Fact]
    public void ButtonsTogglesRadiosTabsAndDropdownOptionsReportTheirLabel()
    {
        var s = new Screen();
        var pick = 1;
        var on = false;
        var label = "Sell all";
        var tabs = UiKit.Tabs([new TabPage("Goods", Ui.Label("g")), new TabPage("Ships", Ui.Label("s"))], () => 0, _ => { }, "tabs");
        s.Root.With(
            UiKit.Button("Buy", () => { }, "buy"),
            UiKit.Button(() => label, () => { }, "dynamic"),
            UiKit.Toggle("Auto-sell", () => on, v => on = v, "auto"),
            UiKit.RadioGroup([new Choice<int>(0, "Low"), new Choice<int>(1, "Mid")], () => pick, v => pick = v, "quality"),
            tabs);
        s.Settle();

        Assert.Equal("Buy", TextOf(s, s.Get("buy")));
        Assert.Equal("Sell all", TextOf(s, s.Get("dynamic")));
        label = "Sell some";
        s.Settle();
        Assert.Equal("Sell some", TextOf(s, s.Get("dynamic")));
        Assert.Equal("Auto-sell", TextOf(s, s.Get("auto")));
        Assert.Equal("Mid", TextOf(s, s.Get("quality/1")));
        Assert.Equal("Ships", TextOf(s, s.Get("tabs/tab/1")));
    }

    [Fact]
    public void ScenarioTextExpectationsReadTheButtonLabel()
    {
        using var game = GameInstance.Create(new MarketGame(), new GameOptions { Headless = true, Width = 640, Height = 360, Gpu = GpuMode.Off });
        var surface = new ControlSurface(game, Path.GetTempPath());
        var scenario = ScenarioParser.Parse("steps:\n  - step: 2\nexpect:\n  - ui: {id: buy, text: Buy}\n  - ui: {id: buy, contains: Bu}\n", "inline.yaml", "label");
        var result = new ScenarioRunner(surface).Run(scenario);
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
    }
}
