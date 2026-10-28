[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# BlendSpace2D class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class BlendSpace2D
```

A plane of poses, for example direction by speed. The samples are joined into triangles once (a Delaunay triangulation); a point inside a triangle blends its three corners by barycentric weights, and a point outside the outline counts as the nearest point on it.

## Constructors

| Member | Description |
| --- | --- |
| [`BlendSpace2D(Vector2[])`](#m-ctor-vector2) | Creates the space from the positions of its samples. |

### <a id="m-ctor-vector2"></a>BlendSpace2D(Vector2\[\])

```csharp
public BlendSpace2D(params Vector2[] points);
```

Creates the space from the positions of its samples.

**Parameters**

- `points` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)\[\]): Where each sample sits: at least three, not all on one line, no two equal.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The points cannot form a triangle.

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | The number of samples. |
| [`TriangleCount`](#m-trianglecount) | The number of triangles. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of samples.

**Type:** `int`

### <a id="m-trianglecount"></a>TriangleCount

```csharp
public int TriangleCount { get; }
```

The number of triangles.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Weights(Vector2, Span<float>)`](#m-weights-vector2-span-float) | Writes one weight per sample; they add up to 1 and at most three are not zero. |

### <a id="m-weights-vector2-span-float"></a>Weights(Vector2, Span&lt;float&gt;)

```csharp
public void Weights(Vector2 point, Span<float> weights);
```

Writes one weight per sample; they add up to 1 and at most three are not zero.

**Parameters**

- `point` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): The input point.
- `weights` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<float>`): Receives [`BlendSpace2D.Count`](BlendSpace2D.md#m-count) weights.
