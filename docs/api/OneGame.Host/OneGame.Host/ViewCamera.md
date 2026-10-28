[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# ViewCamera struct

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ViewCamera`](ViewCamera.md)&gt;

```csharp
public readonly struct ViewCamera : IEquatable<ViewCamera>
```

A camera as resolved for a frame: where it is and how it projects.

## Constructors

| Member | Description |
| --- | --- |
| [`ViewCamera(Matrix4x4, float, float, float, float, EntityId)`](#m-ctor-matrix4x4-float-float-float-float-entityid) | A camera as resolved for a frame: where it is and how it projects. |

### <a id="m-ctor-matrix4x4-float-float-float-float-entityid"></a>ViewCamera(Matrix4x4, float, float, float, float, EntityId)

```csharp
public ViewCamera(Matrix4x4 World, float YFov, float OrthoHeight, float Near, float Far, EntityId Source);
```

A camera as resolved for a frame: where it is and how it projects.

**Parameters**

- `World` ([`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)): Camera-to-world transform (looks down its -Z).
- `YFov` (`float`): Vertical field of view in radians (perspective).
- `OrthoHeight` (`float`): Visible height in world units; 0 for a perspective camera.
- `Near` (`float`): Near plane distance.
- `Far` (`float`): Far plane distance.
- `Source` ([`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)): The entity that described the camera; none for the default or an override.

## Properties

| Member | Description |
| --- | --- |
| [`Far`](#m-far) | Far plane distance. |
| [`IsOrthographic`](#m-isorthographic) | True for an orthographic projection. |
| [`Near`](#m-near) | Near plane distance. |
| [`OrthoHeight`](#m-orthoheight) | Visible height in world units; 0 for a perspective camera. |
| [`Position`](#m-position) | Position in the world. |
| [`Source`](#m-source) | The entity that described the camera; none for the default or an override. |
| [`View`](#m-view) | The world-to-camera transform. |
| [`World`](#m-world) | Camera-to-world transform (looks down its -Z). |
| [`YFov`](#m-yfov) | Vertical field of view in radians (perspective). |

### <a id="m-far"></a>Far

```csharp
public float Far { get; init; }
```

Far plane distance.

**Type:** `float`

### <a id="m-isorthographic"></a>IsOrthographic

```csharp
public bool IsOrthographic { get; }
```

True for an orthographic projection.

**Type:** `bool`

### <a id="m-near"></a>Near

```csharp
public float Near { get; init; }
```

Near plane distance.

**Type:** `float`

### <a id="m-orthoheight"></a>OrthoHeight

```csharp
public float OrthoHeight { get; init; }
```

Visible height in world units; 0 for a perspective camera.

**Type:** `float`

### <a id="m-position"></a>Position

```csharp
public Vector3 Position { get; }
```

Position in the world.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-source"></a>Source

```csharp
public EntityId Source { get; init; }
```

The entity that described the camera; none for the default or an override.

**Type:** [`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)

### <a id="m-view"></a>View

```csharp
public Matrix4x4 View { get; }
```

The world-to-camera transform.

**Type:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)

### <a id="m-world"></a>World

```csharp
public Matrix4x4 World { get; init; }
```

Camera-to-world transform (looks down its -Z).

**Type:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)

### <a id="m-yfov"></a>YFov

```csharp
public float YFov { get; init; }
```

Vertical field of view in radians (perspective).

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`Projection(float)`](#m-projection-float) | The projection for a target of the given width over height. |

### <a id="m-projection-float"></a>Projection(float)

```csharp
public Matrix4x4 Projection(float aspect);
```

The projection for a target of the given width over height.

**Parameters**

- `aspect` (`float`)

**Returns:** [`Matrix4x4`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)
