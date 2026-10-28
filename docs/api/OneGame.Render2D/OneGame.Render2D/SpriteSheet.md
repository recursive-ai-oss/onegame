[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# SpriteSheet class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)

```csharp
public sealed class SpriteSheet
```

The frames of one texture: a grid of equal cells or explicit rectangles, plus named [`SpriteClip`](SpriteClip.md)s. A sheet is view data (it holds a GPU texture and no game state); build it once and draw from it every frame.

## Properties

| Member | Description |
| --- | --- |
| [`ClipNames`](#m-clipnames) | The names of the clips added so far. |
| [`FrameCount`](#m-framecount) | How many frames the sheet has. |
| [`Resized`](#m-resized) | True once a reload gave the texture another size and the grid was cut again. |
| [`Texture`](#m-texture) | The image all the frames are cut from. |

### <a id="m-clipnames"></a>ClipNames

```csharp
public IEnumerable<string> ClipNames { get; }
```

The names of the clips added so far.

**Type:** [`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)`<string>`

### <a id="m-framecount"></a>FrameCount

```csharp
public int FrameCount { get; }
```

How many frames the sheet has.

**Type:** `int`

### <a id="m-resized"></a>Resized

```csharp
public bool Resized { get; }
```

True once a reload gave the texture another size and the grid was cut again. Frame numbers past the new count then show the last frame instead of throwing.

**Type:** `bool`

### <a id="m-texture"></a>Texture

```csharp
public Texture2D Texture { get; }
```

The image all the frames are cut from.

**Type:** [`Texture2D`](Texture2D.md)

## Methods

| Member | Description |
| --- | --- |
| [`AddClip(string, ReadOnlySpan<int>, float, bool)`](#m-addclip-string-readonlyspan-int-float-bool) | Adds a clip of the given frames in order (they may repeat or skip). |
| [`AddClip(string, int, int, float, bool)`](#m-addclip-string-int-int-float-bool) | Adds a clip of consecutive frames. |
| [`Clip(string)`](#m-clip-string) | Finds a clip by name. |
| [`Frame(int)`](#m-frame-int) | The pixel rectangle of a frame. |
| [`Frames(Texture2D, ReadOnlySpan<PixelRect>)`](#m-frames-texture2d-readonlyspan-pixelrect) | Uses explicit rectangles as the frames, numbered in the order given. |
| [`Grid(Texture2D, int, int, int, int)`](#m-grid-texture2d-int-int-int-int) | Cuts the texture into a grid of equal cells, numbered left to right then top to bottom. |

### <a id="m-addclip-string-readonlyspan-int-float-bool"></a>AddClip(string, ReadOnlySpan&lt;int&gt;, float, bool)

```csharp
public SpriteClip AddClip(string name, ReadOnlySpan<int> frames, float fps, bool loop = true);
```

Adds a clip of the given frames in order (they may repeat or skip).

**Parameters**

- `name` (`string`): The clip's name.
- `frames` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<int>`): Frame numbers.
- `fps` (`float`): Frames per second; above zero.
- `loop` (`bool`): Start over after the last frame, or hold it.

**Returns:** [`SpriteClip`](SpriteClip.md) — The clip, which is also kept under its name.

### <a id="m-addclip-string-int-int-float-bool"></a>AddClip(string, int, int, float, bool)

```csharp
public SpriteClip AddClip(string name, int firstFrame, int frameCount, float fps, bool loop = true);
```

Adds a clip of consecutive frames.

**Parameters**

- `name` (`string`): The clip's name.
- `firstFrame` (`int`): The first frame.
- `frameCount` (`int`): How many frames.
- `fps` (`float`): Frames per second; above zero.
- `loop` (`bool`): Start over after the last frame, or hold it.

**Returns:** [`SpriteClip`](SpriteClip.md) — The clip, which is also kept under its name.

### <a id="m-clip-string"></a>Clip(string)

```csharp
public SpriteClip Clip(string name);
```

Finds a clip by name.

**Parameters**

- `name` (`string`): The clip's name.

**Returns:** [`SpriteClip`](SpriteClip.md) — The clip.

**Exceptions**

- [`UnknownClipException`](UnknownClipException.md): The sheet has no such clip.

### <a id="m-frame-int"></a>Frame(int)

```csharp
public PixelRect Frame(int index);
```

The pixel rectangle of a frame.

**Parameters**

- `index` (`int`): The frame number.

**Returns:** [`PixelRect`](PixelRect.md) — The rectangle.

### <a id="m-frames-texture2d-readonlyspan-pixelrect"></a>Frames(Texture2D, ReadOnlySpan&lt;PixelRect&gt;)

```csharp
public static SpriteSheet Frames(Texture2D texture, ReadOnlySpan<PixelRect> frames);
```

Uses explicit rectangles as the frames, numbered in the order given.

**Parameters**

- `texture` ([`Texture2D`](Texture2D.md)): The image.
- `frames` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`PixelRect`](PixelRect.md)&gt;): Pixel rectangles inside the texture.

**Returns:** [`SpriteSheet`](SpriteSheet.md) — The sheet.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): There are no frames, or one lies outside the texture.

### <a id="m-grid-texture2d-int-int-int-int"></a>Grid(Texture2D, int, int, int, int)

```csharp
public static SpriteSheet Grid(Texture2D texture, int frameWidth, int frameHeight, int margin = 0, int spacing = 0);
```

Cuts the texture into a grid of equal cells, numbered left to right then top to bottom.

**Parameters**

- `texture` ([`Texture2D`](Texture2D.md)): The image.
- `frameWidth` (`int`): Cell width in pixels.
- `frameHeight` (`int`): Cell height in pixels.
- `margin` (`int`): Pixels around the whole grid.
- `spacing` (`int`): Pixels between cells.

**Returns:** [`SpriteSheet`](SpriteSheet.md) — The sheet. It follows the texture: when a development reload changes the image's size, the grid is cut again.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): Not even one cell fits in the texture.
