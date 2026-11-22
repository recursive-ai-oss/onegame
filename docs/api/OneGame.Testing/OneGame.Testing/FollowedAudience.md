[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# FollowedAudience record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FollowedAudience`](FollowedAudience.md)&gt;

```csharp
public sealed record FollowedAudience : IEquatable<FollowedAudience>
```

Who a client's replica of a followed scope is, as the scope's authority knows it, so the cluster can compare the replica with the authority's own record of that audience (see [`ProcessCluster.Audience()`](ProcessCluster.md#m-audience-followedaudience)). The client may reach the scope through relays, or under an identity the authority has no connection for.

## Constructors

| Member | Description |
| --- | --- |
| [`FollowedAudience(string, uint, string, uint, uint, bool, uint[]?)`](#m-ctor-string-uint-string-uint-uint-bool-uint) | Who a client's replica of a followed scope is, as the scope's authority knows it, so the cluster can compare the replica with the authority's own record of that audience (see [`ProcessCluster.Audience()`](ProcessCluster.md#m-audience-followedaudience)). |

### <a id="m-ctor-string-uint-string-uint-uint-bool-uint"></a>FollowedAudience(string, uint, string, uint, uint, bool, uint\[\]?)

```csharp
public FollowedAudience(string Client, uint Scope, string Server, uint Player, uint Team = 0, bool Relay = false, uint[]? Interest = null);
```

Who a client's replica of a followed scope is, as the scope's authority knows it, so the cluster can compare the replica with the authority's own record of that audience (see [`ProcessCluster.Audience()`](ProcessCluster.md#m-audience-followedaudience)). The client may reach the scope through relays, or under an identity the authority has no connection for.

**Parameters**

- `Client` (`string`): The client node whose replica is compared.
- `Scope` (`uint`): The scope the replica follows.
- `Server` (`string`): The server node that is the scope's authority.
- `Player` (`uint`): The audience's player id.
- `Team` (`uint`): The audience's team id.
- `Relay` (`bool`): True for a relay audience.
- `Interest` (`uint[]?`): The interest keys the replica holds; null when it holds the whole scope.

## Properties

| Member | Description |
| --- | --- |
| [`Client`](#m-client) | The client node whose replica is compared. |
| [`Interest`](#m-interest) | The interest keys the replica holds; null when it holds the whole scope. |
| [`Key`](#m-key) | The text that names the audience (player, team, relay and the interest keys). |
| [`Player`](#m-player) | The audience's player id. |
| [`Relay`](#m-relay) | True for a relay audience. |
| [`Scope`](#m-scope) | The scope the replica follows. |
| [`Server`](#m-server) | The server node that is the scope's authority. |
| [`Team`](#m-team) | The audience's team id. |

### <a id="m-client"></a>Client

```csharp
public string Client { get; init; }
```

The client node whose replica is compared.

**Type:** `string`

### <a id="m-interest"></a>Interest

```csharp
public uint[]? Interest { get; init; }
```

The interest keys the replica holds; null when it holds the whole scope.

**Type:** `uint[]?`

### <a id="m-key"></a>Key

```csharp
public string Key { get; }
```

The text that names the audience (player, team, relay and the interest keys).

**Type:** `string`

### <a id="m-player"></a>Player

```csharp
public uint Player { get; init; }
```

The audience's player id.

**Type:** `uint`

### <a id="m-relay"></a>Relay

```csharp
public bool Relay { get; init; }
```

True for a relay audience.

**Type:** `bool`

### <a id="m-scope"></a>Scope

```csharp
public uint Scope { get; init; }
```

The scope the replica follows.

**Type:** `uint`

### <a id="m-server"></a>Server

```csharp
public string Server { get; init; }
```

The server node that is the scope's authority.

**Type:** `string`

### <a id="m-team"></a>Team

```csharp
public uint Team { get; init; }
```

The audience's team id.

**Type:** `uint`

## Methods

| Member | Description |
| --- | --- |
| [`ToJson()`](#m-tojson) | The audience as the JSON object the server's `audience_watch` and `hash_at` take. |

### <a id="m-tojson"></a>ToJson()

```csharp
public string ToJson();
```

The audience as the JSON object the server's `audience_watch` and `hash_at` take.

**Returns:** `string`
