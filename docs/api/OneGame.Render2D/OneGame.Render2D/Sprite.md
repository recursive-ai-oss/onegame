[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# Sprite struct

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)

```csharp
public struct Sprite
```

One sprite to draw this frame. World coordinates have +X right and +Y down, like the screen. Build one with [`Sprite.Create()`](Sprite.md#m-create-texture2d-vector2) and set what differs from the defaults; a struct made with `new Sprite()` has no texture and is skipped.

## Properties

| Member | Description |
| --- | --- |
| [`Flip`](#m-flip) | Mirroring. |
| [`Material`](#m-material) | A custom pixel shader, or null for the plain one. |
| [`Pivot`](#m-pivot) | The pivot as a fraction of the sprite: (0,0) top-left, (0.5,0.5) centre. |
| [`Position`](#m-position) | Where the pivot is, in world units. |
| [`Rotation`](#m-rotation) | Rotation in radians about the pivot; positive turns clockwise on screen. |
| [`Scale`](#m-scale) | World units per source pixel, per axis. |
| [`Source`](#m-source) | The part of the texture to draw, in pixels. |
| [`Texture`](#m-texture) | The image to draw. |
| [`Tint`](#m-tint) | Colour multiplied into the texture, sRGB with straight alpha. |
| [`Z`](#m-z) | Draw order inside the layer: lower is behind. |

### <a id="m-flip"></a>Flip

```csharp
public SpriteFlip Flip { get; set; }
```

Mirroring.

**Type:** [`SpriteFlip`](SpriteFlip.md)

### <a id="m-material"></a>Material

```csharp
public Material? Material { get; set; }
```

A custom pixel shader, or null for the plain one.

**Type:** [`Material`](Material.md)?

### <a id="m-pivot"></a>Pivot

```csharp
public Vector2 Pivot { get; set; }
```

The pivot as a fraction of the sprite: (0,0) top-left, (0.5,0.5) centre.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-position"></a>Position

```csharp
public Vector2 Position { get; set; }
```

Where the pivot is, in world units.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-rotation"></a>Rotation

```csharp
public float Rotation { get; set; }
```

Rotation in radians about the pivot; positive turns clockwise on screen.

**Type:** `float`

### <a id="m-scale"></a>Scale

```csharp
public Vector2 Scale { get; set; }
```

World units per source pixel, per axis.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-source"></a>Source

```csharp
public PixelRect Source { get; set; }
```

The part of the texture to draw, in pixels. An empty rectangle (the default) means the whole texture.

**Type:** [`PixelRect`](PixelRect.md)

### <a id="m-texture"></a>Texture

```csharp
public Texture2D? Texture { get; set; }
```

The image to draw. Sprites sharing a texture and material are drawn together.

**Type:** [`Texture2D`](Texture2D.md)?

### <a id="m-tint"></a>Tint

```csharp
public Color Tint { get; set; }
```

Colour multiplied into the texture, sRGB with straight alpha. White draws the texture unchanged.

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-z"></a>Z

```csharp
public float Z { get; set; }
```

Draw order inside the layer: lower is behind. Equal values draw in submission order.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`Create(Texture2D, Vector2)`](#m-create-texture2d-vector2) | Makes a sprite with the pivot at its centre, scale 1, white tint and no rotation. |

### <a id="m-create-texture2d-vector2"></a>Create(Texture2D, Vector2)

```csharp
public static Sprite Create(Texture2D texture, Vector2 position);
```

Makes a sprite with the pivot at its centre, scale 1, white tint and no rotation.

**Parameters**

- `texture` ([`Texture2D`](Texture2D.md)): The image.
- `position` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Where the pivot goes, in world units.

**Returns:** [`Sprite`](Sprite.md) — The sprite.
