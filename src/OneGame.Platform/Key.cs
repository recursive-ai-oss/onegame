namespace OneGame.Platform;

/// <summary>A physical key, named by its position on a US keyboard, not by the character the current layout gives it.</summary>
public enum Key : ushort
{
    /// <summary>No key, or a key this layer has no name for.</summary>
    Unknown,
    /// <summary>The A key (by position on a US layout).</summary>
    A,
    /// <summary>The B key (by position on a US layout).</summary>
    B,
    /// <summary>The C key (by position on a US layout).</summary>
    C,
    /// <summary>The D key (by position on a US layout).</summary>
    D,
    /// <summary>The E key (by position on a US layout).</summary>
    E,
    /// <summary>The F key (by position on a US layout).</summary>
    F,
    /// <summary>The G key (by position on a US layout).</summary>
    G,
    /// <summary>The H key (by position on a US layout).</summary>
    H,
    /// <summary>The I key (by position on a US layout).</summary>
    I,
    /// <summary>The J key (by position on a US layout).</summary>
    J,
    /// <summary>The K key (by position on a US layout).</summary>
    K,
    /// <summary>The L key (by position on a US layout).</summary>
    L,
    /// <summary>The M key (by position on a US layout).</summary>
    M,
    /// <summary>The N key (by position on a US layout).</summary>
    N,
    /// <summary>The O key (by position on a US layout).</summary>
    O,
    /// <summary>The P key (by position on a US layout).</summary>
    P,
    /// <summary>The Q key (by position on a US layout).</summary>
    Q,
    /// <summary>The R key (by position on a US layout).</summary>
    R,
    /// <summary>The S key (by position on a US layout).</summary>
    S,
    /// <summary>The T key (by position on a US layout).</summary>
    T,
    /// <summary>The U key (by position on a US layout).</summary>
    U,
    /// <summary>The V key (by position on a US layout).</summary>
    V,
    /// <summary>The W key (by position on a US layout).</summary>
    W,
    /// <summary>The X key (by position on a US layout).</summary>
    X,
    /// <summary>The Y key (by position on a US layout).</summary>
    Y,
    /// <summary>The Z key (by position on a US layout).</summary>
    Z,
    /// <summary>The 1 key on the number row.</summary>
    Digit1,
    /// <summary>The 2 key on the number row.</summary>
    Digit2,
    /// <summary>The 3 key on the number row.</summary>
    Digit3,
    /// <summary>The 4 key on the number row.</summary>
    Digit4,
    /// <summary>The 5 key on the number row.</summary>
    Digit5,
    /// <summary>The 6 key on the number row.</summary>
    Digit6,
    /// <summary>The 7 key on the number row.</summary>
    Digit7,
    /// <summary>The 8 key on the number row.</summary>
    Digit8,
    /// <summary>The 9 key on the number row.</summary>
    Digit9,
    /// <summary>The 0 key on the number row.</summary>
    Digit0,
    /// <summary>Function key F1.</summary>
    F1,
    /// <summary>Function key F2.</summary>
    F2,
    /// <summary>Function key F3.</summary>
    F3,
    /// <summary>Function key F4.</summary>
    F4,
    /// <summary>Function key F5.</summary>
    F5,
    /// <summary>Function key F6.</summary>
    F6,
    /// <summary>Function key F7.</summary>
    F7,
    /// <summary>Function key F8.</summary>
    F8,
    /// <summary>Function key F9.</summary>
    F9,
    /// <summary>Function key F10.</summary>
    F10,
    /// <summary>Function key F11.</summary>
    F11,
    /// <summary>Function key F12.</summary>
    F12,
    /// <summary>Function key F13.</summary>
    F13,
    /// <summary>Function key F14.</summary>
    F14,
    /// <summary>Function key F15.</summary>
    F15,
    /// <summary>Function key F16.</summary>
    F16,
    /// <summary>Function key F17.</summary>
    F17,
    /// <summary>Function key F18.</summary>
    F18,
    /// <summary>Function key F19.</summary>
    F19,
    /// <summary>Function key F20.</summary>
    F20,
    /// <summary>Function key F21.</summary>
    F21,
    /// <summary>Function key F22.</summary>
    F22,
    /// <summary>Function key F23.</summary>
    F23,
    /// <summary>Function key F24.</summary>
    F24,
    /// <summary>Escape.</summary>
    Escape,
    /// <summary>Tab.</summary>
    Tab,
    /// <summary>Caps Lock.</summary>
    CapsLock,
    /// <summary>Left Shift.</summary>
    LeftShift,
    /// <summary>Right Shift.</summary>
    RightShift,
    /// <summary>Left Control.</summary>
    LeftControl,
    /// <summary>Right Control.</summary>
    RightControl,
    /// <summary>Left Alt.</summary>
    LeftAlt,
    /// <summary>Right Alt or AltGr.</summary>
    RightAlt,
    /// <summary>Left Windows, Command or Super key.</summary>
    LeftMeta,
    /// <summary>Right Windows, Command or Super key.</summary>
    RightMeta,
    /// <summary>The context-menu key.</summary>
    Menu,
    /// <summary>Space bar.</summary>
    Space,
    /// <summary>Enter on the main block.</summary>
    Enter,
    /// <summary>Backspace.</summary>
    Backspace,
    /// <summary>Minus and underscore key.</summary>
    Minus,
    /// <summary>Equals and plus key.</summary>
    Equal,
    /// <summary>Left bracket key.</summary>
    BracketLeft,
    /// <summary>Right bracket key.</summary>
    BracketRight,
    /// <summary>Backslash key.</summary>
    Backslash,
    /// <summary>Semicolon key.</summary>
    Semicolon,
    /// <summary>Quote key.</summary>
    Quote,
    /// <summary>Backquote and tilde key.</summary>
    Backquote,
    /// <summary>Comma key.</summary>
    Comma,
    /// <summary>Period key.</summary>
    Period,
    /// <summary>Slash key.</summary>
    Slash,
    /// <summary>The extra key next to left Shift on ISO keyboards.</summary>
    IntlBackslash,
    /// <summary>Insert.</summary>
    Insert,
    /// <summary>Delete.</summary>
    Delete,
    /// <summary>Home.</summary>
    Home,
    /// <summary>End.</summary>
    End,
    /// <summary>Page Up.</summary>
    PageUp,
    /// <summary>Page Down.</summary>
    PageDown,
    /// <summary>Up arrow.</summary>
    ArrowUp,
    /// <summary>Down arrow.</summary>
    ArrowDown,
    /// <summary>Left arrow.</summary>
    ArrowLeft,
    /// <summary>Right arrow.</summary>
    ArrowRight,
    /// <summary>Print Screen.</summary>
    PrintScreen,
    /// <summary>Scroll Lock.</summary>
    ScrollLock,
    /// <summary>Pause.</summary>
    Pause,
    /// <summary>Num Lock.</summary>
    NumLock,
    /// <summary>Keypad 0.</summary>
    Keypad0,
    /// <summary>Keypad 1.</summary>
    Keypad1,
    /// <summary>Keypad 2.</summary>
    Keypad2,
    /// <summary>Keypad 3.</summary>
    Keypad3,
    /// <summary>Keypad 4.</summary>
    Keypad4,
    /// <summary>Keypad 5.</summary>
    Keypad5,
    /// <summary>Keypad 6.</summary>
    Keypad6,
    /// <summary>Keypad 7.</summary>
    Keypad7,
    /// <summary>Keypad 8.</summary>
    Keypad8,
    /// <summary>Keypad 9.</summary>
    Keypad9,
    /// <summary>Keypad decimal point.</summary>
    KeypadDecimal,
    /// <summary>Keypad plus.</summary>
    KeypadAdd,
    /// <summary>Keypad minus.</summary>
    KeypadSubtract,
    /// <summary>Keypad multiply.</summary>
    KeypadMultiply,
    /// <summary>Keypad divide.</summary>
    KeypadDivide,
    /// <summary>Keypad enter.</summary>
    KeypadEnter,
}
