[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SequenceResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SequenceResult`](SequenceResult.md)&gt;

```csharp
public sealed record SequenceResult : IEquatable<SequenceResult>
```

Result of `capture_sequence`: a folder of numbered PNGs and a manifest.

## Constructors

| Member | Description |
| --- | --- |
| [`SequenceResult(string, string, int, int, int, int)`](#m-ctor-string-string-int-int-int-int) | Result of `capture_sequence`: a folder of numbered PNGs and a manifest. |

### <a id="m-ctor-string-string-int-int-int-int"></a>SequenceResult(string, string, int, int, int, int)

```csharp
public SequenceResult(string Directory, string Manifest, int Frames, int Width, int Height, int Fps);
```

Result of `capture_sequence`: a folder of numbered PNGs and a manifest.

**Parameters**

- `Directory` (`string`): The folder written, under the output folder.
- `Manifest` (`string`): The manifest file's path.
- `Frames` (`int`): How many frames were captured.
- `Width` (`int`): Pixel width of each frame.
- `Height` (`int`): Pixel height of each frame.
- `Fps` (`int`): Playback frames per second.

## Properties

| Member | Description |
| --- | --- |
| [`Directory`](#m-directory) | The folder written, under the output folder. |
| [`Fps`](#m-fps) | Playback frames per second. |
| [`Frames`](#m-frames) | How many frames were captured. |
| [`Height`](#m-height) | Pixel height of each frame. |
| [`Manifest`](#m-manifest) | The manifest file's path. |
| [`Width`](#m-width) | Pixel width of each frame. |

### <a id="m-directory"></a>Directory

```csharp
public string Directory { get; init; }
```

The folder written, under the output folder.

**Type:** `string`

### <a id="m-fps"></a>Fps

```csharp
public int Fps { get; init; }
```

Playback frames per second.

**Type:** `int`

### <a id="m-frames"></a>Frames

```csharp
public int Frames { get; init; }
```

How many frames were captured.

**Type:** `int`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Pixel height of each frame.

**Type:** `int`

### <a id="m-manifest"></a>Manifest

```csharp
public string Manifest { get; init; }
```

The manifest file's path.

**Type:** `string`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Pixel width of each frame.

**Type:** `int`
