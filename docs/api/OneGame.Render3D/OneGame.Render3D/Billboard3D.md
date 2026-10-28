[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# Billboard3D struct

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public struct Billboard3D
```

One camera-facing quad of a [`BillboardBatch3D`](BillboardBatch3D.md).

## Properties

| Member | Description |
| --- | --- |
| [`Additive`](#m-additive) | True to add the colour to the scene instead of blending it over; additive quads need no sorting. |
| [`Color`](#m-color) | Linear-light RGB with straight alpha. |
| [`Position`](#m-position) | Centre in world space. |
| [`Rotation`](#m-rotation) | Rotation in radians about the view direction; positive turns clockwise as seen from the camera. |
| [`Size`](#m-size) | Width and height in world units. |
| [`Uv`](#m-uv) | The part of the texture to show as (u, v, width, height) in 0..1 with the origin at the top-left; a zero rectangle means the whole texture. |

### <a id="m-additive"></a>Additive

```csharp
public bool Additive { get; set; }
```

True to add the colour to the scene instead of blending it over; additive quads need no sorting.

**Type:** `bool`

### <a id="m-color"></a>Color

```csharp
public Vector4 Color { get; set; }
```

Linear-light RGB with straight alpha. RGB above 1 is allowed: the scene is high dynamic range, so bright quads bloom.

**Type:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)

### <a id="m-position"></a>Position

```csharp
public Vector3 Position { get; set; }
```

Centre in world space.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-rotation"></a>Rotation

```csharp
public float Rotation { get; set; }
```

Rotation in radians about the view direction; positive turns clockwise as seen from the camera.

**Type:** `float`

### <a id="m-size"></a>Size

```csharp
public Vector2 Size { get; set; }
```

Width and height in world units.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-uv"></a>Uv

```csharp
public Vector4 Uv { get; set; }
```

The part of the texture to show as (u, v, width, height) in 0..1 with the origin at the top-left; a zero rectangle means the whole texture.

**Type:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)
