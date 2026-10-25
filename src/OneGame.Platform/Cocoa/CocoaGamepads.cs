namespace OneGame.Platform.Cocoa;

/// <summary>One reading of a controller in the standard layout, as the GameController framework reports it.</summary>
internal struct PadReading
{
    /// <summary>Bit per raw button, in the order of <see cref="CocoaPad.ButtonNames"/>.</summary>
    public uint Buttons;

    /// <summary>Stick positions in -1..1 with up positive (the framework's convention); triggers in 0..1.</summary>
    public float LeftX, LeftY, RightX, RightY, LeftTrigger, RightTrigger;

    public void Set(int button, bool down)
    {
        if (down)
        {
            Buttons |= 1u << button;
        }
    }
}

/// <summary>A GameController device fed through the same mapper as every other pad, with an identity mapping onto the standard layout.</summary>
internal sealed class CocoaPad
{
    // GCExtendedGamepad is already the standard layout, so the raw numbering below maps one to one.
    internal static readonly string[] ButtonNames =
        ["a", "b", "x", "y", "leftshoulder", "rightshoulder", "back", "start", "leftstick", "rightstick", "dpup", "dpdown", "dpleft", "dpright", "guide", "misc1"];

    private const string IdentityMapping =
        "00000000000000000000000000000000,GameController,a:b0,b:b1,x:b2,y:b3,leftshoulder:b4,rightshoulder:b5,back:b6,start:b7,leftstick:b8,rightstick:b9,"
        + "dpup:b10,dpdown:b11,dpleft:b12,dpright:b13,guide:b14,misc1:b15,leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,";

    private readonly GamepadMapper _mapper;

    public CocoaPad(int id, float stickDeadZone, float triggerDeadZone, Action<PlatformEvent> sink)
    {
        Id = id;
        if (!GamepadMapping.TryParse(IdentityMapping, out var mapping))
        {
            throw new InvalidOperationException("built-in GameController mapping is malformed");
        }

        _mapper = new GamepadMapper(mapping!, id, ButtonNames.Length, 6, 0, stickDeadZone, triggerDeadZone, sink);
    }

    public int Id { get; }

    /// <summary>Applies a reading. The framework's y axes point up and its triggers run 0 to 1; the mapper wants y down and triggers as -1 to 1.</summary>
    public void Update(in PadReading r)
    {
        for (var i = 0; i < ButtonNames.Length; i++)
        {
            _mapper.SetButton(i, (r.Buttons & (1u << i)) != 0);
        }

        _mapper.SetAxis(0, r.LeftX);
        _mapper.SetAxis(1, -r.LeftY);
        _mapper.SetAxis(2, r.RightX);
        _mapper.SetAxis(3, -r.RightY);
        _mapper.SetAxis(4, (r.LeftTrigger * 2f) - 1f);
        _mapper.SetAxis(5, (r.RightTrigger * 2f) - 1f);
        _mapper.Flush();
    }
}

/// <summary>Controllers the GameController framework knows about, polled once per pump.</summary>
internal sealed class CocoaGamepads
{
    private readonly PlatformApp _app;
    private readonly Func<int> _nextId;
    private readonly Dictionary<nint, CocoaPad> _pads = [];
    private readonly List<nint> _seen = [];
    private bool _loaded;
    private bool _unavailable;

    public CocoaGamepads(PlatformApp app, Func<int> nextId)
    {
        _app = app;
        _nextId = nextId;
    }

    /// <summary>The number of connected controllers.</summary>
    public int Count => _pads.Count;

    /// <summary>Registers a controller. <paramref name="key"/> is any value that identifies it (the framework's object pointer).</summary>
    public CocoaPad Connect(nint key, string name)
    {
        var id = _nextId();
        var pad = new CocoaPad(id, _app.Options.StickDeadZone, _app.Options.TriggerDeadZone, e => _app.Post(e));
        _pads[key] = pad;
        _app.Input.AddGamepad(id, name, isMapped: true, canRumble: false);
        _app.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadConnected, GamepadId = id });
        return pad;
    }

    /// <summary>Forgets a controller that went away.</summary>
    public void Disconnect(nint key)
    {
        if (_pads.Remove(key, out var pad))
        {
            _app.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadDisconnected, GamepadId = pad.Id });
        }
    }

    /// <summary>Looks at every controller and reports what changed.</summary>
    public void Poll()
    {
        if (_unavailable || !ObjC.IsSupported)
        {
            return;
        }

        if (!_loaded)
        {
            _loaded = true;
            if (!ObjC.LoadGameController())
            {
                _unavailable = true;
                return;
            }
        }

        using var pool = new Pool();
        var list = ObjC.Send(ObjC.Class("GCController"), "controllers");
        var count = list == 0 ? 0 : (int)Msg.UL(list, ObjC.Sel("count"));
        _seen.Clear();
        for (var i = 0; i < count; i++)
        {
            var controller = Msg.Obj(list, ObjC.Sel("objectAtIndex:"), (nuint)i);
            var profile = ObjC.Send(controller, "extendedGamepad");
            if (profile == 0)
            {
                // Micro gamepads and remotes have no standard layout.
                continue;
            }

            _seen.Add(controller);
            if (!_pads.TryGetValue(controller, out var pad))
            {
                var name = ObjC.ToManaged(ObjC.Send(controller, "vendorName"));
                pad = Connect(controller, name.Length > 0 ? name : "Game Controller");
            }

            pad.Update(Read(profile));
        }

        foreach (var key in (nint[])[.. _pads.Keys])
        {
            if (!_seen.Contains(key))
            {
                Disconnect(key);
            }
        }
    }

    private static PadReading Read(nint profile)
    {
        var r = default(PadReading);
        r.Set(0, Pressed(profile, "buttonA"));
        r.Set(1, Pressed(profile, "buttonB"));
        r.Set(2, Pressed(profile, "buttonX"));
        r.Set(3, Pressed(profile, "buttonY"));
        r.Set(4, Pressed(profile, "leftShoulder"));
        r.Set(5, Pressed(profile, "rightShoulder"));
        r.Set(6, Pressed(profile, "buttonOptions"));
        r.Set(7, Pressed(profile, "buttonMenu"));
        r.Set(8, Pressed(profile, "leftThumbstickButton"));
        r.Set(9, Pressed(profile, "rightThumbstickButton"));
        var dpad = ObjC.Send(profile, "dpad");
        if (dpad != 0)
        {
            r.Set(10, Pressed(dpad, "up"));
            r.Set(11, Pressed(dpad, "down"));
            r.Set(12, Pressed(dpad, "left"));
            r.Set(13, Pressed(dpad, "right"));
        }

        r.Set(14, Pressed(profile, "buttonHome"));
        r.LeftTrigger = Value(profile, "leftTrigger");
        r.RightTrigger = Value(profile, "rightTrigger");
        var left = ObjC.Send(profile, "leftThumbstick");
        var right = ObjC.Send(profile, "rightThumbstick");
        if (left != 0)
        {
            r.LeftX = Value(left, "xAxis");
            r.LeftY = Value(left, "yAxis");
        }

        if (right != 0)
        {
            r.RightX = Value(right, "xAxis");
            r.RightY = Value(right, "yAxis");
        }

        return r;
    }

    // Some elements exist only on newer systems or particular pads; a missing one reads as not pressed.
    private static nint Element(nint owner, string name)
    {
        var sel = ObjC.Sel(name);
        return Msg.Flag(owner, ObjC.Sel("respondsToSelector:"), sel) != 0 ? Msg.Obj(owner, sel) : 0;
    }

    private static bool Pressed(nint owner, string name)
    {
        var e = Element(owner, name);
        return e != 0 && Msg.Flag(e, ObjC.Sel("isPressed")) != 0;
    }

    private static float Value(nint owner, string name)
    {
        var e = Element(owner, name);
        return e == 0 ? 0 : Msg.Flt(e, ObjC.Sel("value"));
    }
}
