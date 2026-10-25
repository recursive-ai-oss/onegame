namespace OneGame.UI;

/// <summary>What happened. The first group is input fed to the UI; the second is delivered to elements; some kinds are both.</summary>
public enum UiEventKind
{
    /// <summary>The pointer moved to (X, Y) in screen pixels. Input and element event.</summary>
    PointerMove,
    /// <summary>A pointer button went down at (X, Y). Input and element event.</summary>
    PointerDown,
    /// <summary>A pointer button was released at (X, Y). Input and element event.</summary>
    PointerUp,
    /// <summary>Input: the pointer left the window. Element event: the pointer left this element.</summary>
    PointerLeave,
    /// <summary>Element event: the pointer entered this element (not sent to ancestors that were already hovered).</summary>
    PointerEnter,
    /// <summary>Scroll wheel or touchpad scroll at (X, Y) by (DeltaX, DeltaY) pixels. Positive Y scrolls content up (reveals what is below). Input and element event.</summary>
    Wheel,
    /// <summary>A key went down; <see cref="UiEvent.Key"/> is a DOM key name. Input and element event (sent to the focused element).</summary>
    KeyDown,
    /// <summary>A key was released. Input and element event.</summary>
    KeyUp,
    /// <summary>Committed text from the keyboard or an input method. Input and element event (sent to the focused element).</summary>
    TextInput,
    /// <summary>
    /// Text an input method is still composing (a pre-edit string). Input and element event (sent to the focused element). <see cref="UiEvent.Text"/> is the whole pre-edit
    /// text, empty when the composition ended, and <see cref="UiEvent.CursorStart"/>/<see cref="UiEvent.CursorEnd"/> mark the highlighted part. The finished text arrives as <see cref="TextInput"/>.
    /// </summary>
    Composition,
    /// <summary>A directional or tab-like focus move from a gamepad, d-pad or keyboard. Input; the focused element sees it first and may handle it.</summary>
    Navigate,
    /// <summary>The confirm button (gamepad A, Enter): clicks the focused element. Input.</summary>
    Activate,
    /// <summary>The back button (gamepad B, Escape). Input; delivered to the focused element and its ancestors.</summary>
    Cancel,
    /// <summary>Element event: a press and release on the same element, or an activation of the focused one.</summary>
    Click,
    /// <summary>Element event: the element gained focus.</summary>
    Focus,
    /// <summary>Element event: the element lost focus.</summary>
    Blur,
}

/// <summary>A direction for <see cref="UiEventKind.Navigate"/>.</summary>
public enum NavDirection
{
    /// <summary>Toward the top of the screen.</summary>
    Up,
    /// <summary>Toward the bottom.</summary>
    Down,
    /// <summary>Toward the left.</summary>
    Left,
    /// <summary>Toward the right.</summary>
    Right,
    /// <summary>The next element in focus order (Tab, right bumper).</summary>
    Next,
    /// <summary>The previous element in focus order (Shift+Tab, left bumper).</summary>
    Previous,
}

/// <summary>Modifier keys held during a key event.</summary>
[Flags]
public enum KeyModifiers
{
    /// <summary>No modifier.</summary>
    None = 0,
    /// <summary>Shift.</summary>
    Shift = 1,
    /// <summary>Control.</summary>
    Ctrl = 2,
    /// <summary>Alt or Option.</summary>
    Alt = 4,
    /// <summary>Windows, Command or Super.</summary>
    Meta = 8,
}

/// <summary>One input or element event. Build input with the static factories; handlers receive the same shape.</summary>
public readonly record struct UiEvent
{
    /// <summary>What happened.</summary>
    public UiEventKind Kind { get; init; }

    /// <summary>Pointer X in screen pixels.</summary>
    public float X { get; init; }

    /// <summary>Pointer Y in screen pixels.</summary>
    public float Y { get; init; }

    /// <summary>Pointer button: 0 primary, 1 secondary, 2 middle. A <see cref="UiEventKind.Click"/> raised by keyboard or gamepad activation carries -1.</summary>
    public int Button { get; init; }

    /// <summary>Horizontal scroll amount in pixels.</summary>
    public float DeltaX { get; init; }

    /// <summary>Vertical scroll amount in pixels.</summary>
    public float DeltaY { get; init; }

    /// <summary>The DOM <c>KeyboardEvent.key</c> value, such as <c>Enter</c>, <c>ArrowLeft</c>, <c>a</c>.</summary>
    public string? Key { get; init; }

    /// <summary>Modifiers held during a key event.</summary>
    public KeyModifiers Modifiers { get; init; }

    /// <summary>True for auto-repeat key events.</summary>
    public bool Repeat { get; init; }

    /// <summary>The committed text of a <see cref="UiEventKind.TextInput"/>.</summary>
    public string? Text { get; init; }

    /// <summary>For <see cref="UiEventKind.Composition"/>: index in <see cref="Text"/> where the highlighted part starts, or -1 for none.</summary>
    public int CursorStart { get; init; }

    /// <summary>For <see cref="UiEventKind.Composition"/>: index where the highlighted part ends (the caret when equal to <see cref="CursorStart"/>), or -1.</summary>
    public int CursorEnd { get; init; }

    /// <summary>The direction of a <see cref="UiEventKind.Navigate"/>.</summary>
    public NavDirection Direction { get; init; }

    /// <summary>A pointer move.</summary>
    public static UiEvent PointerMove(float x, float y) => new() { Kind = UiEventKind.PointerMove, X = x, Y = y };

    /// <summary>A pointer button press.</summary>
    public static UiEvent PointerDown(float x, float y, int button = 0) => new() { Kind = UiEventKind.PointerDown, X = x, Y = y, Button = button };

    /// <summary>A pointer button release.</summary>
    public static UiEvent PointerUp(float x, float y, int button = 0) => new() { Kind = UiEventKind.PointerUp, X = x, Y = y, Button = button };

    /// <summary>The pointer left the window.</summary>
    public static UiEvent PointerLeftWindow() => new() { Kind = UiEventKind.PointerLeave };

    /// <summary>A scroll.</summary>
    public static UiEvent Wheel(float x, float y, float dx, float dy) => new() { Kind = UiEventKind.Wheel, X = x, Y = y, DeltaX = dx, DeltaY = dy };

    /// <summary>A key press.</summary>
    public static UiEvent KeyDown(string key, KeyModifiers modifiers = KeyModifiers.None, bool repeat = false) =>
        new() { Kind = UiEventKind.KeyDown, Key = key, Modifiers = modifiers, Repeat = repeat };

    /// <summary>A key release.</summary>
    public static UiEvent KeyUp(string key, KeyModifiers modifiers = KeyModifiers.None) =>
        new() { Kind = UiEventKind.KeyUp, Key = key, Modifiers = modifiers };

    /// <summary>Committed text.</summary>
    public static UiEvent TextInput(string text) => new() { Kind = UiEventKind.TextInput, Text = text };

    /// <summary>An input method's pre-edit text. An empty string ends the composition.</summary>
    public static UiEvent Composition(string text, int cursorStart = -1, int cursorEnd = -1) =>
        new() { Kind = UiEventKind.Composition, Text = text, CursorStart = cursorStart, CursorEnd = cursorEnd };

    /// <summary>A focus move.</summary>
    public static UiEvent Navigate(NavDirection direction) => new() { Kind = UiEventKind.Navigate, Direction = direction };

    /// <summary>The confirm button.</summary>
    public static UiEvent Activate() => new() { Kind = UiEventKind.Activate };

    /// <summary>The back button.</summary>
    public static UiEvent Cancel() => new() { Kind = UiEventKind.Cancel };
}

/// <summary>An event handler. Return true when the event was handled, which stops it bubbling to ancestors and suppresses the default action.</summary>
/// <param name="target">The element the handler is attached to.</param>
/// <param name="e">The event.</param>
public delegate bool UiHandler(Element target, UiEvent e);
