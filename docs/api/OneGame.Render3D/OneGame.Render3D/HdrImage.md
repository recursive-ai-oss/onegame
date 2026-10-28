[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# HdrImage class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class HdrImage
```

A floating-point panorama in linear RGB, rows from the top. Used as the source of an environment: the panorama maps direction `d` (+Y up) to `u = atan2(d.x, -d.z) / 2pi + 1 (wrapping)` and `v = acos(d.y) / pi`, so the middle of the image looks down +Z (towards the front of a glTF model seen from the default camera).

## Constructors

| Member | Description |
| --- | --- |
| [`HdrImage(int, int, float[])`](#m-ctor-int-int-float) | Creates an image from existing data. |

### <a id="m-ctor-int-int-float"></a>HdrImage(int, int, float\[\])

```csharp
public HdrImage(int width, int height, float[] rgb);
```

Creates an image from existing data.

**Parameters**

- `width` (`int`): Width in pixels.
- `height` (`int`): Height in pixels.
- `rgb` (`float[]`): Width x height x 3 floats, linear.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height in pixels. |
| [`Rgb`](#m-rgb) | Linear red, green, blue per pixel, rows from the top. |
| [`Width`](#m-width) | Width in pixels. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; }
```

Height in pixels.

**Type:** `int`

### <a id="m-rgb"></a>Rgb

```csharp
public float[] Rgb { get; }
```

Linear red, green, blue per pixel, rows from the top.

**Type:** `float[]`

### <a id="m-width"></a>Width

```csharp
public int Width { get; }
```

Width in pixels.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`CreateSky(int, int, Vector3, Vector3, Vector3, Vector3?, Vector3, float)`](#m-createsky-int-int-vector3-vector3-vector3-vector3-vector3-float) | A procedural sky: a gradient from `ground` below the horizon through `horizon` to `zenith`, with an optional sun disc. |
| [`Decode(ReadOnlySpan<byte>, HdrLimits?)`](#m-decode-readonlyspan-byte-hdrlimits) | Decodes a Radiance RGBE file (flat or run-length encoded). |
| [`DirectionToUv(Vector3)`](#m-directiontouv-vector3) | The panorama coordinates of a direction (see the type remarks). |

### <a id="m-createsky-int-int-vector3-vector3-vector3-vector3-vector3-float"></a>CreateSky(int, int, Vector3, Vector3, Vector3, Vector3?, Vector3, float)

```csharp
public static HdrImage CreateSky(int width, int height, Vector3 zenith, Vector3 horizon, Vector3 ground, Vector3? sunDirection = default, Vector3 sunColor = default, float sunAngle = 0.05f);
```

A procedural sky: a gradient from `ground` below the horizon through `horizon` to `zenith`, with an optional sun disc. Deterministic, so tests and tools can make environments without a file.

**Parameters**

- `width` (`int`): Width in pixels.
- `height` (`int`): Height in pixels.
- `zenith` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Radiance straight up.
- `horizon` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Radiance at the horizon.
- `ground` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Radiance straight down.
- `sunDirection` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)?): Direction towards the sun, or null for none.
- `sunColor` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Radiance added inside the sun disc.
- `sunAngle` (`float`): Angular radius of the sun disc in radians.

**Returns:** [`HdrImage`](HdrImage.md) — The panorama.

### <a id="m-decode-readonlyspan-byte-hdrlimits"></a>Decode(ReadOnlySpan&lt;byte&gt;, HdrLimits?)

```csharp
public static HdrImage Decode(ReadOnlySpan<byte> data, HdrLimits? limits = null);
```

Decodes a Radiance RGBE file (flat or run-length encoded). Everything is bounds-checked and sized from the header before allocating.

**Parameters**

- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The file bytes.
- `limits` ([`HdrLimits`](HdrLimits.md)?): Size limits, or null for the defaults.

**Returns:** [`HdrImage`](HdrImage.md) — The image.

**Exceptions**

- [`HdrException`](HdrException.md): The data is not a supported Radiance file or exceeds a limit.

### <a id="m-directiontouv-vector3"></a>DirectionToUv(Vector3)

```csharp
public static Vector2 DirectionToUv(Vector3 d);
```

The panorama coordinates of a direction (see the type remarks).

**Parameters**

- `d` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3))

**Returns:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)
