[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# CaptureResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CaptureResult`](CaptureResult.md)&gt;

```csharp
public sealed record CaptureResult : IEquatable<CaptureResult>
```

Result of `capture_frame`.

## Constructors

| Member | Description |
| --- | --- |
| [`CaptureResult(string?, int, int, float, string, string, long, string)`](#m-ctor-string-int-int-float-string-string-long-string) | Result of `capture_frame`. |

### <a id="m-ctor-string-int-int-float-string-string-long-string"></a>CaptureResult(string?, int, int, float, string, string, long, string)

```csharp
public CaptureResult(string? Path, int Width, int Height, float Scale, string Sha256, string Renderer, long Tick, string Hash);
```

Result of `capture_frame`.

**Parameters**

- `Path` (`string?`)
- `Width` (`int`)
- `Height` (`int`)
- `Scale` (`float`)
- `Sha256` (`string`)
- `Renderer` (`string`)
- `Tick` (`long`)
- `Hash` (`string`)

## Properties

| Member | Description |
| --- | --- |
| [`Hash`](#m-hash) |  |
| [`Height`](#m-height) |  |
| [`Path`](#m-path) |  |
| [`Renderer`](#m-renderer) |  |
| [`Scale`](#m-scale) |  |
| [`Sha256`](#m-sha256) |  |
| [`Tick`](#m-tick) |  |
| [`Width`](#m-width) |  |

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

**Type:** `string`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

**Type:** `int`

### <a id="m-path"></a>Path

```csharp
public string? Path { get; init; }
```

**Type:** `string?`

### <a id="m-renderer"></a>Renderer

```csharp
public string Renderer { get; init; }
```

**Type:** `string`

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; init; }
```

**Type:** `float`

### <a id="m-sha256"></a>Sha256

```csharp
public string Sha256 { get; init; }
```

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

**Type:** `long`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

**Type:** `int`
