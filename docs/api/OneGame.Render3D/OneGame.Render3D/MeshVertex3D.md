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

## Properties

| Member | Description |
| --- | --- |
| [`Color`](#m-color) | Linear RGBA multiplied into the base colour (white leaves it unchanged). |
| [`Normal`](#m-normal) | Surface direction. |
| [`Position`](#m-position) | Where the vertex is. |
| [`Uv`](#m-uv) | Texture coordinates of the base colour, emissive and other textures. |

### <a id="m-color"></a>Color

```csharp
public Vector4 Color { get; init; }
```

Linear RGBA multiplied into the base colour (white leaves it unchanged).

**Type:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)

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
