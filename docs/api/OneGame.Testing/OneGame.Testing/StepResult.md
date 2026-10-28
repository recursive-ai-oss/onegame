[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# StepResult record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`StepResult`](StepResult.md)&gt;

```csharp
public sealed record StepResult : IEquatable<StepResult>
```

Outcome of one step.

## Constructors

| Member | Description |
| --- | --- |
| [`StepResult(int, string, int, bool, string?, string?)`](#m-ctor-int-string-int-bool-string-string) | Outcome of one step. |

### <a id="m-ctor-int-string-int-bool-string-string"></a>StepResult(int, string, int, bool, string?, string?)

```csharp
public StepResult(int Index, string Kind, int Line, bool Ok, string? Error, string? Node = null);
```

Outcome of one step.

**Parameters**

- `Index` (`int`): Position in the scenario, from 0.
- `Kind` (`string`): The step's verb.
- `Line` (`int`): Line in the scenario file.
- `Ok` (`bool`): False when the step failed; the scenario stops there.
- `Error` (`string?`): What went wrong, with the control error code.
- `Node` (`string?`): The cluster node the step was aimed at, or null.

## Properties

| Member | Description |
| --- | --- |
| [`Error`](#m-error) | What went wrong, with the control error code. |
| [`Index`](#m-index) | Position in the scenario, from 0. |
| [`Kind`](#m-kind) | The step's verb. |
| [`Line`](#m-line) | Line in the scenario file. |
| [`Node`](#m-node) | The cluster node the step was aimed at, or null. |
| [`Ok`](#m-ok) | False when the step failed; the scenario stops there. |

### <a id="m-error"></a>Error

```csharp
public string? Error { get; init; }
```

What went wrong, with the control error code.

**Type:** `string?`

### <a id="m-index"></a>Index

```csharp
public int Index { get; init; }
```

Position in the scenario, from 0.

**Type:** `int`

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; init; }
```

The step's verb.

**Type:** `string`

### <a id="m-line"></a>Line

```csharp
public int Line { get; init; }
```

Line in the scenario file.

**Type:** `int`

### <a id="m-node"></a>Node

```csharp
public string? Node { get; init; }
```

The cluster node the step was aimed at, or null.

**Type:** `string?`

### <a id="m-ok"></a>Ok

```csharp
public bool Ok { get; init; }
```

False when the step failed; the scenario stops there.

**Type:** `bool`
