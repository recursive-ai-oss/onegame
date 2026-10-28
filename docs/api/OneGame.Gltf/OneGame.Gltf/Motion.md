[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# Motion class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public abstract class Motion
```

Something that produces a pose from a phase: one clip, or a blend of clips chosen by inputs. Phase is a share of the motion's length (0 to 1), so clips of different lengths in one blend stay in step. Use a motion by hand, or give it to an [`Animator`](Animator.md) state.

## Constructors

| Member | Description |
| --- | --- |
| [`Motion()`](#m-ctor) |  |

### <a id="m-ctor"></a>Motion()

```csharp
protected Motion();
```

## Properties

| Member | Description |
| --- | --- |
| [`Duration`](#m-duration) | How long one pass takes in seconds at the current inputs. |
| [`X`](#m-x) | The first input value. |
| [`XParameter`](#m-xparameter) | The input this motion reads for its first axis (set by [`Animator`](Animator.md) from the parameter of this name), or null. |
| [`Y`](#m-y) | The second input value. |
| [`YParameter`](#m-yparameter) | The input for the second axis of a 2D blend, or null. |

### <a id="m-duration"></a>Duration

```csharp
public abstract float Duration { get; }
```

How long one pass takes in seconds at the current inputs.

**Type:** `float`

### <a id="m-x"></a>X

```csharp
public float X { get; set; }
```

The first input value.

**Type:** `float`

### <a id="m-xparameter"></a>XParameter

```csharp
public string? XParameter { get; init; }
```

The input this motion reads for its first axis (set by [`Animator`](Animator.md) from the parameter of this name), or null.

**Type:** `string?`

### <a id="m-y"></a>Y

```csharp
public float Y { get; set; }
```

The second input value.

**Type:** `float`

### <a id="m-yparameter"></a>YParameter

```csharp
public string? YParameter { get; init; }
```

The input for the second axis of a 2D blend, or null.

**Type:** `string?`

## Methods

| Member | Description |
| --- | --- |
| [`Sample(Rig, float, NodePose, NodePose)`](#m-sample-rig-float-nodepose-nodepose) | Writes the pose at a phase. |

### <a id="m-sample-rig-float-nodepose-nodepose"></a>Sample(Rig, float, NodePose, NodePose)

```csharp
public abstract void Sample(Rig rig, float phase, NodePose dst, NodePose scratch);
```

Writes the pose at a phase.

**Parameters**

- `rig` ([`Rig`](Rig.md)): The rig the clips belong to.
- `phase` (`float`): 0 to 1 through one pass.
- `dst` ([`NodePose`](NodePose.md)): Receives the pose.
- `scratch` ([`NodePose`](NodePose.md)): A second pose buffer the motion may use.
