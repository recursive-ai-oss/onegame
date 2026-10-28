[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# Rig class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class Rig
```

A model's skeleton for animation: its rest pose, node lookup by name and clip lookup by name. Everything that samples or blends poses starts from one of these. A rig is read-only and can be shared by any number of animators.

## Constructors

| Member | Description |
| --- | --- |
| [`Rig(GltfModel)`](#m-ctor-gltfmodel) | Creates the rig of a model. |

### <a id="m-ctor-gltfmodel"></a>Rig(GltfModel)

```csharp
public Rig(GltfModel model);
```

Creates the rig of a model.

**Parameters**

- `model` ([`GltfModel`](GltfModel.md)): An imported model.

## Properties

| Member | Description |
| --- | --- |
| [`Model`](#m-model) | The model. |
| [`NodeCount`](#m-nodecount) | The number of nodes in a pose. |

### <a id="m-model"></a>Model

```csharp
public GltfModel Model { get; }
```

The model.

**Type:** [`GltfModel`](GltfModel.md)

### <a id="m-nodecount"></a>NodeCount

```csharp
public int NodeCount { get; }
```

The number of nodes in a pose.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Clip(string)`](#m-clip-string) | Finds a clip by name. |
| [`ComputeWorld(NodePose)`](#m-computeworld-nodepose) | Computes every node's transform in the model's root space for a pose, into a buffer owned by the calling thread (valid until this thread's next call, on any rig). |
| [`CreatePose()`](#m-createpose) | Makes a pose set to the rest pose. |
| [`Node(string)`](#m-node-string) | Finds a node by name. |
| [`Reset(NodePose)`](#m-reset-nodepose) | Sets every node of a pose to its rest value, without allocating. |
| [`RestTranslation(int)`](#m-resttranslation-int) | The rest value of a node's translation. |
| [`Sample(AnimationClip, float, NodePose)`](#m-sample-animationclip-float-nodepose) | Samples a clip at a time into a pose, from the rest pose, so properties the clip does not animate are at rest. |

### <a id="m-clip-string"></a>Clip(string)

```csharp
public AnimationClip Clip(string name);
```

Finds a clip by name.

**Parameters**

- `name` (`string`): The animation's name in the file.

**Returns:** [`AnimationClip`](AnimationClip.md)

**Exceptions**

- [`KeyNotFoundException`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception): There is no such clip; the message lists the names.

### <a id="m-computeworld-nodepose"></a>ComputeWorld(NodePose)

```csharp
public ReadOnlySpan<Matrix4x4> ComputeWorld(NodePose pose);
```

Computes every node's transform in the model's root space for a pose, into a buffer owned by the calling thread (valid until this thread's next call, on any rig).

**Parameters**

- `pose` ([`NodePose`](NodePose.md)): The pose.

**Returns:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)&gt;

### <a id="m-createpose"></a>CreatePose()

```csharp
public NodePose CreatePose();
```

Makes a pose set to the rest pose. Poses are plain buffers: make the ones you need once and reuse them.

**Returns:** [`NodePose`](NodePose.md)

### <a id="m-node-string"></a>Node(string)

```csharp
public int Node(string name);
```

Finds a node by name.

**Parameters**

- `name` (`string`): The node's name in the file.

**Returns:** `int` — The node index.

**Exceptions**

- [`KeyNotFoundException`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception): There is no such node; the message lists the names.

### <a id="m-reset-nodepose"></a>Reset(NodePose)

```csharp
public void Reset(NodePose pose);
```

Sets every node of a pose to its rest value, without allocating.

**Parameters**

- `pose` ([`NodePose`](NodePose.md)): A pose made by [`Rig.CreatePose()`](Rig.md#m-createpose) (or by this model's [`GltfModel.CreateRestPose()`](GltfModel.md#m-createrestpose)).

### <a id="m-resttranslation-int"></a>RestTranslation(int)

```csharp
public Vector3 RestTranslation(int node);
```

The rest value of a node's translation.

**Parameters**

- `node` (`int`)

**Returns:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-sample-animationclip-float-nodepose"></a>Sample(AnimationClip, float, NodePose)

```csharp
public void Sample(AnimationClip clip, float seconds, NodePose pose);
```

Samples a clip at a time into a pose, from the rest pose, so properties the clip does not animate are at rest.

**Parameters**

- `clip` ([`AnimationClip`](AnimationClip.md)): The clip.
- `seconds` (`float`): Time in the clip; outside it the first or last values hold.
- `pose` ([`NodePose`](NodePose.md)): Receives the result.
