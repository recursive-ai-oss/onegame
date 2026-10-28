[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioDeviceInfo struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudioDeviceInfo`](AudioDeviceInfo.md)&gt;

```csharp
public readonly struct AudioDeviceInfo : IEquatable<AudioDeviceInfo>
```

One output device a backend can open.

## Constructors

| Member | Description |
| --- | --- |
| [`AudioDeviceInfo(string, string, bool)`](#m-ctor-string-string-bool) | One output device a backend can open. |

### <a id="m-ctor-string-string-bool"></a>AudioDeviceInfo(string, string, bool)

```csharp
public AudioDeviceInfo(string Name, string Description, bool IsDefault);
```

One output device a backend can open.

**Parameters**

- `Name` (`string`): The name to put in [`AudioDeviceOptions.DeviceName`](AudioDeviceOptions.md#m-devicename).
- `Description` (`string`): A human-readable label.
- `IsDefault` (`bool`): Whether this is the system default.

## Properties

| Member | Description |
| --- | --- |
| [`Description`](#m-description) | A human-readable label. |
| [`IsDefault`](#m-isdefault) | Whether this is the system default. |
| [`Name`](#m-name) | The name to put in [`AudioDeviceOptions.DeviceName`](AudioDeviceOptions.md#m-devicename). |

### <a id="m-description"></a>Description

```csharp
public string Description { get; init; }
```

A human-readable label.

**Type:** `string`

### <a id="m-isdefault"></a>IsDefault

```csharp
public bool IsDefault { get; init; }
```

Whether this is the system default.

**Type:** `bool`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The name to put in [`AudioDeviceOptions.DeviceName`](AudioDeviceOptions.md#m-devicename).

**Type:** `string`
