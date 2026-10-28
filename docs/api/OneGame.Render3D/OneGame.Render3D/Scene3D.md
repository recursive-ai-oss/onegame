[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# Scene3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class Scene3D
```

Everything one frame draws: camera, models, lights, environment and post-processing.

## Constructors

| Member | Description |
| --- | --- |
| [`Scene3D()`](#m-ctor) |  |

### <a id="m-ctor"></a>Scene3D()

```csharp
public Scene3D();
```

## Properties

| Member | Description |
| --- | --- |
| [`Batches`](#m-batches) | Copies of models drawn in bulk (see [`Scene3D.AddInstanced()`](Scene3D.md#m-addinstanced-model3d-int)). |
| [`Billboards`](#m-billboards) | Camera-facing quads (particles, glows) drawn after the surfaces; see [`BillboardBatch3D`](BillboardBatch3D.md). |
| [`Camera`](#m-camera) | The camera. |
| [`Environment`](#m-environment) | Image-based lighting and backdrop. |
| [`Lights`](#m-lights) | Punctual lights. |
| [`Models`](#m-models) | Models to draw. |
| [`Post`](#m-post) | Bloom, exposure and tone mapping. |
| [`Quality`](#m-quality) | The quality level: bounds shadow map size, cascades, ambient occlusion, multisampling, bloom and environment size. |
| [`Samples`](#m-samples) | Multisampling: 1 (off), 4 or 8 (a request for 2 uses 4), limited by the device and by [`Scene3D.Quality`](Scene3D.md#m-quality). |
| [`Shadows`](#m-shadows) | Shadow controls; a light casts shadows when its [`Light3D.CastShadows`](Light3D.md#m-castshadows) is set. |
| [`Ssao`](#m-ssao) | Screen-space ambient occlusion. |

### <a id="m-batches"></a>Batches

```csharp
public List<InstanceBatch3D> Batches { get; }
```

Copies of models drawn in bulk (see [`Scene3D.AddInstanced()`](Scene3D.md#m-addinstanced-model3d-int)).

**Type:** [`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`InstanceBatch3D`](InstanceBatch3D.md)&gt;

### <a id="m-billboards"></a>Billboards

```csharp
public List<BillboardBatch3D> Billboards { get; }
```

Camera-facing quads (particles, glows) drawn after the surfaces; see [`BillboardBatch3D`](BillboardBatch3D.md).

**Type:** [`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`BillboardBatch3D`](BillboardBatch3D.md)&gt;

### <a id="m-camera"></a>Camera

```csharp
public Camera3D Camera { get; set; }
```

The camera.

**Type:** [`Camera3D`](Camera3D.md)

### <a id="m-environment"></a>Environment

```csharp
public Environment3D Environment { get; }
```

Image-based lighting and backdrop.

**Type:** [`Environment3D`](Environment3D.md)

### <a id="m-lights"></a>Lights

```csharp
public List<Light3D> Lights { get; }
```

Punctual lights. At most [`Renderer3D.MaxLights`](Renderer3D.md#m-maxlights) are used.

**Type:** [`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`Light3D`](Light3D.md)&gt;

### <a id="m-models"></a>Models

```csharp
public List<ModelInstance3D> Models { get; }
```

Models to draw.

**Type:** [`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`ModelInstance3D`](ModelInstance3D.md)&gt;

### <a id="m-post"></a>Post

```csharp
public PostSettings Post { get; }
```

Bloom, exposure and tone mapping.

**Type:** [`PostSettings`](PostSettings.md)

### <a id="m-quality"></a>Quality

```csharp
public QualityLevel Quality { get; set; }
```

The quality level: bounds shadow map size, cascades, ambient occlusion, multisampling, bloom and environment size. Change it any time.

**Type:** [`QualityLevel`](QualityLevel.md)

### <a id="m-samples"></a>Samples

```csharp
public int Samples { get; set; }
```

Multisampling: 1 (off), 4 or 8 (a request for 2 uses 4), limited by the device and by [`Scene3D.Quality`](Scene3D.md#m-quality). 0 (the default) takes the level's own count.

**Type:** `int`

### <a id="m-shadows"></a>Shadows

```csharp
public ShadowSettings Shadows { get; }
```

Shadow controls; a light casts shadows when its [`Light3D.CastShadows`](Light3D.md#m-castshadows) is set.

**Type:** [`ShadowSettings`](ShadowSettings.md)

### <a id="m-ssao"></a>Ssao

```csharp
public SsaoSettings Ssao { get; }
```

Screen-space ambient occlusion.

**Type:** [`SsaoSettings`](SsaoSettings.md)

## Methods

| Member | Description |
| --- | --- |
| [`Add(Model3D, Matrix4x4?)`](#m-add-model3d-matrix4x4) | Adds a copy of a model and returns it. |
| [`AddInstanced(Model3D, int)`](#m-addinstanced-model3d-int) | Adds a batch that draws many copies of a static model with per-copy transforms and colours. |
| [`AddModelLights(ModelInstance3D)`](#m-addmodellights-modelinstance3d) | Adds the lights the model's glTF file defines (KHR\_lights\_punctual) at their current positions, through the instance transform. |
| [`CameraOf(ModelInstance3D, int)`](#m-cameraof-modelinstance3d-int) | The camera a node of the model carries, placed through the instance's transform. |

### <a id="m-add-model3d-matrix4x4"></a>Add(Model3D, Matrix4x4?)

```csharp
public ModelInstance3D Add(Model3D model, Matrix4x4? transform = default);
```

Adds a copy of a model and returns it.

**Parameters**

- `model` ([`Model3D`](Model3D.md))
- `transform` ([`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)?)

**Returns:** [`ModelInstance3D`](ModelInstance3D.md)

### <a id="m-addinstanced-model3d-int"></a>AddInstanced(Model3D, int)

```csharp
public InstanceBatch3D AddInstanced(Model3D model, int capacity = 64);
```

Adds a batch that draws many copies of a static model with per-copy transforms and colours.

**Parameters**

- `model` ([`Model3D`](Model3D.md)): The model.
- `capacity` (`int`): Copies to reserve room for; the batch grows as needed.

**Returns:** [`InstanceBatch3D`](InstanceBatch3D.md)

### <a id="m-addmodellights-modelinstance3d"></a>AddModelLights(ModelInstance3D)

```csharp
public void AddModelLights(ModelInstance3D instance);
```

Adds the lights the model's glTF file defines (KHR\_lights\_punctual) at their current positions, through the instance transform.

**Parameters**

- `instance` ([`ModelInstance3D`](ModelInstance3D.md))

### <a id="m-cameraof-modelinstance3d-int"></a>CameraOf(ModelInstance3D, int)

```csharp
public static Camera3D? CameraOf(ModelInstance3D instance, int cameraIndex = 0);
```

The camera a node of the model carries, placed through the instance's transform.

**Parameters**

- `instance` ([`ModelInstance3D`](ModelInstance3D.md))
- `cameraIndex` (`int`)

**Returns:** [`Camera3D`](Camera3D.md)?
