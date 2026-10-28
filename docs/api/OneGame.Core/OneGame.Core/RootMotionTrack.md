[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# RootMotionTrack class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class RootMotionTrack
```

The movement of an animation's root bone, one fixed-point step per simulation tick. Sim code that lets an animation move a character keeps an integer tick counter in synced state and applies [`RootMotionTrack.DeltaAt()`](RootMotionTrack.md#m-deltaat-long-bool) each tick, so every node computes the same position. Make one at load time with `RootMotion.Bake` (in `OneGame.Gltf`) or from your own numbers; the track itself holds only [`Fixed`](Fixed.md) values and is safe to use in sim code.

## Constructors

| Member | Description |
| --- | --- |
| [`RootMotionTrack(FVec3[])`](#m-ctor-fvec3) | Creates a track from per-tick steps. |

### <a id="m-ctor-fvec3"></a>RootMotionTrack(FVec3\[\])

```csharp
public RootMotionTrack(FVec3[] deltas);
```

Creates a track from per-tick steps.

**Parameters**

- `deltas` ([`FVec3`](FVec3.md)\[\]): Movement during tick 0, tick 1, and so on, in the model's own axes (-Z is forward).

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): There are no steps.

## Properties

| Member | Description |
| --- | --- |
| [`Length`](#m-length) | The number of ticks in one pass of the animation. |
| [`Total`](#m-total) | The movement over one whole pass. |

### <a id="m-length"></a>Length

```csharp
public int Length { get; }
```

The number of ticks in one pass of the animation.

**Type:** `int`

### <a id="m-total"></a>Total

```csharp
public FVec3 Total { get; }
```

The movement over one whole pass.

**Type:** [`FVec3`](FVec3.md)

## Methods

| Member | Description |
| --- | --- |
| [`DeltaAt(long, bool)`](#m-deltaat-long-bool) | The movement during one tick. |
| [`Displacement(long, long, bool)`](#m-displacement-long-long-bool) | The movement from the start of tick `from` up to the start of tick `to`. |
| [`Step(Transform3, long, bool)`](#m-step-transform3-long-bool) | Moves a transform by the step of one tick, turned by the transform's own rotation. |

### <a id="m-deltaat-long-bool"></a>DeltaAt(long, bool)

```csharp
public FVec3 DeltaAt(long tick, bool loop = true);
```

The movement during one tick.

**Parameters**

- `tick` (`long`): Ticks since the animation started. Negative is zero movement; past the end it wraps when `loop` is set and is zero otherwise.
- `loop` (`bool`): Whether the animation repeats.

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-displacement-long-long-bool"></a>Displacement(long, long, bool)

```csharp
public FVec3 Displacement(long from, long to, bool loop = true);
```

The movement from the start of tick `from` up to the start of tick `to`.

**Parameters**

- `from` (`long`): The first tick, at least 0.
- `to` (`long`): The tick to stop before, not less than `from`.
- `loop` (`bool`): Whether the animation repeats.

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-step-transform3-long-bool"></a>Step(Transform3, long, bool)

```csharp
public Transform3 Step(Transform3 at, long tick, bool loop = true);
```

Moves a transform by the step of one tick, turned by the transform's own rotation.

**Parameters**

- `at` ([`Transform3`](Transform3.md)): Where the character is.
- `tick` (`long`): Ticks since the animation started.
- `loop` (`bool`): Whether the animation repeats.

**Returns:** [`Transform3`](Transform3.md)
