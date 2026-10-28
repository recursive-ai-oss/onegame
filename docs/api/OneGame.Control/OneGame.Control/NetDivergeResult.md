[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# NetDivergeResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NetDivergeResult`](NetDivergeResult.md)&gt;

```csharp
public sealed record NetDivergeResult : IEquatable<NetDivergeResult>
```

Result of `net_diverge`: which entity was corrupted in the local replica.

## Constructors

| Member | Description |
| --- | --- |
| [`NetDivergeResult(string, long, string)`](#m-ctor-string-long-string) | Result of `net_diverge`: which entity was corrupted in the local replica. |

### <a id="m-ctor-string-long-string"></a>NetDivergeResult(string, long, string)

```csharp
public NetDivergeResult(string Entity, long Tick, string Hash);
```

Result of `net_diverge`: which entity was corrupted in the local replica.

**Parameters**

- `Entity` (`string`): The entity, as `Type#id`.
- `Tick` (`long`): The replica's tick.
- `Hash` (`string`): The replica's hash after the change.

## Properties

| Member | Description |
| --- | --- |
| [`Entity`](#m-entity) | The entity, as `Type#id`. |
| [`Hash`](#m-hash) | The replica's hash after the change. |
| [`Tick`](#m-tick) | The replica's tick. |

### <a id="m-entity"></a>Entity

```csharp
public string Entity { get; init; }
```

The entity, as `Type#id`.

**Type:** `string`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The replica's hash after the change.

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The replica's tick.

**Type:** `long`
