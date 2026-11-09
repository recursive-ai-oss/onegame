[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# IScene3DProbe interface

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public interface IScene3DProbe
```

What the `camera`, `pick` and `project` operations ask of a 3D scene: the camera in use, a view-only override of it, a world point's place on screen and the model under a pixel. [`SceneView3D`](SceneView3D.md) answers from the world's entities; [`ManualScene3D`](ManualScene3D.md) answers for a scene a game fills by hand.

## Properties

| Member | Description |
| --- | --- |
| [`Camera`](#m-camera) | The camera in use. |
| [`CameraOverride`](#m-cameraoverride) | A view-only camera that replaces the game's own; null follows the game again. |

### <a id="m-camera"></a>Camera

```csharp
ViewCamera Camera { get; }
```

The camera in use.

**Type:** [`ViewCamera`](ViewCamera.md)

### <a id="m-cameraoverride"></a>CameraOverride

```csharp
ViewCamera? CameraOverride { get; set; }
```

A view-only camera that replaces the game's own; null follows the game again.

**Type:** [`ViewCamera`](ViewCamera.md)?

## Methods

| Member | Description |
| --- | --- |
| [`Cast(PickRay)`](#m-cast-pickray) | The nearest model along a ray, or null. |
| [`Prepare(World, long, Fixed)`](#m-prepare-world-long-fixed) | Brings the answers up to date with the world before a question is asked. |
| [`Ray(float, float, int, int)`](#m-ray-float-float-int-int) | The ray through a pixel of a target of the given size. |
| [`TryProject(Vector3, int, int, out Vector2, out float)`](#m-tryproject-vector3-int-int-vector2-float) | Where a world point lands in a target of the given size. |

### <a id="m-cast-pickray"></a>Cast(PickRay)

```csharp
PickHit? Cast(PickRay ray);
```

The nearest model along a ray, or null.

**Parameters**

- `ray` ([`PickRay`](PickRay.md)): The ray.

**Returns:** [`PickHit`](PickHit.md)?

### <a id="m-prepare-world-long-fixed"></a>Prepare(World, long, Fixed)

```csharp
void Prepare(World world, long tick, Fixed alpha);
```

Brings the answers up to date with the world before a question is asked.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The world being drawn.
- `tick` (`long`): Its tick.
- `alpha` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): How far the frame is between ticks.

### <a id="m-ray-float-float-int-int"></a>Ray(float, float, int, int)

```csharp
PickRay Ray(float x, float y, int width, int height);
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
bool TryProject(Vector3 point, int width, int height, out Vector2 pixel, out float depth);
```

Where a world point lands in a target of the given size.

**Parameters**

- `point` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The world position.
- `width` (`int`): Target width in pixels.
- `height` (`int`): Target height in pixels.
- `pixel` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): The position in pixels.
- `depth` (`float`): Distance in front of the camera.

**Returns:** `bool` — False when the point is behind the camera or beyond its planes.
