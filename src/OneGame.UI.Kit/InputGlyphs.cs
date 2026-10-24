namespace OneGame.UI.Kit;

/// <summary>The kind of device the player last used, which decides what an <see cref="InputGlyphs"/> glyph shows.</summary>
public enum InputDevice
{
    /// <summary>Keyboard: the glyph is a key cap with the key's name.</summary>
    Keyboard,
    /// <summary>Mouse: the glyph is a mouse with the button highlighted.</summary>
    Mouse,
    /// <summary>Gamepad: the glyph is a button, shoulder, stick or d-pad picture.</summary>
    Gamepad,
}

/// <summary>
/// Maps game actions to the button each device uses, and tracks which device the player used last, so a prompt such as "Press (A) to buy" can show the right picture.
/// Bind actions once, feed every input event through <see cref="Observe"/> (<see cref="KitHost.Dispatch"/> does it), and put <see cref="UiKit.InputGlyph"/> elements in the UI.
/// </summary>
public sealed class InputGlyphs
{
    private readonly Dictionary<string, (string? Keyboard, string? Mouse, string? Gamepad)> _bindings = new(StringComparer.Ordinal);

    /// <summary>The device used last. Starts as <see cref="InputDevice.Keyboard"/>.</summary>
    public InputDevice Device { get; private set; }

    /// <summary>Increases whenever the device or a binding changes, so glyph elements know to update.</summary>
    public int Version { get; private set; }

    /// <summary>
    /// Binds an action. Keyboard names are key names (<c>Enter</c>, <c>Escape</c>, <c>E</c>, <c>ArrowUp</c>); mouse names are <c>left</c>, <c>right</c>, <c>middle</c>, <c>move</c>;
    /// gamepad names are <c>a b x y lb rb lt rt ls rs dpad dpad-up dpad-down dpad-left dpad-right start select</c>.
    /// </summary>
    /// <param name="action">The action's name.</param>
    /// <param name="keyboard">The key, or null for none.</param>
    /// <param name="mouse">The mouse button, or null for none.</param>
    /// <param name="gamepad">The gamepad control, or null for none.</param>
    /// <returns>This object, for chaining.</returns>
    public InputGlyphs Bind(string action, string? keyboard = null, string? mouse = null, string? gamepad = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(action);
        _bindings[action] = (keyboard, mouse, gamepad);
        Version++;
        return this;
    }

    /// <summary>Sets the device directly, for a game that learns about the device from somewhere other than UI events.</summary>
    public void SetDevice(InputDevice device)
    {
        if (device != Device)
        {
            Device = device;
            Version++;
        }
    }

    /// <summary>
    /// Notes which device an input event came from: pointer events and wheel mean the mouse, keys and text mean the keyboard,
    /// and navigate, activate and cancel mean the gamepad (a keyboard's arrow keys arrive as keys, not as navigate).
    /// </summary>
    public void Observe(in UiEvent e)
    {
        switch (e.Kind)
        {
            case UiEventKind.PointerMove:
            case UiEventKind.PointerDown:
            case UiEventKind.PointerUp:
            case UiEventKind.Wheel:
                SetDevice(InputDevice.Mouse);
                break;
            case UiEventKind.KeyDown:
            case UiEventKind.KeyUp:
            case UiEventKind.TextInput:
                SetDevice(InputDevice.Keyboard);
                break;
            case UiEventKind.Navigate:
            case UiEventKind.Activate:
            case UiEventKind.Cancel:
                SetDevice(InputDevice.Gamepad);
                break;
            default:
                break;
        }
    }

    /// <summary>The name an action is bound to on the current device, or on the nearest device that has one (gamepad, then keyboard, then mouse); null when the action is unbound.</summary>
    public string? NameFor(string action, out InputDevice device)
    {
        device = Device;
        if (!_bindings.TryGetValue(action, out var b))
        {
            return null;
        }

        InputDevice[] order = Device switch
        {
            InputDevice.Gamepad => [InputDevice.Gamepad, InputDevice.Keyboard, InputDevice.Mouse],
            InputDevice.Mouse => [InputDevice.Mouse, InputDevice.Keyboard, InputDevice.Gamepad],
            _ => [InputDevice.Keyboard, InputDevice.Mouse, InputDevice.Gamepad],
        };
        foreach (var d in order)
        {
            var name = d switch
            {
                InputDevice.Keyboard => b.Keyboard,
                InputDevice.Mouse => b.Mouse,
                _ => b.Gamepad,
            };
            if (name is not null)
            {
                device = d;
                return name;
            }
        }

        return null;
    }
}

public static partial class UiKit
{
    /// <summary>
    /// A picture of the control an action uses on the device the player used last (kind <c>InputGlyph</c>, role <c>img</c>). <see cref="Element.Value"/> is <c>device:name</c>, such as <c>gamepad:a</c> or <c>keyboard:Enter</c>.
    /// Keyboard glyphs are a key cap with the key's name as text; the others are pictures from <see cref="KitArt"/>, which must be registered in the art atlas.
    /// The element has the class <c>keyboard</c>, <c>mouse</c> or <c>gamepad</c>, and <c>unbound</c> when the action has no binding.
    /// </summary>
    /// <param name="glyphs">The bindings and last-used device.</param>
    /// <param name="action">The action to show.</param>
    /// <param name="id">The id, or null.</param>
    public static Element InputGlyph(InputGlyphs glyphs, string action, string? id = null)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        ArgumentException.ThrowIfNullOrEmpty(action);
        var root = Make("InputGlyph", "img", "kit-glyph", id);
        var label = Ui.Label("").Class("kit-glyph-label");
        root.Add(label);
        var lastVersion = -1;
        root.OnUpdate(e =>
        {
            if (glyphs.Version == lastVersion)
            {
                return;
            }

            lastVersion = glyphs.Version;
            var name = glyphs.NameFor(action, out var device);
            e.SetClass("keyboard", name is not null && device == InputDevice.Keyboard);
            e.SetClass("mouse", name is not null && device == InputDevice.Mouse);
            e.SetClass("gamepad", name is not null && device == InputDevice.Gamepad);
            e.SetClass("unbound", name is null);
            if (name is null)
            {
                e.Value = null;
                label.IsVisible = false;
                e.Style(s => s.BackgroundImage(""));
                return;
            }

            e.Value = DeviceName(device) + ":" + name;
            var art = KitArt.GlyphId(device, name);
            var cap = device == InputDevice.Keyboard && !KitArt.IsPictureKey(name);
            label.IsVisible = cap;
            label.Text = cap ? KeyLabel(name) : "";
            e.Style(s => s.BackgroundImage(art).BackgroundSlice(Length.Ui(device == InputDevice.Keyboard ? KitArt.KeySlice : 0)));
        });
        return root;
    }

    private static string DeviceName(InputDevice d) => d switch
    {
        InputDevice.Keyboard => "keyboard",
        InputDevice.Mouse => "mouse",
        _ => "gamepad",
    };

    // The text on a key cap: DOM key names become what is printed on keyboards.
    private static string KeyLabel(string name) => name switch
    {
        "Escape" => "Esc",
        " " => "Space",
        "Control" => "Ctrl",
        "Backspace" => "Bksp",
        _ => name.Length == 1 ? name.ToUpperInvariant() : name,
    };
}
