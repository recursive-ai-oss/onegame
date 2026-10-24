namespace OneGame.Platform.Tests;

/// <summary>Capability sets for real devices, as the Linux gamepad drivers report them, plus helpers to feed event streams.</summary>
internal static class GamepadFixtures
{
    public const int Syn = 0, Key = 1, Abs = 3;

    public static GamepadMappingDatabase LoadDatabase()
    {
        var db = new GamepadMappingDatabase();
        db.AddText(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "testdata", "gamecontrollerdb-subset.txt")), "Linux");
        return db;
    }

    private static Dictionary<int, (int, int, int)> Axes(bool triggers, int stickRange = 32768, int triggerMax = 255, bool unsignedSticks = false)
    {
        var abs = new Dictionary<int, (int, int, int)>
        {
            [0] = unsignedSticks ? (0, 255, 128) : (-stickRange, stickRange - 1, 0),
            [1] = unsignedSticks ? (0, 255, 128) : (-stickRange, stickRange - 1, 0),
            [3] = unsignedSticks ? (0, 255, 128) : (-stickRange, stickRange - 1, 0),
            [4] = unsignedSticks ? (0, 255, 128) : (-stickRange, stickRange - 1, 0),
            [0x10] = (-1, 1, 0),
            [0x11] = (-1, 1, 0),
        };
        if (triggers)
        {
            abs[2] = (0, triggerMax, 0);
            abs[5] = (0, triggerMax, 0);
        }

        return abs;
    }

    /// <summary>Xbox 360 pad on the xpad driver (045e:028e, version 0x0114).</summary>
    public static EvdevCapabilities Xbox360() => new()
    {
        Name = "Microsoft X-Box 360 pad",
        Bus = 3,
        Vendor = 0x045e,
        Product = 0x028e,
        Version = 0x0114,
        Keys = [0x130, 0x131, 0x133, 0x134, 0x136, 0x137, 0x13a, 0x13b, 0x13c, 0x13d, 0x13e],
        Abs = Axes(true),
        CanRumble = true,
    };

    /// <summary>DualShock 4 on the hid-playstation driver (054c:09cc, version 0x8111). The two triggers are also buttons.</summary>
    public static EvdevCapabilities DualShock4() => new()
    {
        Name = "Sony Interactive Entertainment Wireless Controller",
        Bus = 3,
        Vendor = 0x054c,
        Product = 0x09cc,
        Version = 0x8111,
        Keys = [0x130, 0x131, 0x133, 0x134, 0x136, 0x137, 0x138, 0x139, 0x13a, 0x13b, 0x13c, 0x13d, 0x13e],
        Abs = Axes(true, unsignedSticks: true),
        CanRumble = true,
    };

    /// <summary>Switch Pro controller on the hid-nintendo driver (057e:2009, version 0x8111). Triggers are buttons only.</summary>
    public static EvdevCapabilities SwitchPro() => new()
    {
        Name = "Nintendo Switch Pro Controller",
        Bus = 3,
        Vendor = 0x057e,
        Product = 0x2009,
        Version = 0x8111,
        Keys = [0x130, 0x131, 0x133, 0x134, 0x135, 0x136, 0x137, 0x138, 0x139, 0x13a, 0x13b, 0x13c, 0x13d, 0x13e],
        Abs = Axes(false, 32767),
    };

    public static (EvdevGamepad Pad, List<PlatformEvent> Events) Open(EvdevCapabilities caps, GamepadMappingDatabase? db = null)
    {
        var events = new List<PlatformEvent>();
        var pad = new EvdevGamepad(7, caps, db ?? LoadDatabase(), 0.1f, 0.03f, events.Add);
        events.Clear();
        return (pad, events);
    }

    public static void Press(EvdevGamepad pad, int code, bool down)
    {
        pad.Handle(Key, code, down ? 1 : 0);
        pad.Handle(Syn, 0, 0);
    }

    public static void Move(EvdevGamepad pad, int code, int value)
    {
        pad.Handle(Abs, code, value);
        pad.Handle(Syn, 0, 0);
    }
}
