[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# Bind static class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public static class Bind
```

Short ways to write bindings: `Bind.Key(Key.Space)`, `Bind.Wasd`, `Bind.LeftStick`.

## Properties

| Member | Description |
| --- | --- |
| [`Arrows`](#m-arrows) | The arrow keys as a vector. |
| [`DPad`](#m-dpad) | The pad's d-pad as a vector. |
| [`LeftStick`](#m-leftstick) | The left stick as a vector. |
| [`RightStick`](#m-rightstick) | The right stick as a vector. |
| [`Wasd`](#m-wasd) | W, A, S, D as a vector (by key position, so it works on other layouts too). |

### <a id="m-arrows"></a>Arrows

```csharp
public static ActionBinding Arrows { get; }
```

The arrow keys as a vector.

**Type:** [`ActionBinding`](ActionBinding.md)

### <a id="m-dpad"></a>DPad

```csharp
public static ActionBinding DPad { get; }
```

The pad's d-pad as a vector.

**Type:** [`ActionBinding`](ActionBinding.md)

### <a id="m-leftstick"></a>LeftStick

```csharp
public static ActionBinding LeftStick { get; }
```

The left stick as a vector.

**Type:** [`ActionBinding`](ActionBinding.md)

### <a id="m-rightstick"></a>RightStick

```csharp
public static ActionBinding RightStick { get; }
```

The right stick as a vector.

**Type:** [`ActionBinding`](ActionBinding.md)

### <a id="m-wasd"></a>Wasd

```csharp
public static ActionBinding Wasd { get; }
```

W, A, S, D as a vector (by key position, so it works on other layouts too).

**Type:** [`ActionBinding`](ActionBinding.md)

## Methods

| Member | Description |
| --- | --- |
| [`Key(Key)`](#m-key-key) | A keyboard key. |
| [`Keys(Key, Key, Key, Key)`](#m-keys-key-key-key-key) | Four keys making a vector. |
| [`Mouse(MouseButton)`](#m-mouse-mousebutton) | A mouse button. |
| [`Pad(GamepadButton)`](#m-pad-gamepadbutton) | A pad button (South is A on Xbox and Cross on PlayStation). |
| [`PadAxis(GamepadAxis, int)`](#m-padaxis-gamepadaxis-int) | A pad axis. |
| [`Pads(GamepadButton, GamepadButton, GamepadButton, GamepadButton)`](#m-pads-gamepadbutton-gamepadbutton-gamepadbutton-gamepadbutton) | Four pad buttons making a vector. |
| [`Pair(InputSource, InputSource)`](#m-pair-inputsource-inputsource) | Two sources making a 1D axis: `negative` gives -1, `positive` gives +1. |
| [`Quad(InputSource, InputSource, InputSource, InputSource)`](#m-quad-inputsource-inputsource-inputsource-inputsource) | Four sources making a 2D vector. |
| [`Stick(bool)`](#m-stick-bool) | A pad stick as a vector. |
| [`Touch(TouchRegion)`](#m-touch-touchregion) | A finger down in part of the window. |
| [`Wheel(int, bool)`](#m-wheel-int-bool) | The mouse wheel in notches per frame (positive away from the user). |

### <a id="m-key-key"></a>Key(Key)

```csharp
public static InputSource Key(Key key);
```

A keyboard key.

**Parameters**

- `key` ([`Key`](Key.md))

**Returns:** [`InputSource`](InputSource.md)

### <a id="m-keys-key-key-key-key"></a>Keys(Key, Key, Key, Key)

```csharp
public static ActionBinding Keys(Key left, Key right, Key up, Key down);
```

Four keys making a vector.

**Parameters**

- `left` ([`Key`](Key.md))
- `right` ([`Key`](Key.md))
- `up` ([`Key`](Key.md))
- `down` ([`Key`](Key.md))

**Returns:** [`ActionBinding`](ActionBinding.md)

### <a id="m-mouse-mousebutton"></a>Mouse(MouseButton)

```csharp
public static InputSource Mouse(MouseButton button);
```

A mouse button.

**Parameters**

- `button` ([`MouseButton`](MouseButton.md))

**Returns:** [`InputSource`](InputSource.md)

### <a id="m-pad-gamepadbutton"></a>Pad(GamepadButton)

```csharp
public static InputSource Pad(GamepadButton button);
```

A pad button (South is A on Xbox and Cross on PlayStation).

**Parameters**

- `button` ([`GamepadButton`](GamepadButton.md))

**Returns:** [`InputSource`](InputSource.md)

### <a id="m-padaxis-gamepadaxis-int"></a>PadAxis(GamepadAxis, int)

```csharp
public static InputSource PadAxis(GamepadAxis axis, int sign = 1);
```

A pad axis. For buttons and composites it counts as pressed in the direction of `sign`; on a 1D axis action it is the value, inverted when `sign` is -1.

**Parameters**

- `axis` ([`GamepadAxis`](GamepadAxis.md))
- `sign` (`int`)

**Returns:** [`InputSource`](InputSource.md)

### <a id="m-pads-gamepadbutton-gamepadbutton-gamepadbutton-gamepadbutton"></a>Pads(GamepadButton, GamepadButton, GamepadButton, GamepadButton)

```csharp
public static ActionBinding Pads(GamepadButton left, GamepadButton right, GamepadButton up, GamepadButton down);
```

Four pad buttons making a vector.

**Parameters**

- `left` ([`GamepadButton`](GamepadButton.md))
- `right` ([`GamepadButton`](GamepadButton.md))
- `up` ([`GamepadButton`](GamepadButton.md))
- `down` ([`GamepadButton`](GamepadButton.md))

**Returns:** [`ActionBinding`](ActionBinding.md)

### <a id="m-pair-inputsource-inputsource"></a>Pair(InputSource, InputSource)

```csharp
public static ActionBinding Pair(InputSource negative, InputSource positive);
```

Two sources making a 1D axis: `negative` gives -1, `positive` gives +1.

**Parameters**

- `negative` ([`InputSource`](InputSource.md))
- `positive` ([`InputSource`](InputSource.md))

**Returns:** [`ActionBinding`](ActionBinding.md)

### <a id="m-quad-inputsource-inputsource-inputsource-inputsource"></a>Quad(InputSource, InputSource, InputSource, InputSource)

```csharp
public static ActionBinding Quad(InputSource left, InputSource right, InputSource up, InputSource down);
```

Four sources making a 2D vector. Up is -y, down is +y.

**Parameters**

- `left` ([`InputSource`](InputSource.md))
- `right` ([`InputSource`](InputSource.md))
- `up` ([`InputSource`](InputSource.md))
- `down` ([`InputSource`](InputSource.md))

**Returns:** [`ActionBinding`](ActionBinding.md)

### <a id="m-stick-bool"></a>Stick(bool)

```csharp
public static ActionBinding Stick(bool right);
```

A pad stick as a vector.

**Parameters**

- `right` (`bool`)

**Returns:** [`ActionBinding`](ActionBinding.md)

### <a id="m-touch-touchregion"></a>Touch(TouchRegion)

```csharp
public static InputSource Touch(TouchRegion region = 0);
```

A finger down in part of the window.

**Parameters**

- `region` ([`TouchRegion`](TouchRegion.md))

**Returns:** [`InputSource`](InputSource.md)

### <a id="m-wheel-int-bool"></a>Wheel(int, bool)

```csharp
public static InputSource Wheel(int sign = 1, bool horizontal = false);
```

The mouse wheel in notches per frame (positive away from the user).

**Parameters**

- `sign` (`int`)
- `horizontal` (`bool`)

**Returns:** [`InputSource`](InputSource.md)
