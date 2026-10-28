[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfBounds struct

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GltfBounds`](GltfBounds.md)&gt;

```csharp
public readonly struct GltfBounds : IEquatable<GltfBounds>
```

An axis-aligned box.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfBounds(Vector3, Vector3)`](#m-ctor-vector3-vector3) | An axis-aligned box. |

### <a id="m-ctor-vector3-vector3"></a>GltfBounds(Vector3, Vector3)

```csharp
public GltfBounds(Vector3 Min, Vector3 Max);
```

An axis-aligned box.

**Parameters**

- `Min` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The smallest corner.
- `Max` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The largest corner.

## Properties

| Member | Description |
| --- | --- |
| [`Max`](#m-max) | The largest corner. |
| [`Min`](#m-min) | The smallest corner. |

### <a id="m-max"></a>Max

```csharp
public Vector3 Max { get; init; }
```

The largest corner.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-min"></a>Min

```csharp
public Vector3 Min { get; init; }
```

The smallest corner.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)
