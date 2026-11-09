[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# RayHit3D struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`RayHit3D`](RayHit3D.md)&gt;

```csharp
public readonly struct RayHit3D : IEquatable<RayHit3D>
```

A nearest 3D query hit.

## Constructors

| Member | Description |
| --- | --- |
| [`RayHit3D(BodyRef, FVec3, FVec3, Fixed)`](#m-ctor-bodyref-fvec3-fvec3-fixed) | A nearest 3D query hit. |

### <a id="m-ctor-bodyref-fvec3-fvec3-fixed"></a>RayHit3D(BodyRef, FVec3, FVec3, Fixed)

```csharp
public RayHit3D(BodyRef Body, FVec3 Point, FVec3 Normal, Fixed Distance);
```

A nearest 3D query hit.

**Parameters**

- `Body` ([`BodyRef`](BodyRef.md)): Body hit.
- `Point` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)): Local hit point.
- `Normal` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)): Surface normal.
- `Distance` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): Distance along the cast.

## Properties

| Member | Description |
| --- | --- |
| [`Body`](#m-body) | Body hit. |
| [`Distance`](#m-distance) | Distance along the cast. |
| [`Normal`](#m-normal) | Surface normal. |
| [`Point`](#m-point) | Local hit point. |

### <a id="m-body"></a>Body

```csharp
public BodyRef Body { get; init; }
```

Body hit.

**Type:** [`BodyRef`](BodyRef.md)

### <a id="m-distance"></a>Distance

```csharp
public Fixed Distance { get; init; }
```

Distance along the cast.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-normal"></a>Normal

```csharp
public FVec3 Normal { get; init; }
```

Surface normal.

**Type:** [`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)

### <a id="m-point"></a>Point

```csharp
public FVec3 Point { get; init; }
```

Local hit point.

**Type:** [`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)
