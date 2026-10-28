[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# PlatformEvent struct

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PlatformEvent`](PlatformEvent.md)&gt;

```csharp
public readonly struct PlatformEvent : IEquatable<PlatformEvent>
```

One input or lifecycle event. Every source (OS, gamepad devices, synthetic injection) produces the same shape, and only the fields listed for the event's kind are meaningful. Positions and sizes are in physical pixels.

## Properties

| Member | Description |
| --- | --- |
| [`Button`](#m-button) | The button for pointer button events. |
| [`CursorEnd`](#m-cursorend) | For [`PlatformEventKind.TextComposition`](PlatformEventKind.md#m-textcomposition): UTF-16 index where the cursor or highlight ends (equal to [`PlatformEvent.CursorStart`](PlatformEvent.md#m-cursorstart) for a plain caret), or -1. |
| [`CursorStart`](#m-cursorstart) | For [`PlatformEventKind.TextComposition`](PlatformEventKind.md#m-textcomposition): UTF-16 index in [`PlatformEvent.Text`](PlatformEvent.md#m-text) where the input method's cursor or highlight starts, or -1 when it asks for no cursor. |
| [`DeltaX`](#m-deltax) | Horizontal wheel or relative motion amount. |
| [`DeltaY`](#m-deltay) | Vertical wheel or relative motion amount. |
| [`Focused`](#m-focused) | True for gained focus or pointer entered, false for lost or left. |
| [`GamepadAxis`](#m-gamepadaxis) | The axis for [`PlatformEventKind.GamepadAxisMoved`](PlatformEventKind.md#m-gamepadaxismoved). |
| [`GamepadButton`](#m-gamepadbutton) | The button for gamepad button events. |
| [`GamepadId`](#m-gamepadid) | The gamepad for gamepad events. |
| [`Height`](#m-height) | Window height in pixels for [`PlatformEventKind.WindowResized`](PlatformEventKind.md#m-windowresized). |
| [`Insets`](#m-insets) | The safe area for [`PlatformEventKind.SafeAreaChanged`](PlatformEventKind.md#m-safeareachanged): how far content must stay from each edge. |
| [`IsRepeat`](#m-isrepeat) | True when a [`PlatformEventKind.KeyDown`](PlatformEventKind.md#m-keydown) is auto-repeat of a key already held. |
| [`Key`](#m-key) | The physical key for key events. |
| [`Kind`](#m-kind) | What happened. |
| [`Lifecycle`](#m-lifecycle) | What happened for [`PlatformEventKind.AppLifecycle`](PlatformEventKind.md#m-applifecycle). |
| [`Modifiers`](#m-modifiers) | Modifiers held at the time, for key and pointer events. |
| [`Pressure`](#m-pressure) | How hard the finger presses, 0 to 1, for touch events; 1 where the screen cannot tell. |
| [`Scale`](#m-scale) | Content scale for [`PlatformEventKind.WindowScaleChanged`](PlatformEventKind.md#m-windowscalechanged). |
| [`Text`](#m-text) | The typed text for [`PlatformEventKind.TextInput`](PlatformEventKind.md#m-textinput): one or more complete Unicode characters. |
| [`TouchId`](#m-touchid) | The finger for touch events. |
| [`Value`](#m-value) | The new axis value for [`PlatformEventKind.GamepadAxisMoved`](PlatformEventKind.md#m-gamepadaxismoved). |
| [`Width`](#m-width) | Window width in pixels for [`PlatformEventKind.WindowResized`](PlatformEventKind.md#m-windowresized). |
| [`WindowId`](#m-windowid) | The window the event belongs to, or 0 for events that have none (gamepads, quit). |
| [`X`](#m-x) | Pointer x in window pixels. |
| [`Y`](#m-y) | Pointer y in window pixels. |

### <a id="m-button"></a>Button

```csharp
public MouseButton Button { get; init; }
```

The button for pointer button events.

**Type:** [`MouseButton`](MouseButton.md)

### <a id="m-cursorend"></a>CursorEnd

```csharp
public int CursorEnd { get; init; }
```

For [`PlatformEventKind.TextComposition`](PlatformEventKind.md#m-textcomposition): UTF-16 index where the cursor or highlight ends (equal to [`PlatformEvent.CursorStart`](PlatformEvent.md#m-cursorstart) for a plain caret), or -1.

**Type:** `int`

### <a id="m-cursorstart"></a>CursorStart

```csharp
public int CursorStart { get; init; }
```

For [`PlatformEventKind.TextComposition`](PlatformEventKind.md#m-textcomposition): UTF-16 index in [`PlatformEvent.Text`](PlatformEvent.md#m-text) where the input method's cursor or highlight starts, or -1 when it asks for no cursor.

**Type:** `int`

### <a id="m-deltax"></a>DeltaX

```csharp
public float DeltaX { get; init; }
```

Horizontal wheel or relative motion amount.

**Type:** `float`

### <a id="m-deltay"></a>DeltaY

```csharp
public float DeltaY { get; init; }
```

Vertical wheel or relative motion amount.

**Type:** `float`

### <a id="m-focused"></a>Focused

```csharp
public bool Focused { get; init; }
```

True for gained focus or pointer entered, false for lost or left.

**Type:** `bool`

### <a id="m-gamepadaxis"></a>GamepadAxis

```csharp
public GamepadAxis GamepadAxis { get; init; }
```

The axis for [`PlatformEventKind.GamepadAxisMoved`](PlatformEventKind.md#m-gamepadaxismoved).

**Type:** [`GamepadAxis`](GamepadAxis.md)

### <a id="m-gamepadbutton"></a>GamepadButton

```csharp
public GamepadButton GamepadButton { get; init; }
```

The button for gamepad button events.

**Type:** [`GamepadButton`](GamepadButton.md)

### <a id="m-gamepadid"></a>GamepadId

```csharp
public int GamepadId { get; init; }
```

The gamepad for gamepad events. Ids are never reused within one run.

**Type:** `int`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Window height in pixels for [`PlatformEventKind.WindowResized`](PlatformEventKind.md#m-windowresized).

**Type:** `int`

### <a id="m-insets"></a>Insets

```csharp
public EdgeInsets Insets { get; init; }
```

The safe area for [`PlatformEventKind.SafeAreaChanged`](PlatformEventKind.md#m-safeareachanged): how far content must stay from each edge.

**Type:** [`EdgeInsets`](EdgeInsets.md)

### <a id="m-isrepeat"></a>IsRepeat

```csharp
public bool IsRepeat { get; init; }
```

True when a [`PlatformEventKind.KeyDown`](PlatformEventKind.md#m-keydown) is auto-repeat of a key already held.

**Type:** `bool`

### <a id="m-key"></a>Key

```csharp
public Key Key { get; init; }
```

The physical key for key events.

**Type:** [`Key`](Key.md)

### <a id="m-kind"></a>Kind

```csharp
public PlatformEventKind Kind { get; init; }
```

What happened.

**Type:** [`PlatformEventKind`](PlatformEventKind.md)

### <a id="m-lifecycle"></a>Lifecycle

```csharp
public AppLifecycleState Lifecycle { get; init; }
```

What happened for [`PlatformEventKind.AppLifecycle`](PlatformEventKind.md#m-applifecycle).

**Type:** [`AppLifecycleState`](AppLifecycleState.md)

### <a id="m-modifiers"></a>Modifiers

```csharp
public KeyModifiers Modifiers { get; init; }
```

Modifiers held at the time, for key and pointer events.

**Type:** [`KeyModifiers`](KeyModifiers.md)

### <a id="m-pressure"></a>Pressure

```csharp
public float Pressure { get; init; }
```

How hard the finger presses, 0 to 1, for touch events; 1 where the screen cannot tell.

**Type:** `float`

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; init; }
```

Content scale for [`PlatformEventKind.WindowScaleChanged`](PlatformEventKind.md#m-windowscalechanged).

**Type:** `float`

### <a id="m-text"></a>Text

```csharp
public string? Text { get; init; }
```

The typed text for [`PlatformEventKind.TextInput`](PlatformEventKind.md#m-textinput): one or more complete Unicode characters.

**Type:** `string?`

### <a id="m-touchid"></a>TouchId

```csharp
public int TouchId { get; init; }
```

The finger for touch events. Ids are small integers that are reused once a finger lifts.

**Type:** `int`

### <a id="m-value"></a>Value

```csharp
public float Value { get; init; }
```

The new axis value for [`PlatformEventKind.GamepadAxisMoved`](PlatformEventKind.md#m-gamepadaxismoved).

**Type:** `float`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Window width in pixels for [`PlatformEventKind.WindowResized`](PlatformEventKind.md#m-windowresized).

**Type:** `int`

### <a id="m-windowid"></a>WindowId

```csharp
public int WindowId { get; init; }
```

The window the event belongs to, or 0 for events that have none (gamepads, quit).

**Type:** `int`

### <a id="m-x"></a>X

```csharp
public float X { get; init; }
```

Pointer x in window pixels.

**Type:** `float`

### <a id="m-y"></a>Y

```csharp
public float Y { get; init; }
```

Pointer y in window pixels.

**Type:** `float`
