using OneGame.Host;
using OneGame.Platform;

namespace OneGame.Windowed.Tests;

/// <summary>Platform events becoming UI input in the host, with no window system involved.</summary>
public class HostInputTests
{
    private static GameInstance Start() => GameInstance.Create(new WindowedGame(), new GameOptions { Headless = true, Width = 640, Height = 360 });

    [Fact]
    public void TheLeftStickMovesFocusOnceThenRepeatsWhileHeld()
    {
        using var game = Start();
        var pad = game.Platform.Injector.GamepadConnect();
        game.Ui.Focus(game.Ui.Find("click")!);
        Assert.Equal("click", game.Ui.Focused!.Id);

        // Pushing down moves to the next control below, once.
        game.Platform.Injector.GamepadAxis(pad, GamepadAxis.LeftY, 1f);
        game.Step();
        Assert.Equal("name", game.Ui.Focused!.Id);

        // Holding it past the repeat delay pushes down again, but there is nothing below the last control, so focus stays.
        game.Step(40);
        Assert.Equal("name", game.Ui.Focused!.Id);

        // Releasing and pushing up goes back.
        game.Platform.Injector.GamepadAxis(pad, GamepadAxis.LeftY, 0f);
        game.Step();
        game.Platform.Injector.GamepadAxis(pad, GamepadAxis.LeftY, -0.9f);
        game.Step();
        Assert.Equal("click", game.Ui.Focused!.Id);

        // A stick wobbling between the thresholds is not a new push.
        game.Platform.Injector.GamepadAxis(pad, GamepadAxis.LeftY, -0.5f);
        game.Step(3);
        Assert.Equal("click", game.Ui.Focused!.Id);
    }

    [Fact]
    public void ButtonsActivateAndCancelThroughTheUi()
    {
        using var game = Start();
        var pad = game.Platform.Injector.GamepadConnect();
        game.Ui.Focus(game.Ui.Find("click")!);
        game.Platform.Injector.GamepadButton(pad, GamepadButton.South, true);
        game.Platform.Injector.GamepadButton(pad, GamepadButton.South, false);
        game.Step(2);
        Assert.Equal(1, ((Clicker)game.World.Entities[0]).Clicks);
    }

    [Fact]
    public void AnInputMethodSessionComposesThenCommitsInTheFocusedField()
    {
        using var game = Start();
        var inj = game.Platform.Injector;
        var window = game.Window.Id;
        game.Ui.Focus(game.Ui.Find("name")!);
        game.Step();
        Assert.True(game.Window.TextInputActive);
        var area = game.Window.TextInputArea;
        Assert.True(area.Width > 0 && area.Height > 0);

        inj.Composition(window, "ni", 0, 2);
        game.Step();
        Assert.Equal("Name: [ni]", game.Ui.Find("name")!.Text);

        inj.Composition(window, string.Empty);
        inj.Text(window, "x");
        game.Step();
        Assert.Equal("Name: x", game.Ui.Find("name")!.Text);

        // Focus moving to a button switches the input method off.
        game.Ui.Focus(game.Ui.Find("click")!);
        game.Step();
        Assert.False(game.Window.TextInputActive);
    }

    [Fact]
    public void TheCommandLineChoosesBackendDisplayAndPresentMode()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var code = GameApp.Run(new WindowedGame(), ["--backend", "vulkan"], output, error);
        Assert.Equal(ExitCodes.Usage, code);
        Assert.Contains("--backend is wayland or x11", error.ToString(), StringComparison.Ordinal);

        code = GameApp.Run(new WindowedGame(), ["--present", "fast"], output, error);
        Assert.Equal(ExitCodes.Usage, code);

        // Headless ignores the window-system options but accepts them.
        code = GameApp.Run(new WindowedGame(), ["--headless", "--frames", "2", "--backend", "x11", "--display", ":99", "--present", "immediate"], output, error);
        Assert.Equal(ExitCodes.Ok, code);
    }
}
