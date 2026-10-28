[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Divergence record

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Divergence`](Divergence.md)&gt;

```csharp
public sealed record Divergence : IEquatable<Divergence>
```

The first place a replay stopped matching the recording.

## Constructors

| Member | Description |
| --- | --- |
| [`Divergence(DivergenceReason, long, long, ulong, ulong, IReadOnlyList<StateDifference>, string)`](#m-ctor-divergencereason-long-long-ulong-ulong-ireadonlylist-statedifference-string) | The first place a replay stopped matching the recording. |

### <a id="m-ctor-divergencereason-long-long-ulong-ulong-ireadonlylist-statedifference-string"></a>Divergence(DivergenceReason, long, long, ulong, ulong, IReadOnlyList&lt;StateDifference&gt;, string)

```csharp
public Divergence(DivergenceReason Reason, long Tick, long LastGoodTick, ulong ExpectedHash, ulong ActualHash, IReadOnlyList<StateDifference> Differences, string Detail);
```

The first place a replay stopped matching the recording.

**Parameters**

- `Reason` ([`DivergenceReason`](DivergenceReason.md)): What kind of mismatch.
- `Tick` (`long`): The completed tick count at which the mismatch was seen.
- `LastGoodTick` (`long`): The last tick at which the replay matched; the real divergence is in (LastGoodTick, Tick\].
- `ExpectedHash` (`ulong`): The recorded hash.
- `ActualHash` (`ulong`): The replayed hash.
- `Differences` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`StateDifference`](StateDifference.md)&gt;): The entities, tables or streams that differ; empty if the checkpoint kept no digest.
- `Detail` (`string`): A sentence on what happened.

## Properties

| Member | Description |
| --- | --- |
| [`ActualHash`](#m-actualhash) | The replayed hash. |
| [`Detail`](#m-detail) | A sentence on what happened. |
| [`Differences`](#m-differences) | The entities, tables or streams that differ; empty if the checkpoint kept no digest. |
| [`ExpectedHash`](#m-expectedhash) | The recorded hash. |
| [`LastGoodTick`](#m-lastgoodtick) | The last tick at which the replay matched; the real divergence is in (LastGoodTick, Tick\]. |
| [`Reason`](#m-reason) | What kind of mismatch. |
| [`Tick`](#m-tick) | The completed tick count at which the mismatch was seen. |

### <a id="m-actualhash"></a>ActualHash

```csharp
public ulong ActualHash { get; init; }
```

The replayed hash.

**Type:** `ulong`

### <a id="m-detail"></a>Detail

```csharp
public string Detail { get; init; }
```

A sentence on what happened.

**Type:** `string`

### <a id="m-differences"></a>Differences

```csharp
public IReadOnlyList<StateDifference> Differences { get; init; }
```

The entities, tables or streams that differ; empty if the checkpoint kept no digest.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`StateDifference`](StateDifference.md)&gt;

### <a id="m-expectedhash"></a>ExpectedHash

```csharp
public ulong ExpectedHash { get; init; }
```

The recorded hash.

**Type:** `ulong`

### <a id="m-lastgoodtick"></a>LastGoodTick

```csharp
public long LastGoodTick { get; init; }
```

The last tick at which the replay matched; the real divergence is in (LastGoodTick, Tick\].

**Type:** `long`

### <a id="m-reason"></a>Reason

```csharp
public DivergenceReason Reason { get; init; }
```

What kind of mismatch.

**Type:** [`DivergenceReason`](DivergenceReason.md)

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The completed tick count at which the mismatch was seen.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`ToJson()`](#m-tojson) | The divergence as JSON. |
| [`ToString()`](#m-tostring) |  |

### <a id="m-tojson"></a>ToJson()

```csharp
public string ToJson();
```

The divergence as JSON.

**Returns:** `string`

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

Overrides [`Object.ToString()`](https://learn.microsoft.com/dotnet/api/system.object.tostring).

**Returns:** `string`
