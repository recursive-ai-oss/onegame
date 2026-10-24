namespace OneGame.Platform;

/// <summary>Current state of one gamepad on the standard layout.</summary>
public sealed class GamepadState
{
    private uint _buttons;
    private readonly float[] _axes = new float[6];

    internal GamepadState(int id, string name, bool isMapped, bool canRumble)
    {
        Id = id;
        Name = name;
        IsMapped = isMapped;
        CanRumble = canRumble;
    }

    /// <summary>The gamepad id carried by its events.</summary>
    public int Id { get; }

    /// <summary>The device name.</summary>
    public string Name { get; }

    /// <summary>True when a mapping from the community database (or the kernel's standard layout) was found for the device.</summary>
    public bool IsMapped { get; }

    /// <summary>True when the device accepts rumble requests.</summary>
    public bool CanRumble { get; }

    /// <summary>Whether the device is still connected.</summary>
    public bool IsConnected { get; internal set; } = true;

    /// <summary>Whether a button is held.</summary>
    public bool IsDown(GamepadButton button) => (_buttons & (1u << (int)button)) != 0;

    /// <summary>The current value of an axis.</summary>
    public float GetAxis(GamepadAxis axis) => _axes[(int)axis];

    internal void SetButton(GamepadButton button, bool down)
    {
        var bit = 1u << (int)button;
        _buttons = down ? _buttons | bit : _buttons & ~bit;
    }

    internal void SetAxis(GamepadAxis axis, float value) => _axes[(int)axis] = value;
}

/// <summary>What is currently held: keys, pointer, buttons and gamepads. It is updated by the same path that queues events, so injected and real input agree.</summary>
public sealed class InputState
{
    private readonly bool[] _keys = new bool[(int)Key.KeypadEnter + 1];
    private readonly bool[] _buttons = new bool[5];
    private readonly Dictionary<int, GamepadState> _gamepads = [];

    internal InputState()
    {
    }

    /// <summary>Whether a key is held.</summary>
    public bool IsKeyDown(Key key) => (uint)key < (uint)_keys.Length && _keys[(int)key];

    /// <summary>Whether a pointer button is held.</summary>
    public bool IsButtonDown(MouseButton button) => (uint)button < (uint)_buttons.Length && _buttons[(int)button];

    /// <summary>The last known pointer x in pixels.</summary>
    public float PointerX { get; private set; }

    /// <summary>The last known pointer y in pixels.</summary>
    public float PointerY { get; private set; }

    /// <summary>The modifiers implied by the keys currently held.</summary>
    public KeyModifiers Modifiers
    {
        get
        {
            var m = KeyModifiers.None;
            if (_keys[(int)Key.LeftShift] || _keys[(int)Key.RightShift])
            {
                m |= KeyModifiers.Shift;
            }

            if (_keys[(int)Key.LeftControl] || _keys[(int)Key.RightControl])
            {
                m |= KeyModifiers.Control;
            }

            if (_keys[(int)Key.LeftAlt] || _keys[(int)Key.RightAlt])
            {
                m |= KeyModifiers.Alt;
            }

            if (_keys[(int)Key.LeftMeta] || _keys[(int)Key.RightMeta])
            {
                m |= KeyModifiers.Meta;
            }

            return m;
        }
    }

    /// <summary>The state of a connected gamepad, or null when the id is unknown.</summary>
    public GamepadState? GetGamepad(int id) => _gamepads.GetValueOrDefault(id);

    internal GamepadState AddGamepad(int id, string name, bool isMapped, bool canRumble)
    {
        var state = new GamepadState(id, name, isMapped, canRumble);
        _gamepads[id] = state;
        return state;
    }

    internal void Apply(in PlatformEvent e)
    {
        switch (e.Kind)
        {
            case PlatformEventKind.KeyDown:
            case PlatformEventKind.KeyUp:
                if ((uint)e.Key < (uint)_keys.Length)
                {
                    _keys[(int)e.Key] = e.Kind == PlatformEventKind.KeyDown;
                }

                break;
            case PlatformEventKind.PointerMoved:
                PointerX = e.X;
                PointerY = e.Y;
                break;
            case PlatformEventKind.PointerButtonDown:
            case PlatformEventKind.PointerButtonUp:
                if ((uint)e.Button < (uint)_buttons.Length)
                {
                    _buttons[(int)e.Button] = e.Kind == PlatformEventKind.PointerButtonDown;
                }

                break;
            case PlatformEventKind.WindowFocusChanged when !e.Focused:
                // Key-up events never arrive for a window that lost focus, so release everything to avoid stuck keys.
                Array.Clear(_keys);
                Array.Clear(_buttons);
                break;
            case PlatformEventKind.GamepadButtonDown:
            case PlatformEventKind.GamepadButtonUp:
                _gamepads.GetValueOrDefault(e.GamepadId)?.SetButton(e.GamepadButton, e.Kind == PlatformEventKind.GamepadButtonDown);
                break;
            case PlatformEventKind.GamepadAxisMoved:
                _gamepads.GetValueOrDefault(e.GamepadId)?.SetAxis(e.GamepadAxis, e.Value);
                break;
            case PlatformEventKind.GamepadDisconnected:
                if (_gamepads.Remove(e.GamepadId, out var gone))
                {
                    gone.IsConnected = false;
                }

                break;
        }
    }
}
