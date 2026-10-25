using OneGame.Platform;
using OneGame.Steam.Native;

namespace OneGame.Steam.Input;

/// <summary>
/// A stand-in for Steam Input for the harness: it follows the same rules as the real thing (an action reads as inactive unless it is in the controller's active set, handles are
/// found by name, origins describe the button an action is bound to) but is driven by <see cref="ISteamInputDriver"/> instead of a pad. It is used when Steam is not
/// running and the process was started with <c>ONEGAME_STEAM_INPUT=fake</c>, and by tests that need a controller without hardware.
/// </summary>
internal sealed class FakeSteamInputNative : ISteamInputNative, ISteamInputDriver
{
    /// <summary>The environment variable that turns the stand-in on when Steam is absent.</summary>
    internal const string EnableVariable = "ONEGAME_STEAM_INPUT";

    private readonly SteamInputActions _actions;
    private readonly List<FakePad> _pads = [];
    private readonly List<string> _names = [];
    private int _nextHandle = 1000;
    private bool _kbShown;

    internal FakeSteamInputNative(SteamInputActions? actions = null) => _actions = actions ?? new SteamInputActions();

    public bool IsFake => true;

    /// <summary>Calls the code under test made, in order, for assertions.</summary>
    internal List<string> Calls { get; } = [];

    /// <summary>The last on-screen keyboard rectangle requested.</summary>
    internal (int X, int Y, int Width, int Height)? KeyboardRect { get; private set; }

    /// <summary>Whether the on-screen keyboard is up.</summary>
    internal bool KeyboardShown => _kbShown;

    /// <summary>When false, <see cref="ShowKeyboard"/> refuses, as Steam does where it has no keyboard to show.</summary>
    internal bool KeyboardAvailable { get; set; } = true;

    public bool Init(string? manifestPath)
    {
        Calls.Add("init");
        return true;
    }

    public void Shutdown() => Calls.Add("shutdown");

    public void RunFrame() { }

    public int Connect(int inputType)
    {
        var index = 1;
        while (_pads.Exists(p => p.Index == index))
        {
            index++;
        }

        _pads.Add(new FakePad(index, (ulong)(5000 + index), inputType));
        return index;
    }

    public void Disconnect(int index) => _pads.RemoveAll(p => p.Index == index);

    public bool SetDigital(int index, string action, bool down)
    {
        var pad = _pads.Find(p => p.Index == index) ?? throw new ArgumentException($"No fake controller {index}.", nameof(index));
        if (Kind(action) != SteamActionKind.Digital)
        {
            throw new ArgumentException($"'{action}' is not a declared digital action.", nameof(action));
        }

        pad.Digital[action] = down;
        return InActiveSet(pad, action);
    }

    public bool SetAnalog(int index, string action, float x, float y)
    {
        var pad = _pads.Find(p => p.Index == index) ?? throw new ArgumentException($"No fake controller {index}.", nameof(index));
        if (Kind(action) is not (SteamActionKind.Stick or SteamActionKind.Trigger))
        {
            throw new ArgumentException($"'{action}' is not a declared analog action.", nameof(action));
        }

        pad.Analog[action] = (x, y);
        return InActiveSet(pad, action);
    }

    public int GetConnectedControllers(Span<ulong> handles)
    {
        var n = Math.Min(handles.Length, _pads.Count);
        for (var i = 0; i < n; i++)
        {
            handles[i] = _pads[i].Handle;
        }

        return n;
    }

    public int GetInputType(ulong controller) => Pad(controller)?.InputType ?? 0;

    public ulong GetActionSet(string name) => _actions.FindSet(name) is null ? 0 : Handle("set:" + name);

    public ulong GetDigitalAction(string name) => Kind(name) == SteamActionKind.Digital ? Handle("a:" + name) : 0;

    public ulong GetAnalogAction(string name) => Kind(name) is SteamActionKind.Stick or SteamActionKind.Trigger ? Handle("a:" + name) : 0;

    public void ActivateActionSet(ulong controller, ulong set)
    {
        if (Pad(controller) is { } p)
        {
            p.Set = set;
            Calls.Add("activate " + NameOf(set));
        }
    }

    public ulong GetCurrentActionSet(ulong controller) => Pad(controller)?.Set ?? 0;

    public DigitalReading ReadDigital(ulong controller, ulong action)
    {
        var p = Pad(controller);
        var name = NameOf(action);
        if (p is null || name is null)
        {
            return default;
        }

        var active = InActiveSet(p, name);
        return new DigitalReading(active && p.Digital.GetValueOrDefault(name), active);
    }

    public AnalogReading ReadAnalog(ulong controller, ulong action)
    {
        var p = Pad(controller);
        var name = NameOf(action);
        if (p is null || name is null)
        {
            return default;
        }

        var active = InActiveSet(p, name);
        var (x, y) = p.Analog.GetValueOrDefault(name);
        return new AnalogReading(active ? x : 0, active ? y : 0, active);
    }

    public int GetDigitalOrigins(ulong controller, ulong set, ulong action, Span<int> origins) => Origins(action, origins);

    public int GetAnalogOrigins(ulong controller, ulong set, ulong action, Span<int> origins) => Origins(action, origins);

    public int TranslateOrigin(int destinationInputType, int origin) => origin;

    public string GlyphPath(int origin, int size, uint flags) => $"fake-glyphs/origin-{origin}-{size}.png";

    public string OriginName(int origin) => $"origin {origin}";

    public bool ShowKeyboard(int x, int y, int width, int height)
    {
        Calls.Add($"keyboard {x},{y},{width},{height}");
        if (!KeyboardAvailable)
        {
            return false;
        }

        _kbShown = true;
        KeyboardRect = (x, y, width, height);
        return true;
    }

    public bool DismissKeyboard()
    {
        Calls.Add("keyboard dismissed");
        var was = _kbShown;
        _kbShown = false;
        return was;
    }

    // The fake binds an action to the Xbox button of its declared target, so glyphs have something to show; actions without a target have no button.
    private int Origins(ulong action, Span<int> origins)
    {
        var name = NameOf(action);
        var o = 0;
        foreach (var set in _actions.Sets)
        {
            foreach (var d in set.Digital)
            {
                if (d.Name == name && d.Button is { } b)
                {
                    o = ButtonOrigin(b);
                }
            }

            foreach (var d in set.Sticks)
            {
                if (d.Name == name && d.Stick is { } s)
                {
                    o = s == SteamStick.Left ? 126 : 132;
                }
            }

            foreach (var d in set.Triggers)
            {
                if (d.Name == name && d.Axis is { } a)
                {
                    o = a == GamepadAxis.LeftTrigger ? 122 : 124;
                }
            }
        }

        if (o == 0 || origins.Length == 0)
        {
            return 0;
        }

        origins[0] = o;
        return 1;
    }

    private static int ButtonOrigin(GamepadButton b) => b switch
    {
        GamepadButton.South => 114,
        GamepadButton.East => 115,
        GamepadButton.West => 116,
        GamepadButton.North => 117,
        GamepadButton.LeftShoulder => 118,
        GamepadButton.RightShoulder => 119,
        GamepadButton.Start => 120,
        GamepadButton.Back => 121,
        GamepadButton.LeftStick => 127,
        GamepadButton.RightStick => 133,
        GamepadButton.DPadUp => 138,
        GamepadButton.DPadDown => 139,
        GamepadButton.DPadLeft => 140,
        GamepadButton.DPadRight => 141,
        _ => 0,
    };

    private SteamActionKind? Kind(string name)
    {
        foreach (var (n, k, _) in _actions.AllActions())
        {
            if (n == name)
            {
                return k;
            }
        }

        return null;
    }

    private bool InActiveSet(FakePad p, string action)
    {
        foreach (var (n, _, set) in _actions.AllActions())
        {
            if (n == action)
            {
                return p.Set == Handle("set:" + set);
            }
        }

        return false;
    }

    private FakePad? Pad(ulong handle) => _pads.Find(p => p.Handle == handle);

    private ulong Handle(string key)
    {
        var i = _names.IndexOf(key);
        if (i < 0)
        {
            _names.Add(key);
            i = _names.Count - 1;
        }

        return (ulong)(_nextHandle + i);
    }

    private string? NameOf(ulong handle)
    {
        var i = (int)(handle - (ulong)_nextHandle);
        if (i < 0 || i >= _names.Count)
        {
            return null;
        }

        var key = _names[i];
        return key.StartsWith("set:", StringComparison.Ordinal) ? key[4..] : key[2..];
    }

    private sealed class FakePad(int index, ulong handle, int inputType)
    {
        public int Index { get; } = index;
        public ulong Handle { get; } = handle;
        public int InputType { get; } = inputType;
        public ulong Set { get; set; }
        public Dictionary<string, bool> Digital { get; } = [];
        public Dictionary<string, (float, float)> Analog { get; } = [];
    }
}
