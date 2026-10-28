[API reference](../../index.md) › [OneGame.Text](../index.md) › OneGame.Text

# FontLibrary class

**Namespace:** `OneGame.Text`  
**Package:** [`RecursiveAI.OneGame.Text`](../index.md)

```csharp
public sealed class FontLibrary
```

The fonts of a game: faces loaded from asset bytes, grouped by family, resolved per `font-family` list, weight and slant into a fallback chain. Not thread safe: use it from the thread that runs the UI.

**Example**

Load fonts in `GameDefinition.DefineFonts`; the first family added is the default, and style rules name families with `font-family`:

```csharp
public override void DefineFonts(FontLibrary fonts)
{
    fonts.Add(File.ReadAllBytes("fonts/Body-Regular.ttf"));
    fonts.Add(File.ReadAllBytes("fonts/Body-Bold.ttf"));      // same family, a heavier weight
}
```

## Constructors

| Member | Description |
| --- | --- |
| [`FontLibrary()`](#m-ctor) |  |

### <a id="m-ctor"></a>FontLibrary()

```csharp
public FontLibrary();
```

## Fields

| Member | Description |
| --- | --- |
| [`MaxFaces`](#m-maxfaces) | The most faces a library holds. |
| [`MaxFontBytes`](#m-maxfontbytes) | The largest font file accepted, in bytes. |

### <a id="m-maxfaces"></a>MaxFaces

```csharp
public const int MaxFaces = 256;
```

The most faces a library holds.

**Type:** `int`

### <a id="m-maxfontbytes"></a>MaxFontBytes

```csharp
public const int MaxFontBytes = 67108864;
```

The largest font file accepted, in bytes.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`FaceCount`](#m-facecount) | Gets how many faces are loaded. |

### <a id="m-facecount"></a>FaceCount

```csharp
public int FaceCount { get; }
```

Gets how many faces are loaded.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Add(FontFace, string?)`](#m-add-fontface-string) | Adds a face that is already loaded, so another component (a OneRender page) can hold the same object. |
| [`Add(byte[], string?, int)`](#m-add-byte-string-int) | Loads a font file (TrueType, OpenType/CFF or a collection's first face) and adds its face. |
| [`AddFallback(int)`](#m-addfallback-int) | Makes a loaded face a last resort for every stack, after the families the style names (for scripts the main font lacks). |
| [`Face(int)`](#m-face-int) | Returns a loaded face. |
| [`SetAlias(string, string)`](#m-setalias-string-string) | Lets a generic name such as `sans-serif` or a theme's font name stand for a loaded family. |

### <a id="m-add-fontface-string"></a>Add(FontFace, string?)

```csharp
public int Add(FontFace face, string? family = null);
```

Adds a face that is already loaded, so another component (a OneRender page) can hold the same object.

**Parameters**

- `face` (`FontFace`): The face; the library keeps a reference.
- `family` (`string?`): The family name to register the face under; the font's own name when `null`.

**Returns:** `int` — The face id used by glyph atlas keys and positioned glyphs.

**Exceptions**

- [`TextLimitException`](TextLimitException.md): The library already holds the most faces it accepts.

### <a id="m-add-byte-string-int"></a>Add(byte\[\], string?, int)

```csharp
public int Add(byte[] fontData, string? family = null, int faceIndex = 0);
```

Loads a font file (TrueType, OpenType/CFF or a collection's first face) and adds its face.

**Parameters**

- `fontData` (`byte[]`): The file's bytes. The library keeps them.
- `family` (`string?`): The family name to register the face under; the font's own name when `null`.
- `faceIndex` (`int`): Which face of a collection.

**Returns:** `int` — The face id used by glyph atlas keys and positioned glyphs.

**Exceptions**

- [`TextLimitException`](TextLimitException.md): The file or the library is over its budget.
- `FontFormatException`: The file is not a usable font.

### <a id="m-addfallback-int"></a>AddFallback(int)

```csharp
public void AddFallback(int faceId);
```

Makes a loaded face a last resort for every stack, after the families the style names (for scripts the main font lacks).

**Parameters**

- `faceId` (`int`): A face id returned by [`FontLibrary.Add()`](FontLibrary.md#m-add-byte-string-int).

### <a id="m-face-int"></a>Face(int)

```csharp
public FontFace Face(int faceId);
```

Returns a loaded face.

**Parameters**

- `faceId` (`int`): A face id.

**Returns:** `FontFace` — The face.

**Exceptions**

- [`FontNotFoundException`](FontNotFoundException.md): No such face.

### <a id="m-setalias-string-string"></a>SetAlias(string, string)

```csharp
public void SetAlias(string name, string family);
```

Lets a generic name such as `sans-serif` or a theme's font name stand for a loaded family.

**Parameters**

- `name` (`string`): The name used in `font-family`.
- `family` (`string`): A loaded family.
