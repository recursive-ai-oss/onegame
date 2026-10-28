[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# Camera3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class Camera3D
```

A camera. It looks down its own -Z axis with +Y up (the glTF convention); [`Camera3D.World`](Camera3D.md#m-world) places it. Matrices follow `System.Numerics`: row vectors, right-handed, depth 0..1.

## Constructors

| Member | Description |
| --- | --- |
| [`Camera3D()`](#m-ctor) |  |

### <a id="m-ctor"></a>Camera3D()

```csharp
public Camera3D();
```

## Properties

| Member | Description |
| --- | --- |
| [`Far`](#m-far) | Distance of the far plane. |
| [`Near`](#m-near) | Distance of the near plane. |
| [`OrthoHeight`](#m-orthoheight) | Visible height in world units (orthographic). |
| [`Orthographic`](#m-orthographic) | True for an orthographic projection ([`Camera3D.OrthoHeight`](Camera3D.md#m-orthoheight) sizes it) instead of a perspective one ([`Camera3D.YFov`](Camera3D.md#m-yfov)). |
| [`Position`](#m-position) | Position in the world. |
| [`View`](#m-view) | World-to-camera transform. |
| [`World`](#m-world) | Camera-to-world transform (rigid: rotation and translation only). |
| [`YFov`](#m-yfov) | Vertical field of view in radians (perspective). |

### <a id="m-far"></a>Far

```csharp
public float Far { get; set; }
```

Distance of the far plane. Lighting clusters need a finite far plane; an infinite value is treated as 1000 there.

**Type:** `float`

### <a id="m-near"></a>Near

```csharp
public float Near { get; set; }
```

Distance of the near plane.

**Type:** `float`

### <a id="m-orthoheight"></a>OrthoHeight

```csharp
public float OrthoHeight { get; set; }
```

Visible height in world units (orthographic).

**Type:** `float`

### <a id="m-orthographic"></a>Orthographic

```csharp
public bool Orthographic { get; set; }
```

True for an orthographic projection ([`Camera3D.OrthoHeight`](Camera3D.md#m-orthoheight) sizes it) instead of a perspective one ([`Camera3D.YFov`](Camera3D.md#m-yfov)).

**Type:** `bool`

### <a id="m-position"></a>Position

```csharp
public Vector3 Position { get; }
```

Position in the world.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-view"></a>View

```csharp
public Matrix4x4 View { get; }
```

World-to-camera transform.

**Type:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)

### <a id="m-world"></a>World

```csharp
public Matrix4x4 World { get; set; }
```

Camera-to-world transform (rigid: rotation and translation only).

**Type:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)

### <a id="m-yfov"></a>YFov

```csharp
public float YFov { get; set; }
```

Vertical field of view in radians (perspective).

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`Frame(Vector3, Vector3, float, float, float, float, float)`](#m-frame-vector3-vector3-float-float-float-float-float) | An orbit camera placed so that a box fits the view whatever the angle: the distance is the one at which the box's bounding sphere fits the shorter side of the image. |
| [`FromGltf(GltfCamera, Matrix4x4)`](#m-fromgltf-gltfcamera-matrix4x4) | The camera a glTF file describes, placed by the world matrix of the node that carries it. |
| [`LookAt(Vector3, Vector3, Vector3)`](#m-lookat-vector3-vector3-vector3) | A camera at `eye` looking at `target`. |
| [`Orbit(Vector3, float, float, float)`](#m-orbit-vector3-float-float-float) | A camera on a sphere around `target`. |
| [`Projection(float)`](#m-projection-float) | Projection for a render target of the given width over height. |

### <a id="m-frame-vector3-vector3-float-float-float-float-float"></a>Frame(Vector3, Vector3, float, float, float, float, float)

```csharp
public static Camera3D Frame(Vector3 min, Vector3 max, float yawDegrees, float pitchDegrees, float aspect, float yfov = 0.7853982f, float margin = 1.1f);
```

An orbit camera placed so that a box fits the view whatever the angle: the distance is the one at which the box's bounding sphere fits the shorter side of the image. Near and far planes come from the box.

**Parameters**

- `min` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Box minimum.
- `max` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Box maximum.
- `yawDegrees` (`float`): Yaw, as for [`Camera3D.Orbit()`](Camera3D.md#m-orbit-vector3-float-float-float).
- `pitchDegrees` (`float`): Pitch, as for [`Camera3D.Orbit()`](Camera3D.md#m-orbit-vector3-float-float-float).
- `aspect` (`float`): Width divided by height of the target.
- `yfov` (`float`): Vertical field of view in radians.
- `margin` (`float`): Extra distance as a factor (1 fits the sphere exactly).

**Returns:** [`Camera3D`](Camera3D.md)

### <a id="m-fromgltf-gltfcamera-matrix4x4"></a>FromGltf(GltfCamera, Matrix4x4)

```csharp
public static Camera3D FromGltf(GltfCamera camera, Matrix4x4 nodeWorld);
```

The camera a glTF file describes, placed by the world matrix of the node that carries it.

**Parameters**

- `camera` ([`GltfCamera`](../../OneGame.Gltf/OneGame.Gltf/GltfCamera.md))
- `nodeWorld` ([`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4))

**Returns:** [`Camera3D`](Camera3D.md)

### <a id="m-lookat-vector3-vector3-vector3"></a>LookAt(Vector3, Vector3, Vector3)

```csharp
public static Camera3D LookAt(Vector3 eye, Vector3 target, Vector3 up);
```

A camera at `eye` looking at `target`.

**Parameters**

- `eye` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): World position of the camera.
- `target` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The point it looks at.
- `up` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Which way is up, usually [`Vector3.UnitY`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3).

**Returns:** [`Camera3D`](Camera3D.md)

### <a id="m-orbit-vector3-float-float-float"></a>Orbit(Vector3, float, float, float)

```csharp
public static Camera3D Orbit(Vector3 target, float distance, float yawDegrees, float pitchDegrees);
```

A camera on a sphere around `target`. Yaw 0 and pitch 0 look from +Z towards -Z (the front of a glTF model); positive yaw moves the camera towards +X, positive pitch raises it.

**Parameters**

- `target` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The point the camera looks at.
- `distance` (`float`): Distance from the target in world units (metres for glTF models).
- `yawDegrees` (`float`): Angle around the vertical axis, in degrees.
- `pitchDegrees` (`float`): Angle above the horizon, in degrees.

**Returns:** [`Camera3D`](Camera3D.md)

### <a id="m-projection-float"></a>Projection(float)

```csharp
public Matrix4x4 Projection(float aspect);
```

Projection for a render target of the given width over height.

**Parameters**

- `aspect` (`float`): Width divided by height.

**Returns:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4) — The projection matrix.
