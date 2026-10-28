[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# AnimationClip class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class AnimationClip
```

An animation clip: channels that drive node properties over time.

## Constructors

| Member | Description |
| --- | --- |
| [`AnimationClip()`](#m-ctor) |  |

### <a id="m-ctor"></a>AnimationClip()

```csharp
public AnimationClip();
```

## Properties

| Member | Description |
| --- | --- |
| [`Channels`](#m-channels) | The channels. |
| [`Duration`](#m-duration) | The time of the last key of any sampler, in seconds. |
| [`Name`](#m-name) | The clip's name, or empty. |
| [`Samplers`](#m-samplers) | The samplers the channels use. |

### <a id="m-channels"></a>Channels

```csharp
public AnimationChannel[] Channels { get; }
```

The channels.

**Type:** [`AnimationChannel`](AnimationChannel.md)\[\]

### <a id="m-duration"></a>Duration

```csharp
public float Duration { get; }
```

The time of the last key of any sampler, in seconds.

**Type:** `float`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The clip's name, or empty.

**Type:** `string`

### <a id="m-samplers"></a>Samplers

```csharp
public AnimationSampler[] Samplers { get; }
```

The samplers the channels use.

**Type:** [`AnimationSampler`](AnimationSampler.md)\[\]

## Methods

| Member | Description |
| --- | --- |
| [`Apply(NodePose, float)`](#m-apply-nodepose-float) | Writes the animated properties at a time into a pose; other properties are left alone. |

### <a id="m-apply-nodepose-float"></a>Apply(NodePose, float)

```csharp
public void Apply(NodePose pose, float time);
```

Writes the animated properties at a time into a pose; other properties are left alone.

**Parameters**

- `pose` ([`NodePose`](NodePose.md)): The pose to change, made by [`GltfModel.CreateRestPose()`](GltfModel.md#m-createrestpose).
- `time` (`float`): Seconds; times outside the clip use its first or last values. Loop with `time % Duration`.
