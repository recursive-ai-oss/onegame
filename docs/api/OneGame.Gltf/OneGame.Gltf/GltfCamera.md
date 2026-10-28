[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# GltfCamera class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class GltfCamera
```

A perspective or orthographic camera.

## Constructors

| Member | Description |
| --- | --- |
| [`GltfCamera()`](#m-ctor) |  |

### <a id="m-ctor"></a>GltfCamera()

```csharp
public GltfCamera();
```

## Properties

| Member | Description |
| --- | --- |
| [`AspectRatio`](#m-aspectratio) | Width over height, or null to use the viewport's (perspective). |
| [`Name`](#m-name) | The camera's name, or empty. |
| [`Perspective`](#m-perspective) | True for a perspective camera. |
| [`XMag`](#m-xmag) | Half the view width (orthographic). |
| [`YFov`](#m-yfov) | Vertical field of view in radians (perspective). |
| [`YMag`](#m-ymag) | Half the view height (orthographic). |
| [`ZFar`](#m-zfar) | Far plane distance, or null for an infinite projection (perspective only). |
| [`ZNear`](#m-znear) | Near plane distance. |

### <a id="m-aspectratio"></a>AspectRatio

```csharp
public float? AspectRatio { get; }
```

Width over height, or null to use the viewport's (perspective).

**Type:** `float?`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The camera's name, or empty.

**Type:** `string`

### <a id="m-perspective"></a>Perspective

```csharp
public bool Perspective { get; }
```

True for a perspective camera.

**Type:** `bool`

### <a id="m-xmag"></a>XMag

```csharp
public float XMag { get; }
```

Half the view width (orthographic).

**Type:** `float`

### <a id="m-yfov"></a>YFov

```csharp
public float YFov { get; }
```

Vertical field of view in radians (perspective).

**Type:** `float`

### <a id="m-ymag"></a>YMag

```csharp
public float YMag { get; }
```

Half the view height (orthographic).

**Type:** `float`

### <a id="m-zfar"></a>ZFar

```csharp
public float? ZFar { get; }
```

Far plane distance, or null for an infinite projection (perspective only).

**Type:** `float?`

### <a id="m-znear"></a>ZNear

```csharp
public float ZNear { get; }
```

Near plane distance.

**Type:** `float`
