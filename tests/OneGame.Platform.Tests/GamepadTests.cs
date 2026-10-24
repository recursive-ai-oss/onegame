namespace OneGame.Platform.Tests;

public class GamepadMappingTests
{
    [Fact]
    public void ParsesButtonsAxesHatsHalfAxesAndInversion()
    {
        Assert.True(GamepadMapping.TryParse(
            "0300000000000000000000000000ABCD,Test,a:b0,dpup:h0.1,leftx:a0~,lefttrigger:+a2,-righty:b3,platform:Linux,", out var m));
        Assert.Equal("0300000000000000000000000000abcd", m!.DeviceId);
        Assert.Equal("Test", m.Name);
        Assert.Equal("Linux", m.Platform);
        Assert.Equal(5, m.Bindings.Length);
        Assert.Equal(GamepadMapping.SourceKind.Hat, m.Bindings[1].Source);
        Assert.Equal(1, m.Bindings[1].HatMask);
        Assert.True(m.Bindings[2].Invert);
        Assert.Equal(1, m.Bindings[3].SourceHalf);
        Assert.Equal(-1, m.Bindings[4].TargetHalf);
    }

    [Theory]
    [InlineData("")]
    [InlineData("# comment")]
    [InlineData("xyz,Short,a:b0")]
    [InlineData("0300000000000000000000000000abcd,OnlyName")]
    public void RejectsCommentsAndMalformedLines(string line) => Assert.False(GamepadMapping.TryParse(line, out _));

    [Fact]
    public void DatabaseFindsExactAndFallsBackAcrossVersions()
    {
        var db = GamepadFixtures.LoadDatabase();
        Assert.Equal(5, db.Count);
        Assert.NotNull(db.Find("030000004c050000cc09000011810000"));

        // Same model, different version and name checksum: still found.
        Assert.Equal("PS4 Controller", db.Find("030000004c050000cc09000011810001")!.Name);
        Assert.Null(db.Find("03000000deadbeef00000000000000ff"));
    }

    [Fact]
    public void DatabaseSkipsOtherPlatforms()
    {
        var db = new GamepadMappingDatabase();
        var added = db.AddText(
            "03000000000000000000000000000001,A,a:b0,platform:Windows,\n03000000000000000000000000000002,B,a:b0,platform:Linux,\n", "Linux");
        Assert.Equal(1, added);
    }

    [Fact]
    public void GuidMatchesTheCommunityLayout()
    {
        Assert.Equal("030000005e0400008e02000014010000", EvdevGamepad.MakeDeviceId(GamepadFixtures.Xbox360()));
        Assert.Equal("030000004c050000cc09000011810000", EvdevGamepad.MakeDeviceId(GamepadFixtures.DualShock4()));
        Assert.Equal("030000007e0500000920000011810000", EvdevGamepad.MakeDeviceId(GamepadFixtures.SwitchPro()));
    }
}

public class EvdevGamepadTests
{
    private static IEnumerable<PlatformEvent> Buttons(List<PlatformEvent> events) =>
        events.Where(e => e.Kind is PlatformEventKind.GamepadButtonDown or PlatformEventKind.GamepadButtonUp);

    [Theory]
    [InlineData("xbox", 0x130, GamepadButton.South)]
    [InlineData("xbox", 0x131, GamepadButton.East)]
    [InlineData("xbox", 0x133, GamepadButton.West)]
    [InlineData("xbox", 0x134, GamepadButton.North)]
    [InlineData("xbox", 0x136, GamepadButton.LeftShoulder)]
    [InlineData("xbox", 0x137, GamepadButton.RightShoulder)]
    [InlineData("xbox", 0x13a, GamepadButton.Back)]
    [InlineData("xbox", 0x13b, GamepadButton.Start)]
    [InlineData("xbox", 0x13c, GamepadButton.Guide)]
    [InlineData("xbox", 0x13d, GamepadButton.LeftStick)]
    [InlineData("xbox", 0x13e, GamepadButton.RightStick)]
    [InlineData("ps4", 0x130, GamepadButton.South)]
    [InlineData("ps4", 0x131, GamepadButton.East)]
    [InlineData("ps4", 0x134, GamepadButton.West)]
    [InlineData("ps4", 0x133, GamepadButton.North)]
    [InlineData("ps4", 0x136, GamepadButton.LeftShoulder)]
    [InlineData("ps4", 0x13a, GamepadButton.Back)]
    [InlineData("ps4", 0x13b, GamepadButton.Start)]
    [InlineData("ps4", 0x13c, GamepadButton.Guide)]
    [InlineData("ps4", 0x13d, GamepadButton.LeftStick)]
    [InlineData("ps4", 0x13e, GamepadButton.RightStick)]
    [InlineData("switch", 0x130, GamepadButton.South)]
    [InlineData("switch", 0x131, GamepadButton.East)]
    [InlineData("switch", 0x134, GamepadButton.West)]
    [InlineData("switch", 0x133, GamepadButton.North)]
    [InlineData("switch", 0x135, GamepadButton.Misc1)]
    [InlineData("switch", 0x136, GamepadButton.LeftShoulder)]
    [InlineData("switch", 0x137, GamepadButton.RightShoulder)]
    [InlineData("switch", 0x13a, GamepadButton.Back)]
    [InlineData("switch", 0x13b, GamepadButton.Start)]
    [InlineData("switch", 0x13c, GamepadButton.Guide)]
    [InlineData("switch", 0x13d, GamepadButton.LeftStick)]
    [InlineData("switch", 0x13e, GamepadButton.RightStick)]
    public void DatabaseMapsEachPhysicalButtonToTheStandardLayout(string device, int code, GamepadButton expected)
    {
        var caps = device switch { "xbox" => GamepadFixtures.Xbox360(), "ps4" => GamepadFixtures.DualShock4(), _ => GamepadFixtures.SwitchPro() };
        var (pad, events) = GamepadFixtures.Open(caps);
        Assert.True(pad.Mapped);

        GamepadFixtures.Press(pad, code, true);
        GamepadFixtures.Press(pad, code, false);

        var seen = Buttons(events).ToList();
        Assert.Equal(2, seen.Count);
        Assert.Equal(PlatformEventKind.GamepadButtonDown, seen[0].Kind);
        Assert.Equal(expected, seen[0].GamepadButton);
        Assert.Equal(PlatformEventKind.GamepadButtonUp, seen[1].Kind);
        Assert.Equal(7, seen[0].GamepadId);
    }

    [Theory]
    [InlineData("xbox")]
    [InlineData("ps4")]
    [InlineData("switch")]
    public void HatDrivesTheDirectionalPad(string device)
    {
        var caps = device switch { "xbox" => GamepadFixtures.Xbox360(), "ps4" => GamepadFixtures.DualShock4(), _ => GamepadFixtures.SwitchPro() };
        var (pad, events) = GamepadFixtures.Open(caps);

        GamepadFixtures.Move(pad, 0x11, -1); // up
        GamepadFixtures.Move(pad, 0x10, 1); // right: up-right diagonal
        GamepadFixtures.Move(pad, 0x11, 0);
        GamepadFixtures.Move(pad, 0x10, 0);

        var seen = Buttons(events).Select(e => (e.Kind, e.GamepadButton)).ToList();
        Assert.Equal(
            [
                (PlatformEventKind.GamepadButtonDown, GamepadButton.DPadUp),
                (PlatformEventKind.GamepadButtonDown, GamepadButton.DPadRight),
                (PlatformEventKind.GamepadButtonUp, GamepadButton.DPadUp),
                (PlatformEventKind.GamepadButtonUp, GamepadButton.DPadRight),
            ],
            seen);
    }

    [Fact]
    public void XboxSticksAndTriggersUseTheirOwnAxes()
    {
        var (pad, events) = GamepadFixtures.Open(GamepadFixtures.Xbox360());

        GamepadFixtures.Move(pad, 0, 32767); // left stick full right
        GamepadFixtures.Move(pad, 4, -32768); // right stick full up
        GamepadFixtures.Move(pad, 2, 255); // left trigger fully pressed
        GamepadFixtures.Move(pad, 5, 0);

        var axes = events.Where(e => e.Kind == PlatformEventKind.GamepadAxisMoved).ToList();
        Assert.Contains(axes, e => e.GamepadAxis == GamepadAxis.LeftX && e.Value > 0.99f);
        Assert.Contains(axes, e => e.GamepadAxis == GamepadAxis.RightY && e.Value < -0.99f);
        Assert.Contains(axes, e => e.GamepadAxis == GamepadAxis.LeftTrigger && e.Value > 0.99f);
        Assert.DoesNotContain(axes, e => e.GamepadAxis == GamepadAxis.RightTrigger);
    }

    [Fact]
    public void PlayStationAxesFollowTheirDifferentLayout()
    {
        // In the database the DS4's left trigger is ABS_Z (index 2) and its right stick x is ABS_RX (index 3).
        var (pad, events) = GamepadFixtures.Open(GamepadFixtures.DualShock4());

        GamepadFixtures.Move(pad, 3, 255); // ABS_RX full right
        GamepadFixtures.Move(pad, 5, 255); // ABS_RZ, the right trigger

        var axes = events.Where(e => e.Kind == PlatformEventKind.GamepadAxisMoved).ToList();
        Assert.Contains(axes, e => e.GamepadAxis == GamepadAxis.RightX && e.Value > 0.99f);
        Assert.Contains(axes, e => e.GamepadAxis == GamepadAxis.RightTrigger && e.Value > 0.99f);
    }

    [Fact]
    public void SwitchProTriggersAreButtonsThatDriveTheTriggerAxes()
    {
        var (pad, events) = GamepadFixtures.Open(GamepadFixtures.SwitchPro());

        GamepadFixtures.Press(pad, 0x138, true); // ZL
        GamepadFixtures.Press(pad, 0x138, false);
        GamepadFixtures.Move(pad, 3, 32767); // right stick x is ABS_RX, which is axis index 2 on this pad

        var axes = events.Where(e => e.Kind == PlatformEventKind.GamepadAxisMoved).ToList();
        Assert.Contains(axes, e => e.GamepadAxis == GamepadAxis.LeftTrigger && e.Value == 1f);
        Assert.Contains(axes, e => e.GamepadAxis == GamepadAxis.LeftTrigger && e.Value == 0f);
        Assert.Contains(axes, e => e.GamepadAxis == GamepadAxis.RightX && e.Value > 0.99f);
    }

    [Fact]
    public void StickDeadZoneIsRadialAndRescaled()
    {
        var (pad, events) = GamepadFixtures.Open(GamepadFixtures.Xbox360());

        GamepadFixtures.Move(pad, 0, 2000); // about 0.06: inside the dead zone
        Assert.DoesNotContain(events, e => e.Kind == PlatformEventKind.GamepadAxisMoved);

        GamepadFixtures.Move(pad, 0, 16384); // 0.5 -> (0.5 - 0.1) / 0.9
        var moved = events.Single(e => e.Kind == PlatformEventKind.GamepadAxisMoved);
        Assert.Equal(GamepadAxis.LeftX, moved.GamepadAxis);
        Assert.InRange(moved.Value, 0.44f, 0.45f);

        // A diagonal that is inside the circle on each axis but outside it overall is not clipped to a square.
        events.Clear();
        GamepadFixtures.Move(pad, 0, 0);
        GamepadFixtures.Move(pad, 0, 3300);
        GamepadFixtures.Move(pad, 1, 3300);
        Assert.Contains(events, e => e.GamepadAxis == GamepadAxis.LeftY && e.Value > 0f);
    }

    [Fact]
    public void DeviceWithoutDatabaseEntryGetsTheKernelStandardLayout()
    {
        var (pad, events) = GamepadFixtures.Open(GamepadFixtures.Xbox360(), new GamepadMappingDatabase());
        Assert.False(pad.Mapped);

        GamepadFixtures.Press(pad, 0x130, true);
        GamepadFixtures.Move(pad, 0x10, -1);
        GamepadFixtures.Move(pad, 5, 255);

        Assert.Contains(events, e => e.GamepadButton == GamepadButton.South && e.Kind == PlatformEventKind.GamepadButtonDown);
        Assert.Contains(events, e => e.GamepadButton == GamepadButton.DPadLeft && e.Kind == PlatformEventKind.GamepadButtonDown);
        Assert.Contains(events, e => e.GamepadAxis == GamepadAxis.RightTrigger && e.Value > 0.99f);
    }

    [Fact]
    public void KeyboardsAndMiceAreNotGamepads()
    {
        var keyboard = new EvdevCapabilities { Name = "kbd", Keys = [30, 31, 32, 28] };
        var mouse = new EvdevCapabilities { Name = "mouse", Keys = [0x110, 0x111], Abs = new() { [0] = (0, 100, 0), [1] = (0, 100, 0) } };
        Assert.False(EvdevGamepad.LooksLikeGamepad(keyboard));
        Assert.False(EvdevGamepad.LooksLikeGamepad(mouse));
        Assert.True(EvdevGamepad.LooksLikeGamepad(GamepadFixtures.Xbox360()));
    }
}
