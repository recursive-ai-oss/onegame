[API reference](../../index.md) › [OneGame.Particles](../index.md) › OneGame.Particles

# ParticleGradient class

**Namespace:** `OneGame.Particles`  
**Package:** [`RecursiveAI.OneGame.Particles`](../index.md)

```csharp
public sealed class ParticleGradient
```

A colour that changes over a particle's life, as straight lines between stops. Colours are sRGB with straight alpha, components 0 to 1. Values above 1 are allowed in 3D, where they glow (the scene is high dynamic range); 2D clamps. Immutable.

## Methods

| Member | Description |
| --- | --- |
| [`Between(Vector4, Vector4)`](#m-between-vector4-vector4) | A straight line between two colours. |
| [`Constant(Vector4)`](#m-constant-vector4) | The same colour for the whole life. |
| [`Evaluate(float)`](#m-evaluate-float) | The colour at a point of the life. |
| [`FadeOut(Vector4)`](#m-fadeout-vector4) | The colour fading out: opaque at birth, transparent at the end. |
| [`Of((float T, Vector4 Color)[])`](#m-of-float-t-vector4-color) | A gradient through the given stops, which must be in increasing order of `T`. |
| [`Rgb(uint, float)`](#m-rgb-uint-float) | Makes a colour from a 0xRRGGBB value. |

### <a id="m-between-vector4-vector4"></a>Between(Vector4, Vector4)

```csharp
public static ParticleGradient Between(Vector4 from, Vector4 to);
```

A straight line between two colours.

**Parameters**

- `from` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)): Colour at birth.
- `to` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)): Colour at the end.

**Returns:** [`ParticleGradient`](ParticleGradient.md) — The gradient.

### <a id="m-constant-vector4"></a>Constant(Vector4)

```csharp
public static ParticleGradient Constant(Vector4 color);
```

The same colour for the whole life.

**Parameters**

- `color` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)): sRGB with straight alpha.

**Returns:** [`ParticleGradient`](ParticleGradient.md) — The gradient.

### <a id="m-evaluate-float"></a>Evaluate(float)

```csharp
public Vector4 Evaluate(float t);
```

The colour at a point of the life.

**Parameters**

- `t` (`float`): Fraction of the life, 0 to 1.

**Returns:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4) — sRGB with straight alpha.

### <a id="m-fadeout-vector4"></a>FadeOut(Vector4)

```csharp
public static ParticleGradient FadeOut(Vector4 color);
```

The colour fading out: opaque at birth, transparent at the end.

**Parameters**

- `color` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)): The colour (its alpha is the opacity at birth).

**Returns:** [`ParticleGradient`](ParticleGradient.md) — The gradient.

### <a id="m-of-float-t-vector4-color"></a>Of((float T, Vector4 Color)\[\])

```csharp
public static ParticleGradient Of(params (float T, Vector4 Color)[] stops);
```

A gradient through the given stops, which must be in increasing order of `T`.

**Parameters**

- `stops` (`(float T,` [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4) `Color)[]`): Pairs of life fraction and colour; at least one.

**Returns:** [`ParticleGradient`](ParticleGradient.md) — The gradient.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): There are no stops, or they are not in increasing order.

### <a id="m-rgb-uint-float"></a>Rgb(uint, float)

```csharp
public static Vector4 Rgb(uint rgb, float alpha = 1f);
```

Makes a colour from a 0xRRGGBB value.

**Parameters**

- `rgb` (`uint`): Red in the third byte, green in the second, blue in the first.
- `alpha` (`float`): Opacity, 0 to 1.

**Returns:** [`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4) — The colour.
