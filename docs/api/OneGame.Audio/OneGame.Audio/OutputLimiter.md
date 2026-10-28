[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# OutputLimiter static class

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public static class OutputLimiter
```

Keeps float samples inside the range a device accepts.

## Methods

| Member | Description |
| --- | --- |
| [`Apply(float, OutputLimiterMode)`](#m-apply-float-outputlimitermode) | Limits one sample. |
| [`Apply(Span<float>, OutputLimiterMode)`](#m-apply-span-float-outputlimitermode) | Limits a buffer in place. |

### <a id="m-apply-float-outputlimitermode"></a>Apply(float, OutputLimiterMode)

```csharp
public static float Apply(float x, OutputLimiterMode mode);
```

Limits one sample. Not-a-number becomes 0.

**Parameters**

- `x` (`float`): The sample.
- `mode` ([`OutputLimiterMode`](OutputLimiterMode.md)): The limiter.

**Returns:** `float`

### <a id="m-apply-span-float-outputlimitermode"></a>Apply(Span&lt;float&gt;, OutputLimiterMode)

```csharp
public static void Apply(Span<float> samples, OutputLimiterMode mode);
```

Limits a buffer in place.

**Parameters**

- `samples` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<float>`): The samples.
- `mode` ([`OutputLimiterMode`](OutputLimiterMode.md)): The limiter.
