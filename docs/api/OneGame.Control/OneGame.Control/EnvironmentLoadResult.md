[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# EnvironmentLoadResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`EnvironmentLoadResult`](EnvironmentLoadResult.md)&gt;

```csharp
public sealed record EnvironmentLoadResult : IEquatable<EnvironmentLoadResult>
```

Progress of an asynchronous environment load started with the `environment` operation's `load` or `sky` argument.

## Constructors

| Member | Description |
| --- | --- |
| [`EnvironmentLoadResult(string, double, string, string?)`](#m-ctor-string-double-string-string) | Progress of an asynchronous environment load started with the `environment` operation's `load` or `sky` argument. |

### <a id="m-ctor-string-double-string-string"></a>EnvironmentLoadResult(string, double, string, string?)

```csharp
public EnvironmentLoadResult(string State, double Progress, string Slot, string? Error);
```

Progress of an asynchronous environment load started with the `environment` operation's `load` or `sky` argument.

**Parameters**

- `State` (`string`): converting, uploading, filtering, ready, failed or canceled. Ready means the map has been (or is about to be) swapped in.
- `Progress` (`double`): 0 to 1.
- `Slot` (`string`): map or next: the scene environment the result replaces.
- `Error` (`string?`): Why a failed load failed.

## Properties

| Member | Description |
| --- | --- |
| [`Error`](#m-error) | Why a failed load failed. |
| [`Progress`](#m-progress) | 0 to 1. |
| [`Slot`](#m-slot) | map or next: the scene environment the result replaces. |
| [`State`](#m-state) | converting, uploading, filtering, ready, failed or canceled. |

### <a id="m-error"></a>Error

```csharp
public string? Error { get; init; }
```

Why a failed load failed.

**Type:** `string?`

### <a id="m-progress"></a>Progress

```csharp
public double Progress { get; init; }
```

0 to 1.

**Type:** `double`

### <a id="m-slot"></a>Slot

```csharp
public string Slot { get; init; }
```

map or next: the scene environment the result replaces.

**Type:** `string`

### <a id="m-state"></a>State

```csharp
public string State { get; init; }
```

converting, uploading, filtering, ready, failed or canceled. Ready means the map has been (or is about to be) swapped in.

**Type:** `string`
