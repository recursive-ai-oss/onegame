[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# InputGlyphs class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)

```csharp
public sealed class InputGlyphs
```

Maps game actions to the button each device uses, and tracks which device the player used last, so a prompt such as "Press (A) to buy" can show the right picture. Bind actions once, feed every input event through [`InputGlyphs.Observe()`](InputGlyphs.md#m-observe-uievent) (the tree reports every dispatched event to the kit), and put [`UiKit.InputGlyph()`](UiKit.md#m-inputglyph-inputglyphs-string-string) elements in the UI.

## Constructors

| Member | Description |
| --- | --- |
| [`InputGlyphs()`](#m-ctor) |  |

### <a id="m-ctor"></a>InputGlyphs()

```csharp
public InputGlyphs();
```

## Properties

| Member | Description |
| --- | --- |
| [`Device`](#m-device) | The device used last. |
| [`Version`](#m-version) | Increases whenever the device or a binding changes, so glyph elements know to update. |

### <a id="m-device"></a>Device

```csharp
public InputDevice Device { get; }
```

The device used last. Starts as [`InputDevice.Keyboard`](InputDevice.md#m-keyboard).

**Type:** [`InputDevice`](InputDevice.md)

### <a id="m-version"></a>Version

```csharp
public int Version { get; }
```

Increases whenever the device or a binding changes, so glyph elements know to update.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Bind(string, string?, string?, string?)`](#m-bind-string-string-string-string) | Binds an action. |
| [`NameFor(string, out InputDevice)`](#m-namefor-string-inputdevice) | The name an action is bound to on the current device, or on the nearest device that has one (gamepad, then keyboard, then mouse); null when the action is unbound. |
| [`Observe(in UiEvent)`](#m-observe-uievent) | Notes which device an input event came from: pointer events and wheel mean the mouse, keys and text mean the keyboard, and navigate, activate and cancel mean the gamepad (a keyboard's arrow keys arrive as keys, not as navigate). |
| [`SetDevice(InputDevice)`](#m-setdevice-inputdevice) | Sets the device directly, for a game that learns about the device from somewhere other than UI events. |
| [`SetGamepad(string, string?)`](#m-setgamepad-string-string) | Changes only an action's gamepad control, keeping its keyboard and mouse names; for a source that learns the real button at run time (the player's own controller layout). |

### <a id="m-bind-string-string-string-string"></a>Bind(string, string?, string?, string?)

```csharp
public InputGlyphs Bind(string action, string? keyboard = null, string? mouse = null, string? gamepad = null);
```

Binds an action. Keyboard names are key names (`Enter`, `Escape`, `E`, `ArrowUp`); mouse names are `left`, `right`, `middle`, `move`; gamepad names are `a b x y lb rb lt rt ls rs dpad dpad-up dpad-down dpad-left dpad-right start select`.

**Parameters**

- `action` (`string`): The action's name.
- `keyboard` (`string?`): The key, or null for none.
- `mouse` (`string?`): The mouse button, or null for none.
- `gamepad` (`string?`): The gamepad control, or null for none.

**Returns:** [`InputGlyphs`](InputGlyphs.md) — This object, for chaining.

### <a id="m-namefor-string-inputdevice"></a>NameFor(string, out InputDevice)

```csharp
public string? NameFor(string action, out InputDevice device);
```

The name an action is bound to on the current device, or on the nearest device that has one (gamepad, then keyboard, then mouse); null when the action is unbound.

**Parameters**

- `action` (`string`)
- `device` ([`InputDevice`](InputDevice.md))

**Returns:** `string?`

### <a id="m-observe-uievent"></a>Observe(in UiEvent)

```csharp
public void Observe(in UiEvent e);
```

Notes which device an input event came from: pointer events and wheel mean the mouse, keys and text mean the keyboard, and navigate, activate and cancel mean the gamepad (a keyboard's arrow keys arrive as keys, not as navigate).

**Parameters**

- `e` ([`UiEvent`](../../OneGame.UI/OneGame.UI/UiEvent.md))

### <a id="m-setdevice-inputdevice"></a>SetDevice(InputDevice)

```csharp
public void SetDevice(InputDevice device);
```

Sets the device directly, for a game that learns about the device from somewhere other than UI events.

**Parameters**

- `device` ([`InputDevice`](InputDevice.md))

### <a id="m-setgamepad-string-string"></a>SetGamepad(string, string?)

```csharp
public void SetGamepad(string action, string? gamepad);
```

Changes only an action's gamepad control, keeping its keyboard and mouse names; for a source that learns the real button at run time (the player's own controller layout). An unbound action gets a gamepad-only binding.

**Parameters**

- `action` (`string`): The action's name.
- `gamepad` (`string?`): The gamepad control name (see [`InputGlyphs.Bind()`](InputGlyphs.md#m-bind-string-string-string-string)), or null for none.
