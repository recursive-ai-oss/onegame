[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# UiCanvas class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class UiCanvas : IDisposable
```

Draws UI shapes, images and text into the current frame, in coordinates of framebuffer pixels (origin top-left, y down). It keeps stacks of clips, transforms and opacity groups that nest and must balance by the end of the frame. [`UiCanvas.Draw()`](UiCanvas.md#m-draw-displaylist-textquadbuilder-glyphatlas-artatlas) replays a whole UI display list; the other methods draw single primitives for HUDs and tools. Obtain it from [`Renderer2D.Ui`](Renderer2D.md#m-ui).

## Properties

| Member | Description |
| --- | --- |
| [`MissingArt`](#m-missingart) | Images the display list named that the atlas could not find, drawn as magenta boxes; counts since the renderer was made. |

### <a id="m-missingart"></a>MissingArt

```csharp
public int MissingArt { get; }
```

Images the display list named that the atlas could not find, drawn as magenta boxes; counts since the renderer was made.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Border(Rect, Color, Edges, CornerRadii)`](#m-border-rect-color-edges-cornerradii) | Strokes the inside edge of a rounded rectangle, with a thickness per side. |
| [`Dispose()`](#m-dispose) | Releases the atlas page textures. |
| [`Draw(DisplayList, TextQuadBuilder, GlyphAtlas, ArtAtlas)`](#m-draw-displaylist-textquadbuilder-glyphatlas-artatlas) | Draws a display list: every command in order, with its clips, transforms and opacity groups. |
| [`Fill(Rect, in Gradient, CornerRadii)`](#m-fill-rect-gradient-cornerradii) | Fills a rounded rectangle with a gradient. |
| [`Fill(Rect, Color, CornerRadii)`](#m-fill-rect-color-cornerradii) | Fills a rounded rectangle with a colour. |
| [`Image(Rect, Texture2D, Color, CornerRadii)`](#m-image-rect-texture2d-color-cornerradii) | Draws a texture (or part of it) into a rectangle. |
| [`Image(Rect, Texture2D, PixelRect, Color, CornerRadii)`](#m-image-rect-texture2d-pixelrect-color-cornerradii) | Draws part of a texture into a rectangle. |
| [`PopClip()`](#m-popclip) | Ends the clip of the matching [`UiCanvas.PushClip()`](UiCanvas.md#m-pushclip-rect-cornerradii). |
| [`PopOpacity()`](#m-popopacity) | Ends the group of the matching [`UiCanvas.PushOpacity()`](UiCanvas.md#m-pushopacity-float) and blends it onto what was drawn before. |
| [`PopTransform()`](#m-poptransform) | Ends the transform of the matching [`UiCanvas.PushTransform()`](UiCanvas.md#m-pushtransform-matrix3x2). |
| [`PushClip(Rect, CornerRadii)`](#m-pushclip-rect-cornerradii) | Intersects the clip with a rounded rectangle until [`UiCanvas.PopClip()`](UiCanvas.md#m-popclip). |
| [`PushOpacity(float)`](#m-pushopacity-float) | Draws everything until [`UiCanvas.PopOpacity()`](UiCanvas.md#m-popopacity) as a group and blends the group with `opacity`, so overlapping shapes inside do not show through each other. |
| [`PushTransform(Matrix3x2)`](#m-pushtransform-matrix3x2) | Applies `matrix` to everything drawn until [`UiCanvas.PopTransform()`](UiCanvas.md#m-poptransform). |
| [`Shadow(Rect, Color, CornerRadii, Vector2, float, float)`](#m-shadow-rect-color-cornerradii-vector2-float-float) | Draws a soft shadow of a rounded box. |
| [`Text(TextQuadBuilder, GlyphAtlas, string, in TextStyle, Vector2, Color)`](#m-text-textquadbuilder-glyphatlas-string-textstyle-vector2-color) | Draws one line of text with its top-left corner at a point, outside any display list. |

### <a id="m-border-rect-color-edges-cornerradii"></a>Border(Rect, Color, Edges, CornerRadii)

```csharp
public void Border(Rect bounds, Color color, Edges widths, CornerRadii radii = default);
```

Strokes the inside edge of a rounded rectangle, with a thickness per side.

**Parameters**

- `bounds` ([`Rect`](../../OneGame.UI/OneGame.UI/Rect.md)): The outer rectangle.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): sRGB, straight alpha.
- `widths` ([`Edges`](../../OneGame.UI/OneGame.UI/Edges.md)): Thickness of the left, top, right and bottom edges.
- `radii` ([`CornerRadii`](../../OneGame.UI/OneGame.UI/CornerRadii.md)): Corner radii of the outer edge.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the atlas page textures.

### <a id="m-draw-displaylist-textquadbuilder-glyphatlas-artatlas"></a>Draw(DisplayList, TextQuadBuilder, GlyphAtlas, ArtAtlas)

```csharp
public void Draw(DisplayList list, TextQuadBuilder text, GlyphAtlas glyphs, ArtAtlas art);
```

Draws a display list: every command in order, with its clips, transforms and opacity groups. Text comes from `text` and `glyphs`, images from `art`. The atlases' pages are uploaded here, so call it after the frame's lookups are done.

**Parameters**

- `list` ([`DisplayList`](../../OneGame.UI/OneGame.UI/DisplayList.md)): The list from `UiTree.Frame()`.
- `text` ([`TextQuadBuilder`](../../OneGame.Text/OneGame.Text/TextQuadBuilder.md)): Turns text runs into glyph quads; built over the same measurer the UI laid out with.
- `glyphs` ([`GlyphAtlas`](../../OneGame.Text/OneGame.Text/GlyphAtlas.md)): The glyph atlas `text` draws from.
- `art` ([`ArtAtlas`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md)): The SVG and image atlas.

### <a id="m-fill-rect-gradient-cornerradii"></a>Fill(Rect, in Gradient, CornerRadii)

```csharp
public void Fill(Rect bounds, in Gradient gradient, CornerRadii radii = default);
```

Fills a rounded rectangle with a gradient.

**Parameters**

- `bounds` ([`Rect`](../../OneGame.UI/OneGame.UI/Rect.md)): The rectangle.
- `gradient` ([`Gradient`](Gradient.md)): The colour ramp.
- `radii` ([`CornerRadii`](../../OneGame.UI/OneGame.UI/CornerRadii.md)): Corner radii.

### <a id="m-fill-rect-color-cornerradii"></a>Fill(Rect, Color, CornerRadii)

```csharp
public void Fill(Rect bounds, Color color, CornerRadii radii = default);
```

Fills a rounded rectangle with a colour.

**Parameters**

- `bounds` ([`Rect`](../../OneGame.UI/OneGame.UI/Rect.md)): The rectangle.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): sRGB, straight alpha.
- `radii` ([`CornerRadii`](../../OneGame.UI/OneGame.UI/CornerRadii.md)): Corner radii.

### <a id="m-image-rect-texture2d-color-cornerradii"></a>Image(Rect, Texture2D, Color, CornerRadii)

```csharp
public void Image(Rect bounds, Texture2D texture, Color tint, CornerRadii radii = default);
```

Draws a texture (or part of it) into a rectangle.

**Parameters**

- `bounds` ([`Rect`](../../OneGame.UI/OneGame.UI/Rect.md)): Destination.
- `texture` ([`Texture2D`](Texture2D.md)): The image.
- `tint` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Colour multiplied in; white for none.
- `radii` ([`CornerRadii`](../../OneGame.UI/OneGame.UI/CornerRadii.md)): Corner radii that round the image.

### <a id="m-image-rect-texture2d-pixelrect-color-cornerradii"></a>Image(Rect, Texture2D, PixelRect, Color, CornerRadii)

```csharp
public void Image(Rect bounds, Texture2D texture, PixelRect source, Color tint, CornerRadii radii = default);
```

Draws part of a texture into a rectangle.

**Parameters**

- `bounds` ([`Rect`](../../OneGame.UI/OneGame.UI/Rect.md)): Destination.
- `texture` ([`Texture2D`](Texture2D.md)): The image.
- `source` ([`PixelRect`](PixelRect.md)): The part to draw, in pixels.
- `tint` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Colour multiplied in; white for none.
- `radii` ([`CornerRadii`](../../OneGame.UI/OneGame.UI/CornerRadii.md)): Corner radii that round the image.

### <a id="m-popclip"></a>PopClip()

```csharp
public void PopClip();
```

Ends the clip of the matching [`UiCanvas.PushClip()`](UiCanvas.md#m-pushclip-rect-cornerradii).

### <a id="m-popopacity"></a>PopOpacity()

```csharp
public void PopOpacity();
```

Ends the group of the matching [`UiCanvas.PushOpacity()`](UiCanvas.md#m-pushopacity-float) and blends it onto what was drawn before.

### <a id="m-poptransform"></a>PopTransform()

```csharp
public void PopTransform();
```

Ends the transform of the matching [`UiCanvas.PushTransform()`](UiCanvas.md#m-pushtransform-matrix3x2).

### <a id="m-pushclip-rect-cornerradii"></a>PushClip(Rect, CornerRadii)

```csharp
public void PushClip(Rect bounds, CornerRadii radii = default);
```

Intersects the clip with a rounded rectangle until [`UiCanvas.PopClip()`](UiCanvas.md#m-popclip). Axis-aligned square clips cut at whole pixels; rounded or turned clips are anti-aliased.

**Parameters**

- `bounds` ([`Rect`](../../OneGame.UI/OneGame.UI/Rect.md)): The rectangle, in the coordinates in effect now.
- `radii` ([`CornerRadii`](../../OneGame.UI/OneGame.UI/CornerRadii.md)): Corner radii, or default for square corners.

### <a id="m-pushopacity-float"></a>PushOpacity(float)

```csharp
public void PushOpacity(float opacity);
```

Draws everything until [`UiCanvas.PopOpacity()`](UiCanvas.md#m-popopacity) as a group and blends the group with `opacity`, so overlapping shapes inside do not show through each other.

**Parameters**

- `opacity` (`float`): 0 (nothing) to 1 (no effect).

### <a id="m-pushtransform-matrix3x2"></a>PushTransform(Matrix3x2)

```csharp
public void PushTransform(Matrix3x2 matrix);
```

Applies `matrix` to everything drawn until [`UiCanvas.PopTransform()`](UiCanvas.md#m-poptransform). The matrix maps coordinates in effect now to the screen the way the display list's transforms do (it already contains its pivot), and nested transforms compose.

**Parameters**

- `matrix` ([`Matrix3x2`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2)): The transform, for row vectors.

### <a id="m-shadow-rect-color-cornerradii-vector2-float-float"></a>Shadow(Rect, Color, CornerRadii, Vector2, float, float)

```csharp
public void Shadow(Rect box, Color color, CornerRadii radii, Vector2 offset, float blur, float spread);
```

Draws a soft shadow of a rounded box. Nothing is drawn under the box itself.

**Parameters**

- `box` ([`Rect`](../../OneGame.UI/OneGame.UI/Rect.md)): The element's box.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Shadow colour.
- `radii` ([`CornerRadii`](../../OneGame.UI/OneGame.UI/CornerRadii.md)): The box's corner radii.
- `offset` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): How far the shadow is moved.
- `blur` (`float`): Blur radius in pixels (twice the standard deviation of the blur).
- `spread` (`float`): How much the shadow's shape grows beyond the box.

### <a id="m-text-textquadbuilder-glyphatlas-string-textstyle-vector2-color"></a>Text(TextQuadBuilder, GlyphAtlas, string, in TextStyle, Vector2, Color)

```csharp
public void Text(TextQuadBuilder text, GlyphAtlas glyphs, string line, in TextStyle style, Vector2 topLeft, Color color);
```

Draws one line of text with its top-left corner at a point, outside any display list. This is how the immediate-mode text of the host's `Draw2D` reaches the screen. The coordinates are the ones in effect now.

**Parameters**

- `text` ([`TextQuadBuilder`](../../OneGame.Text/OneGame.Text/TextQuadBuilder.md)): Builds the glyph quads; made over the measurer the caller sized the text with.
- `glyphs` ([`GlyphAtlas`](../../OneGame.Text/OneGame.Text/GlyphAtlas.md)): The glyph atlas `text` draws from.
- `line` (`string`): The string; newlines are not broken.
- `style` ([`TextStyle`](../../OneGame.UI/OneGame.UI/TextStyle.md)): Family, size and spacing.
- `topLeft` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Where the line box starts.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Text colour, straight alpha.
