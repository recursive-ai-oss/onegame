[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# BodyFluidState struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`BodyFluidState`](BodyFluidState.md)&gt;

```csharp
public readonly struct BodyFluidState : IEquatable<BodyFluidState>
```

What a fluid does to one body: how much of it is covered and the average force on it.

## Constructors

| Member | Description |
| --- | --- |
| [`BodyFluidState(Fixed, Fixed, FVec2)`](#m-ctor-fixed-fixed-fvec2) | What a fluid does to one body: how much of it is covered and the average force on it. |

### <a id="m-ctor-fixed-fixed-fvec2"></a>BodyFluidState(Fixed, Fixed, FVec2)

```csharp
public BodyFluidState(Fixed Submerged, Fixed WettedArea, FVec2 Force);
```

What a fluid does to one body: how much of it is covered and the average force on it.

**Parameters**

- `Submerged` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): The covered fraction of the body's area, 0 to 1.
- `WettedArea` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): How much of the body's surface the particles touch.
- `Force` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): The average force the particles put on the body.

## Properties

| Member | Description |
| --- | --- |
| [`Force`](#m-force) | The average force the particles put on the body. |
| [`Submerged`](#m-submerged) | The covered fraction of the body's area, 0 to 1. |
| [`WettedArea`](#m-wettedarea) | How much of the body's surface the particles touch. |

### <a id="m-force"></a>Force

```csharp
public FVec2 Force { get; init; }
```

The average force the particles put on the body.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-submerged"></a>Submerged

```csharp
public Fixed Submerged { get; init; }
```

The covered fraction of the body's area, 0 to 1.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-wettedarea"></a>WettedArea

```csharp
public Fixed WettedArea { get; init; }
```

How much of the body's surface the particles touch.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)
