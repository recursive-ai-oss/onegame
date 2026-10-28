[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ParticleEmitterInfo record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ParticleEmitterInfo`](ParticleEmitterInfo.md)&gt;

```csharp
public sealed record ParticleEmitterInfo : IEquatable<ParticleEmitterInfo>
```

One emitter in a `particles` report.

## Constructors

| Member | Description |
| --- | --- |
| [`ParticleEmitterInfo(string, int, int, bool, long, long, string, string, double[])`](#m-ctor-string-int-int-bool-long-long-string-string-double) | One emitter in a `particles` report. |

### <a id="m-ctor-string-int-int-bool-long-long-string-string-double"></a>ParticleEmitterInfo(string, int, int, bool, long, long, string, string, double\[\])

```csharp
public ParticleEmitterInfo(string Name, int Live, int Capacity, bool Emitting, long Spawned, long Dropped, string Blend, string Space, double[] Position);
```

One emitter in a `particles` report.

**Parameters**

- `Name` (`string`): The emitter's name (empty when it has none).
- `Live` (`int`): Particles alive.
- `Capacity` (`int`): Most particles it can hold.
- `Emitting` (`bool`): Whether it is still producing particles from its rate.
- `Spawned` (`long`): Particles born so far.
- `Dropped` (`long`): Births dropped because it was full.
- `Blend` (`string`): alpha or additive.
- `Space` (`string`): plane (2D) or volume (3D).
- `Position` (`double[]`): Where the emitter is \[x, y, z\].

## Properties

| Member | Description |
| --- | --- |
| [`Blend`](#m-blend) | alpha or additive. |
| [`Capacity`](#m-capacity) | Most particles it can hold. |
| [`Dropped`](#m-dropped) | Births dropped because it was full. |
| [`Emitting`](#m-emitting) | Whether it is still producing particles from its rate. |
| [`Live`](#m-live) | Particles alive. |
| [`Name`](#m-name) | The emitter's name (empty when it has none). |
| [`Position`](#m-position) | Where the emitter is \[x, y, z\]. |
| [`Space`](#m-space) | plane (2D) or volume (3D). |
| [`Spawned`](#m-spawned) | Particles born so far. |

### <a id="m-blend"></a>Blend

```csharp
public string Blend { get; init; }
```

alpha or additive.

**Type:** `string`

### <a id="m-capacity"></a>Capacity

```csharp
public int Capacity { get; init; }
```

Most particles it can hold.

**Type:** `int`

### <a id="m-dropped"></a>Dropped

```csharp
public long Dropped { get; init; }
```

Births dropped because it was full.

**Type:** `long`

### <a id="m-emitting"></a>Emitting

```csharp
public bool Emitting { get; init; }
```

Whether it is still producing particles from its rate.

**Type:** `bool`

### <a id="m-live"></a>Live

```csharp
public int Live { get; init; }
```

Particles alive.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The emitter's name (empty when it has none).

**Type:** `string`

### <a id="m-position"></a>Position

```csharp
public double[] Position { get; init; }
```

Where the emitter is \[x, y, z\].

**Type:** `double[]`

### <a id="m-space"></a>Space

```csharp
public string Space { get; init; }
```

plane (2D) or volume (3D).

**Type:** `string`

### <a id="m-spawned"></a>Spawned

```csharp
public long Spawned { get; init; }
```

Particles born so far.

**Type:** `long`
