[API reference](../../index.md) › [OneGame.Particles](../index.md) › OneGame.Particles

# ParticleSample struct

**Namespace:** `OneGame.Particles`  
**Package:** [`RecursiveAI.OneGame.Particles`](../index.md)

```csharp
public readonly struct ParticleSample
```

One live particle as a renderer needs it, from [`ParticleEmitter.Sample()`](ParticleEmitter.md#m-sample-int).

## Properties

| Member | Description |
| --- | --- |
| [`Color`](#m-color) | sRGB colour with straight alpha from the gradient (not including [`ParticleSettings.Emission`](ParticleSettings.md#m-emission)). |
| [`Frame`](#m-frame) | The sheet frame shown, counting row by row. |
| [`Life`](#m-life) | How far through its life the particle is, 0 to 1. |
| [`Position`](#m-position) | Where the particle is, in world units. |
| [`Rotation`](#m-rotation) | Rotation in radians. |
| [`Size`](#m-size) | Width and height in world units, after the size curve. |
| [`Uv`](#m-uv) | The frame's rectangle in the texture as (u, v, width, height), 0 to 1, origin top-left. |

### <a id="m-color"></a>Color

```csharp
public Vector4 Color { get; init; }
```

sRGB colour with straight alpha from the gradient (not including [`ParticleSettings.Emission`](ParticleSettings.md#m-emission)).

**Type:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)

### <a id="m-frame"></a>Frame

```csharp
public int Frame { get; init; }
```

The sheet frame shown, counting row by row.

**Type:** `int`

### <a id="m-life"></a>Life

```csharp
public float Life { get; init; }
```

How far through its life the particle is, 0 to 1.

**Type:** `float`

### <a id="m-position"></a>Position

```csharp
public Vector3 Position { get; init; }
```

Where the particle is, in world units.

**Type:** [`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="m-rotation"></a>Rotation

```csharp
public float Rotation { get; init; }
```

Rotation in radians.

**Type:** `float`

### <a id="m-size"></a>Size

```csharp
public float Size { get; init; }
```

Width and height in world units, after the size curve.

**Type:** `float`

### <a id="m-uv"></a>Uv

```csharp
public Vector4 Uv { get; init; }
```

The frame's rectangle in the texture as (u, v, width, height), 0 to 1, origin top-left.

**Type:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)
