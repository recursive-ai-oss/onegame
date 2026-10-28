[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# TextureTransform struct

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TextureTransform`](TextureTransform.md)&gt;

```csharp
public readonly struct TextureTransform : IEquatable<TextureTransform>
```

A KHR\_texture\_transform: offset, rotation and scale of the texture coordinates.

## Constructors

| Member | Description |
| --- | --- |
| [`TextureTransform(Vector2, float, Vector2)`](#m-ctor-vector2-float-vector2) | A KHR\_texture\_transform: offset, rotation and scale of the texture coordinates. |

### <a id="m-ctor-vector2-float-vector2"></a>TextureTransform(Vector2, float, Vector2)

```csharp
public TextureTransform(Vector2 Offset, float Rotation, Vector2 Scale);
```

A KHR\_texture\_transform: offset, rotation and scale of the texture coordinates.

**Parameters**

- `Offset` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Offset in texture-size units.
- `Rotation` (`float`): Rotation in radians; positive turns the image clockwise on screen (see [`TextureTransform.ToMatrix()`](TextureTransform.md#m-tomatrix)).
- `Scale` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Scale of the coordinates.

## Properties

| Member | Description |
| --- | --- |
| [`Identity`](#m-identity) | The identity transform. |
| [`Offset`](#m-offset) | Offset in texture-size units. |
| [`Rotation`](#m-rotation) | Rotation in radians; positive turns the image clockwise on screen (see [`TextureTransform.ToMatrix()`](TextureTransform.md#m-tomatrix)). |
| [`Scale`](#m-scale) | Scale of the coordinates. |

### <a id="m-identity"></a>Identity

```csharp
public static TextureTransform Identity { get; }
```

The identity transform.

**Type:** [`TextureTransform`](TextureTransform.md)

### <a id="m-offset"></a>Offset

```csharp
public Vector2 Offset { get; init; }
```

Offset in texture-size units.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-rotation"></a>Rotation

```csharp
public float Rotation { get; init; }
```

Rotation in radians; positive turns the image clockwise on screen (see [`TextureTransform.ToMatrix()`](TextureTransform.md#m-tomatrix)).

**Type:** `float`

### <a id="m-scale"></a>Scale

```csharp
public Vector2 Scale { get; init; }
```

Scale of the coordinates.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

| Member | Description |
| --- | --- |
| [`ToMatrix()`](#m-tomatrix) | The matrix `T * R * S` of the extension, in row-vector convention: `uv' = uv * M`. |

### <a id="m-tomatrix"></a>ToMatrix()

```csharp
public Matrix3x2 ToMatrix();
```

The matrix `T * R * S` of the extension, in row-vector convention: `uv' = uv * M`.

**Returns:** [`Matrix3x2`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2)
