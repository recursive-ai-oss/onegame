[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# AtlasPage class

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)

```csharp
public sealed class AtlasPage
```

One texture-sized page of pixels in system memory. The atlas writes into it; the renderer uploads the dirty region to a texture and samples it. Rows are tightly packed ([`AtlasPage.Stride`](AtlasPage.md#m-stride) = width × bytes per pixel), top row first.

## Properties

| Member | Description |
| --- | --- |
| [`BytesPerPixel`](#m-bytesperpixel) | Gets the bytes per pixel (1 or 4). |
| [`Dirty`](#m-dirty) | Gets the part of the page changed since the last [`AtlasPage.TakeDirty()`](AtlasPage.md#m-takedirty). |
| [`Format`](#m-format) | Gets the pixel layout. |
| [`Height`](#m-height) | Gets the height in pixels. |
| [`Index`](#m-index) | Gets the page number within its atlas. |
| [`Pixels`](#m-pixels) | Gets all pixels of the page. |
| [`Stride`](#m-stride) | Gets the bytes per row. |
| [`Version`](#m-version) | Gets a counter that grows with every write or clear. |
| [`Width`](#m-width) | Gets the width in pixels. |

### <a id="m-bytesperpixel"></a>BytesPerPixel

```csharp
public int BytesPerPixel { get; }
```

Gets the bytes per pixel (1 or 4).

**Type:** `int`

### <a id="m-dirty"></a>Dirty

```csharp
public AtlasRect Dirty { get; }
```

Gets the part of the page changed since the last [`AtlasPage.TakeDirty()`](AtlasPage.md#m-takedirty). A new page starts fully dirty.

**Type:** [`AtlasRect`](AtlasRect.md)

### <a id="m-format"></a>Format

```csharp
public AtlasFormat Format { get; }
```

Gets the pixel layout.

**Type:** [`AtlasFormat`](AtlasFormat.md)

### <a id="m-height"></a>Height

```csharp
public int Height { get; }
```

Gets the height in pixels.

**Type:** `int`

### <a id="m-index"></a>Index

```csharp
public int Index { get; }
```

Gets the page number within its atlas. The renderer keeps one texture per index.

**Type:** `int`

### <a id="m-pixels"></a>Pixels

```csharp
public ReadOnlySpan<byte> Pixels { get; }
```

Gets all pixels of the page.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`

### <a id="m-stride"></a>Stride

```csharp
public int Stride { get; }
```

Gets the bytes per row.

**Type:** `int`

### <a id="m-version"></a>Version

```csharp
public int Version { get; }
```

Gets a counter that grows with every write or clear. Equal values mean equal pixels.

**Type:** `int`

### <a id="m-width"></a>Width

```csharp
public int Width { get; }
```

Gets the width in pixels.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`RowOf(AtlasRect, int)`](#m-rowof-atlasrect-int) | Returns the bytes of one row of `rect` (its width × bytes per pixel), for uploading a dirty region row by row. |
| [`TakeDirty()`](#m-takedirty) | Returns the dirty region and marks the page clean. |

### <a id="m-rowof-atlasrect-int"></a>RowOf(AtlasRect, int)

```csharp
public ReadOnlySpan<byte> RowOf(AtlasRect rect, int row);
```

Returns the bytes of one row of `rect` (its width × bytes per pixel), for uploading a dirty region row by row.

**Parameters**

- `rect` ([`AtlasRect`](AtlasRect.md)): A rectangle inside the page.
- `row` (`int`): Row within the rectangle, 0-based.

**Returns:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>` — The row's bytes.

### <a id="m-takedirty"></a>TakeDirty()

```csharp
public AtlasRect TakeDirty();
```

Returns the dirty region and marks the page clean. Call it once per upload.

**Returns:** [`AtlasRect`](AtlasRect.md) — The changed rectangle, empty when nothing changed.
