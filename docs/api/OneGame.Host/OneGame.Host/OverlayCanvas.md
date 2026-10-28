[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# OverlayCanvas class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class OverlayCanvas
```

The few drawing calls an [`IFrameOverlay`](IFrameOverlay.md) has: filled rectangles and one-line text, in framebuffer pixels.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height of the target in pixels. |
| [`Scale`](#m-scale) | The UI scale of the frame; sizes given in points are multiplied by it. |
| [`Width`](#m-width) | Width of the target in pixels. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; }
```

Height of the target in pixels.

**Type:** `int`

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; }
```

The UI scale of the frame; sizes given in points are multiplied by it.

**Type:** `float`

### <a id="m-width"></a>Width

```csharp
public int Width { get; }
```

Width of the target in pixels.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Fill(float, float, float, float, Color)`](#m-fill-float-float-float-float-color) | Fills a rectangle. |
| [`Measure(string, float)`](#m-measure-string-float) | The width of a line of text when drawn by [`OverlayCanvas.Text()`](OverlayCanvas.md#m-text-string-float-float-float-color). |
| [`Text(string, float, float, float, Color)`](#m-text-string-float-float-float-color) | Draws one line of text in the game's first font. |

### <a id="m-fill-float-float-float-float-color"></a>Fill(float, float, float, float, Color)

```csharp
public void Fill(float x, float y, float width, float height, Color color);
```

Fills a rectangle.

**Parameters**

- `x` (`float`): Left edge in pixels.
- `y` (`float`): Top edge in pixels.
- `width` (`float`): Width in pixels.
- `height` (`float`): Height in pixels.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Fill colour, straight alpha.

### <a id="m-measure-string-float"></a>Measure(string, float)

```csharp
public float Measure(string line, float size);
```

The width of a line of text when drawn by [`OverlayCanvas.Text()`](OverlayCanvas.md#m-text-string-float-float-float-color).

**Parameters**

- `line` (`string`): The text.
- `size` (`float`): Font size in pixels.

**Returns:** `float` — Pixels.

### <a id="m-text-string-float-float-float-color"></a>Text(string, float, float, float, Color)

```csharp
public void Text(string line, float x, float y, float size, Color color);
```

Draws one line of text in the game's first font.

**Parameters**

- `line` (`string`): The text; newlines are not broken.
- `x` (`float`): Left edge in pixels.
- `y` (`float`): Top edge in pixels.
- `size` (`float`): Font size in pixels.
- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Text colour, straight alpha.
