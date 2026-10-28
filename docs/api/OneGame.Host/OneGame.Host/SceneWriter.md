[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# SceneWriter class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class SceneWriter
```

What an entity's [`IView3D.Describe3D()`](IView3D.md#m-describe3d-scenewriter) writes into: models, lights and cameras for one frame. Calls only collect; the host turns them into a renderer scene afterwards, so describing costs no allocation once the lists have grown.

## Properties

| Member | Description |
| --- | --- |
| [`Alpha`](#m-alpha) | How far past [`SceneWriter.Tick`](SceneWriter.md#m-tick) the frame is, from 0 to just under 1: pass both to `Timed<T>.ValueAt(tick, alpha)` to move smoothly between ticks. |
| [`DeltaTime`](#m-deltatime) | View time since the previous prepared frame, in seconds; zero when the same moment is described again or time went backwards. |
| [`Entity`](#m-entity) | The entity being described. |
| [`ParticleEmitters`](#m-particleemitters) | The emitters handed to `Particles` in the last prepared frame, in call order. |
| [`Tick`](#m-tick) | The tick being shown. |
| [`Time`](#m-time) | View time in seconds: the shown tick and fraction at the simulation's rate. |

### <a id="m-alpha"></a>Alpha

```csharp
public Fixed Alpha { get; }
```

How far past [`SceneWriter.Tick`](SceneWriter.md#m-tick) the frame is, from 0 to just under 1: pass both to `Timed<T>.ValueAt(tick, alpha)` to move smoothly between ticks.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-deltatime"></a>DeltaTime

```csharp
public float DeltaTime { get; }
```

View time since the previous prepared frame, in seconds; zero when the same moment is described again or time went backwards.

**Type:** `float`

### <a id="m-entity"></a>Entity

```csharp
public EntityId Entity { get; set; }
```

The entity being described. A [`SceneView3D.Describer`](SceneView3D.md#m-describer) that draws on behalf of entities sets it before each model, so picking reports that entity.

**Type:** [`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)

### <a id="m-particleemitters"></a>ParticleEmitters

```csharp
public IReadOnlyList<ParticleEmitter> ParticleEmitters { get; }
```

The emitters handed to `Particles` in the last prepared frame, in call order. The harness reads it.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ParticleEmitter`](../../OneGame.Particles/OneGame.Particles/ParticleEmitter.md)&gt;

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; }
```

The tick being shown.

**Type:** `long`

### <a id="m-time"></a>Time

```csharp
public float Time { get; }
```

View time in seconds: the shown tick and fraction at the simulation's rate. Particles read it, so captures and replays repeat exactly.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`Camera(in Matrix4x4, float, int, float, float)`](#m-camera-matrix4x4-float-int-float-float) | Offers a camera placed by a rigid camera-to-world matrix (it looks down its -Z). |
| [`Camera(Transform3, float, int, float, float, float)`](#m-camera-transform3-float-int-float-float-float) | Offers a camera. |
| [`DirectionalLight(Transform3, Vector3, float)`](#m-directionallight-transform3-vector3-float) | A light that shines along the transform's forward direction, as from infinitely far away. |
| [`DirectionalLight(Vector3, Vector3, float, bool)`](#m-directionallight-vector3-vector3-float-bool) | A light from infinitely far away. |
| [`Instance(string, Transform3, Vector4?)`](#m-instance-string-transform3-vector4) | Shows one copy of a static model among many: every copy of the same model in a frame is drawn with one instanced draw per primitive and culled against the camera and the shadow maps, so thousands of trees or rocks are cheap. |
| [`Instance(string, in Matrix4x4, Vector4?)`](#m-instance-string-matrix4x4-vector4) | Shows one copy of a static model among many at a world matrix; see the [`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md) form. |
| [`Model(string, Transform3, bool)`](#m-model-string-transform3-bool) | Shows a model from the [`ModelLibrary`](ModelLibrary.md) at a transform. |
| [`Model(string, in Matrix4x4, bool)`](#m-model-string-matrix4x4-bool) | Shows a model at a world matrix. |
| [`Model(string, Transform3, float, bool)`](#m-model-string-transform3-float-bool) | Shows a model with an extra view-only scale on top of its transform, for a pulse or a hover highlight. |
| [`Particles(ParticleEmitter, Transform3, BillboardTexture3D?)`](#m-particles-particleemitter-transform3-billboardtexture3d) | Shows a particle emitter at an entity transform's position; see [`SceneWriter.Particles()`](SceneWriter.md#m-particles-particleemitter-vector3-billboardtexture3d). |
| [`Particles(ParticleEmitter, Vector3, BillboardTexture3D?)`](#m-particles-particleemitter-vector3-billboardtexture3d) | Shows a particle emitter in the scene as camera-facing quads. |
| [`PointLight(Transform3, Vector3, float, float)`](#m-pointlight-transform3-vector3-float-float) | A light that shines in all directions from the transform's position. |
| [`PointLight(Vector3, Vector3, float, float)`](#m-pointlight-vector3-vector3-float-float) | A light that shines in all directions from a position. |
| [`SpotLight(Transform3, Vector3, float, float, float, float)`](#m-spotlight-transform3-vector3-float-float-float-float) | A cone of light from the transform's position along its forward direction. |
| [`ToMatrix(Transform3)`](#m-tomatrix-transform3) | The matrix that places a thing with this transform in the world (scale, then rotation, then translation; row vectors). |

### <a id="m-camera-matrix4x4-float-int-float-float"></a>Camera(in Matrix4x4, float, int, float, float)

```csharp
public void Camera(in Matrix4x4 world, float fovDegrees = 45f, int priority = 0, float near = 0.1f, float far = 1000f);
```

Offers a camera placed by a rigid camera-to-world matrix (it looks down its -Z). Same choice rule as the transform form.

**Parameters**

- `world` ([`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)): Camera-to-world matrix.
- `fovDegrees` (`float`): Vertical field of view.
- `priority` (`int`): Higher wins.
- `near` (`float`): Near plane distance.
- `far` (`float`): Far plane distance.

### <a id="m-camera-transform3-float-int-float-float-float"></a>Camera(Transform3, float, int, float, float, float)

```csharp
public void Camera(Transform3 transform, float fovDegrees = 45f, int priority = 0, float near = 0.1f, float far = 1000f, float orthoHeight = 0f);
```

Offers a camera. The frame uses the offered camera with the highest `priority` (the first one on a tie), so a game can keep a spectator camera and a player camera and choose one by state.

**Parameters**

- `transform` ([`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md)): Where it is; it looks along its forward direction.
- `fovDegrees` (`float`): Vertical field of view.
- `priority` (`int`): Higher wins.
- `near` (`float`): Near plane distance.
- `far` (`float`): Far plane distance.
- `orthoHeight` (`float`): Greater than zero for an orthographic camera that shows this much world height.

### <a id="m-directionallight-transform3-vector3-float"></a>DirectionalLight(Transform3, Vector3, float)

```csharp
public void DirectionalLight(Transform3 transform, Vector3 color, float lux);
```

A light that shines along the transform's forward direction, as from infinitely far away.

**Parameters**

- `transform` ([`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md)): Its rotation gives the direction.
- `color` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Linear RGB.
- `lux` (`float`): Intensity.

### <a id="m-directionallight-vector3-vector3-float-bool"></a>DirectionalLight(Vector3, Vector3, float, bool)

```csharp
public void DirectionalLight(Vector3 direction, Vector3 color, float lux, bool castShadows = false);
```

A light from infinitely far away.

**Parameters**

- `direction` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The way the light travels.
- `color` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Linear RGB.
- `lux` (`float`): Intensity.
- `castShadows` (`bool`): Whether models and instances cast shadows from this light (cascaded; needs a quality level that allows shadows).

### <a id="m-instance-string-transform3-vector4"></a>Instance(string, Transform3, Vector4?)

```csharp
public void Instance(string model, Transform3 transform, Vector4? tint = default);
```

Shows one copy of a static model among many: every copy of the same model in a frame is drawn with one instanced draw per primitive and culled against the camera and the shadow maps, so thousands of trees or rocks are cheap. The model is drawn in its rest pose (no skin or animation), cannot be picked, and should have a uniform, unmirrored scale.

**Parameters**

- `model` (`string`): The name the model was added under.
- `transform` ([`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md)): Where it is.
- `tint` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)?): Linear RGBA multiplied into the base colour; null leaves it unchanged.

**Exceptions**

- [`UnknownModelException`](UnknownModelException.md): The library has no such model.

### <a id="m-instance-string-matrix4x4-vector4"></a>Instance(string, in Matrix4x4, Vector4?)

```csharp
public void Instance(string model, in Matrix4x4 world, Vector4? tint = default);
```

Shows one copy of a static model among many at a world matrix; see the [`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md) form.

**Parameters**

- `model` (`string`): The name the model was added under.
- `world` ([`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)): Where it is; scale must be uniform and unmirrored.
- `tint` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)?): Linear RGBA multiplied into the base colour; null leaves it unchanged.

**Exceptions**

- [`UnknownModelException`](UnknownModelException.md): The library has no such model.

### <a id="m-model-string-transform3-bool"></a>Model(string, Transform3, bool)

```csharp
public void Model(string model, Transform3 transform, bool pickable = true);
```

Shows a model from the [`ModelLibrary`](ModelLibrary.md) at a transform.

**Parameters**

- `model` (`string`): The name the model was added under.
- `transform` ([`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md)): Where it is.
- `pickable` (`bool`): Whether `pick` can hit it.

**Exceptions**

- [`UnknownModelException`](UnknownModelException.md): The library has no such model.

### <a id="m-model-string-matrix4x4-bool"></a>Model(string, in Matrix4x4, bool)

```csharp
public void Model(string model, in Matrix4x4 world, bool pickable = true);
```

Shows a model at a world matrix. For view code that animates with floats; synced transforms use the [`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md) form.

**Parameters**

- `model` (`string`): The name the model was added under.
- `world` ([`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)): Where it is (row vectors, +Y up).
- `pickable` (`bool`): Whether `pick` can hit it.

**Exceptions**

- [`UnknownModelException`](UnknownModelException.md): The library has no such model.

### <a id="m-model-string-transform3-float-bool"></a>Model(string, Transform3, float, bool)

```csharp
public void Model(string model, Transform3 transform, float extraScale, bool pickable = true);
```

Shows a model with an extra view-only scale on top of its transform, for a pulse or a hover highlight.

**Parameters**

- `model` (`string`): The name the model was added under.
- `transform` ([`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md)): Where it is.
- `extraScale` (`float`): Uniform factor applied in the model's own space.
- `pickable` (`bool`): Whether `pick` can hit it.

### <a id="m-particles-particleemitter-transform3-billboardtexture3d"></a>Particles(ParticleEmitter, Transform3, BillboardTexture3D?)

```csharp
public void Particles(ParticleEmitter emitter, Transform3 at, BillboardTexture3D? texture = null);
```

Shows a particle emitter at an entity transform's position; see [`SceneWriter.Particles()`](SceneWriter.md#m-particles-particleemitter-vector3-billboardtexture3d).

**Parameters**

- `emitter` ([`ParticleEmitter`](../../OneGame.Particles/OneGame.Particles/ParticleEmitter.md)): The emitter.
- `at` ([`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md)): Where it is.
- `texture` ([`BillboardTexture3D`](../../OneGame.Render3D/OneGame.Render3D/BillboardTexture3D.md)?): The image or sheet its settings describe; null shows a soft round spot.

### <a id="m-particles-particleemitter-vector3-billboardtexture3d"></a>Particles(ParticleEmitter, Vector3, BillboardTexture3D?)

```csharp
public void Particles(ParticleEmitter emitter, Vector3 position, BillboardTexture3D? texture = null);
```

Shows a particle emitter in the scene as camera-facing quads. Call it every frame from view code, with the emitter you keep in a field: the first call of a frame moves it to `position` and steps it by the frame's [`SceneWriter.DeltaTime`](SceneWriter.md#m-deltatime). Its settings should use [`ParticleSpace.Volume`](../../OneGame.Particles/OneGame.Particles/ParticleSpace.md#m-volume). Quads are depth tested against the scene and sorted far to near; additive emitters add their light and glow through bloom when [`ParticleSettings.Emission`](../../OneGame.Particles/OneGame.Particles/ParticleSettings.md#m-emission) is above 1. An emitter that is not passed in a frame neither moves nor draws.

**Parameters**

- `emitter` ([`ParticleEmitter`](../../OneGame.Particles/OneGame.Particles/ParticleEmitter.md)): The emitter.
- `position` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Where it is, in world units.
- `texture` ([`BillboardTexture3D`](../../OneGame.Render3D/OneGame.Render3D/BillboardTexture3D.md)?): The image or sheet its settings describe; null shows a soft round spot.

### <a id="m-pointlight-transform3-vector3-float-float"></a>PointLight(Transform3, Vector3, float, float)

```csharp
public void PointLight(Transform3 transform, Vector3 color, float candela, float range = 0f);
```

A light that shines in all directions from the transform's position.

**Parameters**

- `transform` ([`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md)): Where it is.
- `color` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Linear RGB.
- `candela` (`float`): Intensity.
- `range` (`float`): Distance at which it fades out; 0 for none.

### <a id="m-pointlight-vector3-vector3-float-float"></a>PointLight(Vector3, Vector3, float, float)

```csharp
public void PointLight(Vector3 position, Vector3 color, float candela, float range = 0f);
```

A light that shines in all directions from a position.

**Parameters**

- `position` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Where it is.
- `color` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Linear RGB.
- `candela` (`float`): Intensity.
- `range` (`float`): Distance at which it fades out; 0 for none.

### <a id="m-spotlight-transform3-vector3-float-float-float-float"></a>SpotLight(Transform3, Vector3, float, float, float, float)

```csharp
public void SpotLight(Transform3 transform, Vector3 color, float candela, float innerDegrees, float outerDegrees, float range = 0f);
```

A cone of light from the transform's position along its forward direction.

**Parameters**

- `transform` ([`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md)): Where it is and which way it points.
- `color` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Linear RGB.
- `candela` (`float`): Intensity.
- `innerDegrees` (`float`): Half-angle of the full-strength cone.
- `outerDegrees` (`float`): Half-angle at which the light reaches zero.
- `range` (`float`): Distance at which it fades out; 0 for none.

### <a id="m-tomatrix-transform3"></a>ToMatrix(Transform3)

```csharp
public static Matrix4x4 ToMatrix(Transform3 t);
```

The matrix that places a thing with this transform in the world (scale, then rotation, then translation; row vectors).

**Parameters**

- `t` ([`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md)): The transform.

**Returns:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4) — The world matrix.
