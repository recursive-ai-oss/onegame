[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# FilterStatus record

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FilterStatus`](FilterStatus.md)&gt;

```csharp
public sealed record FilterStatus : IEquatable<FilterStatus>
```

A bus filter's setting.

## Constructors

| Member | Description |
| --- | --- |
| [`FilterStatus(float, float)`](#m-ctor-float-float) | A bus filter's setting. |

### <a id="m-ctor-float-float"></a>FilterStatus(float, float)

```csharp
public FilterStatus(float CutoffHz, float Q);
```

A bus filter's setting.

**Parameters**

- `CutoffHz` (`float`): The corner frequency.
- `Q` (`float`): The resonance; 0 means flat.

## Properties

| Member | Description |
| --- | --- |
| [`CutoffHz`](#m-cutoffhz) | The corner frequency. |
| [`Q`](#m-q) | The resonance; 0 means flat. |

### <a id="m-cutoffhz"></a>CutoffHz

```csharp
public float CutoffHz { get; init; }
```

The corner frequency.

**Type:** `float`

### <a id="m-q"></a>Q

```csharp
public float Q { get; init; }
```

The resonance; 0 means flat.

**Type:** `float`
