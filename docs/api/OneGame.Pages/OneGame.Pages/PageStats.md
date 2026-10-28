[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages

# PageStats struct

**Namespace:** `OneGame.Pages`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PageStats`](PageStats.md)&gt;

```csharp
public readonly struct PageStats : IEquatable<PageStats>
```

What the last [`PageRenderer.AddPasses()`](PageRenderer.md#m-addpasses-rendergraph-graphtexture-ireadonlylist-pagescreen-pagescopes-pageframe) did.

## Constructors

| Member | Description |
| --- | --- |
| [`PageStats(int, int, int, int, int, int, int, int, int)`](#m-ctor-int-int-int-int-int-int-int-int-int) | What the last [`PageRenderer.AddPasses()`](PageRenderer.md#m-addpasses-rendergraph-graphtexture-ireadonlylist-pagescreen-pagescopes-pageframe) did. |

### <a id="m-ctor-int-int-int-int-int-int-int-int-int"></a>PageStats(int, int, int, int, int, int, int, int, int)

```csharp
public PageStats(int Passes, int Instances, int Layers, int Dropped, int MissingImages, int RefusedGlyphs, int PathBitmaps, int ArtPages, int GlyphPages);
```

What the last [`PageRenderer.AddPasses()`](PageRenderer.md#m-addpasses-rendergraph-graphtexture-ireadonlylist-pagescreen-pagescopes-pageframe) did.

**Parameters**

- `Passes` (`int`)
- `Instances` (`int`)
- `Layers` (`int`)
- `Dropped` (`int`)
- `MissingImages` (`int`)
- `RefusedGlyphs` (`int`)
- `PathBitmaps` (`int`)
- `ArtPages` (`int`)
- `GlyphPages` (`int`)

## Properties

| Member | Description |
| --- | --- |
| [`ArtPages`](#m-artpages) |  |
| [`Dropped`](#m-dropped) |  |
| [`GlyphPages`](#m-glyphpages) |  |
| [`Instances`](#m-instances) |  |
| [`Layers`](#m-layers) |  |
| [`MissingImages`](#m-missingimages) |  |
| [`Passes`](#m-passes) |  |
| [`PathBitmaps`](#m-pathbitmaps) |  |
| [`RefusedGlyphs`](#m-refusedglyphs) |  |

### <a id="m-artpages"></a>ArtPages

```csharp
public int ArtPages { get; init; }
```

**Type:** `int`

### <a id="m-dropped"></a>Dropped

```csharp
public int Dropped { get; init; }
```

**Type:** `int`

### <a id="m-glyphpages"></a>GlyphPages

```csharp
public int GlyphPages { get; init; }
```

**Type:** `int`

### <a id="m-instances"></a>Instances

```csharp
public int Instances { get; init; }
```

**Type:** `int`

### <a id="m-layers"></a>Layers

```csharp
public int Layers { get; init; }
```

**Type:** `int`

### <a id="m-missingimages"></a>MissingImages

```csharp
public int MissingImages { get; init; }
```

**Type:** `int`

### <a id="m-passes"></a>Passes

```csharp
public int Passes { get; init; }
```

**Type:** `int`

### <a id="m-pathbitmaps"></a>PathBitmaps

```csharp
public int PathBitmaps { get; init; }
```

**Type:** `int`

### <a id="m-refusedglyphs"></a>RefusedGlyphs

```csharp
public int RefusedGlyphs { get; init; }
```

**Type:** `int`
