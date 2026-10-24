using OneGame.Host;
using OneGame.UI.Kit.Tests.Guide;

namespace OneGame.UI.Kit.Tests;

/// <summary>The guide's "inside a game" setup, run in a headless host.</summary>
public sealed class HostIntegrationTests
{
    [Fact]
    public void KitScreenWorksInAHeadlessGameAndSurvivesOpeningTheScreenAgain()
    {
        using var game = GameInstance.Create(new MarketGame(), new GameOptions { Headless = true, Width = 640, Height = 360, Gpu = GpuMode.Off });
        game.Step(3);
        var buy = game.Ui.Find("buy");
        Assert.NotNull(buy);
        Assert.Equal("button", buy.Role);
        Assert.NotNull(game.Ui.Find("qty/track"));

        // The tooltip layer exists inside the screen and is hidden until something is hovered.
        var tooltips = game.Ui.Query("Tooltip");
        Assert.Single(tooltips);
        Assert.False(tooltips[0].IsVisible);

        // Opening the screen again rebuilds it and the tooltip layer comes along (the host clears the root).
        game.OpenScreen("main");
        game.Step(2);
        Assert.Single(game.Ui.Query("Tooltip"));
        Assert.NotNull(game.Ui.Find("qty/track"));
    }

    [Fact]
    public void InputSentStraightToTheTreeStillSwitchesTheGlyphDevice()
    {
        var tree = new UiTree();
        tree.SetViewport(200, 100);
        var kit = new KitHost(tree);
        kit.Glyphs.Bind("confirm", keyboard: "Enter", gamepad: "a");
        tree.Dispatch(UiEvent.Activate());
        Assert.Equal(InputDevice.Gamepad, kit.Glyphs.Device);
        tree.Dispatch(UiEvent.KeyDown("Enter"));
        Assert.Equal(InputDevice.Keyboard, kit.Glyphs.Device);
    }
}
