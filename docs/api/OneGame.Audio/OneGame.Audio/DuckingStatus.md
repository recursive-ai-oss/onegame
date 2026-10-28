[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# DuckingStatus record

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DuckingStatus`](DuckingStatus.md)&gt;

```csharp
public sealed record DuckingStatus : IEquatable<DuckingStatus>
```

A bus's ducking link.

## Constructors

| Member | Description |
| --- | --- |
| [`DuckingStatus(int, float, float, float, float)`](#m-ctor-int-float-float-float-float) | A bus's ducking link. |

### <a id="m-ctor-int-float-float-float-float"></a>DuckingStatus(int, float, float, float, float)

```csharp
public DuckingStatus(int WatchedBus, float ThresholdDb, float ReductionDb, float AttackMs, float ReleaseMs);
```

A bus's ducking link.

**Parameters**

- `WatchedBus` (`int`): The bus whose level triggers it.
- `ThresholdDb` (`float`): Trigger level in dBFS.
- `ReductionDb` (`float`): Depth in dB.
- `AttackMs` (`float`): Attack time constant.
- `ReleaseMs` (`float`): Release time constant.

## Properties

| Member | Description |
| --- | --- |
| [`AttackMs`](#m-attackms) | Attack time constant. |
| [`ReductionDb`](#m-reductiondb) | Depth in dB. |
| [`ReleaseMs`](#m-releasems) | Release time constant. |
| [`ThresholdDb`](#m-thresholddb) | Trigger level in dBFS. |
| [`WatchedBus`](#m-watchedbus) | The bus whose level triggers it. |

### <a id="m-attackms"></a>AttackMs

```csharp
public float AttackMs { get; init; }
```

Attack time constant.

**Type:** `float`

### <a id="m-reductiondb"></a>ReductionDb

```csharp
public float ReductionDb { get; init; }
```

Depth in dB.

**Type:** `float`

### <a id="m-releasems"></a>ReleaseMs

```csharp
public float ReleaseMs { get; init; }
```

Release time constant.

**Type:** `float`

### <a id="m-thresholddb"></a>ThresholdDb

```csharp
public float ThresholdDb { get; init; }
```

Trigger level in dBFS.

**Type:** `float`

### <a id="m-watchedbus"></a>WatchedBus

```csharp
public int WatchedBus { get; init; }
```

The bus whose level triggers it.

**Type:** `int`
