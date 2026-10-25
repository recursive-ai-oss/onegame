namespace OneGame.Platform;

/// <summary>
/// Makes input events without hardware: for the harness, tests and agents driving a running game. Events enter the same queue and
/// state as real ones, so game code cannot tell them apart.
/// </summary>
public sealed class InputInjector
{
    private readonly PlatformApp _app;
    private int _nextGamepadId = 1;

    internal InputInjector(PlatformApp app) => _app = app;

    /// <summary>Presses or releases a physical key. Modifiers come from the modifier keys currently held.</summary>
    public void Key(int windowId, Key key, bool down)
    {
        var repeat = down && _app.Input.IsKeyDown(key);
        var mods = _app.Input.Modifiers;
        _app.Post(new PlatformEvent
        {
            Kind = down ? PlatformEventKind.KeyDown : PlatformEventKind.KeyUp,
            WindowId = windowId,
            Key = key,
            Modifiers = mods,
            IsRepeat = repeat,
        });
    }

    /// <summary>Presses and releases a key.</summary>
    public void KeyPress(int windowId, Key key)
    {
        Key(windowId, key, true);
        Key(windowId, key, false);
    }

    /// <summary>Types text, as if produced by the keyboard layout. <paramref name="text"/> must not be empty.</summary>
    public void Text(int windowId, string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        _app.Post(new PlatformEvent { Kind = PlatformEventKind.TextInput, WindowId = windowId, Text = text, Modifiers = _app.Input.Modifiers });
    }

    /// <summary>Reports an input method's pre-edit string, as a real IME would while the user composes. An empty string ends the composition.</summary>
    /// <param name="windowId">The window.</param>
    /// <param name="preedit">The whole pre-edit text.</param>
    /// <param name="cursorStart">UTF-16 index where the highlighted part starts, or -1 for no cursor. Defaults to the end of the text.</param>
    /// <param name="cursorEnd">UTF-16 index where it ends. Defaults to <paramref name="cursorStart"/>.</param>
    public void Composition(int windowId, string preedit, int? cursorStart = null, int? cursorEnd = null)
    {
        ArgumentNullException.ThrowIfNull(preedit);
        var start = cursorStart ?? preedit.Length;
        _app.Post(new PlatformEvent { Kind = PlatformEventKind.TextComposition, WindowId = windowId, Text = preedit, CursorStart = start, CursorEnd = cursorEnd ?? start });
    }

    /// <summary>Moves the pointer to a position in window pixels.</summary>
    public void PointerMove(int windowId, float x, float y) =>
        _app.Post(new PlatformEvent { Kind = PlatformEventKind.PointerMoved, WindowId = windowId, X = x, Y = y, Modifiers = _app.Input.Modifiers });

    /// <summary>Presses or releases a pointer button at the current pointer position.</summary>
    public void PointerButton(int windowId, MouseButton button, bool down) =>
        _app.Post(new PlatformEvent
        {
            Kind = down ? PlatformEventKind.PointerButtonDown : PlatformEventKind.PointerButtonUp,
            WindowId = windowId,
            Button = button,
            X = _app.Input.PointerX,
            Y = _app.Input.PointerY,
            Modifiers = _app.Input.Modifiers,
        });

    /// <summary>Turns the wheel; amounts are in notches.</summary>
    public void Wheel(int windowId, float deltaX, float deltaY) =>
        _app.Post(new PlatformEvent { Kind = PlatformEventKind.PointerWheel, WindowId = windowId, DeltaX = deltaX, DeltaY = deltaY, Modifiers = _app.Input.Modifiers });

    /// <summary>Reports raw pointer motion in pixels, as sent while the cursor is locked.</summary>
    public void RelativeMotion(int windowId, float deltaX, float deltaY) =>
        _app.Post(new PlatformEvent { Kind = PlatformEventKind.PointerRelativeMotion, WindowId = windowId, DeltaX = deltaX, DeltaY = deltaY });

    /// <summary>Reports that the OS asked a window to close.</summary>
    public void CloseRequest(int windowId) =>
        _app.Post(new PlatformEvent { Kind = PlatformEventKind.WindowCloseRequested, WindowId = windowId });

    /// <summary>Connects a virtual gamepad that already speaks the standard layout, and returns its id.</summary>
    public int GamepadConnect(string name = "Virtual Gamepad")
    {
        var id = _nextGamepadId++;
        _app.Gamepads.RegisterSynthetic(id, name);
        return id;
    }

    /// <summary>Removes a virtual gamepad.</summary>
    public void GamepadDisconnect(int gamepadId) =>
        _app.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadDisconnected, GamepadId = gamepadId });

    /// <summary>Presses or releases a button on a gamepad.</summary>
    public void GamepadButton(int gamepadId, GamepadButton button, bool down) =>
        _app.Post(new PlatformEvent
        {
            Kind = down ? PlatformEventKind.GamepadButtonDown : PlatformEventKind.GamepadButtonUp,
            GamepadId = gamepadId,
            GamepadButton = button,
        });

    /// <summary>Moves a gamepad axis. Values are used as given, with no dead zone.</summary>
    public void GamepadAxis(int gamepadId, GamepadAxis axis, float value) =>
        _app.Post(new PlatformEvent { Kind = PlatformEventKind.GamepadAxisMoved, GamepadId = gamepadId, GamepadAxis = axis, Value = value });
}
