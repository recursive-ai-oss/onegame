[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# CaptureResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CaptureResult`](CaptureResult.md)&gt;

```csharp
public sealed record CaptureResult : IEquatable<CaptureResult>
```

Result of `capture_frame`. The PNG itself travels beside the JSON ([`ControlResult.Png`](ControlResult.md#m-png)) or in `Path`.

## Constructors

| Member | Description |
| --- | --- |
| [`CaptureResult(string?, int, int, float, string, string, long, string)`](#m-ctor-string-int-int-float-string-string-long-string) | Result of `capture_frame`. |

### <a id="m-ctor-string-int-int-float-string-string-long-string"></a>CaptureResult(string?, int, int, float, string, string, long, string)

```csharp
public CaptureResult(string? Path, int Width, int Height, float Scale, string Sha256, string Renderer, long Tick, string Hash);
```

Result of `capture_frame`. The PNG itself travels beside the JSON ([`ControlResult.Png`](ControlResult.md#m-png)) or in `Path`.

**Parameters**

- `Path` (`string?`): The file written under the output folder, or null when none was asked for.
- `Width` (`int`): Pixel width.
- `Height` (`int`): Pixel height.
- `Scale` (`float`): The UI scale used.
- `Sha256` (`string`): Hash of the PNG bytes; the same state on the same renderer gives the same hash.
- `Renderer` (`string`): What drew it, such as `2d` or `cpu`.
- `Tick` (`long`): The world's tick when it was taken.
- `Hash` (`string`): The state hash when it was taken.

## Properties

| Member | Description |
| --- | --- |
| [`Hash`](#m-hash) | The state hash when it was taken. |
| [`Height`](#m-height) | Pixel height. |
| [`Path`](#m-path) | The file written under the output folder, or null when none was asked for. |
| [`Renderer`](#m-renderer) | What drew it, such as `2d` or `cpu`. |
| [`Scale`](#m-scale) | The UI scale used. |
| [`Sha256`](#m-sha256) | Hash of the PNG bytes; the same state on the same renderer gives the same hash. |
| [`Tick`](#m-tick) | The world's tick when it was taken. |
| [`Width`](#m-width) | Pixel width. |

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The state hash when it was taken.

**Type:** `string`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Pixel height.

**Type:** `int`

### <a id="m-path"></a>Path

```csharp
public string? Path { get; init; }
```

The file written under the output folder, or null when none was asked for.

**Type:** `string?`

### <a id="m-renderer"></a>Renderer

```csharp
public string Renderer { get; init; }
```

What drew it, such as `2d` or `cpu`.

**Type:** `string`

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; init; }
```

The UI scale used.

**Type:** `float`

### <a id="m-sha256"></a>Sha256

```csharp
public string Sha256 { get; init; }
```

Hash of the PNG bytes; the same state on the same renderer gives the same hash.

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The world's tick when it was taken.

**Type:** `long`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Pixel width.

**Type:** `int`
