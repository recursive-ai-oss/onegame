[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SequenceResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SequenceResult`](SequenceResult.md)&gt;

```csharp
public sealed record SequenceResult : IEquatable<SequenceResult>
```

Result of `capture_sequence`.

## Constructors

| Member | Description |
| --- | --- |
| [`SequenceResult(string, string, int, int, int, int)`](#m-ctor-string-string-int-int-int-int) | Result of `capture_sequence`. |

### <a id="m-ctor-string-string-int-int-int-int"></a>SequenceResult(string, string, int, int, int, int)

```csharp
public SequenceResult(string Directory, string Manifest, int Frames, int Width, int Height, int Fps);
```

Result of `capture_sequence`.

**Parameters**

- `Directory` (`string`)
- `Manifest` (`string`)
- `Frames` (`int`)
- `Width` (`int`)
- `Height` (`int`)
- `Fps` (`int`)

## Properties

| Member | Description |
| --- | --- |
| [`Directory`](#m-directory) |  |
| [`Fps`](#m-fps) |  |
| [`Frames`](#m-frames) |  |
| [`Height`](#m-height) |  |
| [`Manifest`](#m-manifest) |  |
| [`Width`](#m-width) |  |

### <a id="m-directory"></a>Directory

```csharp
public string Directory { get; init; }
```

**Type:** `string`

### <a id="m-fps"></a>Fps

```csharp
public int Fps { get; init; }
```

**Type:** `int`

### <a id="m-frames"></a>Frames

```csharp
public int Frames { get; init; }
```

**Type:** `int`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

**Type:** `int`

### <a id="m-manifest"></a>Manifest

```csharp
public string Manifest { get; init; }
```

**Type:** `string`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

**Type:** `int`
