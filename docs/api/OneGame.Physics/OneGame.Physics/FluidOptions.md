[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# FluidOptions class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public sealed class FluidOptions
```

How to make a particle fluid with [`PhysicsSpace2D.AddFluid()`](PhysicsSpace2D.md#m-addfluid-fluidoptions): a preset, the particle spacing, and optional overrides. Every fluid in one space shares the same spacing.

## Constructors

| Member | Description |
| --- | --- |
| [`FluidOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>FluidOptions()

```csharp
public FluidOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Friction`](#m-friction) | Overrides the friction against bodies; null keeps the preset's. |
| [`GravityScale`](#m-gravityscale) | Overrides how strongly gravity pulls on the fluid (1 is normal); null keeps the preset's. |
| [`Layer`](#m-layer) | Overrides the collision layer (0 to 31) the fluid meets bodies on; null keeps the preset's. |
| [`MaxSpeed`](#m-maxspeed) | Overrides the particle speed limit in units per second; null keeps the preset's. |
| [`Preset`](#m-preset) | The material to start from. |
| [`RestDensity`](#m-restdensity) | Overrides the rest density in kg per cubic metre (water is 1000); null keeps the preset's. |
| [`Spacing`](#m-spacing) | The distance between neighbouring particles at rest, in world units (one particle is a disc of about this size). |
| [`Viscosity`](#m-viscosity) | Overrides the preset's viscosity (how fast neighbouring particles even out their speeds); null keeps the preset's. |

### <a id="m-friction"></a>Friction

```csharp
public Fixed? Friction { get; set; }
```

Overrides the friction against bodies; null keeps the preset's.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)?

### <a id="m-gravityscale"></a>GravityScale

```csharp
public Fixed? GravityScale { get; set; }
```

Overrides how strongly gravity pulls on the fluid (1 is normal); null keeps the preset's.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)?

### <a id="m-layer"></a>Layer

```csharp
public int? Layer { get; set; }
```

Overrides the collision layer (0 to 31) the fluid meets bodies on; null keeps the preset's.

**Type:** `int?`

### <a id="m-maxspeed"></a>MaxSpeed

```csharp
public Fixed? MaxSpeed { get; set; }
```

Overrides the particle speed limit in units per second; null keeps the preset's.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)?

### <a id="m-preset"></a>Preset

```csharp
public FluidPreset Preset { get; set; }
```

The material to start from. Default water.

**Type:** [`FluidPreset`](FluidPreset.md)

### <a id="m-restdensity"></a>RestDensity

```csharp
public Fixed? RestDensity { get; set; }
```

Overrides the rest density in kg per cubic metre (water is 1000); null keeps the preset's.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)?

### <a id="m-spacing"></a>Spacing

```csharp
public Fixed Spacing { get; set; }
```

The distance between neighbouring particles at rest, in world units (one particle is a disc of about this size). Smaller is finer and costs more particles. Default 0.1. All fluids in a space must use the same value.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-viscosity"></a>Viscosity

```csharp
public Fixed? Viscosity { get; set; }
```

Overrides the preset's viscosity (how fast neighbouring particles even out their speeds); null keeps the preset's.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)?
