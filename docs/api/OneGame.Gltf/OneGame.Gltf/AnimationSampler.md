[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# AnimationSampler class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class AnimationSampler
```

Keys and values of one animated property.

## Constructors

| Member | Description |
| --- | --- |
| [`AnimationSampler()`](#m-ctor) |  |

### <a id="m-ctor"></a>AnimationSampler()

```csharp
public AnimationSampler();
```

## Properties

| Member | Description |
| --- | --- |
| [`Components`](#m-components) | Floats in one value: 3 for translation and scale, 4 for rotation, the target count for weights. |
| [`Input`](#m-input) | Key times in seconds, increasing. |
| [`Interpolation`](#m-interpolation) | How values between keys are computed. |
| [`IsRotation`](#m-isrotation) | True when the values are rotations (renormalized after interpolation). |
| [`Output`](#m-output) | Key values, flattened. |

### <a id="m-components"></a>Components

```csharp
public int Components { get; }
```

Floats in one value: 3 for translation and scale, 4 for rotation, the target count for weights.

**Type:** `int`

### <a id="m-input"></a>Input

```csharp
public float[] Input { get; }
```

Key times in seconds, increasing.

**Type:** `float[]`

### <a id="m-interpolation"></a>Interpolation

```csharp
public AnimationInterpolation Interpolation { get; }
```

How values between keys are computed.

**Type:** [`AnimationInterpolation`](AnimationInterpolation.md)

### <a id="m-isrotation"></a>IsRotation

```csharp
public bool IsRotation { get; }
```

True when the values are rotations (renormalized after interpolation).

**Type:** `bool`

### <a id="m-output"></a>Output

```csharp
public float[] Output { get; }
```

Key values, flattened. Linear and step: [`AnimationSampler.Components`](AnimationSampler.md#m-components) floats per key. Cubic spline: in-tangent, value and out-tangent per key.

**Type:** `float[]`

## Methods

| Member | Description |
| --- | --- |
| [`Evaluate(float, Span<float>)`](#m-evaluate-float-span-float) | Evaluates the sampler at a time; times outside the keys use the first or last value. |

### <a id="m-evaluate-float-span-float"></a>Evaluate(float, Span&lt;float&gt;)

```csharp
public void Evaluate(float time, Span<float> result);
```

Evaluates the sampler at a time; times outside the keys use the first or last value.

**Parameters**

- `time` (`float`): Seconds.
- `result` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<float>`): Receives [`AnimationSampler.Components`](AnimationSampler.md#m-components) floats.
