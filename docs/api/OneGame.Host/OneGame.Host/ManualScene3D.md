[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# ManualScene3D class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IScene3DProbe`](IScene3DProbe.md)

```csharp
public sealed class ManualScene3D : IScene3DProbe
```

Lets the live-control operations `camera`, `pick` and `project` work on a scene a game fills by hand (a [`Render3DStage`](Render3DStage.md) built without a [`SceneView3D`](SceneView3D.md)). The stage creates one over its [`Render3DStage.Scene`](Render3DStage.md#m-scene); a game with its own stage hands the scene to [`ManualScene3D(…)`](ManualScene3D.md#m-ctor-scene3d) and returns it from [`IScene3DStage.Manual`](IScene3DStage.md#m-manual). The camera is the scene's own, as the game last set it; an override set by the `camera` operation replaces it for drawing too, until reset. Picking is triangle-exact at the models' rest pose and reports each hit by the instance's [`ModelInstance3D.Label`](../../OneGame.Render3D/OneGame.Render3D/ModelInstance3D.md#m-label); hits carry no entity.

## Constructors

| Member | Description |
| --- | --- |
| [`ManualScene3D(Scene3D)`](#m-ctor-scene3d) | Creates the probe over a scene. |

### <a id="m-ctor-scene3d"></a>ManualScene3D(Scene3D)

```csharp
public ManualScene3D(Scene3D scene);
```

Creates the probe over a scene.

**Parameters**

- `scene` ([`Scene3D`](../../OneGame.Render3D/OneGame.Render3D/Scene3D.md)): The scene the game draws each frame.

## Properties

| Member | Description |
| --- | --- |
| [`Camera`](#m-camera) | The camera in use. |
| [`CameraOverride`](#m-cameraoverride) | A view-only camera that replaces the game's own; null follows the game again. |
| [`Scene`](#m-scene) | The scene being probed. |

### <a id="m-camera"></a>Camera

```csharp
public ViewCamera Camera { get; }
```

The camera in use.

**Type:** [`ViewCamera`](ViewCamera.md)

### <a id="m-cameraoverride"></a>CameraOverride

```csharp
public ViewCamera? CameraOverride { get; set; }
```

A view-only camera that replaces the game's own; null follows the game again.

**Type:** [`ViewCamera`](ViewCamera.md)?

### <a id="m-scene"></a>Scene

```csharp
public Scene3D Scene { get; }
```

The scene being probed.

**Type:** [`Scene3D`](../../OneGame.Render3D/OneGame.Render3D/Scene3D.md)

## Methods

| Member | Description |
| --- | --- |
| [`ApplyOverride()`](#m-applyoverride) | Puts the override into the scene's camera. |
| [`Cast(PickRay)`](#m-cast-pickray) | The nearest model along a ray, or null. |
| [`Prepare(World, long, Fixed)`](#m-prepare-world-long-fixed) | Brings the answers up to date with the world before a question is asked. |
| [`Ray(float, float, int, int)`](#m-ray-float-float-int-int) | The ray through a pixel of a target of the given size. |
| [`TryProject(Vector3, int, int, out Vector2, out float)`](#m-tryproject-vector3-int-int-vector2-float) | Where a world point lands in a target of the given size. |

### <a id="m-applyoverride"></a>ApplyOverride()

```csharp
public void ApplyOverride();
```

Puts the override into the scene's camera. A stage calls it after the game's own per-frame update, so the override wins.

### <a id="m-cast-pickray"></a>Cast(PickRay)

```csharp
public PickHit? Cast(PickRay ray);
```

The nearest model along a ray, or null.

**Parameters**

- `ray` ([`PickRay`](PickRay.md)): The ray.

**Returns:** [`PickHit`](PickHit.md)?

### <a id="m-prepare-world-long-fixed"></a>Prepare(World, long, Fixed)

```csharp
public void Prepare(World world, long tick, Fixed alpha);
```

Brings the answers up to date with the world before a question is asked.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The world being drawn.
- `tick` (`long`): Its tick.
- `alpha` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): How far the frame is between ticks.

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

Where a world point lands in a target of the given size.

**Parameters**

- `point` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The world position.
- `width` (`int`): Target width in pixels.
- `height` (`int`): Target height in pixels.
- `pixel` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): The position in pixels.
- `depth` (`float`): Distance in front of the camera.

**Returns:** `bool` — False when the point is behind the camera or beyond its planes.
