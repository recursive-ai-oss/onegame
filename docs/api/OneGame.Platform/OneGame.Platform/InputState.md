[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# InputState class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class InputState
```

What is currently held: keys, pointer, buttons and gamepads. It is updated by the same path that queues events, so injected and real input agree.

## Properties

| Member | Description |
| --- | --- |
| [`Modifiers`](#m-modifiers) | The modifiers implied by the keys currently held. |
| [`PointerX`](#m-pointerx) | The last known pointer x in pixels. |
| [`PointerY`](#m-pointery) | The last known pointer y in pixels. |
| [`TouchCount`](#m-touchcount) | How many fingers are on the screen. |

### <a id="m-modifiers"></a>Modifiers

```csharp
public KeyModifiers Modifiers { get; }
```

The modifiers implied by the keys currently held.

**Type:** [`KeyModifiers`](KeyModifiers.md)

### <a id="m-pointerx"></a>PointerX

```csharp
public float PointerX { get; }
```

The last known pointer x in pixels.

**Type:** `float`

### <a id="m-pointery"></a>PointerY

```csharp
public float PointerY { get; }
```

The last known pointer y in pixels.

**Type:** `float`

### <a id="m-touchcount"></a>TouchCount

```csharp
public int TouchCount { get; }
```

How many fingers are on the screen.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`GetGamepad(int)`](#m-getgamepad-int) | The state of a connected gamepad, or null when the id is unknown. |
| [`IsButtonDown(MouseButton)`](#m-isbuttondown-mousebutton) | Whether a pointer button is held. |
| [`IsKeyDown(Key)`](#m-iskeydown-key) | Whether a key is held. |
| [`TryGetTouch(int, out float, out float)`](#m-trygettouch-int-float-float) | Where a finger is, in window pixels. |

### <a id="m-getgamepad-int"></a>GetGamepad(int)

```csharp
public GamepadState? GetGamepad(int id);
```

The state of a connected gamepad, or null when the id is unknown.

**Parameters**

- `id` (`int`)

**Returns:** [`GamepadState`](GamepadState.md)?

### <a id="m-isbuttondown-mousebutton"></a>IsButtonDown(MouseButton)

```csharp
public bool IsButtonDown(MouseButton button);
```

Whether a pointer button is held.

**Parameters**

- `button` ([`MouseButton`](MouseButton.md))

**Returns:** `bool`

### <a id="m-iskeydown-key"></a>IsKeyDown(Key)

```csharp
public bool IsKeyDown(Key key);
```

Whether a key is held.

**Parameters**

- `key` ([`Key`](Key.md))

**Returns:** `bool`

### <a id="m-trygettouch-int-float-float"></a>TryGetTouch(int, out float, out float)

```csharp
public bool TryGetTouch(int touchId, out float x, out float y);
```

Where a finger is, in window pixels. False when that finger is not down.

**Parameters**

- `touchId` (`int`)
- `x` (`float`)
- `y` (`float`)

**Returns:** `bool`
