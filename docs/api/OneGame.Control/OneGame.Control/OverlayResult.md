[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# OverlayResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`OverlayResult`](OverlayResult.md)&gt;

```csharp
public sealed record OverlayResult : IEquatable<OverlayResult>
```

Result of the `overlay` operation.

## Constructors

| Member | Description |
| --- | --- |
| [`OverlayResult(bool, string?, string[], bool, string[])`](#m-ctor-bool-string-string-bool-string) | Result of the `overlay` operation. |

### <a id="m-ctor-bool-string-string-bool-string"></a>OverlayResult(bool, string?, string\[\], bool, string\[\])

```csharp
public OverlayResult(bool Visible, string? Key, string[] Chord, bool Masked, string[] Lines);
```

Result of the `overlay` operation.

**Parameters**

- `Visible` (`bool`): Whether the debug panel is shown.
- `Key` (`string?`): The toggle key, or null for none.
- `Chord` (`string[]`): The toggle buttons on a gamepad, or empty for none.
- `Masked` (`bool`): Whether machine-dependent figures show as `--`.
- `Lines` (`string[]`): The panel's text now.

## Properties

| Member | Description |
| --- | --- |
| [`Chord`](#m-chord) | The toggle buttons on a gamepad, or empty for none. |
| [`Key`](#m-key) | The toggle key, or null for none. |
| [`Lines`](#m-lines) | The panel's text now. |
| [`Masked`](#m-masked) | Whether machine-dependent figures show as `--`. |
| [`Visible`](#m-visible) | Whether the debug panel is shown. |

### <a id="m-chord"></a>Chord

```csharp
public string[] Chord { get; init; }
```

The toggle buttons on a gamepad, or empty for none.

**Type:** `string[]`

### <a id="m-key"></a>Key

```csharp
public string? Key { get; init; }
```

The toggle key, or null for none.

**Type:** `string?`

### <a id="m-lines"></a>Lines

```csharp
public string[] Lines { get; init; }
```

The panel's text now.

**Type:** `string[]`

### <a id="m-masked"></a>Masked

```csharp
public bool Masked { get; init; }
```

Whether machine-dependent figures show as `--`.

**Type:** `bool`

### <a id="m-visible"></a>Visible

```csharp
public bool Visible { get; init; }
```

Whether the debug panel is shown.

**Type:** `bool`
