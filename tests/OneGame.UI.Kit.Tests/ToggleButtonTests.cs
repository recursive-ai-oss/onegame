using System.Text.Json;
using OneGame.Control;
using OneGame.Core;
using OneGame.Host;
using OneGame.Testing;

namespace OneGame.UI.Kit.Tests;

/// <summary>A game with a "FAST" toggle button: the pressed state lives in the kit, not in a game-side rule.</summary>
public sealed class FastGame : GameDefinition
{
    private KitHost? _kit;

    public bool Fast { get; set; }

    public override string Name => "FastGame";

    public override void RegisterEntities(EntityRegistry registry)
    {
    }

    public override void Setup(World world)
    {
    }

    public override void Style(UiTree ui) => _kit = new KitHost(ui);

    public override void DefineScreens(ScreenSet screens) =>
        screens.Add("main", _ => _kit!.AttachTo(Ui.Row(
            UiKit.ToggleButton("FAST", () => Fast, v => Fast = v, "fast"),
            UiKit.Button("Slow", () => { }, "slow")).Padding(16).Gap(8)));
}

public sealed class ToggleButtonTests
{
    private static JsonElement Dump(Screen s, string id)
    {
        using var doc = JsonDocument.Parse(s.Ui.DumpJson(s.Get(id), false, false));
        return doc.RootElement.Clone();
    }

    [Fact]
    public void ActivatingToggleButtonFlipsItsStateClassAndInspectJson()
    {
        var s = new Screen();
        var fast = false;
        s.Root.With(UiKit.ToggleButton("FAST", () => fast, v => fast = v, "fast"), UiKit.Button("Slow", () => { }, "slow"));
        s.Settle();

        var e = s.Get("fast");
        Assert.Equal(false, e.Pressed);
        Assert.DoesNotContain("pressed", e.Classes);
        Assert.False(Dump(s, "fast").GetProperty("pressed").GetBoolean());
        // A plain button is not a toggle: no pressed field at all.
        Assert.False(Dump(s, "slow").TryGetProperty("pressed", out _));
        Assert.Null(s.Get("slow").Pressed);

        s.Click(e);
        Assert.True(fast);
        Assert.True(e.Pressed);
        Assert.Contains("pressed", e.Classes);
        Assert.True(Dump(s, "fast").GetProperty("pressed").GetBoolean());
        Assert.Equal("FAST", Dump(s, "fast").GetProperty("text").GetString());

        // The keyboard does the same, and the state follows the game's value when the game changes it.
        s.Key("Enter");
        s.Settle();
        Assert.False(fast);
        fast = true;
        s.Settle();
        Assert.True(Dump(s, "fast").GetProperty("pressed").GetBoolean());
    }

    [Fact]
    public void ADisabledToggleStaysSwitchedOnAndDoesNotRespondToClicks()
    {
        var s = new Screen();
        var on = true;
        s.Root.With(UiKit.ToggleButton("FAST", () => on, v => on = v, "fast"));
        s.Settle();
        var e = s.Get("fast");
        e.IsEnabled = false;
        s.Settle();
        s.Click(e);
        Assert.True(on);
        Assert.True(Dump(s, "fast").GetProperty("pressed").GetBoolean());
        Assert.Contains("disabled", Dump(s, "fast").GetProperty("states").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public void ScenariosReadAndDriveThePressedState()
    {
        using var game = GameInstance.Create(new FastGame(), new GameOptions { Headless = true, Width = 640, Height = 360, Gpu = GpuMode.Off });
        var surface = new ControlSurface(game, Path.GetTempPath());
        var scenario = ScenarioParser.Parse(
            "steps:\n  - step: 2\n  - click: fast\n  - step: 1\nexpect:\n  - ui: { id: fast, pressed: true }\n  - ui: { id: fast, text: FAST, pressed: true, enabled: true }\n", "inline.yaml", "pressed");
        var result = new ScenarioRunner(surface).Run(scenario);
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));

        // Wrong expectation and a non-toggle both fail with a message that says what was found.
        var wrong = new ScenarioRunner(surface).Run(ScenarioParser.Parse("steps:\n  - step: 1\nexpect:\n  - ui: { id: fast, pressed: false }\n", "inline.yaml", "wrong"));
        Assert.False(wrong.Passed);
        Assert.Contains("pressed=true", wrong.Expectations[0].Message, StringComparison.Ordinal);
        var plain = new ScenarioRunner(surface).Run(ScenarioParser.Parse("steps:\n  - step: 1\nexpect:\n  - ui: { id: slow, pressed: false }\n", "inline.yaml", "plain"));
        Assert.False(plain.Passed);
        Assert.Contains("not a toggle", plain.Expectations[0].Message, StringComparison.Ordinal);

        // The same facts through the live-control query.
        using var query = JsonDocument.Parse(surface.Call("ui_query", """{"selector":"#fast"}"""));
        Assert.True(query.RootElement[0].GetProperty("pressed").GetBoolean());
    }
}
