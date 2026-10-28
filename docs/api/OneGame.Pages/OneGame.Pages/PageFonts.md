[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages

# PageFonts class

**Namespace:** `OneGame.Pages`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)

```csharp
public sealed class PageFonts
```

The fonts of a page, shared with the game's [`FontLibrary`](../../OneGame.Text/OneGame.Text/FontLibrary.md): each face is loaded once and the same `FontFace` object sits in OneRender's catalog and in the library, so a page and a `OneGame.UI` screen shape and draw with identical faces and glyph bitmaps.

## Constructors

| Member | Description |
| --- | --- |
| [`PageFonts(FontLibrary)`](#m-ctor-fontlibrary) | Creates an empty set of page fonts over the game's library. |

### <a id="m-ctor-fontlibrary"></a>PageFonts(FontLibrary)

```csharp
public PageFonts(FontLibrary library);
```

Creates an empty set of page fonts over the game's library.

**Parameters**

- `library` ([`FontLibrary`](../../OneGame.Text/OneGame.Text/FontLibrary.md)): The library faces are shared with.

## Properties

| Member | Description |
| --- | --- |
| [`Catalog`](#m-catalog) | OneRender's font catalog. |
| [`Provider`](#m-provider) | The font provider a page is created with. |

### <a id="m-catalog"></a>Catalog

```csharp
public FontCatalog Catalog { get; }
```

OneRender's font catalog.

**Type:** `FontCatalog`

### <a id="m-provider"></a>Provider

```csharp
public IFontProvider Provider { get; }
```

The font provider a page is created with.

**Type:** `IFontProvider`

## Methods

| Member | Description |
| --- | --- |
| [`Add(byte[], string?, int)`](#m-add-byte-string-int) | Loads a font file and registers its face for pages and for the game's text. |

### <a id="m-add-byte-string-int"></a>Add(byte\[\], string?, int)

```csharp
public int Add(byte[] fontData, string? family = null, int faceIndex = 0);
```

Loads a font file and registers its face for pages and for the game's text.

**Parameters**

- `fontData` (`byte[]`): The file's bytes (TrueType or OpenType).
- `family` (`string?`): The family name used in `font-family`; the font's own name when null.
- `faceIndex` (`int`): Which face of a collection.

**Returns:** `int` — The face id in the game's [`FontLibrary`](../../OneGame.Text/OneGame.Text/FontLibrary.md).

**Exceptions**

- `FontFormatException`: The file is not a usable font.
- [`TextLimitException`](../../OneGame.Text/OneGame.Text/TextLimitException.md): The library is full or the file is over its limit.
