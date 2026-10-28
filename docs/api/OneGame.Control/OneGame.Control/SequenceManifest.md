[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SequenceManifest record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SequenceManifest`](SequenceManifest.md)&gt;

```csharp
public sealed record SequenceManifest : IEquatable<SequenceManifest>
```

The manifest of a captured sequence (also written to `manifest.json` beside the frames).

## Constructors

| Member | Description |
| --- | --- |
| [`SequenceManifest(string, int, int, float, int, int, string, SequenceFrame[])`](#m-ctor-string-int-int-float-int-int-string-sequenceframe) | The manifest of a captured sequence (also written to `manifest.json` beside the frames). |

### <a id="m-ctor-string-int-int-float-int-int-string-sequenceframe"></a>SequenceManifest(string, int, int, float, int, int, string, SequenceFrame\[\])

```csharp
public SequenceManifest(string Game, int Width, int Height, float Scale, int Fps, int TicksPerFrame, string Renderer, SequenceFrame[] Frames);
```

The manifest of a captured sequence (also written to `manifest.json` beside the frames).

**Parameters**

- `Game` (`string`): The game's name.
- `Width` (`int`): Pixel width of each frame.
- `Height` (`int`): Pixel height of each frame.
- `Scale` (`float`): The UI scale.
- `Fps` (`int`): Frames per second of the playback the sequence is meant for.
- `TicksPerFrame` (`int`): Simulation ticks between captured frames.
- `Renderer` (`string`): What drew the frames.
- `Frames` ([`SequenceFrame`](SequenceFrame.md)\[\]): The frames in order.

## Properties

| Member | Description |
| --- | --- |
| [`Fps`](#m-fps) | Frames per second of the playback the sequence is meant for. |
| [`Frames`](#m-frames) | The frames in order. |
| [`Game`](#m-game) | The game's name. |
| [`Height`](#m-height) | Pixel height of each frame. |
| [`Renderer`](#m-renderer) | What drew the frames. |
| [`Scale`](#m-scale) | The UI scale. |
| [`TicksPerFrame`](#m-ticksperframe) | Simulation ticks between captured frames. |
| [`Width`](#m-width) | Pixel width of each frame. |

### <a id="m-fps"></a>Fps

```csharp
public int Fps { get; init; }
```

Frames per second of the playback the sequence is meant for.

**Type:** `int`

### <a id="m-frames"></a>Frames

```csharp
public SequenceFrame[] Frames { get; init; }
```

The frames in order.

**Type:** [`SequenceFrame`](SequenceFrame.md)\[\]

### <a id="m-game"></a>Game

```csharp
public string Game { get; init; }
```

The game's name.

**Type:** `string`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Pixel height of each frame.

**Type:** `int`

### <a id="m-renderer"></a>Renderer

```csharp
public string Renderer { get; init; }
```

What drew the frames.

**Type:** `string`

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; init; }
```

The UI scale.

**Type:** `float`

### <a id="m-ticksperframe"></a>TicksPerFrame

```csharp
public int TicksPerFrame { get; init; }
```

Simulation ticks between captured frames.

**Type:** `int`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Pixel width of each frame.

**Type:** `int`
