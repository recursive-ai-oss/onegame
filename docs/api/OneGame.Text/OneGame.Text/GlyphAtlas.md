[API reference](../../index.md) › [OneGame.Text](../index.md) › OneGame.Text

# GlyphAtlas class

**Namespace:** `OneGame.Text`  
**Package:** [`RecursiveAI.OneGame.Text`](../index.md)

```csharp
public sealed class GlyphAtlas
```

Glyph coverage atlas. Glyphs are rasterized on first use at the exact pixel size and a quarter-pixel horizontal position, from the face's outline, into [`AtlasFormat.R8`](../../OneGame.Atlas/OneGame.Atlas/AtlasFormat.md#m-r8) pages. Keyed by face, glyph, size (to 1/64 px) and subpixel step; when the pages are full the least recently used page is cleared as a whole.

## Constructors

| Member | Description |
| --- | --- |
| [`GlyphAtlas(FontLibrary, AtlasOptions?, GlyphLimits?)`](#m-ctor-fontlibrary-atlasoptions-glyphlimits) | Creates an empty atlas. |

### <a id="m-ctor-fontlibrary-atlasoptions-glyphlimits"></a>GlyphAtlas(FontLibrary, AtlasOptions?, GlyphLimits?)

```csharp
public GlyphAtlas(FontLibrary library, AtlasOptions? options = null, GlyphLimits? limits = null);
```

Creates an empty atlas.

**Parameters**

- `library` ([`FontLibrary`](FontLibrary.md)): The fonts glyphs come from.
- `options` ([`AtlasOptions`](../../OneGame.Atlas/OneGame.Atlas/AtlasOptions.md)?): Page sizes; the defaults when `null`.
- `limits` ([`GlyphLimits`](GlyphLimits.md)?): Budgets; the defaults when `null`.

## Fields

| Member | Description |
| --- | --- |
| [`ExternalFaceBase`](#m-externalfacebase) | The first face id handed out by [`GlyphAtlas.RegisterFace()`](GlyphAtlas.md#m-registerface-ulong-fontface); ids below it are the library's own. |
| [`MaxExternalFaces`](#m-maxexternalfaces) | The most faces [`GlyphAtlas.RegisterFace()`](GlyphAtlas.md#m-registerface-ulong-fontface) accepts. |
| [`SubpixelSteps`](#m-subpixelsteps) | The number of horizontal subpixel positions a glyph is rasterized at. |

### <a id="m-externalfacebase"></a>ExternalFaceBase

```csharp
public const int ExternalFaceBase = 1048576;
```

The first face id handed out by [`GlyphAtlas.RegisterFace()`](GlyphAtlas.md#m-registerface-ulong-fontface); ids below it are the library's own.

**Type:** `int`

### <a id="m-maxexternalfaces"></a>MaxExternalFaces

```csharp
public const int MaxExternalFaces = 4096;
```

The most faces [`GlyphAtlas.RegisterFace()`](GlyphAtlas.md#m-registerface-ulong-fontface) accepts.

**Type:** `int`

### <a id="m-subpixelsteps"></a>SubpixelSteps

```csharp
public const int SubpixelSteps = 4;
```

The number of horizontal subpixel positions a glyph is rasterized at.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Pages`](#m-pages) | Gets the pages. |

### <a id="m-pages"></a>Pages

```csharp
public AtlasPages Pages { get; }
```

Gets the pages. The renderer uploads [`AtlasPage.TakeDirty()`](../../OneGame.Atlas/OneGame.Atlas/AtlasPage.md#m-takedirty) of each and keeps one texture per page.

**Type:** [`AtlasPages`](../../OneGame.Atlas/OneGame.Atlas/AtlasPages.md)

## Methods

| Member | Description |
| --- | --- |
| [`BeginFrame(long)`](#m-beginframe-long) | Starts a frame; see [`AtlasPages.BeginFrame()`](../../OneGame.Atlas/OneGame.Atlas/AtlasPages.md#m-beginframe-long). |
| [`RegisterFace(ulong, FontFace)`](#m-registerface-ulong-fontface) | Makes a face that is not in the [`FontLibrary`](FontLibrary.md) drawable (OneRender's display lists carry their own face objects, identified by a stable 64-bit key). |
| [`TryGet(int, int, float, int, out GlyphInfo)`](#m-tryget-int-int-float-int-glyphinfo) | Looks up a glyph, rasterizing it on first use. |

### <a id="m-beginframe-long"></a>BeginFrame(long)

```csharp
public void BeginFrame(long frame);
```

Starts a frame; see [`AtlasPages.BeginFrame()`](../../OneGame.Atlas/OneGame.Atlas/AtlasPages.md#m-beginframe-long).

**Parameters**

- `frame` (`long`): A number that grows with every frame.

### <a id="m-registerface-ulong-fontface"></a>RegisterFace(ulong, FontFace)

```csharp
public int RegisterFace(ulong key, FontFace face);
```

Makes a face that is not in the [`FontLibrary`](FontLibrary.md) drawable (OneRender's display lists carry their own face objects, identified by a stable 64-bit key). The same key always gives the same id.

**Parameters**

- `key` (`ulong`): A key that is equal for equal font files, such as OneRender's `FaceKey`.
- `face` (`FontFace`): The face; kept for the atlas's lifetime.

**Returns:** `int` — A face id for [`GlyphAtlas.TryGet()`](GlyphAtlas.md#m-tryget-int-int-float-int-glyphinfo), or -1 when [`GlyphAtlas.MaxExternalFaces`](GlyphAtlas.md#m-maxexternalfaces) faces are already registered.

### <a id="m-tryget-int-int-float-int-glyphinfo"></a>TryGet(int, int, float, int, out GlyphInfo)

```csharp
public AtlasStatus TryGet(int faceId, int glyphId, float size, int subpixelStep, out GlyphInfo info);
```

Looks up a glyph, rasterizing it on first use.

**Parameters**

- `faceId` (`int`): Face id from a [`PositionedGlyph`](PositionedGlyph.md).
- `glyphId` (`int`): Glyph index.
- `size` (`float`): Font size in pixels (rounded to 1/64).
- `subpixelStep` (`int`): Horizontal position within the pixel in quarters, 0 to [`GlyphAtlas.SubpixelSteps`](GlyphAtlas.md#m-subpixelsteps) - 1.
- `info` ([`GlyphInfo`](GlyphInfo.md)): The glyph when the result is [`AtlasStatus.Ok`](../../OneGame.Atlas/OneGame.Atlas/AtlasStatus.md#m-ok).

**Returns:** [`AtlasStatus`](../../OneGame.Atlas/OneGame.Atlas/AtlasStatus.md) — The status. A size or bitmap beyond the budget returns [`AtlasStatus.TooLarge`](../../OneGame.Atlas/OneGame.Atlas/AtlasStatus.md#m-toolarge) before any bitmap is allocated.
