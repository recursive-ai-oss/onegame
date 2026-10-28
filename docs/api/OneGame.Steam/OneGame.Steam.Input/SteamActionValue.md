[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamActionValue record

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SteamActionValue`](SteamActionValue.md)&gt;

```csharp
public sealed record SteamActionValue : IEquatable<SteamActionValue>
```

An action value, for the report.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamActionValue(int, string, string, bool, float, float)`](#m-ctor-int-string-string-bool-float-float) | An action value, for the report. |

### <a id="m-ctor-int-string-string-bool-float-float"></a>SteamActionValue(int, string, string, bool, float, float)

```csharp
public SteamActionValue(int Controller, string Action, string Kind, bool Down, float X, float Y);
```

An action value, for the report.

**Parameters**

- `Controller` (`int`): The controller's index.
- `Action` (`string`): The action's name.
- `Kind` (`string`): digital, stick or trigger.
- `Down` (`bool`): A digital action's state.
- `X` (`float`): The first axis.
- `Y` (`float`): The second axis.

## Properties

| Member | Description |
| --- | --- |
| [`Action`](#m-action) | The action's name. |
| [`Controller`](#m-controller) | The controller's index. |
| [`Down`](#m-down) | A digital action's state. |
| [`Kind`](#m-kind) | digital, stick or trigger. |
| [`X`](#m-x) | The first axis. |
| [`Y`](#m-y) | The second axis. |

### <a id="m-action"></a>Action

```csharp
public string Action { get; init; }
```

The action's name.

**Type:** `string`

### <a id="m-controller"></a>Controller

```csharp
public int Controller { get; init; }
```

The controller's index.

**Type:** `int`

### <a id="m-down"></a>Down

```csharp
public bool Down { get; init; }
```

A digital action's state.

**Type:** `bool`

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; init; }
```

digital, stick or trigger.

**Type:** `string`

### <a id="m-x"></a>X

```csharp
public float X { get; init; }
```

The first axis.

**Type:** `float`

### <a id="m-y"></a>Y

```csharp
public float Y { get; init; }
```

The second axis.

**Type:** `float`
