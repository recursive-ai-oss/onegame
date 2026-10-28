[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Checkpoint record

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Checkpoint`](Checkpoint.md)&gt;

```csharp
public sealed record Checkpoint : IEquatable<Checkpoint>
```

A hash of the whole world after a tick, with the per-part digest when it was small enough to keep.

## Constructors

| Member | Description |
| --- | --- |
| [`Checkpoint(long, ulong, StateDigest?)`](#m-ctor-long-ulong-statedigest) | A hash of the whole world after a tick, with the per-part digest when it was small enough to keep. |

### <a id="m-ctor-long-ulong-statedigest"></a>Checkpoint(long, ulong, StateDigest?)

```csharp
public Checkpoint(long Tick, ulong Hash, StateDigest? Detail);
```

A hash of the whole world after a tick, with the per-part digest when it was small enough to keep.

**Parameters**

- `Tick` (`long`): The completed tick count.
- `Hash` (`ulong`): The world hash.
- `Detail` ([`StateDigest`](StateDigest.md)?): Per-entity, per-table digest, or null.

## Properties

| Member | Description |
| --- | --- |
| [`Detail`](#m-detail) | Per-entity, per-table digest, or null. |
| [`Hash`](#m-hash) | The world hash. |
| [`Tick`](#m-tick) | The completed tick count. |

### <a id="m-detail"></a>Detail

```csharp
public StateDigest? Detail { get; init; }
```

Per-entity, per-table digest, or null.

**Type:** [`StateDigest`](StateDigest.md)?

### <a id="m-hash"></a>Hash

```csharp
public ulong Hash { get; init; }
```

The world hash.

**Type:** `ulong`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The completed tick count.

**Type:** `long`
