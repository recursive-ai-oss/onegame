[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# Camera2D struct

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)

```csharp
public struct Camera2D
```

The view onto the world: where it looks, how far in, and how it is turned.

## Properties

| Member | Description |
| --- | --- |
| [`Default`](#m-default) | A camera looking at the origin at zoom 1. |
| [`PixelSnap`](#m-pixelsnap) | Round sprite positions to whole pixels, which keeps pixel art crisp while the camera moves. |
| [`Position`](#m-position) | The world point at the centre of the screen. |
| [`Rotation`](#m-rotation) | Rotation in radians; positive turns the view clockwise. |
| [`Zoom`](#m-zoom) | Physical pixels per world unit. |

### <a id="m-default"></a>Default

```csharp
public static Camera2D Default { get; }
```

A camera looking at the origin at zoom 1.

**Type:** [`Camera2D`](Camera2D.md)

### <a id="m-pixelsnap"></a>PixelSnap

```csharp
public bool PixelSnap { get; set; }
```

Round sprite positions to whole pixels, which keeps pixel art crisp while the camera moves. Meant for an unrotated camera.

**Type:** `bool`

### <a id="m-position"></a>Position

```csharp
public Vector2 Position { get; set; }
```

The world point at the centre of the screen.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-rotation"></a>Rotation

```csharp
public float Rotation { get; set; }
```

Rotation in radians; positive turns the view clockwise.

**Type:** `float`

### <a id="m-zoom"></a>Zoom

```csharp
public float Zoom { get; set; }
```

Physical pixels per world unit. A HiDPI game multiplies its zoom by the display scale.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`View(Vector2, Vector2)`](#m-view-vector2-vector2) | The matrix taking world points to framebuffer pixels (row vectors) for a layer that follows the camera by `parallax`. |

### <a id="m-view-vector2-vector2"></a>View(Vector2, Vector2)

```csharp
public Matrix3x2 View(Vector2 screenSize, Vector2 parallax);
```

The matrix taking world points to framebuffer pixels (row vectors) for a layer that follows the camera by `parallax`.

**Parameters**

- `screenSize` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Target size in pixels.
- `parallax` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): 1 follows the camera exactly; 0 stays fixed on screen; between scrolls slower.

**Returns:** [`Matrix3x2`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2) — The world-to-screen matrix.
