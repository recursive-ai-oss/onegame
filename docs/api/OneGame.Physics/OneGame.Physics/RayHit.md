[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# RayHit struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`RayHit`](RayHit.md)&gt;

```csharp
public readonly struct RayHit : IEquatable<RayHit>
```

What a ray met.

## Constructors

| Member | Description |
| --- | --- |
| [`RayHit(BodyRef, FVec2, FVec2, Fixed)`](#m-ctor-bodyref-fvec2-fvec2-fixed) | What a ray met. |

### <a id="m-ctor-bodyref-fvec2-fvec2-fixed"></a>RayHit(BodyRef, FVec2, FVec2, Fixed)

```csharp
public RayHit(BodyRef Body, FVec2 Point, FVec2 Normal, Fixed Distance);
```

What a ray met.

**Parameters**

- `Body` ([`BodyRef`](BodyRef.md)): The body that was hit.
- `Point` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where the ray touched it.
- `Normal` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): The surface direction there, pointing back at the ray's origin side (zero if the ray began inside).
- `Distance` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): How far from the origin.

## Properties

| Member | Description |
| --- | --- |
| [`Body`](#m-body) | The body that was hit. |
| [`Distance`](#m-distance) | How far from the origin. |
| [`Normal`](#m-normal) | The surface direction there, pointing back at the ray's origin side (zero if the ray began inside). |
| [`Point`](#m-point) | Where the ray touched it. |

### <a id="m-body"></a>Body

```csharp
public BodyRef Body { get; init; }
```

The body that was hit.

**Type:** [`BodyRef`](BodyRef.md)

### <a id="m-distance"></a>Distance

```csharp
public Fixed Distance { get; init; }
```

How far from the origin.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-normal"></a>Normal

```csharp
public FVec2 Normal { get; init; }
```

The surface direction there, pointing back at the ray's origin side (zero if the ray began inside).

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-point"></a>Point

```csharp
public FVec2 Point { get; init; }
```

Where the ray touched it.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)
