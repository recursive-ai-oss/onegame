[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# FluidDrainOptions class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public sealed class FluidDrainOptions
```

A drain: a volume that removes the particles of its fluid at the end of every step. See [`PhysicsSpace2D.AddDrain()`](PhysicsSpace2D.md#m-adddrain-fluidref-fluiddrainoptions).

## Constructors

| Member | Description |
| --- | --- |
| [`FluidDrainOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>FluidDrainOptions()

```csharp
public FluidDrainOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Enabled`](#m-enabled) | Whether the drain removes particles. |
| [`Position`](#m-position) | The volume's centre. |
| [`Shape`](#m-shape) | The volume shape. |
| [`Size`](#m-size) | Half extents for a box, the radius in X for a circle. |

### <a id="m-enabled"></a>Enabled

```csharp
public bool Enabled { get; set; }
```

Whether the drain removes particles. Default true.

**Type:** `bool`

### <a id="m-position"></a>Position

```csharp
public FVec2 Position { get; set; }
```

The volume's centre.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-shape"></a>Shape

```csharp
public FluidVolumeShape Shape { get; set; }
```

The volume shape.

**Type:** [`FluidVolumeShape`](FluidVolumeShape.md)

### <a id="m-size"></a>Size

```csharp
public FVec2 Size { get; set; }
```

Half extents for a box, the radius in X for a circle. Default 0.5 by 0.5.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)
