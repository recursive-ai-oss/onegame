[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# NetWaitResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NetWaitResult`](NetWaitResult.md)&gt;

```csharp
public sealed record NetWaitResult : IEquatable<NetWaitResult>
```

Result of `net_wait`: whether the tick was reached before the timeout, and where the replica is.

## Constructors

| Member | Description |
| --- | --- |
| [`NetWaitResult(bool, long, string)`](#m-ctor-bool-long-string) | Result of `net_wait`: whether the tick was reached before the timeout, and where the replica is. |

### <a id="m-ctor-bool-long-string"></a>NetWaitResult(bool, long, string)

```csharp
public NetWaitResult(bool Reached, long Tick, string Hash);
```

Result of `net_wait`: whether the tick was reached before the timeout, and where the replica is.

**Parameters**

- `Reached` (`bool`): True when the replica reached the tick.
- `Tick` (`long`): The replica's tick now.
- `Hash` (`string`): Its state hash now.

## Properties

| Member | Description |
| --- | --- |
| [`Hash`](#m-hash) | Its state hash now. |
| [`Reached`](#m-reached) | True when the replica reached the tick. |
| [`Tick`](#m-tick) | The replica's tick now. |

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

Its state hash now.

**Type:** `string`

### <a id="m-reached"></a>Reached

```csharp
public bool Reached { get; init; }
```

True when the replica reached the tick.

**Type:** `bool`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The replica's tick now.

**Type:** `long`
