[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# LoudnessStatus record

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`LoudnessStatus`](LoudnessStatus.md)&gt;

```csharp
public sealed record LoudnessStatus : IEquatable<LoudnessStatus>
```

Loudness at the output. A value is null while it is silent or unmeasured.

## Constructors

| Member | Description |
| --- | --- |
| [`LoudnessStatus(double?, double?, double?, double?, double)`](#m-ctor-double-double-double-double-double) | Loudness at the output. |

### <a id="m-ctor-double-double-double-double-double"></a>LoudnessStatus(double?, double?, double?, double?, double)

```csharp
public LoudnessStatus(double? MomentaryLufs, double? ShortTermLufs, double? IntegratedLufs, double? TruePeakDbtp, double MeasuredSeconds);
```

Loudness at the output. A value is null while it is silent or unmeasured.

**Parameters**

- `MomentaryLufs` (`double?`): Over the last 400 ms.
- `ShortTermLufs` (`double?`): Over the last 3 s.
- `IntegratedLufs` (`double?`): Gated, since the last reset.
- `TruePeakDbtp` (`double?`): Highest true peak since the last reset.
- `MeasuredSeconds` (`double`): Audio measured since the last reset.

## Properties

| Member | Description |
| --- | --- |
| [`IntegratedLufs`](#m-integratedlufs) | Gated, since the last reset. |
| [`MeasuredSeconds`](#m-measuredseconds) | Audio measured since the last reset. |
| [`MomentaryLufs`](#m-momentarylufs) | Over the last 400 ms. |
| [`ShortTermLufs`](#m-shorttermlufs) | Over the last 3 s. |
| [`TruePeakDbtp`](#m-truepeakdbtp) | Highest true peak since the last reset. |

### <a id="m-integratedlufs"></a>IntegratedLufs

```csharp
public double? IntegratedLufs { get; init; }
```

Gated, since the last reset.

**Type:** `double?`

### <a id="m-measuredseconds"></a>MeasuredSeconds

```csharp
public double MeasuredSeconds { get; init; }
```

Audio measured since the last reset.

**Type:** `double`

### <a id="m-momentarylufs"></a>MomentaryLufs

```csharp
public double? MomentaryLufs { get; init; }
```

Over the last 400 ms.

**Type:** `double?`

### <a id="m-shorttermlufs"></a>ShortTermLufs

```csharp
public double? ShortTermLufs { get; init; }
```

Over the last 3 s.

**Type:** `double?`

### <a id="m-truepeakdbtp"></a>TruePeakDbtp

```csharp
public double? TruePeakDbtp { get; init; }
```

Highest true peak since the last reset.

**Type:** `double?`
