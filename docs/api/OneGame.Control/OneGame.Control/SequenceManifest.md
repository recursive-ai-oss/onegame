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

- `Game` (`string`)
- `Width` (`int`)
- `Height` (`int`)
- `Scale` (`float`)
- `Fps` (`int`)
- `TicksPerFrame` (`int`)
- `Renderer` (`string`)
- `Frames` ([`SequenceFrame`](SequenceFrame.md)\[\])

## Properties

| Member | Description |
| --- | --- |
| [`Fps`](#m-fps) |  |
| [`Frames`](#m-frames) |  |
| [`Game`](#m-game) |  |
| [`Height`](#m-height) |  |
| [`Renderer`](#m-renderer) |  |
| [`Scale`](#m-scale) |  |
| [`TicksPerFrame`](#m-ticksperframe) |  |
| [`Width`](#m-width) |  |

### <a id="m-fps"></a>Fps

```csharp
public int Fps { get; init; }
```

**Type:** `int`

### <a id="m-frames"></a>Frames

```csharp
public SequenceFrame[] Frames { get; init; }
```

**Type:** [`SequenceFrame`](SequenceFrame.md)\[\]

### <a id="m-game"></a>Game

```csharp
public string Game { get; init; }
```

**Type:** `string`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

**Type:** `int`

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

### <a id="m-ticksperframe"></a>TicksPerFrame

```csharp
public int TicksPerFrame { get; init; }
```

**Type:** `int`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

**Type:** `int`
