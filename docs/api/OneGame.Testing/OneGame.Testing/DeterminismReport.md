[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# DeterminismReport record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DeterminismReport`](DeterminismReport.md)&gt;

```csharp
public sealed record DeterminismReport : IEquatable<DeterminismReport>
```

The outcome of a determinism check.

## Constructors

| Member | Description |
| --- | --- |
| [`DeterminismReport(string, bool, int, int, ulong, ulong)`](#m-ctor-string-bool-int-int-ulong-ulong) | The outcome of a determinism check. |

### <a id="m-ctor-string-bool-int-int-ulong-ulong"></a>DeterminismReport(string, bool, int, int, ulong, ulong)

```csharp
public DeterminismReport(string Check, bool Identical, int Frames, int FirstMismatchFrame, ulong Expected, ulong Actual);
```

The outcome of a determinism check.

**Parameters**

- `Check` (`string`): Which check ran.
- `Identical` (`bool`): True when every frame's hash matched.
- `Frames` (`int`): Frames compared.
- `FirstMismatchFrame` (`int`): The first frame whose hash differed, or -1.
- `Expected` (`ulong`): The hash of the reference run at that frame.
- `Actual` (`ulong`): The hash of the other run at that frame.

## Properties

| Member | Description |
| --- | --- |
| [`Actual`](#m-actual) | The hash of the other run at that frame. |
| [`Check`](#m-check) | Which check ran. |
| [`Expected`](#m-expected) | The hash of the reference run at that frame. |
| [`FirstMismatchFrame`](#m-firstmismatchframe) | The first frame whose hash differed, or -1. |
| [`Frames`](#m-frames) | Frames compared. |
| [`Identical`](#m-identical) | True when every frame's hash matched. |

### <a id="m-actual"></a>Actual

```csharp
public ulong Actual { get; init; }
```

The hash of the other run at that frame.

**Type:** `ulong`

### <a id="m-check"></a>Check

```csharp
public string Check { get; init; }
```

Which check ran.

**Type:** `string`

### <a id="m-expected"></a>Expected

```csharp
public ulong Expected { get; init; }
```

The hash of the reference run at that frame.

**Type:** `ulong`

### <a id="m-firstmismatchframe"></a>FirstMismatchFrame

```csharp
public int FirstMismatchFrame { get; init; }
```

The first frame whose hash differed, or -1.

**Type:** `int`

### <a id="m-frames"></a>Frames

```csharp
public int Frames { get; init; }
```

Frames compared.

**Type:** `int`

### <a id="m-identical"></a>Identical

```csharp
public bool Identical { get; init; }
```

True when every frame's hash matched.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`ThrowIfDifferent()`](#m-throwifdifferent) | Throws [`DeterminismException`](DeterminismException.md) unless the runs were identical. |

### <a id="m-throwifdifferent"></a>ThrowIfDifferent()

```csharp
public void ThrowIfDifferent();
```

Throws [`DeterminismException`](DeterminismException.md) unless the runs were identical.
