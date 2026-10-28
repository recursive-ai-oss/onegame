[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# ClipMotion class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)  
**Inherits:** [`Motion`](Motion.md)

```csharp
public sealed class ClipMotion : Motion
```

A single clip.

## Constructors

| Member | Description |
| --- | --- |
| [`ClipMotion(AnimationClip)`](#m-ctor-animationclip) | Creates a motion from a clip. |

### <a id="m-ctor-animationclip"></a>ClipMotion(AnimationClip)

```csharp
public ClipMotion(AnimationClip clip);
```

Creates a motion from a clip.

**Parameters**

- `clip` ([`AnimationClip`](AnimationClip.md)): The clip.

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
