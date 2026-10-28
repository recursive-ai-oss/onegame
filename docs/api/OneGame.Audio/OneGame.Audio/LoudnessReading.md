[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# LoudnessReading struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`LoudnessReading`](LoudnessReading.md)&gt;

```csharp
public readonly struct LoudnessReading : IEquatable<LoudnessReading>
```

What the loudness meter reads at the output (EBU R 128 / ITU-R BS.1770). Silence reads -infinity, shown as [`Double.NegativeInfinity`](https://learn.microsoft.com/dotnet/api/system.double).

## Constructors

| Member | Description |
| --- | --- |
| [`LoudnessReading(double, double, double, double, long)`](#m-ctor-double-double-double-double-long) | What the loudness meter reads at the output (EBU R 128 / ITU-R BS.1770). |

### <a id="m-ctor-double-double-double-double-long"></a>LoudnessReading(double, double, double, double, long)

```csharp
public LoudnessReading(double MomentaryLufs, double ShortTermLufs, double IntegratedLufs, double TruePeakDbtp, long Frames);
```

What the loudness meter reads at the output (EBU R 128 / ITU-R BS.1770). Silence reads -infinity, shown as [`Double.NegativeInfinity`](https://learn.microsoft.com/dotnet/api/system.double).

**Parameters**

- `MomentaryLufs` (`double`): Loudness over the last 400 ms.
- `ShortTermLufs` (`double`): Loudness over the last 3 s.
- `IntegratedLufs` (`double`): Gated loudness since the last reset.
- `TruePeakDbtp` (`double`): The highest true peak since the last reset, in dBTP.
- `Frames` (`long`): Frames measured since the last reset.

## Properties

| Member | Description |
| --- | --- |
| [`Frames`](#m-frames) | Frames measured since the last reset. |
| [`IntegratedLufs`](#m-integratedlufs) | Gated loudness since the last reset. |
| [`MomentaryLufs`](#m-momentarylufs) | Loudness over the last 400 ms. |
| [`ShortTermLufs`](#m-shorttermlufs) | Loudness over the last 3 s. |
| [`TruePeakDbtp`](#m-truepeakdbtp) | The highest true peak since the last reset, in dBTP. |

### <a id="m-frames"></a>Frames

```csharp
public long Frames { get; init; }
```

Frames measured since the last reset.

**Type:** `long`

### <a id="m-integratedlufs"></a>IntegratedLufs

```csharp
public double IntegratedLufs { get; init; }
```

Gated loudness since the last reset.

**Type:** `double`

### <a id="m-momentarylufs"></a>MomentaryLufs

```csharp
public double MomentaryLufs { get; init; }
```

Loudness over the last 400 ms.

**Type:** `double`

### <a id="m-shorttermlufs"></a>ShortTermLufs

```csharp
public double ShortTermLufs { get; init; }
```

Loudness over the last 3 s.

**Type:** `double`

### <a id="m-truepeakdbtp"></a>TruePeakDbtp

```csharp
public double TruePeakDbtp { get; init; }
```

The highest true peak since the last reset, in dBTP.

**Type:** `double`
