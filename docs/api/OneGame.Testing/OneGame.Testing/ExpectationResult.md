[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ExpectationResult record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ExpectationResult`](ExpectationResult.md)&gt;

```csharp
public sealed record ExpectationResult : IEquatable<ExpectationResult>
```

Outcome of one expectation. On failure [`ExpectationResult.Expected`](ExpectationResult.md#m-expected) and [`ExpectationResult.Actual`](ExpectationResult.md#m-actual) are the diff.

## Constructors

| Member | Description |
| --- | --- |
| [`ExpectationResult(int, string, int, bool, string?, string?, string?, string?, string?, string?)`](#m-ctor-int-string-int-bool-string-string-string-string-string-string) | Outcome of one expectation. |

### <a id="m-ctor-int-string-int-bool-string-string-string-string-string-string"></a>ExpectationResult(int, string, int, bool, string?, string?, string?, string?, string?, string?)

```csharp
public ExpectationResult(int Index, string Kind, int Line, bool Passed, string? Target, string? Expected, string? Actual, string? Message, string? Node = null, string? ReferenceAction = null);
```

Outcome of one expectation. On failure [`ExpectationResult.Expected`](ExpectationResult.md#m-expected) and [`ExpectationResult.Actual`](ExpectationResult.md#m-actual) are the diff.

**Parameters**

- `Index` (`int`): Position among the final expectations, or the step index for inline ones.
- `Kind` (`string`): The expectation's kind.
- `Line` (`int`): Line in the scenario file.
- `Passed` (`bool`): Whether it held.
- `Target` (`string?`): What was looked at, such as `Shop#1.Gold` or `#buy`.
- `Expected` (`string?`): What the scenario said.
- `Actual` (`string?`): What the game had.
- `Message` (`string?`): A sentence on a failure, or a note on a pass.
- `Node` (`string?`): The cluster node the expectation looked at, or null.
- `ReferenceAction` (`string?`): `created` or `updated` when a capture check wrote its reference image (update mode), otherwise null.

## Properties

| Member | Description |
| --- | --- |
| [`Actual`](#m-actual) | What the game had. |
| [`Expected`](#m-expected) | What the scenario said. |
| [`Index`](#m-index) | Position among the final expectations, or the step index for inline ones. |
| [`Kind`](#m-kind) | The expectation's kind. |
| [`Line`](#m-line) | Line in the scenario file. |
| [`Message`](#m-message) | A sentence on a failure, or a note on a pass. |
| [`Node`](#m-node) | The cluster node the expectation looked at, or null. |
| [`Passed`](#m-passed) | Whether it held. |
| [`ReferenceAction`](#m-referenceaction) | `created` or `updated` when a capture check wrote its reference image (update mode), otherwise null. |
| [`Target`](#m-target) | What was looked at, such as `Shop#1.Gold` or `#buy`. |

### <a id="m-actual"></a>Actual

```csharp
public string? Actual { get; init; }
```

What the game had.

**Type:** `string?`

### <a id="m-expected"></a>Expected

```csharp
public string? Expected { get; init; }
```

What the scenario said.

**Type:** `string?`

### <a id="m-index"></a>Index

```csharp
public int Index { get; init; }
```

Position among the final expectations, or the step index for inline ones.

**Type:** `int`

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; init; }
```

The expectation's kind.

**Type:** `string`

### <a id="m-line"></a>Line

```csharp
public int Line { get; init; }
```

Line in the scenario file.

**Type:** `int`

### <a id="m-message"></a>Message

```csharp
public string? Message { get; init; }
```

A sentence on a failure, or a note on a pass.

**Type:** `string?`

### <a id="m-node"></a>Node

```csharp
public string? Node { get; init; }
```

The cluster node the expectation looked at, or null.

**Type:** `string?`

### <a id="m-passed"></a>Passed

```csharp
public bool Passed { get; init; }
```

Whether it held.

**Type:** `bool`

### <a id="m-referenceaction"></a>ReferenceAction

```csharp
public string? ReferenceAction { get; init; }
```

`created` or `updated` when a capture check wrote its reference image (update mode), otherwise null.

**Type:** `string?`

### <a id="m-target"></a>Target

```csharp
public string? Target { get; init; }
```

What was looked at, such as `Shop#1.Gold` or `#buy`.

**Type:** `string?`
