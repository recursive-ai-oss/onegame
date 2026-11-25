[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# InstanceBatch3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class InstanceBatch3D
```

Many copies of one static model (trees, crowds, rubble), drawn with one instanced draw per primitive. Every frame the renderer culls the copies against the camera and each shadow map and uploads only the visible ones, so tens of thousands are cheap. The model's skin, animation and morph targets are ignored: the batch draws it in its rest pose. Create it with [`Scene3D.AddInstanced()`](Scene3D.md#m-addinstanced-model3d-int). A batch made with [`Scene3D.AddSkinnedInstanced()`](Scene3D.md#m-addskinnedinstanced-model3d-int) instead gives every copy its own pose (see [`InstanceBatch3D.Add()`](InstanceBatch3D.md#m-add-matrix4x4-nodepose-vector4)).

## Properties

| Member | Description |
| --- | --- |
| [`BoundsMargin`](#m-boundsmargin) | How much of the rest-pose bounding radius is added to a posed copy's culling sphere, so limbs that reach out in an animation are not cut off at the screen edge or lose their shadow. |
| [`CastShadows`](#m-castshadows) | Whether the copies cast shadows (when lights do). |
| [`Count`](#m-count) | Number of copies. |
| [`Items`](#m-items) | The copies, [`InstanceBatch3D.Count`](InstanceBatch3D.md#m-count) of them. |
| [`Model`](#m-model) | The model every copy draws. |
| [`Posed`](#m-posed) | True when each copy carries its own pose: the batch came from [`Scene3D.AddSkinnedInstanced()`](Scene3D.md#m-addskinnedinstanced-model3d-int) and the model has at least one skin. |
| [`Visible`](#m-visible) | Whether the batch is drawn. |

### <a id="m-boundsmargin"></a>BoundsMargin

```csharp
public float BoundsMargin { get; set; }
```

How much of the rest-pose bounding radius is added to a posed copy's culling sphere, so limbs that reach out in an animation are not cut off at the screen edge or lose their shadow. 0.5 by default; ignored by batches that are not [`InstanceBatch3D.Posed`](InstanceBatch3D.md#m-posed).

**Type:** `float`

### <a id="m-castshadows"></a>CastShadows

```csharp
public bool CastShadows { get; set; }
```

Whether the copies cast shadows (when lights do).

**Type:** `bool`

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

Number of copies.

**Type:** `int`

### <a id="m-items"></a>Items

```csharp
public Span<InstanceData3D> Items { get; }
```

The copies, [`InstanceBatch3D.Count`](InstanceBatch3D.md#m-count) of them. Edit in place, then call [`InstanceBatch3D.Touch()`](InstanceBatch3D.md#m-touch) if bounds changed.

**Type:** [`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)&lt;[`InstanceData3D`](InstanceData3D.md)&gt;

### <a id="m-model"></a>Model

```csharp
public Model3D Model { get; }
```

The model every copy draws.

**Type:** [`Model3D`](Model3D.md)

### <a id="m-posed"></a>Posed

```csharp
public bool Posed { get; }
```

True when each copy carries its own pose: the batch came from [`Scene3D.AddSkinnedInstanced()`](Scene3D.md#m-addskinnedinstanced-model3d-int) and the model has at least one skin. Copies of other batches, and copies added without a pose, are drawn at the model's rest pose.

**Type:** `bool`

### <a id="m-visible"></a>Visible

```csharp
public bool Visible { get; set; }
```

Whether the batch is drawn.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Add(in Matrix4x4, Vector4?)`](#m-add-matrix4x4-vector4) | Adds a copy and returns its index. |
| [`Add(in Matrix4x4, NodePose, Vector4?)`](#m-add-matrix4x4-nodepose-vector4) | Adds a copy in its own pose. |
| [`Clear()`](#m-clear) | Removes every copy. |
| [`RemoveAt(int)`](#m-removeat-int) | Removes a copy by moving the last one into its place (order is not kept). |
| [`Set(int, in Matrix4x4, Vector4?)`](#m-set-int-matrix4x4-vector4) | Replaces one copy. |
| [`Set(int, in Matrix4x4, NodePose, Vector4?)`](#m-set-int-matrix4x4-nodepose-vector4) | Replaces one copy and its pose. |
| [`SetPose(int, NodePose)`](#m-setpose-int-nodepose) | Gives one copy a new pose and leaves its transform and tint alone. |
| [`Touch()`](#m-touch) | Tells the batch its [`InstanceBatch3D.Items`](InstanceBatch3D.md#m-items) were edited in place, so it recomputes its bounds. |

### <a id="m-add-matrix4x4-vector4"></a>Add(in Matrix4x4, Vector4?)

```csharp
public int Add(in Matrix4x4 transform, Vector4? color = default);
```

Adds a copy and returns its index.

**Parameters**

- `transform` ([`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)): Placement in the world.
- `color` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)?): Tint; null for white.

**Returns:** `int`

### <a id="m-add-matrix4x4-nodepose-vector4"></a>Add(in Matrix4x4, NodePose, Vector4?)

```csharp
public int Add(in Matrix4x4 transform, NodePose pose, Vector4? color = default);
```

Adds a copy in its own pose. The pose is read now, so the caller may change it or reuse the object straight away; each call costs one pass over the model's nodes and skins, so a crowd animated by a few shared clips can evaluate each clip once per frame and add many copies.

**Parameters**

- `transform` ([`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)): Placement in the world.
- `pose` ([`NodePose`](../../OneGame.Gltf/OneGame.Gltf/NodePose.md)): Node pose of this copy, for example from an `Animator` or [`AnimationClip.Apply()`](../../OneGame.Gltf/OneGame.Gltf/AnimationClip.md#m-apply-nodepose-float). It must belong to this batch's model.
- `color` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)?): Tint; null for white.

**Returns:** `int` — The copy's index.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The batch is not [`InstanceBatch3D.Posed`](InstanceBatch3D.md#m-posed).

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Removes every copy.

### <a id="m-removeat-int"></a>RemoveAt(int)

```csharp
public void RemoveAt(int index);
```

Removes a copy by moving the last one into its place (order is not kept).

**Parameters**

- `index` (`int`): Copy index.

### <a id="m-set-int-matrix4x4-vector4"></a>Set(int, in Matrix4x4, Vector4?)

```csharp
public void Set(int index, in Matrix4x4 transform, Vector4? color = default);
```

Replaces one copy.

**Parameters**

- `index` (`int`): Copy index.
- `transform` ([`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)): Placement in the world.
- `color` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)?): Tint; null for white.

### <a id="m-set-int-matrix4x4-nodepose-vector4"></a>Set(int, in Matrix4x4, NodePose, Vector4?)

```csharp
public void Set(int index, in Matrix4x4 transform, NodePose pose, Vector4? color = default);
```

Replaces one copy and its pose.

**Parameters**

- `index` (`int`): Copy index.
- `transform` ([`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)): Placement in the world.
- `pose` ([`NodePose`](../../OneGame.Gltf/OneGame.Gltf/NodePose.md)): Node pose of this copy.
- `color` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)?): Tint; null for white.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The batch is not [`InstanceBatch3D.Posed`](InstanceBatch3D.md#m-posed).

### <a id="m-setpose-int-nodepose"></a>SetPose(int, NodePose)

```csharp
public void SetPose(int index, NodePose pose);
```

Gives one copy a new pose and leaves its transform and tint alone.

**Parameters**

- `index` (`int`): Copy index.
- `pose` ([`NodePose`](../../OneGame.Gltf/OneGame.Gltf/NodePose.md)): Node pose of this copy.

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): The batch is not [`InstanceBatch3D.Posed`](InstanceBatch3D.md#m-posed).

### <a id="m-touch"></a>Touch()

```csharp
public void Touch();
```

Tells the batch its [`InstanceBatch3D.Items`](InstanceBatch3D.md#m-items) were edited in place, so it recomputes its bounds.
