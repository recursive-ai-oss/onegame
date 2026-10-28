[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# ModelInstance3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class ModelInstance3D : IDisposable
```

One placed copy of a [`Model3D`](Model3D.md), with its own pose.

## Properties

| Member | Description |
| --- | --- |
| [`Model`](#m-model) | The model this instance draws. |
| [`NodeWorlds`](#m-nodeworlds) | World matrix of every node of the model (before [`ModelInstance3D.Transform`](ModelInstance3D.md#m-transform)), as of the last [`ModelInstance3D.UpdateTransforms()`](ModelInstance3D.md#m-updatetransforms). |
| [`Pose`](#m-pose) | Per-node translation, rotation, scale and morph weights. |
| [`Transform`](#m-transform) | Placement of the whole model in the world. |
| [`Visible`](#m-visible) | Whether the instance is drawn. |

### <a id="m-model"></a>Model

```csharp
public Model3D Model { get; }
```

The model this instance draws.

**Type:** [`Model3D`](Model3D.md)

### <a id="m-nodeworlds"></a>NodeWorlds

```csharp
public ReadOnlySpan<Matrix4x4> NodeWorlds { get; }
```

World matrix of every node of the model (before [`ModelInstance3D.Transform`](ModelInstance3D.md#m-transform)), as of the last [`ModelInstance3D.UpdateTransforms()`](ModelInstance3D.md#m-updatetransforms).

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)&gt;

### <a id="m-pose"></a>Pose

```csharp
public NodePose Pose { get; }
```

Per-node translation, rotation, scale and morph weights. Change them, then call [`ModelInstance3D.UpdateTransforms()`](ModelInstance3D.md#m-updatetransforms).

**Type:** [`NodePose`](../../OneGame.Gltf/OneGame.Gltf/NodePose.md)

### <a id="m-transform"></a>Transform

```csharp
public Matrix4x4 Transform { get; set; }
```

Placement of the whole model in the world.

**Type:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)

### <a id="m-visible"></a>Visible

```csharp
public bool Visible { get; set; }
```

Whether the instance is drawn.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Animate(int, float, bool)`](#m-animate-int-float-bool) | Evaluates an animation at a time (seconds) into the pose and updates the node transforms. |
| [`Dispose()`](#m-dispose) | Releases the vertex buffers of morphed primitives. |
| [`ResetPose()`](#m-resetpose) | Returns the pose to the model's rest pose. |
| [`UpdateTransforms()`](#m-updatetransforms) | Recomputes the node world matrices from [`ModelInstance3D.Pose`](ModelInstance3D.md#m-pose). |

### <a id="m-animate-int-float-bool"></a>Animate(int, float, bool)

```csharp
public void Animate(int clip, float seconds, bool loop = true);
```

Evaluates an animation at a time (seconds) into the pose and updates the node transforms.

**Parameters**

- `clip` (`int`): Index into [`GltfModel.Animations`](../../OneGame.Gltf/OneGame.Gltf/GltfModel.md#m-animations).
- `seconds` (`float`): Time since the animation started.
- `loop` (`bool`): Wrap the time around the clip's duration.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the vertex buffers of morphed primitives. Call it when an instance of a model with morph targets is discarded.

### <a id="m-resetpose"></a>ResetPose()

```csharp
public void ResetPose();
```

Returns the pose to the model's rest pose.

### <a id="m-updatetransforms"></a>UpdateTransforms()

```csharp
public void UpdateTransforms();
```

Recomputes the node world matrices from [`ModelInstance3D.Pose`](ModelInstance3D.md#m-pose).
