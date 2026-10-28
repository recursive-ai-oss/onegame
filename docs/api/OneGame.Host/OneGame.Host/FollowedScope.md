[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# FollowedScope record

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FollowedScope`](FollowedScope.md)&gt;

```csharp
public sealed record FollowedScope : IEquatable<FollowedScope>
```

One scope a client follows besides its main one (served by its server's relay), for `net_status`.

## Constructors

| Member | Description |
| --- | --- |
| [`FollowedScope(uint, string, long, string)`](#m-ctor-uint-string-long-string) | One scope a client follows besides its main one (served by its server's relay), for `net_status`. |

### <a id="m-ctor-uint-string-long-string"></a>FollowedScope(uint, string, long, string)

```csharp
public FollowedScope(uint Scope, string Replica, long Tick, string Hash);
```

One scope a client follows besides its main one (served by its server's relay), for `net_status`.

**Parameters**

- `Scope` (`uint`): The scope id.
- `Replica` (`string`): Replica state: subscribing, live, resyncing, refused, failed.
- `Tick` (`long`): The replica's tick.
- `Hash` (`string`): The hash of what the replica holds, 16 hex digits.

## Properties

| Member | Description |
| --- | --- |
| [`Hash`](#m-hash) | The hash of what the replica holds, 16 hex digits. |
| [`Replica`](#m-replica) | Replica state: subscribing, live, resyncing, refused, failed. |
| [`Scope`](#m-scope) | The scope id. |
| [`Tick`](#m-tick) | The replica's tick. |

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The hash of what the replica holds, 16 hex digits.

**Type:** `string`

### <a id="m-replica"></a>Replica

```csharp
public string Replica { get; init; }
```

Replica state: subscribing, live, resyncing, refused, failed.

**Type:** `string`

### <a id="m-scope"></a>Scope

```csharp
public uint Scope { get; init; }
```

The scope id.

**Type:** `uint`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The replica's tick.

**Type:** `long`
