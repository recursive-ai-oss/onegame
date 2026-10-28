[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# LimiterStatus record

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`LimiterStatus`](LimiterStatus.md)&gt;

```csharp
public sealed record LimiterStatus : IEquatable<LimiterStatus>
```

The master limiter.

## Constructors

| Member | Description |
| --- | --- |
| [`LimiterStatus(bool, float?, float?, double)`](#m-ctor-bool-float-float-double) | The master limiter. |

### <a id="m-ctor-bool-float-float-double"></a>LimiterStatus(bool, float?, float?, double)

```csharp
public LimiterStatus(bool Enabled, float? CeilingDb, float? ReleaseMs, double GainReductionDb);
```

The master limiter.

**Parameters**

- `Enabled` (`bool`): Whether it is switched on.
- `CeilingDb` (`float?`): Its ceiling, when on.
- `ReleaseMs` (`float?`): Its release, when on.
- `GainReductionDb` (`double`): The reduction in the last block (0 or negative).

## Properties

| Member | Description |
| --- | --- |
| [`CeilingDb`](#m-ceilingdb) | Its ceiling, when on. |
| [`Enabled`](#m-enabled) | Whether it is switched on. |
| [`GainReductionDb`](#m-gainreductiondb) | The reduction in the last block (0 or negative). |
| [`ReleaseMs`](#m-releasems) | Its release, when on. |

### <a id="m-ceilingdb"></a>CeilingDb

```csharp
public float? CeilingDb { get; init; }
```

Its ceiling, when on.

**Type:** `float?`

### <a id="m-enabled"></a>Enabled

```csharp
public bool Enabled { get; init; }
```

Whether it is switched on.

**Type:** `bool`

### <a id="m-gainreductiondb"></a>GainReductionDb

```csharp
public double GainReductionDb { get; init; }
```

The reduction in the last block (0 or negative).

**Type:** `double`

### <a id="m-releasems"></a>ReleaseMs

```csharp
public float? ReleaseMs { get; init; }
```

Its release, when on.

**Type:** `float?`
