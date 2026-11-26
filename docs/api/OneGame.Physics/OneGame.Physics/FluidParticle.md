[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# FluidParticle struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public readonly struct FluidParticle
```

One particle of a fluid, in game types. Views copy these out each frame and draw them.

## Properties

| Member | Description |
| --- | --- |
| [`Fluid`](#m-fluid) | The fluid the particle belongs to. |
| [`Id`](#m-id) | The particle's id; it stays the same for the particle's life while the order of particles in a read changes from step to step. |
| [`Position`](#m-position) | Where the particle is. |
| [`Velocity`](#m-velocity) | How fast it moves. |

### <a id="m-fluid"></a>Fluid

```csharp
public FluidRef Fluid { get; }
```

The fluid the particle belongs to.

**Type:** [`FluidRef`](FluidRef.md)

### <a id="m-id"></a>Id

```csharp
public int Id { get; }
```

The particle's id; it stays the same for the particle's life while the order of particles in a read changes from step to step.

**Type:** `int`

### <a id="m-position"></a>Position

```csharp
public FVec2 Position { get; }
```

Where the particle is.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

### <a id="m-velocity"></a>Velocity

```csharp
public FVec2 Velocity { get; }
```

How fast it moves.

**Type:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)
