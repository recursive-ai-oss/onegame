[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamActionChange struct

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SteamActionChange`](SteamActionChange.md)&gt;

```csharp
public readonly struct SteamActionChange : IEquatable<SteamActionChange>
```

One change of an action's state, as [`SteamInputService.ActionChanged`](SteamInputService.md#m-actionchanged) reports it.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamActionChange(SteamController, string, SteamActionKind, bool, float, float)`](#m-ctor-steamcontroller-string-steamactionkind-bool-float-float) | One change of an action's state, as [`SteamInputService.ActionChanged`](SteamInputService.md#m-actionchanged) reports it. |

### <a id="m-ctor-steamcontroller-string-steamactionkind-bool-float-float"></a>SteamActionChange(SteamController, string, SteamActionKind, bool, float, float)

```csharp
public SteamActionChange(SteamController Controller, string Action, SteamActionKind Kind, bool Down, float X, float Y);
```

One change of an action's state, as [`SteamInputService.ActionChanged`](SteamInputService.md#m-actionchanged) reports it.

**Parameters**

- `Controller` ([`SteamController`](SteamController.md)): The controller it came from.
- `Action` (`string`): The action's name.
- `Kind` ([`SteamActionKind`](SteamActionKind.md)): The action's kind.
- `Down` (`bool`): For a digital action, whether it is down.
- `X` (`float`): For analog actions, the horizontal (or only) axis. Positive is right.
- `Y` (`float`): For two-axis actions, the vertical axis. Positive is up, as Steam reports it.

## Properties

| Member | Description |
| --- | --- |
| [`Action`](#m-action) | The action's name. |
| [`Controller`](#m-controller) | The controller it came from. |
| [`Down`](#m-down) | For a digital action, whether it is down. |
| [`Kind`](#m-kind) | The action's kind. |
| [`X`](#m-x) | For analog actions, the horizontal (or only) axis. |
| [`Y`](#m-y) | For two-axis actions, the vertical axis. |

### <a id="m-action"></a>Action

```csharp
public string Action { get; init; }
```

The action's name.

**Type:** `string`

### <a id="m-controller"></a>Controller

```csharp
public SteamController Controller { get; init; }
```

The controller it came from.

**Type:** [`SteamController`](SteamController.md)

### <a id="m-down"></a>Down

```csharp
public bool Down { get; init; }
```

For a digital action, whether it is down.

**Type:** `bool`

### <a id="m-kind"></a>Kind

```csharp
public SteamActionKind Kind { get; init; }
```

The action's kind.

**Type:** [`SteamActionKind`](SteamActionKind.md)

### <a id="m-x"></a>X

```csharp
public float X { get; init; }
```

For analog actions, the horizontal (or only) axis. Positive is right.

**Type:** `float`

### <a id="m-y"></a>Y

```csharp
public float Y { get; init; }
```

For two-axis actions, the vertical axis. Positive is up, as Steam reports it.

**Type:** `float`
