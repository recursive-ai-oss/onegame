[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# CameraResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CameraResult`](CameraResult.md)&gt;

```csharp
public sealed record CameraResult : IEquatable<CameraResult>
```

Result of `camera`: the camera the 3D view is drawn with.

## Constructors

| Member | Description |
| --- | --- |
| [`CameraResult(double[], double[], double[], double, double, double, double, string?, bool)`](#m-ctor-double-double-double-double-double-double-double-string-bool) | Result of `camera`: the camera the 3D view is drawn with. |

### <a id="m-ctor-double-double-double-double-double-double-double-string-bool"></a>CameraResult(double\[\], double\[\], double\[\], double, double, double, double, string?, bool)

```csharp
public CameraResult(double[] Position, double[] Forward, double[] Up, double FovDegrees, double OrthoHeight, double Near, double Far, string? Entity, bool Overridden);
```

Result of `camera`: the camera the 3D view is drawn with.

**Parameters**

- `Position` (`double[]`): World position.
- `Forward` (`double[]`): Unit vector the camera looks along.
- `Up` (`double[]`): Unit vector pointing up in the image.
- `FovDegrees` (`double`): Vertical field of view (perspective).
- `OrthoHeight` (`double`): Visible world height of an orthographic camera; 0 for perspective.
- `Near` (`double`): Near plane distance.
- `Far` (`double`): Far plane distance.
- `Entity` (`string?`): The entity that offered the camera (`Type#id`), or null for the default or an override.
- `Overridden` (`bool`): True while a `camera` call has replaced the entities' camera.

## Properties

| Member | Description |
| --- | --- |
| [`Entity`](#m-entity) | The entity that offered the camera (`Type#id`), or null for the default or an override. |
| [`Far`](#m-far) | Far plane distance. |
| [`Forward`](#m-forward) | Unit vector the camera looks along. |
| [`FovDegrees`](#m-fovdegrees) | Vertical field of view (perspective). |
| [`Near`](#m-near) | Near plane distance. |
| [`OrthoHeight`](#m-orthoheight) | Visible world height of an orthographic camera; 0 for perspective. |
| [`Overridden`](#m-overridden) | True while a `camera` call has replaced the entities' camera. |
| [`Position`](#m-position) | World position. |
| [`Up`](#m-up) | Unit vector pointing up in the image. |

### <a id="m-entity"></a>Entity

```csharp
public string? Entity { get; init; }
```

The entity that offered the camera (`Type#id`), or null for the default or an override.

**Type:** `string?`

### <a id="m-far"></a>Far

```csharp
public double Far { get; init; }
```

Far plane distance.

**Type:** `double`

### <a id="m-forward"></a>Forward

```csharp
public double[] Forward { get; init; }
```

Unit vector the camera looks along.

**Type:** `double[]`

### <a id="m-fovdegrees"></a>FovDegrees

```csharp
public double FovDegrees { get; init; }
```

Vertical field of view (perspective).

**Type:** `double`

### <a id="m-near"></a>Near

```csharp
public double Near { get; init; }
```

Near plane distance.

**Type:** `double`

### <a id="m-orthoheight"></a>OrthoHeight

```csharp
public double OrthoHeight { get; init; }
```

Visible world height of an orthographic camera; 0 for perspective.

**Type:** `double`

### <a id="m-overridden"></a>Overridden

```csharp
public bool Overridden { get; init; }
```

True while a `camera` call has replaced the entities' camera.

**Type:** `bool`

### <a id="m-position"></a>Position

```csharp
public double[] Position { get; init; }
```

World position.

**Type:** `double[]`

### <a id="m-up"></a>Up

```csharp
public double[] Up { get; init; }
```

Unit vector pointing up in the image.

**Type:** `double[]`
