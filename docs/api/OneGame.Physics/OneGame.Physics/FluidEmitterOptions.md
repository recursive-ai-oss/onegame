[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# FluidEmitterOptions class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public sealed class FluidEmitterOptions
```

An emitter: a nozzle that streams new particles into a fluid. See [`PhysicsSpace2D.AddEmitter()`](PhysicsSpace2D.md#m-addemitter-fluidref-fluidemitteroptions).

## Constructors

| Member | Description |
| --- | --- |
| [`FluidEmitterOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>FluidEmitterOptions()

```csharp
public FluidEmitterOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Direction`](#m-direction) | The direction of the flow (any length except zero). |
| [`Enabled`](#m-enabled) | Whether the nozzle runs. |
| [`MaxEmitted`](#m-maxemitted) | The most particles this nozzle emits over its life; 0 is unlimited. |
| [`Position`](#m-position) | The nozzle centre. |
| [`Shape`](#m-shape) | The nozzle shape. |
| [`Size`](#m-size) | Half extents across the flow for a box, the radius in X for a circle. |
| [`Speed`](#m-speed) | The exit speed in units per second, 0 to 1000. |

### <a id="m-direction"></a>Direction

```csharp
public FVec2 Direction { get; set; }
```

The direction of the flow (any length except zero). Default right.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-enabled"></a>Enabled

```csharp
public bool Enabled { get; set; }
```

Whether the nozzle runs. Default true.

**Type:** `bool`

### <a id="m-maxemitted"></a>MaxEmitted

```csharp
public int MaxEmitted { get; set; }
```

The most particles this nozzle emits over its life; 0 is unlimited.

**Type:** `int`

### <a id="m-position"></a>Position

```csharp
public FVec2 Position { get; set; }
```

The nozzle centre.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-shape"></a>Shape

```csharp
public FluidVolumeShape Shape { get; set; }
```

The nozzle shape.

**Type:** [`FluidVolumeShape`](FluidVolumeShape.md)

### <a id="m-size"></a>Size

```csharp
public FVec2 Size { get; set; }
```

Half extents across the flow for a box, the radius in X for a circle. Default 0.2 by 0.2.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-speed"></a>Speed

```csharp
public Fixed Speed { get; set; }
```

The exit speed in units per second, 0 to 1000. Default 3.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)
