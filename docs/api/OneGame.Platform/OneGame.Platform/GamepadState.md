[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# GamepadState class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class GamepadState
```

Current state of one gamepad on the standard layout.

## Properties

| Member | Description |
| --- | --- |
| [`CanRumble`](#m-canrumble) | True when the device accepts rumble requests. |
| [`Id`](#m-id) | The gamepad id carried by its events. |
| [`IsConnected`](#m-isconnected) | Whether the device is still connected. |
| [`IsMapped`](#m-ismapped) | True when the device matched an entry in the mapping database; false when the standard layout was assumed. |
| [`Name`](#m-name) | The device name. |

### <a id="m-canrumble"></a>CanRumble

```csharp
public bool CanRumble { get; }
```

True when the device accepts rumble requests.

**Type:** `bool`

### <a id="m-id"></a>Id

```csharp
public int Id { get; }
```

The gamepad id carried by its events.

**Type:** `int`

### <a id="m-isconnected"></a>IsConnected

```csharp
public bool IsConnected { get; }
```

Whether the device is still connected.

**Type:** `bool`

### <a id="m-ismapped"></a>IsMapped

```csharp
public bool IsMapped { get; }
```

True when the device matched an entry in the mapping database; false when the standard layout was assumed.

**Type:** `bool`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The device name.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`GetAxis(GamepadAxis)`](#m-getaxis-gamepadaxis) | The current value of an axis. |
| [`IsDown(GamepadButton)`](#m-isdown-gamepadbutton) | Whether a button is held. |

### <a id="m-getaxis-gamepadaxis"></a>GetAxis(GamepadAxis)

```csharp
public float GetAxis(GamepadAxis axis);
```

The current value of an axis.

**Parameters**

- `axis` ([`GamepadAxis`](GamepadAxis.md))

**Returns:** `float`

### <a id="m-isdown-gamepadbutton"></a>IsDown(GamepadButton)

```csharp
public bool IsDown(GamepadButton button);
```

Whether a button is held.

**Parameters**

- `button` ([`GamepadButton`](GamepadButton.md))

**Returns:** `bool`
