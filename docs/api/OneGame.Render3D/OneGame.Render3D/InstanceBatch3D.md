[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# InstanceBatch3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class InstanceBatch3D
```

Many copies of one static model (trees, crowds, rubble), drawn with one instanced draw per primitive. Every frame the renderer culls the copies against the camera and each shadow map and uploads only the visible ones, so tens of thousands are cheap. The model's skin, animation and morph targets are ignored: the batch draws it in its rest pose. Create it with [`Scene3D.AddInstanced()`](Scene3D.md#m-addinstanced-model3d-int).

## Properties

| Member | Description |
| --- | --- |
| [`CastShadows`](#m-castshadows) | Whether the copies cast shadows (when lights do). |
| [`Count`](#m-count) | Number of copies. |
| [`Items`](#m-items) | The copies, [`InstanceBatch3D.Count`](InstanceBatch3D.md#m-count) of them. |
| [`Model`](#m-model) | The model every copy draws. |
| [`Visible`](#m-visible) | Whether the batch is drawn. |

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
| [`Clear()`](#m-clear) | Removes every copy. |
| [`RemoveAt(int)`](#m-removeat-int) | Removes a copy by moving the last one into its place (order is not kept). |
| [`Set(int, in Matrix4x4, Vector4?)`](#m-set-int-matrix4x4-vector4) | Replaces one copy. |
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

### <a id="m-touch"></a>Touch()

```csharp
public void Touch();
```

Tells the batch its [`InstanceBatch3D.Items`](InstanceBatch3D.md#m-items) were edited in place, so it recomputes its bounds.
