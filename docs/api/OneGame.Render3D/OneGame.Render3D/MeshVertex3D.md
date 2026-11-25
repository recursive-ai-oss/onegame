[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# MeshVertex3D struct

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`MeshVertex3D`](MeshVertex3D.md)&gt;

```csharp
public readonly struct MeshVertex3D : IEquatable<MeshVertex3D>
```

One vertex of a [`DynamicMesh3D`](DynamicMesh3D.md). Positions are in mesh space; the normal should be unit length. Colour (linear RGBA) multiplies the material's base colour, so a mesher can bake shading or ambient occlusion into it.

## Constructors

| Member | Description |
| --- | --- |
| [`MeshVertex3D(Vector3, Vector3, Vector2)`](#m-ctor-vector3-vector3-vector2) | A vertex with no vertex colour. |
| [`MeshVertex3D(Vector3, Vector3, Vector2, Vector4)`](#m-ctor-vector3-vector3-vector2-vector4) | One vertex of a [`DynamicMesh3D`](DynamicMesh3D.md). |
| [`MeshVertex3D(Vector3, Vector3, Vector2, Vector4, int)`](#m-ctor-vector3-vector3-vector2-vector4-int) | A vertex that picks a layer of a layered material ([`Renderer3D.CreateLayeredMaterial()`](Renderer3D.md#m-createlayeredmaterial-string-int-int-readonlyspan-readonlymemory-byte-materialalpha3d-bool-vector4-float-bool)). |

### <a id="m-ctor-vector3-vector3-vector2"></a>MeshVertex3D(Vector3, Vector3, Vector2)

```csharp
public MeshVertex3D(Vector3 position, Vector3 normal, Vector2 uv);
```

A vertex with no vertex colour.

**Parameters**

- `position` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Where the vertex is.
- `normal` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Surface direction.
- `uv` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Texture coordinates.

### <a id="m-ctor-vector3-vector3-vector2-vector4"></a>MeshVertex3D(Vector3, Vector3, Vector2, Vector4)

```csharp
public MeshVertex3D(Vector3 Position, Vector3 Normal, Vector2 Uv, Vector4 Color);
```

One vertex of a [`DynamicMesh3D`](DynamicMesh3D.md). Positions are in mesh space; the normal should be unit length. Colour (linear RGBA) multiplies the material's base colour, so a mesher can bake shading or ambient occlusion into it.

**Parameters**

- `Position` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Where the vertex is.
- `Normal` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Surface direction.
- `Uv` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Texture coordinates of the base colour, emissive and other textures.
- `Color` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)): Linear RGBA multiplied into the base colour (white leaves it unchanged).

### <a id="m-ctor-vector3-vector3-vector2-vector4-int"></a>MeshVertex3D(Vector3, Vector3, Vector2, Vector4, int)

```csharp
public MeshVertex3D(Vector3 position, Vector3 normal, Vector2 uv, Vector4 color, int layer);
```

A vertex that picks a layer of a layered material ([`Renderer3D.CreateLayeredMaterial()`](Renderer3D.md#m-createlayeredmaterial-string-int-int-readonlyspan-readonlymemory-byte-materialalpha3d-bool-vector4-float-bool)).

**Parameters**

- `position` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Where the vertex is.
- `normal` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Surface direction.
- `uv` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Texture coordinates within the layer; they repeat outside 0 to 1.
- `color` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)): Linear RGBA multiplied into the base colour.
- `layer` (`int`): Layer index. Give all three vertices of a triangle the same one.

## Properties

| Member | Description |
| --- | --- |
| [`Color`](#m-color) | Linear RGBA multiplied into the base colour (white leaves it unchanged). |
| [`Layer`](#m-layer) | Which layer of a layered material this vertex samples (rounded to the nearest whole number; out-of-range values use the first or last layer). |
| [`Normal`](#m-normal) | Surface direction. |
| [`Position`](#m-position) | Where the vertex is. |
| [`Uv`](#m-uv) | Texture coordinates of the base colour, emissive and other textures. |

### <a id="m-color"></a>Color

```csharp
public Vector4 Color { get; init; }
```

Linear RGBA multiplied into the base colour (white leaves it unchanged).

**Type:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)

### <a id="m-layer"></a>Layer

```csharp
public float Layer { get; init; }
```

Which layer of a layered material this vertex samples (rounded to the nearest whole number; out-of-range values use the first or last layer). Ignored by other materials. 0 by default.

**Type:** `float`

### <a id="m-normal"></a>Normal

```csharp
public Vector3 Normal { get; init; }
```

Surface direction.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-position"></a>Position

```csharp
public Vector3 Position { get; init; }
```

Where the vertex is.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-uv"></a>Uv

```csharp
public Vector2 Uv { get; init; }
```

Texture coordinates of the base colour, emissive and other textures.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)
