[API reference](../../index.md) › [OneGame.Particles](../index.md) › OneGame.Particles

# ParticleCurve class

**Namespace:** `OneGame.Particles`  
**Package:** [`RecursiveAI.OneGame.Particles`](../index.md)

```csharp
public sealed class ParticleCurve
```

A number that changes over a particle's life, as straight lines between stops. `t` runs from 0 (born) to 1 (gone). Before the first stop the first value holds, after the last stop the last. Immutable: make one when the emitter is set up and share it.

## Methods

| Member | Description |
| --- | --- |
| [`Constant(float)`](#m-constant-float) | The same value for the whole life. |
| [`Evaluate(float)`](#m-evaluate-float) | The value at a point of the life. |
| [`Linear(float, float)`](#m-linear-float-float) | A straight line from `from` at birth to `to` at the end. |
| [`Of((float T, float Value)[])`](#m-of-float-t-float-value) | A curve through the given stops, which must be in increasing order of `T`. |

### <a id="m-constant-float"></a>Constant(float)

```csharp
public static ParticleCurve Constant(float value);
```

The same value for the whole life.

**Parameters**

- `value` (`float`): The value.

**Returns:** [`ParticleCurve`](ParticleCurve.md) — The curve.

### <a id="m-evaluate-float"></a>Evaluate(float)

```csharp
public float Evaluate(float t);
```

The value at a point of the life.

**Parameters**

- `t` (`float`): Fraction of the life, 0 to 1.

**Returns:** `float` — The value.

### <a id="m-linear-float-float"></a>Linear(float, float)

```csharp
public static ParticleCurve Linear(float from, float to);
```

A straight line from `from` at birth to `to` at the end.

**Parameters**

- `from` (`float`): Value at t = 0.
- `to` (`float`): Value at t = 1.

**Returns:** [`ParticleCurve`](ParticleCurve.md) — The curve.

### <a id="m-of-float-t-float-value"></a>Of((float T, float Value)\[\])

```csharp
public static ParticleCurve Of(params (float T, float Value)[] stops);
```

A curve through the given stops, which must be in increasing order of `T`.

**Parameters**

- `stops` (`(float T, float Value)[]`): Pairs of life fraction and value; at least one.

**Returns:** [`ParticleCurve`](ParticleCurve.md) — The curve.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): There are no stops, or they are not in increasing order.
