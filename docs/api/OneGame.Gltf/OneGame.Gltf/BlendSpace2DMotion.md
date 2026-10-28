[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# BlendSpace2DMotion class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)  
**Inherits:** [`Motion`](Motion.md)

```csharp
public sealed class BlendSpace2DMotion : Motion
```

Clips blended over a plane, for example direction by speed.

## Constructors

| Member | Description |
| --- | --- |
| [`BlendSpace2DMotion(BlendSpace2D, AnimationClip[])`](#m-ctor-blendspace2d-animationclip) | Creates the motion. |

### <a id="m-ctor-blendspace2d-animationclip"></a>BlendSpace2DMotion(BlendSpace2D, AnimationClip\[\])

```csharp
public BlendSpace2DMotion(BlendSpace2D space, params AnimationClip[] clips);
```

Creates the motion.

**Parameters**

- `space` ([`BlendSpace2D`](BlendSpace2D.md)): The positions of the clips.
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
