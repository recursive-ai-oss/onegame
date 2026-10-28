[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ParticlesReport record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ParticlesReport`](ParticlesReport.md)&gt;

```csharp
public sealed record ParticlesReport : IEquatable<ParticlesReport>
```

Result of `particles`: the emitters the last drawn frame showed and how many particles are alive.

## Constructors

| Member | Description |
| --- | --- |
| [`ParticlesReport(string, int, int, ParticleEmitterInfo[])`](#m-ctor-string-int-int-particleemitterinfo) | Result of `particles`: the emitters the last drawn frame showed and how many particles are alive. |

### <a id="m-ctor-string-int-int-particleemitterinfo"></a>ParticlesReport(string, int, int, ParticleEmitterInfo\[\])

```csharp
public ParticlesReport(string Stage, int Emitters, int Live, ParticleEmitterInfo[] Items);
```

Result of `particles`: the emitters the last drawn frame showed and how many particles are alive.

**Parameters**

- `Stage` (`string`): The render stage's name.
- `Emitters` (`int`): How many emitters were shown.
- `Live` (`int`): Live particles across them.
- `Items` ([`ParticleEmitterInfo`](ParticleEmitterInfo.md)\[\]): One entry per emitter, in the order the game showed them (filtered by `name` when given).

## Properties

| Member | Description |
| --- | --- |
| [`Emitters`](#m-emitters) | How many emitters were shown. |
| [`Items`](#m-items) | One entry per emitter, in the order the game showed them (filtered by `name` when given). |
| [`Live`](#m-live) | Live particles across them. |
| [`Stage`](#m-stage) | The render stage's name. |

### <a id="m-emitters"></a>Emitters

```csharp
public int Emitters { get; init; }
```

How many emitters were shown.

**Type:** `int`

### <a id="m-items"></a>Items

```csharp
public ParticleEmitterInfo[] Items { get; init; }
```

One entry per emitter, in the order the game showed them (filtered by `name` when given).

**Type:** [`ParticleEmitterInfo`](ParticleEmitterInfo.md)\[\]

### <a id="m-live"></a>Live

```csharp
public int Live { get; init; }
```

Live particles across them.

**Type:** `int`

### <a id="m-stage"></a>Stage

```csharp
public string Stage { get; init; }
```

The render stage's name.

**Type:** `string`
