[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# Environment3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class Environment3D
```

Image-based lighting and backdrop of a scene.

## Constructors

| Member | Description |
| --- | --- |
| [`Environment3D()`](#m-ctor) |  |

### <a id="m-ctor"></a>Environment3D()

```csharp
public Environment3D();
```

## Properties

| Member | Description |
| --- | --- |
| [`Background`](#m-background) | What fills the pixels no model covers. |
| [`BackgroundBlur`](#m-backgroundblur) | How blurred the environment backdrop is, 0 (sharp) to 1 (fully blurred). |
| [`BackgroundColor`](#m-backgroundcolor) | Linear colour for [`BackgroundMode.Color`](BackgroundMode.md#m-color). |
| [`Blend`](#m-blend) | How far the scene has moved from [`Environment3D.Map`](Environment3D.md#m-map) to [`Environment3D.NextMap`](Environment3D.md#m-nextmap), 0 to 1 (clamped). |
| [`Intensity`](#m-intensity) | Scale on the light the environment contributes; 0 turns image-based lighting off. |
| [`Map`](#m-map) | The environment to light the scene with; null uses a soft built-in sky. |
| [`NextMap`](#m-nextmap) | The environment to fade towards, or null for none. |
| [`Yaw`](#m-yaw) | Turns the environment about the +Y axis, in radians. |

### <a id="m-background"></a>Background

```csharp
public BackgroundMode Background { get; set; }
```

What fills the pixels no model covers.

**Type:** [`BackgroundMode`](BackgroundMode.md)

### <a id="m-backgroundblur"></a>BackgroundBlur

```csharp
public float BackgroundBlur { get; set; }
```

How blurred the environment backdrop is, 0 (sharp) to 1 (fully blurred).

**Type:** `float`

### <a id="m-backgroundcolor"></a>BackgroundColor

```csharp
public Vector3 BackgroundColor { get; set; }
```

Linear colour for [`BackgroundMode.Color`](BackgroundMode.md#m-color).

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-blend"></a>Blend

```csharp
public float Blend { get; set; }
```

How far the scene has moved from [`Environment3D.Map`](Environment3D.md#m-map) to [`Environment3D.NextMap`](Environment3D.md#m-nextmap), 0 to 1 (clamped). Ignored while [`Environment3D.NextMap`](Environment3D.md#m-nextmap) is null.

**Type:** `float`

### <a id="m-intensity"></a>Intensity

```csharp
public float Intensity { get; set; }
```

Scale on the light the environment contributes; 0 turns image-based lighting off.

**Type:** `float`

### <a id="m-map"></a>Map

```csharp
public EnvironmentMap? Map { get; set; }
```

The environment to light the scene with; null uses a soft built-in sky.

**Type:** [`EnvironmentMap`](EnvironmentMap.md)?

### <a id="m-nextmap"></a>NextMap

```csharp
public EnvironmentMap? NextMap { get; set; }
```

The environment to fade towards, or null for none. The picture is [`Environment3D.Map`](Environment3D.md#m-map) at [`Environment3D.Blend`](Environment3D.md#m-blend) 0 and this map at 1, in between a linear mix of both, in the backdrop and in the diffuse and specular image-based light. Changing [`Environment3D.Blend`](Environment3D.md#m-blend) costs one more cube sample per pixel and refilters nothing. To step through a series of keyframes, when the blend reaches 1 set [`Environment3D.Map`](Environment3D.md#m-map) to this map, this to the following one and [`Environment3D.Blend`](Environment3D.md#m-blend) to 0: the picture does not change. The yaw, intensity and background blur are shared by both maps.

**Type:** [`EnvironmentMap`](EnvironmentMap.md)?

### <a id="m-yaw"></a>Yaw

```csharp
public float Yaw { get; set; }
```

Turns the environment about the +Y axis, in radians.

**Type:** `float`
