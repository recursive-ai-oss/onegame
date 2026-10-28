[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# BlendSpace1DMotion class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)  
**Inherits:** [`Motion`](Motion.md)

```csharp
public sealed class BlendSpace1DMotion : Motion
```

Clips blended along one input, for example speed.

## Constructors

| Member | Description |
| --- | --- |
| [`BlendSpace1DMotion(BlendSpace1D, AnimationClip[])`](#m-ctor-blendspace1d-animationclip) | Creates the motion. |

### <a id="m-ctor-blendspace1d-animationclip"></a>BlendSpace1DMotion(BlendSpace1D, AnimationClip\[\])

```csharp
public BlendSpace1DMotion(BlendSpace1D space, params AnimationClip[] clips);
```

Creates the motion.

**Parameters**

- `space` ([`BlendSpace1D`](BlendSpace1D.md)): The positions of the clips.
- `clips` ([`AnimationClip`](AnimationClip.md)\[\]): One clip per position, in the same order.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The counts differ.

## Properties

| Member | Description |
| --- | --- |
| [`Duration`](#m-duration) | How long one pass takes in seconds at the current inputs. |

### <a id="m-duration"></a>Duration

```csharp
public override float Duration { get; }
```

How long one pass takes in seconds at the current inputs.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`Sample(Rig, float, NodePose, NodePose)`](#m-sample-rig-float-nodepose-nodepose) | Writes the pose at a phase. |

### <a id="m-sample-rig-float-nodepose-nodepose"></a>Sample(Rig, float, NodePose, NodePose)

```csharp
public override void Sample(Rig rig, float phase, NodePose dst, NodePose scratch);
```

Writes the pose at a phase.

**Parameters**

- `rig` ([`Rig`](Rig.md)): The rig the clips belong to.
- `phase` (`float`): 0 to 1 through one pass.
- `dst` ([`NodePose`](NodePose.md)): Receives the pose.
- `scratch` ([`NodePose`](NodePose.md)): A second pose buffer the motion may use.
