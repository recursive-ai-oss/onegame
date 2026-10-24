namespace OneGame.Platform;

/// <summary>A mouse or pointer button.</summary>
public enum MouseButton : byte
{
    /// <summary>The primary button (usually left).</summary>
    Left,

    /// <summary>The secondary button (usually right).</summary>
    Right,

    /// <summary>The wheel button.</summary>
    Middle,

    /// <summary>The first extra button (usually "back").</summary>
    Back,

    /// <summary>The second extra button (usually "forward").</summary>
    Forward,
}

/// <summary>Modifier keys held while an event happened.</summary>
[Flags]
public enum KeyModifiers : byte
{
    /// <summary>No modifier.</summary>
    None = 0,

    /// <summary>Either Shift key.</summary>
    Shift = 1,

    /// <summary>Either Control key.</summary>
    Control = 2,

    /// <summary>Either Alt key.</summary>
    Alt = 4,

    /// <summary>Either Meta (Windows, Command, Super) key.</summary>
    Meta = 8,

    /// <summary>Caps Lock is on.</summary>
    CapsLock = 16,

    /// <summary>Num Lock is on.</summary>
    NumLock = 32,
}

/// <summary>A button on the standard gamepad layout. Names follow the physical position, so South is A on an Xbox pad and Cross on a PlayStation pad.</summary>
public enum GamepadButton : byte
{
    /// <summary>Bottom face button (A, Cross).</summary>
    South,

    /// <summary>Right face button (B, Circle).</summary>
    East,

    /// <summary>Left face button (X, Square).</summary>
    West,

    /// <summary>Top face button (Y, Triangle).</summary>
    North,

    /// <summary>Left shoulder bumper.</summary>
    LeftShoulder,

    /// <summary>Right shoulder bumper.</summary>
    RightShoulder,

    /// <summary>Back, Select, Share or View.</summary>
    Back,

    /// <summary>Start, Options or Menu.</summary>
    Start,

    /// <summary>The central logo button (Guide, PS, Home).</summary>
    Guide,

    /// <summary>Left stick pressed in.</summary>
    LeftStick,

    /// <summary>Right stick pressed in.</summary>
    RightStick,

    /// <summary>Directional pad up.</summary>
    DPadUp,

    /// <summary>Directional pad down.</summary>
    DPadDown,

    /// <summary>Directional pad left.</summary>
    DPadLeft,

    /// <summary>Directional pad right.</summary>
    DPadRight,

    /// <summary>An extra button (Share on Xbox Series, Mute on DualSense, Capture on Switch).</summary>
    Misc1,

    /// <summary>The touchpad pressed in.</summary>
    Touchpad,

    /// <summary>First back paddle.</summary>
    Paddle1,

    /// <summary>Second back paddle.</summary>
    Paddle2,

    /// <summary>Third back paddle.</summary>
    Paddle3,

    /// <summary>Fourth back paddle.</summary>
    Paddle4,
}

/// <summary>An analog axis on the standard gamepad layout. Sticks run from -1 to 1 with up and left negative, as in screen coordinates; triggers run from 0 to 1.</summary>
public enum GamepadAxis : byte
{
    /// <summary>Left stick horizontal; right is positive.</summary>
    LeftX,

    /// <summary>Left stick vertical; down is positive.</summary>
    LeftY,

    /// <summary>Right stick horizontal; right is positive.</summary>
    RightX,

    /// <summary>Right stick vertical; down is positive.</summary>
    RightY,

    /// <summary>Left trigger, 0 released to 1 fully pressed.</summary>
    LeftTrigger,

    /// <summary>Right trigger, 0 released to 1 fully pressed.</summary>
    RightTrigger,
}

/// <summary>What happened.</summary>
public enum PlatformEventKind : byte
{
    /// <summary>No event.</summary>
    None,

    /// <summary>The application was asked to quit (session end, <see cref="PlatformApp.RequestQuit"/>).</summary>
    QuitRequested,

    /// <summary>The user asked to close a window. The window stays open until the game closes it.</summary>
    WindowCloseRequested,

    /// <summary>A window's size in pixels changed.</summary>
    WindowResized,

    /// <summary>A window's content scale changed.</summary>
    WindowScaleChanged,

    /// <summary>A window gained or lost input focus.</summary>
    WindowFocusChanged,

    /// <summary>A key went down (or auto-repeated, see <see cref="PlatformEvent.IsRepeat"/>).</summary>
    KeyDown,

    /// <summary>A key went up.</summary>
    KeyUp,

    /// <summary>Text was typed. <see cref="PlatformEvent.Text"/> holds it.</summary>
    TextInput,

    /// <summary>The pointer moved to <see cref="PlatformEvent.X"/>, <see cref="PlatformEvent.Y"/>.</summary>
    PointerMoved,

    /// <summary>A pointer button went down.</summary>
    PointerButtonDown,

    /// <summary>A pointer button went up.</summary>
    PointerButtonUp,

    /// <summary>The wheel turned; <see cref="PlatformEvent.DeltaX"/> and <see cref="PlatformEvent.DeltaY"/> are in notches, positive Y away from the user.</summary>
    PointerWheel,

    /// <summary>Raw pointer motion in pixels, sent while the cursor is locked.</summary>
    PointerRelativeMotion,

    /// <summary>The pointer entered or left a window; see <see cref="PlatformEvent.Focused"/>.</summary>
    PointerInside,

    /// <summary>A gamepad was connected.</summary>
    GamepadConnected,

    /// <summary>A gamepad was removed.</summary>
    GamepadDisconnected,

    /// <summary>A gamepad button went down.</summary>
    GamepadButtonDown,

    /// <summary>A gamepad button went up.</summary>
    GamepadButtonUp,

    /// <summary>A gamepad axis changed.</summary>
    GamepadAxisMoved,
}

/// <summary>
/// One input or lifecycle event. Every source (OS, gamepad devices, synthetic injection) produces the same shape, and only the fields
/// listed for the event's kind are meaningful. Positions and sizes are in physical pixels.
/// </summary>
public readonly record struct PlatformEvent
{
    /// <summary>What happened.</summary>
    public PlatformEventKind Kind { get; init; }

    /// <summary>The window the event belongs to, or 0 for events that have none (gamepads, quit).</summary>
    public int WindowId { get; init; }

    /// <summary>The physical key for key events.</summary>
    public Key Key { get; init; }

    /// <summary>Modifiers held at the time, for key and pointer events.</summary>
    public KeyModifiers Modifiers { get; init; }

    /// <summary>True when a <see cref="PlatformEventKind.KeyDown"/> is auto-repeat of a key already held.</summary>
    public bool IsRepeat { get; init; }

    /// <summary>The typed text for <see cref="PlatformEventKind.TextInput"/>: one or more complete Unicode characters.</summary>
    public string? Text { get; init; }

    /// <summary>Pointer x in window pixels.</summary>
    public float X { get; init; }

    /// <summary>Pointer y in window pixels.</summary>
    public float Y { get; init; }

    /// <summary>Horizontal wheel or relative motion amount.</summary>
    public float DeltaX { get; init; }

    /// <summary>Vertical wheel or relative motion amount.</summary>
    public float DeltaY { get; init; }

    /// <summary>The button for pointer button events.</summary>
    public MouseButton Button { get; init; }

    /// <summary>Window width in pixels for <see cref="PlatformEventKind.WindowResized"/>.</summary>
    public int Width { get; init; }

    /// <summary>Window height in pixels for <see cref="PlatformEventKind.WindowResized"/>.</summary>
    public int Height { get; init; }

    /// <summary>Content scale for <see cref="PlatformEventKind.WindowScaleChanged"/>.</summary>
    public float Scale { get; init; }

    /// <summary>True for gained focus or pointer entered, false for lost or left.</summary>
    public bool Focused { get; init; }

    /// <summary>The gamepad for gamepad events. Ids are never reused within one run.</summary>
    public int GamepadId { get; init; }

    /// <summary>The button for gamepad button events.</summary>
    public GamepadButton GamepadButton { get; init; }

    /// <summary>The axis for <see cref="PlatformEventKind.GamepadAxisMoved"/>.</summary>
    public GamepadAxis GamepadAxis { get; init; }

    /// <summary>The new axis value for <see cref="PlatformEventKind.GamepadAxisMoved"/>.</summary>
    public float Value { get; init; }
}
