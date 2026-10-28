[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# SceneView3D class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class SceneView3D : IDisposable
```

Turns a world of [`IView3D`](IView3D.md) entities into a 3D scene and answers questions about it: which camera is in use, where a world point lands on screen, which entity is under a screen point. It only reads the world; it never changes state. Everything but [`SceneView3D.Build()`](SceneView3D.md#m-build-scene3d-renderer3d) works without a graphics device.

## Constructors

| Member | Description |
| --- | --- |
| [`SceneView3D(ModelLibrary?)`](#m-ctor-modellibrary) | Creates a view over a model library. |

### <a id="m-ctor-modellibrary"></a>SceneView3D(ModelLibrary?)

```csharp
public SceneView3D(ModelLibrary? models = null);
```

Creates a view over a model library.

**Parameters**

- `models` ([`ModelLibrary`](ModelLibrary.md)?): The models entities can show; a new empty library when null.

## Properties

| Member | Description |
| --- | --- |
| [`Camera`](#m-camera) | The camera in use as of the last [`SceneView3D.Prepare()`](SceneView3D.md#m-prepare-world-long-fixed): the override, else the entity camera with the highest priority, else [`SceneView3D.DefaultCamera`](SceneView3D.md#m-defaultcamera). |
| [`CameraOverride`](#m-cameraoverride) | A camera that replaces the one the entities describe, for tools: the `camera` operation sets it. |
| [`DefaultCamera`](#m-defaultcamera) | The camera used when no entity offers one: back and up from the origin, looking at it. |
| [`Describer`](#m-describer) | Describes the scene from the world as a whole, after the [`IView3D`](IView3D.md) entities have been asked. |
| [`Models`](#m-models) | The models entities can show. |
| [`ParticleEmitters`](#m-particleemitters) | The particle emitters entities showed in the last [`SceneView3D.Prepare()`](SceneView3D.md#m-prepare-world-long-fixed), in call order. |

### <a id="m-camera"></a>Camera

```csharp
public ViewCamera Camera { get; }
```

The camera in use as of the last [`SceneView3D.Prepare()`](SceneView3D.md#m-prepare-world-long-fixed): the override, else the entity camera with the highest priority, else [`SceneView3D.DefaultCamera`](SceneView3D.md#m-defaultcamera).

**Type:** [`ViewCamera`](ViewCamera.md)

### <a id="m-cameraoverride"></a>CameraOverride

```csharp
public ViewCamera? CameraOverride { get; set; }
```

A camera that replaces the one the entities describe, for tools: the `camera` operation sets it. It is a view setting, not simulation state, and recordings do not hold it. Null follows the entities again.

**Type:** [`ViewCamera`](ViewCamera.md)?

### <a id="m-defaultcamera"></a>DefaultCamera

```csharp
public static ViewCamera DefaultCamera { get; }
```

The camera used when no entity offers one: back and up from the origin, looking at it.

**Type:** [`ViewCamera`](ViewCamera.md)

### <a id="m-describer"></a>Describer

```csharp
public Action<World, SceneWriter>? Describer { get; set; }
```

Describes the scene from the world as a whole, after the [`IView3D`](IView3D.md) entities have been asked. A game whose entities live in a library that cannot reference the host (a shared rules assembly) draws them here, from view code. It is called with the world being drawn; it may read any state and writes only into the [`SceneWriter`](SceneWriter.md).

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)&lt;[`World`](../../OneGame.Core/OneGame.Core/World.md), [`SceneWriter`](SceneWriter.md)&gt;?

### <a id="m-models"></a>Models

```csharp
public ModelLibrary Models { get; }
```

The models entities can show.

**Type:** [`ModelLibrary`](ModelLibrary.md)

### <a id="m-particleemitters"></a>ParticleEmitters

```csharp
public IReadOnlyList<ParticleEmitter> ParticleEmitters { get; }
```

The particle emitters entities showed in the last [`SceneView3D.Prepare()`](SceneView3D.md#m-prepare-world-long-fixed), in call order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ParticleEmitter`](../../OneGame.Particles/OneGame.Particles/ParticleEmitter.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`Build(Scene3D, Renderer3D)`](#m-build-scene3d-renderer3d) | Fills a renderer scene from the last [`SceneView3D.Prepare()`](SceneView3D.md#m-prepare-world-long-fixed): camera, model instances and lights. |
| [`Cast(PickRay)`](#m-cast-pickray) | The nearest entity model along a ray, from the last [`SceneView3D.Prepare()`](SceneView3D.md#m-prepare-world-long-fixed). |
| [`Dispose()`](#m-dispose) | Releases the GPU copies of the models. |
| [`Pick(float, float, int, int)`](#m-pick-float-float-int-int) | The nearest entity model under a pixel, from the last [`SceneView3D.Prepare()`](SceneView3D.md#m-prepare-world-long-fixed). |
| [`Prepare(World, long, Fixed)`](#m-prepare-world-long-fixed) | Asks every [`IView3D`](IView3D.md) entity of `world` what it looks like at a moment. |
| [`Ray(float, float, int, int)`](#m-ray-float-float-int-int) | The ray through a pixel of a target of the given size. |
| [`TryProject(Vector3, int, int, out Vector2, out float)`](#m-tryproject-vector3-int-int-vector2-float) | Where a world point lands in a target of `width` x `height` pixels (origin top left). |

### <a id="m-build-scene3d-renderer3d"></a>Build(Scene3D, Renderer3D)

```csharp
public void Build(Scene3D scene, Renderer3D renderer);
```

Fills a renderer scene from the last [`SceneView3D.Prepare()`](SceneView3D.md#m-prepare-world-long-fixed): camera, model instances and lights. Models are uploaded the first time they are drawn.

**Parameters**

- `scene` ([`Scene3D`](../../OneGame.Render3D/OneGame.Render3D/Scene3D.md)): The scene to fill; its camera, models and lights are replaced. Environment and post settings are left alone.
- `renderer` ([`Renderer3D`](../../OneGame.Render3D/OneGame.Render3D/Renderer3D.md)): The renderer that owns the GPU copies.

### <a id="m-cast-pickray"></a>Cast(PickRay)

```csharp
public PickHit? Cast(PickRay ray);
```

The nearest entity model along a ray, from the last [`SceneView3D.Prepare()`](SceneView3D.md#m-prepare-world-long-fixed). Models are tested triangle by triangle at their rest pose, both sides.

**Parameters**

- `ray` ([`PickRay`](PickRay.md)): The ray.

**Returns:** [`PickHit`](PickHit.md)? — The hit, or null.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the GPU copies of the models. The renderer must still be alive.

### <a id="m-pick-float-float-int-int"></a>Pick(float, float, int, int)

```csharp
public PickHit? Pick(float x, float y, int width, int height);
```

The nearest entity model under a pixel, from the last [`SceneView3D.Prepare()`](SceneView3D.md#m-prepare-world-long-fixed).

**Parameters**

- `x` (`float`): Pixels from the left.
- `y` (`float`): Pixels from the top.
- `width` (`int`): Target width in pixels.
- `height` (`int`): Target height in pixels.

**Returns:** [`PickHit`](PickHit.md)? — The hit, or null when the pixel shows no pickable model.

### <a id="m-prepare-world-long-fixed"></a>Prepare(World, long, Fixed)

```csharp
public void Prepare(World world, long tick, Fixed alpha);
```

Asks every [`IView3D`](IView3D.md) entity of `world` what it looks like at a moment. Later calls to the camera, projection and pick methods use the answer.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The world to show: the authority's, or a replica's.
- `tick` (`long`): The tick to show.
- `alpha` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): How far past the tick, from 0 to just under 1.

### <a id="m-ray-float-float-int-int"></a>Ray(float, float, int, int)

```csharp
public PickRay Ray(float x, float y, int width, int height);
```

The ray through a pixel of a target of the given size.

**Parameters**

- `x` (`float`): Pixels from the left.
- `y` (`float`): Pixels from the top.
- `width` (`int`): Target width in pixels.
- `height` (`int`): Target height in pixels.

**Returns:** [`PickRay`](PickRay.md)

### <a id="m-tryproject-vector3-int-int-vector2-float"></a>TryProject(Vector3, int, int, out Vector2, out float)

```csharp
public bool TryProject(Vector3 point, int width, int height, out Vector2 pixel, out float depth);
```

Where a world point lands in a target of `width` x `height` pixels (origin top left).

**Parameters**

- `point` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The world position.
- `width` (`int`): Target width in pixels.
- `height` (`int`): Target height in pixels.
- `pixel` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): The position in pixels; it may lie outside the target.
- `depth` (`float`): Distance in front of the camera along its view axis.

**Returns:** `bool` — False when the point is behind the camera or beyond its near and far planes, in which case it is not on screen at all.
