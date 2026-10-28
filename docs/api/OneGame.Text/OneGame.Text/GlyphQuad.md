[API reference](../../index.md) › [OneGame.Text](../index.md) › OneGame.Text

# GlyphQuad struct

**Namespace:** `OneGame.Text`  
**Package:** [`RecursiveAI.OneGame.Text`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GlyphQuad`](GlyphQuad.md)&gt;

```csharp
public readonly struct GlyphQuad : IEquatable<GlyphQuad>
```

One textured rectangle of a glyph, in screen pixels, ready for the renderer.

## Constructors

| Member | Description |
| --- | --- |
| [`GlyphQuad(int, float, float, float, float, float, float, float, float, Color)`](#m-ctor-int-float-float-float-float-float-float-float-float-color) | One textured rectangle of a glyph, in screen pixels, ready for the renderer. |

### <a id="m-ctor-int-float-float-float-float-float-float-float-float-color"></a>GlyphQuad(int, float, float, float, float, float, float, float, float, Color)

```csharp
public GlyphQuad(int Page, float X0, float Y0, float X1, float Y1, float U0, float V0, float U1, float V1, Color Color);
```

One textured rectangle of a glyph, in screen pixels, ready for the renderer.

**Parameters**

- `Page` (`int`): Glyph atlas page to sample.
- `X0` (`float`): Left edge in screen pixels.
- `Y0` (`float`): Top edge in screen pixels.
- `X1` (`float`): Right edge in screen pixels.
- `Y1` (`float`): Bottom edge in screen pixels.
- `U0` (`float`): Left texture coordinate.
- `V0` (`float`): Top texture coordinate.
- `U1` (`float`): Right texture coordinate.
- `V1` (`float`): Bottom texture coordinate.
- `Color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): Text color (straight alpha); multiply its alpha by the sampled coverage.

## Properties

| Member | Description |
| --- | --- |
| [`Color`](#m-color) | Text color (straight alpha); multiply its alpha by the sampled coverage. |
| [`Page`](#m-page) | Glyph atlas page to sample. |
| [`U0`](#m-u0) | Left texture coordinate. |
| [`U1`](#m-u1) | Right texture coordinate. |
| [`V0`](#m-v0) | Top texture coordinate. |
| [`V1`](#m-v1) | Bottom texture coordinate. |
| [`X0`](#m-x0) | Left edge in screen pixels. |
| [`X1`](#m-x1) | Right edge in screen pixels. |
| [`Y0`](#m-y0) | Top edge in screen pixels. |
| [`Y1`](#m-y1) | Bottom edge in screen pixels. |

### <a id="m-color"></a>Color

```csharp
public Color Color { get; init; }
```

Text color (straight alpha); multiply its alpha by the sampled coverage.

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-page"></a>Page

```csharp
public int Page { get; init; }
```

Glyph atlas page to sample.

**Type:** `int`

### <a id="m-u0"></a>U0

```csharp
public float U0 { get; init; }
```

Left texture coordinate.

**Type:** `float`

### <a id="m-u1"></a>U1

```csharp
public float U1 { get; init; }
```

Right texture coordinate.

**Type:** `float`

### <a id="m-v0"></a>V0

```csharp
public float V0 { get; init; }
```

Top texture coordinate.

**Type:** `float`

### <a id="m-v1"></a>V1

```csharp
public float V1 { get; init; }
```

Bottom texture coordinate.

**Type:** `float`

### <a id="m-x0"></a>X0

```csharp
public float X0 { get; init; }
```

Left edge in screen pixels.

**Type:** `float`

### <a id="m-x1"></a>X1

```csharp
public float X1 { get; init; }
```

Right edge in screen pixels.

**Type:** `float`

### <a id="m-y0"></a>Y0

```csharp
public float Y0 { get; init; }
```

Top edge in screen pixels.

**Type:** `float`

### <a id="m-y1"></a>Y1

```csharp
public float Y1 { get; init; }
```

Bottom edge in screen pixels.

**Type:** `float`
