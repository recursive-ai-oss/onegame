[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# UiEvent struct

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`UiEvent`](UiEvent.md)&gt;

```csharp
public readonly struct UiEvent : IEquatable<UiEvent>
```

One input or element event. Build input with the static factories; handlers receive the same shape.

## Properties

| Member | Description |
| --- | --- |
| [`Button`](#m-button) | Pointer button: 0 primary, 1 secondary, 2 middle. |
| [`CursorEnd`](#m-cursorend) | For [`UiEventKind.Composition`](UiEventKind.md#m-composition): index where the highlighted part ends (the caret when equal to [`UiEvent.CursorStart`](UiEvent.md#m-cursorstart)), or -1. |
| [`CursorStart`](#m-cursorstart) | For [`UiEventKind.Composition`](UiEventKind.md#m-composition): index in [`UiEvent.Text`](UiEvent.md#m-text) where the highlighted part starts, or -1 for none. |
| [`DeltaX`](#m-deltax) | Horizontal scroll amount in pixels. |
| [`DeltaY`](#m-deltay) | Vertical scroll amount in pixels. |
| [`Direction`](#m-direction) | The direction of a [`UiEventKind.Navigate`](UiEventKind.md#m-navigate). |
| [`Key`](#m-key) | The DOM `KeyboardEvent.key` value, such as `Enter`, `ArrowLeft`, `a`. |
| [`Kind`](#m-kind) | What happened. |
| [`Modifiers`](#m-modifiers) | Modifiers held during a key event. |
| [`Repeat`](#m-repeat) | True for auto-repeat key events. |
| [`Text`](#m-text) | The committed text of a [`UiEventKind.TextInput`](UiEventKind.md#m-textinput). |
| [`X`](#m-x) | Pointer X in screen pixels. |
| [`Y`](#m-y) | Pointer Y in screen pixels. |

### <a id="m-button"></a>Button

```csharp
public int Button { get; init; }
```

Pointer button: 0 primary, 1 secondary, 2 middle. A [`UiEventKind.Click`](UiEventKind.md#m-click) raised by keyboard or gamepad activation carries -1.

**Type:** `int`

### <a id="m-cursorend"></a>CursorEnd

```csharp
public int CursorEnd { get; init; }
```

For [`UiEventKind.Composition`](UiEventKind.md#m-composition): index where the highlighted part ends (the caret when equal to [`UiEvent.CursorStart`](UiEvent.md#m-cursorstart)), or -1.

**Type:** `int`

### <a id="m-cursorstart"></a>CursorStart

```csharp
public int CursorStart { get; init; }
```

For [`UiEventKind.Composition`](UiEventKind.md#m-composition): index in [`UiEvent.Text`](UiEvent.md#m-text) where the highlighted part starts, or -1 for none.

**Type:** `int`

### <a id="m-deltax"></a>DeltaX

```csharp
public float DeltaX { get; init; }
```

Horizontal scroll amount in pixels.

**Type:** `float`

### <a id="m-deltay"></a>DeltaY

```csharp
public float DeltaY { get; init; }
```

Vertical scroll amount in pixels.

**Type:** `float`

### <a id="m-direction"></a>Direction

```csharp
public NavDirection Direction { get; init; }
```

The direction of a [`UiEventKind.Navigate`](UiEventKind.md#m-navigate).

**Type:** [`NavDirection`](NavDirection.md)

### <a id="m-key"></a>Key

```csharp
public string? Key { get; init; }
```

The DOM `KeyboardEvent.key` value, such as `Enter`, `ArrowLeft`, `a`.

**Type:** `string?`

### <a id="m-kind"></a>Kind

```csharp
public UiEventKind Kind { get; init; }
```

What happened.

**Type:** [`UiEventKind`](UiEventKind.md)

### <a id="m-modifiers"></a>Modifiers

```csharp
public KeyModifiers Modifiers { get; init; }
```

Modifiers held during a key event.

**Type:** [`KeyModifiers`](KeyModifiers.md)

### <a id="m-repeat"></a>Repeat

```csharp
public bool Repeat { get; init; }
```

True for auto-repeat key events.

**Type:** `bool`

### <a id="m-text"></a>Text

```csharp
public string? Text { get; init; }
```

The committed text of a [`UiEventKind.TextInput`](UiEventKind.md#m-textinput).

**Type:** `string?`

### <a id="m-x"></a>X

```csharp
public float X { get; init; }
```

Pointer X in screen pixels.

**Type:** `float`

### <a id="m-y"></a>Y

```csharp
public float Y { get; init; }
```

Pointer Y in screen pixels.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`Activate()`](#m-activate) | The confirm button. |
| [`Cancel()`](#m-cancel) | The back button. |
| [`Composition(string, int, int)`](#m-composition-string-int-int) | An input method's pre-edit text. |
| [`KeyDown(string, KeyModifiers, bool)`](#m-keydown-string-keymodifiers-bool) | A key press. |
| [`KeyUp(string, KeyModifiers)`](#m-keyup-string-keymodifiers) | A key release. |
| [`Navigate(NavDirection)`](#m-navigate-navdirection) | A focus move. |
| [`PointerDown(float, float, int)`](#m-pointerdown-float-float-int) | A pointer button press. |
| [`PointerLeftWindow()`](#m-pointerleftwindow) | The pointer left the window. |
| [`PointerMove(float, float)`](#m-pointermove-float-float) | A pointer move. |
| [`PointerUp(float, float, int)`](#m-pointerup-float-float-int) | A pointer button release. |
| [`TextInput(string)`](#m-textinput-string) | Committed text. |
| [`Wheel(float, float, float, float)`](#m-wheel-float-float-float-float) | A scroll. |

### <a id="m-activate"></a>Activate()

```csharp
public static UiEvent Activate();
```

The confirm button.

**Returns:** [`UiEvent`](UiEvent.md)

### <a id="m-cancel"></a>Cancel()

```csharp
public static UiEvent Cancel();
```

The back button.

**Returns:** [`UiEvent`](UiEvent.md)

### <a id="m-composition-string-int-int"></a>Composition(string, int, int)

```csharp
public static UiEvent Composition(string text, int cursorStart = -1, int cursorEnd = -1);
```

An input method's pre-edit text. An empty string ends the composition.

**Parameters**

- `text` (`string`)
- `cursorStart` (`int`)
- `cursorEnd` (`int`)

**Returns:** [`UiEvent`](UiEvent.md)

### <a id="m-keydown-string-keymodifiers-bool"></a>KeyDown(string, KeyModifiers, bool)

```csharp
public static UiEvent KeyDown(string key, KeyModifiers modifiers = 0, bool repeat = false);
```

A key press.

**Parameters**

- `key` (`string`)
- `modifiers` ([`KeyModifiers`](KeyModifiers.md))
- `repeat` (`bool`)

**Returns:** [`UiEvent`](UiEvent.md)

### <a id="m-keyup-string-keymodifiers"></a>KeyUp(string, KeyModifiers)

```csharp
public static UiEvent KeyUp(string key, KeyModifiers modifiers = 0);
```

A key release.

**Parameters**

- `key` (`string`)
- `modifiers` ([`KeyModifiers`](KeyModifiers.md))

**Returns:** [`UiEvent`](UiEvent.md)

### <a id="m-navigate-navdirection"></a>Navigate(NavDirection)

```csharp
public static UiEvent Navigate(NavDirection direction);
```

A focus move.

**Parameters**

- `direction` ([`NavDirection`](NavDirection.md))

**Returns:** [`UiEvent`](UiEvent.md)

### <a id="m-pointerdown-float-float-int"></a>PointerDown(float, float, int)

```csharp
public static UiEvent PointerDown(float x, float y, int button = 0);
```

A pointer button press.

**Parameters**

- `x` (`float`)
- `y` (`float`)
- `button` (`int`)

**Returns:** [`UiEvent`](UiEvent.md)

### <a id="m-pointerleftwindow"></a>PointerLeftWindow()

```csharp
public static UiEvent PointerLeftWindow();
```

The pointer left the window.

**Returns:** [`UiEvent`](UiEvent.md)

### <a id="m-pointermove-float-float"></a>PointerMove(float, float)

```csharp
public static UiEvent PointerMove(float x, float y);
```

A pointer move.

**Parameters**

- `x` (`float`)
- `y` (`float`)

**Returns:** [`UiEvent`](UiEvent.md)

### <a id="m-pointerup-float-float-int"></a>PointerUp(float, float, int)

```csharp
public static UiEvent PointerUp(float x, float y, int button = 0);
```

A pointer button release.

**Parameters**

- `x` (`float`)
- `y` (`float`)
- `button` (`int`)

**Returns:** [`UiEvent`](UiEvent.md)

### <a id="m-textinput-string"></a>TextInput(string)

```csharp
public static UiEvent TextInput(string text);
```

Committed text.

**Parameters**

- `text` (`string`)

**Returns:** [`UiEvent`](UiEvent.md)

### <a id="m-wheel-float-float-float-float"></a>Wheel(float, float, float, float)

```csharp
public static UiEvent Wheel(float x, float y, float dx, float dy);
```

A scroll.

**Parameters**

- `x` (`float`)
- `y` (`float`)
- `dx` (`float`)
- `dy` (`float`)

**Returns:** [`UiEvent`](UiEvent.md)
