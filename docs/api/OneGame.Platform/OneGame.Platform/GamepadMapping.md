[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# GamepadMapping class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class GamepadMapping
```

One entry of a gamepad mapping database in the community text format: a device GUID, a name, and comma-separated `target:source` pairs such as `a:b0,leftx:a0,dpup:h0.1`. Sources are buttons (`bN`), axes (`aN`, with an optional `+`/`-` half-axis prefix and `~` inversion suffix) and hats (`hN.mask`).

## Properties

| Member | Description |
| --- | --- |
| [`DeviceId`](#m-deviceid) | The device GUID as 32 lower-case hex digits. |
| [`Name`](#m-name) | The device name from the database. |
| [`Platform`](#m-platform) | The platform field (for example `Linux`), or empty when the entry has none. |

### <a id="m-deviceid"></a>DeviceId

```csharp
public string DeviceId { get; }
```

The device GUID as 32 lower-case hex digits.

**Type:** `string`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The device name from the database.

**Type:** `string`

### <a id="m-platform"></a>Platform

```csharp
public string Platform { get; }
```

The platform field (for example `Linux`), or empty when the entry has none.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`TryParse(string, out GamepadMapping?)`](#m-tryparse-string-gamepadmapping) | Parses one database line. |

### <a id="m-tryparse-string-gamepadmapping"></a>TryParse(string, out GamepadMapping?)

```csharp
public static bool TryParse(string line, out GamepadMapping? mapping);
```

Parses one database line. Returns false for comments, blank lines and lines that are not valid entries.

**Parameters**

- `line` (`string`)
- `mapping` ([`GamepadMapping`](GamepadMapping.md)?)

**Returns:** `bool`
