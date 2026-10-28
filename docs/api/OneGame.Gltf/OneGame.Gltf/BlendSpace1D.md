[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# BlendSpace1D class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class BlendSpace1D
```

A line of poses along one value, for example speed: idle at 0, walk at 1, run at 2. Between two neighbours both count in proportion; outside the ends the nearest one counts alone.

## Constructors

| Member | Description |
| --- | --- |
| [`BlendSpace1D(float[])`](#m-ctor-float) | Creates the space from the positions of its samples. |

### <a id="m-ctor-float"></a>BlendSpace1D(float\[\])

```csharp
public BlendSpace1D(params float[] positions);
```

Creates the space from the positions of its samples.

**Parameters**

- `positions` (`float[]`): Where each sample sits, strictly increasing, at least one.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The positions are empty or not increasing.

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | The number of samples. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of samples.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Locate(float, out int, out int, out float)`](#m-locate-float-int-int-float) | Finds the two samples around a value. |
| [`Weights(float, Span<float>)`](#m-weights-float-span-float) | Writes one weight per sample; they add up to 1. |

### <a id="m-locate-float-int-int-float"></a>Locate(float, out int, out int, out float)

```csharp
public void Locate(float value, out int lower, out int upper, out float t);
```

Finds the two samples around a value.

**Parameters**

- `value` (`float`): The input value.
- `lower` (`int`): The sample at or below the value.
- `upper` (`int`): The sample above it (equal to `lower` at the ends).
- `t` (`float`): The weight of `upper`; `lower` has 1 - t.

### <a id="m-weights-float-span-float"></a>Weights(float, Span&lt;float&gt;)

```csharp
public void Weights(float value, Span<float> weights);
```

Writes one weight per sample; they add up to 1.

**Parameters**

- `value` (`float`): The input value.
- `weights` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<float>`): Receives [`BlendSpace1D.Count`](BlendSpace1D.md#m-count) weights.
