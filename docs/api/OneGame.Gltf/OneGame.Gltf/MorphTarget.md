[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# MorphTarget class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class MorphTarget
```

Per-vertex deltas of one morph target. Absent streams are null.

## Constructors

| Member | Description |
| --- | --- |
| [`MorphTarget()`](#m-ctor) |  |

### <a id="m-ctor"></a>MorphTarget()

```csharp
public MorphTarget();
```

## Properties

| Member | Description |
| --- | --- |
| [`Normals`](#m-normals) | Normal deltas, or null. |
| [`Positions`](#m-positions) | Position deltas, one per vertex, or null. |
| [`Tangents`](#m-tangents) | Tangent deltas (xyz only), or null. |

### <a id="m-normals"></a>Normals

```csharp
public Vector3[]? Normals { get; }
```

Normal deltas, or null.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)\[\]?

### <a id="m-positions"></a>Positions

```csharp
public Vector3[]? Positions { get; }
```

Position deltas, one per vertex, or null.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)\[\]?

### <a id="m-tangents"></a>Tangents

```csharp
public Vector3[]? Tangents { get; }
```

Tangent deltas (xyz only), or null.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)\[\]?
