[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SequenceResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SequenceResult`](SequenceResult.md)&gt;

```csharp
public sealed record SequenceResult : IEquatable<SequenceResult>
```

Result of `capture_sequence`: numbered PNGs, a manifest, mixed WAV audio and optional video.

## Constructors

| Member | Description |
| --- | --- |
| [`SequenceResult(string, string, int, int, int, int, string?, string?, string?)`](#m-ctor-string-string-int-int-int-int-string-string-string) | Result of `capture_sequence`: numbered PNGs, a manifest, mixed WAV audio and optional video. |

### <a id="m-ctor-string-string-int-int-int-int-string-string-string"></a>SequenceResult(string, string, int, int, int, int, string?, string?, string?)

```csharp
public SequenceResult(string Directory, string Manifest, int Frames, int Width, int Height, int Fps, string? Wav = null, string? Mp4 = null, string? Mp4SkipReason = null);
```

Result of `capture_sequence`: numbered PNGs, a manifest, mixed WAV audio and optional video.

**Parameters**

- `Directory` (`string`): The folder written, under the output folder.
- `Manifest` (`string`): The manifest file's path.
- `Frames` (`int`): How many frames were captured.
- `Width` (`int`): Pixel width of each frame.
- `Height` (`int`): Pixel height of each frame.
- `Fps` (`int`): Playback frames per second.
- `Wav` (`string?`): Path of the mixed audio, including silence.
- `Mp4` (`string?`): Path of the muxed video, or null when skipped.
- `Mp4SkipReason` (`string?`): Why no video was produced, or null on success.

## Properties

| Member | Description |
| --- | --- |
| [`Directory`](#m-directory) | The folder written, under the output folder. |
| [`Fps`](#m-fps) | Playback frames per second. |
| [`Frames`](#m-frames) | How many frames were captured. |
| [`Height`](#m-height) | Pixel height of each frame. |
| [`Manifest`](#m-manifest) | The manifest file's path. |
| [`Mp4`](#m-mp4) | Path of the muxed video, or null when skipped. |
| [`Mp4SkipReason`](#m-mp4skipreason) | Why no video was produced, or null on success. |
| [`Wav`](#m-wav) | Path of the mixed audio, including silence. |
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

### <a id="m-mp4"></a>Mp4

```csharp
public string? Mp4 { get; init; }
```

Path of the muxed video, or null when skipped.

**Type:** `string?`

### <a id="m-mp4skipreason"></a>Mp4SkipReason

```csharp
public string? Mp4SkipReason { get; init; }
```

Why no video was produced, or null on success.

**Type:** `string?`

### <a id="m-wav"></a>Wav

```csharp
public string? Wav { get; init; }
```

Path of the mixed audio, including silence.

**Type:** `string?`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Pixel width of each frame.

**Type:** `int`
