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

    /// <summary>True when the device matched an entry in the mapping database; false when the standard layout was assumed.</summary>
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

/// <summary>What a <see cref="PointerEdge"/> reports.</summary>
public enum PointerEdgeKind : byte
{
    /// <summary>A button or finger went down.</summary>
    Down,

    /// <summary>The pointer or a finger moved.</summary>
    Move,

    /// <summary>A button or finger went up.</summary>
    Up,

    /// <summary>The system took a finger away (a call, a gesture): treat it as lifted without a tap.</summary>
    Cancel,
}

/// <summary>
/// One pointer event in the order it happened, with the position it happened at. Reading <see cref="InputState.PointerX"/> at frame time gives
/// where the pointer ended up, which is not where a press or lift happened when the pointer moved within the same frame; the edges are.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="TouchId">The finger, or -1 for the mouse or pen.</param>
/// <param name="Button">The button for mouse edges of kind Down and Up; Left for fingers.</param>
/// <param name="X">Window x in pixels, with the fraction the system reported.</param>
/// <param name="Y">Window y in pixels, with the fraction the system reported.</param>
/// <param name="FromTouch">True for a mouse edge made from the first finger (<see cref="PlatformOptions.TouchEmulatesPointer"/>): the finger's own edge is listed too, so a game that reads both should skip these.</param>
public readonly record struct PointerEdge(PointerEdgeKind Kind, int TouchId, MouseButton Button, float X, float Y, bool FromTouch);

/// <summary>What is currently held: keys, pointer, buttons and gamepads. It is updated by the same path that queues events, so injected and real input agree.</summary>
public sealed class InputState
{
    private readonly bool[] _keys = new bool[(int)Key.KeypadEnter + 1];
    private readonly bool[] _buttons = new bool[5];
    private readonly Dictionary<int, GamepadState> _gamepads = [];
    private readonly Dictionary<int, (float X, float Y)> _touches = [];

    private const int MaxEdges = 1024;
    private const int MaxMoveEdges = 768;
    private readonly List<PointerEdge> _edges = new(64);

    internal InputState()
    {
    }

    // Set by the platform while it posts the mouse events it makes from the first finger.
    internal bool Emulating { get; set; }

    /// <summary>
    /// Every pointer and touch event since the host last ended a frame, oldest first, each with its own position. A press then a long move
    /// in one frame reports the press where it happened, and a finger that moves and lifts in one frame reports the lift where it lifted.
    /// The list is bounded: past 768 entries moves are dropped, and past 1,024 everything is (<see cref="DroppedPointerEdges"/>).
    /// </summary>
    public ReadOnlySpan<PointerEdge> PointerEdges => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_edges);

    /// <summary>How many edges were dropped because a frame never ended (an app that does not call <see cref="EndFrame"/> sees this grow).</summary>
    public int DroppedPointerEdges { get; private set; }

    /// <summary>Forgets the pointer edges. The game host calls it after each frame; an app that drives <see cref="PlatformApp"/> itself should too.</summary>
    public void EndFrame() => _edges.Clear();

    private void AddEdge(PointerEdgeKind kind, int touchId, MouseButton button, float x, float y)
    {
        if (_edges.Count >= MaxEdges || (kind == PointerEdgeKind.Move && _edges.Count >= MaxMoveEdges))
        {
            DroppedPointerEdges++;
            return;
        }

        _edges.Add(new PointerEdge(kind, touchId, button, x, y, Emulating));
    }

    // The action layer walks these every frame without allocating.
    internal Dictionary<int, GamepadState> GamepadStates => _gamepads;

    internal Dictionary<int, (float X, float Y)> TouchPoints => _touches;

    /// <summary>Whether a key is held.</summary>
    public bool IsKeyDown(Key key) => (uint)key < (uint)_keys.Length && _keys[(int)key];

    /// <summary>Whether a pointer button is held.</summary>
    public bool IsButtonDown(MouseButton button) => (uint)button < (uint)_buttons.Length && _buttons[(int)button];

    /// <summary>The last known pointer x in pixels.</summary>
    public float PointerX { get; private set; }

    /// <summary>The last known pointer y in pixels.</summary>
    public float PointerY { get; private set; }

    /// <summary>True while the pointer position and button come from the first finger (<see cref="PlatformOptions.TouchEmulatesPointer"/>) rather than a mouse or pen: a game can skip hover effects then.</summary>
    public bool PointerEmulatedFromTouch { get; private set; }

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

    /// <summary>How many fingers are on the screen.</summary>
    public int TouchCount => _touches.Count;

    /// <summary>Where a finger is, in window pixels. False when that finger is not down.</summary>
    public bool TryGetTouch(int touchId, out float x, out float y)
    {
        if (_touches.TryGetValue(touchId, out var p))
        {
            (x, y) = p;
            return true;
        }

        x = y = 0;
        return false;
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
                PointerEmulatedFromTouch = Emulating;
                AddEdge(PointerEdgeKind.Move, -1, MouseButton.Left, e.X, e.Y);
                break;
            case PlatformEventKind.PointerButtonDown:
            case PlatformEventKind.PointerButtonUp:
                if ((uint)e.Button < (uint)_buttons.Length)
                {
                    _buttons[(int)e.Button] = e.Kind == PlatformEventKind.PointerButtonDown;
                }

                PointerEmulatedFromTouch = Emulating;
                AddEdge(e.Kind == PlatformEventKind.PointerButtonDown ? PointerEdgeKind.Down : PointerEdgeKind.Up, -1, e.Button, e.X, e.Y);

                break;
            case PlatformEventKind.WindowFocusChanged when !e.Focused:
                // Key-up events never arrive for a window that lost focus, so release everything to avoid stuck keys.
                Array.Clear(_keys);
                Array.Clear(_buttons);
                break;
            case PlatformEventKind.TouchDown:
            case PlatformEventKind.TouchMove:
                _touches[e.TouchId] = (e.X, e.Y);
                AddEdge(e.Kind == PlatformEventKind.TouchDown ? PointerEdgeKind.Down : PointerEdgeKind.Move, e.TouchId, MouseButton.Left, e.X, e.Y);
                break;
            case PlatformEventKind.TouchUp:
            case PlatformEventKind.TouchCancel:
                _touches.Remove(e.TouchId);
                AddEdge(e.Kind == PlatformEventKind.TouchUp ? PointerEdgeKind.Up : PointerEdgeKind.Cancel, e.TouchId, MouseButton.Left, e.X, e.Y);
                break;
            case PlatformEventKind.SurfaceLost:
                // Fingers that were down when the window went away never report a lift.
                _touches.Clear();
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
