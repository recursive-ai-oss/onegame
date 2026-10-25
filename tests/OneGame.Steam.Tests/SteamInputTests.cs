using OneGame.Platform;
using OneGame.Steam.Input;
using OneGame.Stores;
using OneGame.UI;
using OneGame.UI.Kit;

namespace OneGame.Steam.Tests;

public class SteamInputTests
{
    private static SteamInputActions Declare() => new SteamInputActions { MajorRevision = 1 }
        .Set("Map", "Map screen", s => s
            .Stick("map_move", "Move", SteamStickMode.JoystickMove, SteamStick.Left)
            .Button("map_select", "Select", GamepadButton.South)
            .Button("map_back", "Back", GamepadButton.East)
            .OnScreens("map"))
        .Set("Trade", "Trading screen", s => s
            .Button("trade_confirm", "Confirm", GamepadButton.South)
            .Trigger("trade_amount", "Amount", GamepadAxis.RightTrigger)
            .OnScreens("trade"))
        .Set("Encounter", "Encounter screen", s => s
            .Button("encounter_act", "Act", GamepadButton.West)
            .OnScreens("encounter"))
        .Set("Menu", "Menus", s => s
            .Button("menu_next", "Next", GamepadButton.DPadDown)
            .Button("menu_accept", "Accept", GamepadButton.South)
            .OnScreens("menu", "settings"));

    private static (SteamInputService Service, FakeSteamInputNative Native) Start(SteamInputActions? actions = null)
    {
        var native = new FakeSteamInputNative(actions ?? Declare());
        return (SteamInputService.Start(native, actions ?? Declare(), null), native);
    }

    [Fact]
    public void WritesTheActionsFileSteamReads()
    {
        var vdf = new SteamInputActions()
            .Set("Map", "Map screen", s => s
                .Stick("move", "Move", SteamStickMode.AbsoluteMouse)
                .Trigger("zoom", "Zoom")
                .Button("select", "Select \"quoted\""))
            .ToVdf();
        const string expected = """
            "In Game Actions"
            {
            	"actions"
            	{
            		"Map"
            		{
            			"title"	"#Set_Map"
            			"StickPadGyro"
            			{
            				"move"
            				{
            					"title"	"#Action_move"
            					"input_mode"	"absolute_mouse"
            				}
            			}
            			"AnalogTrigger"
            			{
            				"zoom"	"#Action_zoom"
            			}
            			"Button"
            			{
            				"select"	"#Action_select"
            			}
            		}
            	}
            	"localization"
            	{
            		"english"
            		{
            			"Set_Map"	"Map screen"
            			"Action_move"	"Move"
            			"Action_zoom"	"Zoom"
            			"Action_select"	"Select \"quoted\""
            		}
            	}
            }

            """;
        Assert.Equal(expected.ReplaceLineEndings("\n"), vdf);
    }

    [Fact]
    public void RevisionsAndLanguagesAreWrittenAndTheFileCheckFindsDrift()
    {
        var a = Declare();
        a.Localization["german"] = new() { ["Set_Map"] = "Karte" };
        var vdf = a.ToVdf();
        Assert.Contains("\"major_revision\"\t\"1\"", vdf);
        Assert.DoesNotContain("minor_revision", vdf);
        Assert.Contains("\"german\"", vdf);
        Assert.True(a.Matches(vdf.Replace("\n", "\r\n"), out _));
        Assert.False(a.Matches(vdf.Replace("\"Confirm\"", "\"Accept\""), out var diff));
        Assert.Contains("Confirm", diff);
    }

    [Fact]
    public void RejectsNamesSteamCouldNotKeepApart()
    {
        Assert.Throws<ArgumentException>(() => new SteamInputActions().Set("A", "A", s => s.Button("x", "X")).Set("B", "B", s => s.Button("x", "X again")));
        Assert.Throws<ArgumentException>(() => new SteamInputActions().Set("A", "A", s => s.Button("has space", "X")));
        Assert.Throws<ArgumentException>(() => new SteamInputActions().Set("A", "A", _ => { }).Set("A", "A", _ => { }));
        Assert.Throws<ArgumentException>(() => new SteamInputActions().Set("A", "A", s => s.Trigger("t", "T", GamepadAxis.LeftX)));
        Assert.Throws<ArgumentException>(() => new SteamInputActions().Set("A", "A", s => s.OnScreens("m")).Set("B", "B", s => s.OnScreens("m")));
    }

    [Fact]
    public void ScreensPickTheirSetAndTheFirstSetIsTheDefault()
    {
        var a = Declare();
        Assert.Equal("Trade", a.SetForScreen("trade")!.Name);
        Assert.Equal("Menu", a.SetForScreen("settings")!.Name);
        Assert.Equal("Map", a.SetForScreen("somewhere-else")!.Name);
        Assert.Equal("Map", a.SetForScreen(null)!.Name);
    }

    [Fact]
    public void OnlyTheActiveSetsActionsAreLive()
    {
        var (svc, native) = Start();
        var driver = svc.Driver!;
        var pad = driver.Connect(3);
        Assert.Equal(StoreResult.Ok, svc.ActivateSet("Map"));
        svc.Update();
        Assert.Equal(["Map"], native.Calls.Where(c => c.StartsWith("activate", StringComparison.Ordinal)).Select(c => c[9..]));
        Assert.Single(svc.Controllers);

        driver.SetDigital(pad, "map_select", true);
        Assert.False(driver.SetDigital(pad, "trade_confirm", true)); // not in the active set
        svc.Update();
        Assert.True(svc.IsDown("map_select"));
        Assert.True(svc.WasPressed("map_select"));
        Assert.False(svc.IsDown("trade_confirm"));
        svc.Update();
        Assert.True(svc.IsDown("map_select"));
        Assert.False(svc.WasPressed("map_select"));

        svc.ActivateSet("Trade");
        svc.Update();
        Assert.False(svc.IsDown("map_select"));
        Assert.True(svc.IsDown("trade_confirm"));
        Assert.Equal("Trade", svc.Describe().Controllers[0].ActiveSet);
    }

    [Fact]
    public void AnalogAndTriggerValuesAndTheEventStream()
    {
        var (svc, _) = Start();
        var changes = new List<SteamActionChange>();
        svc.ActionChanged += changes.Add;
        var pad = svc.Driver!.Connect(3);
        svc.ActivateSet("Map");
        svc.Update();
        svc.Driver.SetAnalog(pad, "map_move", 0.5f, -1f);
        svc.Update();
        svc.GetAnalog("map_move", out var x, out var y);
        Assert.Equal((0.5f, -1f), (x, y));
        svc.Update(); // unchanged: no second event
        svc.ActivateSet("Trade");
        svc.Driver.SetAnalog(pad, "trade_amount", 0.25f, 0);
        svc.Update();
        svc.GetAnalog("trade_amount", out var t, out _);
        Assert.Equal(0.25f, t);
        Assert.Equal(
            [("map_move", 0.5f, -1f), ("map_move", 0f, 0f), ("trade_amount", 0.25f, 0f)],
            changes.Select(c => (c.Action, c.X, c.Y)).ToArray());
    }

    [Fact]
    public void ControllersComeAndGoWithStableIndexes()
    {
        var (svc, _) = Start();
        var log = new List<string>();
        svc.ControllerConnected += c => log.Add("+" + c.Index + c.TypeName);
        svc.ControllerDisconnected += c => log.Add("-" + c.Index);
        var a = svc.Driver!.Connect(3);
        var b = svc.Driver.Connect(14);
        svc.Update();
        svc.Driver.Disconnect(a);
        svc.Update();
        var c2 = svc.Driver.Connect(13);
        svc.Update();
        Assert.Equal(["+1xbox-one", "+2steam-deck", "-1", "+1ps5"], log);
        Assert.Equal([1, 2], svc.Controllers.Select(c => c.Index));
        Assert.Equal(1, c2);
        Assert.Equal(2, b);
    }

    [Fact]
    public void UnknownSetsAndMissingSteamAreValuesNotExceptions()
    {
        var (svc, _) = Start();
        Assert.Equal(StoreResult.NotFound, svc.ActivateSet("Nope"));
        svc.Dispose();
        Assert.Equal(StoreResult.Unavailable, svc.ActivateSet("Map"));

        var off = SteamInputService.Start(new NullStorePlatform("no steam"), Declare());
        Assert.False(off.IsEnabled);
        Assert.Contains("no steam", off.UnavailableReason);
        Assert.Equal(StoreResult.Unavailable, off.ActivateSet("Map"));
        off.Update();
        Assert.False(off.IsDown("map_select"));
        Assert.False(off.Describe().Enabled);
    }

    [Fact]
    public void GlyphsFollowThePlayersButtons()
    {
        var (svc, _) = Start();
        svc.Driver!.Connect(3);
        svc.Update();
        Assert.Equal("a", svc.GetGlyphName("map_select"));
        Assert.Equal("x", svc.GetGlyphName("encounter_act"));
        Assert.Equal("rt", svc.GetGlyphName("trade_amount"));
        Assert.Equal("ls", svc.GetGlyphName("map_move"));
        Assert.Equal("dpad-down", svc.GetGlyphName("menu_next"));
        Assert.EndsWith(".png", svc.GetGlyphPath("map_select", SteamGlyphSize.Large));

        var glyphs = new InputGlyphs().Bind("map_select", keyboard: "Enter");
        svc.BindGlyphs(glyphs);
        glyphs.SetDevice(InputDevice.Gamepad);
        Assert.Equal("a", glyphs.NameFor("map_select", out var device));
        Assert.Equal(InputDevice.Gamepad, device);
        glyphs.SetDevice(InputDevice.Keyboard);
        Assert.Equal("Enter", glyphs.NameFor("map_select", out _)); // the keyboard binding survived
        Assert.Equal("x", glyphs.NameFor("encounter_act", out _)); // an action bound only through Steam
    }

    [Fact]
    public void NoControllerMeansNoGlyph()
    {
        var (svc, _) = Start();
        svc.Update();
        Assert.Null(svc.GetGlyphName("map_select"));
        Assert.Null(svc.GetGlyphPath("map_select"));
    }

    [Theory]
    [InlineData(114, "a")]
    [InlineData(117, "y")]
    [InlineData(120, "start")]
    [InlineData(121, "select")]
    [InlineData(123, "lt")]
    [InlineData(124, "rt")]
    [InlineData(131, "ls")]
    [InlineData(137, "rs")]
    [InlineData(141, "dpad-right")]
    [InlineData(143, null)]
    [InlineData(1, null)]
    [InlineData(9999, null)]
    public void OriginsMapToKitGlyphNames(int origin, string? name) => Assert.Equal(name, SteamOrigins.KitGlyphName(origin));

    [Fact]
    public void TheBridgeTurnsActionsIntoTheEnginesGamepadEvents()
    {
        using var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false });
        var ui = new UiTree();
        ui.SetViewport(800, 600, 1);
        var (svc, native) = Start();
        using var bridge = new SteamInputBridge(svc, app, ui);
        var pad = svc.Driver!.Connect(3);
        bridge.Update("trade");
        Assert.Equal("Trade", svc.ActiveSet);
        var id = bridge.PadFor(pad);
        Assert.NotEqual(0, id);

        svc.Driver.SetDigital(pad, "trade_confirm", true);
        svc.Driver.SetAnalog(pad, "trade_amount", 0.75f, 0);
        bridge.Update("trade");
        app.PumpEvents();
        var gp = app.Input.GetGamepad(id)!;
        Assert.True(gp.IsDown(GamepadButton.South));
        Assert.Equal(0.75f, gp.GetAxis(GamepadAxis.RightTrigger));

        bridge.Update("map"); // the Trade actions are not in Map's set: released
        app.PumpEvents();
        Assert.False(gp.IsDown(GamepadButton.South));
        Assert.Equal(0f, gp.GetAxis(GamepadAxis.RightTrigger));

        svc.Driver.SetAnalog(pad, "map_move", 1f, 1f); // up on the Steam stick is negative Y on the engine's pad
        bridge.Update("map");
        app.PumpEvents();
        Assert.Equal(1f, gp.GetAxis(GamepadAxis.LeftX));
        Assert.Equal(-1f, gp.GetAxis(GamepadAxis.LeftY));
        Assert.Contains("activate Map", native.Calls);

        svc.Driver.Disconnect(pad);
        bridge.Update("map");
        app.PumpEvents();
        Assert.Equal(0, bridge.PadFor(pad));
    }

    [Fact]
    public void TheOnScreenKeyboardFollowsTextInputFocusForPlayersOnAPad()
    {
        using var app = PlatformApp.Create(new PlatformOptions { Backend = PlatformBackendKind.Headless, EnableGamepadDevices = false });
        var ui = new UiTree();
        ui.SetViewport(800, 600, 2);
        var field = UiKit.TextInput(() => "", _ => { }, "Name", id: "name");
        ui.Root.Add(field);
        ui.Frame();
        var (svc, native) = Start();
        var glyphs = new InputGlyphs();
        using var bridge = new SteamInputBridge(svc, app, ui, () => 2f, glyphs);

        ui.Focus(field);
        bridge.Update("menu");
        Assert.False(svc.IsKeyboardVisible); // no controller yet, keyboard player

        svc.Driver!.Connect(3);
        bridge.Update("menu");
        Assert.False(native.KeyboardShown); // a pad is connected but the player last used the keyboard

        glyphs.SetDevice(InputDevice.Gamepad);
        bridge.Update("menu");
        Assert.True(native.KeyboardShown);
        var b = field.AbsoluteBounds;
        Assert.Equal(((int)(b.X * 2), (int)(b.Y * 2), (int)(b.Width * 2), (int)(b.Height * 2)), native.KeyboardRect);
        Assert.True(svc.Describe().Keyboard.Visible);

        ui.Focus(null);
        bridge.Update("menu");
        Assert.False(native.KeyboardShown);
        Assert.False(svc.Describe().Keyboard.Visible);

        // Steam types into the game as ordinary keyboard input, so the text arrives through the normal path.
        ui.Focus(field);
        app.Injector.Text(1, "Ada");
        app.PumpEvents();
        PlatformEvent e;
        while (app.TryGetEvent(out e) && e.Kind != PlatformEventKind.TextInput)
        {
        }

        Assert.Equal(PlatformEventKind.TextInput, e.Kind);
        Assert.Equal("Ada", e.Text);
    }

    [Fact]
    public void RefusedKeyboardIsAFailureValue()
    {
        var (svc, native) = Start();
        native.KeyboardAvailable = false;
        Assert.Equal(StoreResult.Failed, svc.ShowKeyboard(1, 2, 3, 4));
        native.KeyboardAvailable = true;
        Assert.Equal(StoreResult.Ok, svc.ShowKeyboard(1, 2, 3, 4));
    }
}
