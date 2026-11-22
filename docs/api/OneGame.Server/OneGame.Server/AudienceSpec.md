[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# AudienceSpec record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudienceSpec`](AudienceSpec.md)&gt;

```csharp
public sealed record AudienceSpec : IEquatable<AudienceSpec>
```

Who a replica holds a view for, as the authority knows it: the player and team of the trust it was given, whether it is a relay, and the interest cells it holds. A client that follows a scope through other nodes has no connection to the authority, so this is how a harness names its view.

## Constructors

| Member | Description |
| --- | --- |
| [`AudienceSpec(uint, uint, bool, uint[]?)`](#m-ctor-uint-uint-bool-uint) | Who a replica holds a view for, as the authority knows it: the player and team of the trust it was given, whether it is a relay, and the interest cells it holds. |

### <a id="m-ctor-uint-uint-bool-uint"></a>AudienceSpec(uint, uint, bool, uint\[\]?)

```csharp
public AudienceSpec(uint Player, uint Team = 0, bool Relay = false, uint[]? Interest = null);
```

Who a replica holds a view for, as the authority knows it: the player and team of the trust it was given, whether it is a relay, and the interest cells it holds. A client that follows a scope through other nodes has no connection to the authority, so this is how a harness names its view.

**Parameters**

- `Player` (`uint`): The player id of the audience (0 for none).
- `Team` (`uint`): The team id (0 for none).
- `Relay` (`bool`): True for a relay audience, which sees what the entity's owner and team see.
- `Interest` (`uint[]?`): The interest keys the audience holds; null when it holds the whole scope. An empty list is an audience that holds only global entities.

## Properties

| Member | Description |
| --- | --- |
| [`Interest`](#m-interest) | The interest keys the audience holds; null when it holds the whole scope. |
| [`Key`](#m-key) | The text that names this audience in the host's list (player, team, relay and the sorted interest keys). |
| [`Player`](#m-player) | The player id of the audience (0 for none). |
| [`Relay`](#m-relay) | True for a relay audience, which sees what the entity's owner and team see. |
| [`Team`](#m-team) | The team id (0 for none). |

### <a id="m-interest"></a>Interest

```csharp
public uint[]? Interest { get; init; }
```

The interest keys the audience holds; null when it holds the whole scope. An empty list is an audience that holds only global entities.

**Type:** `uint[]?`

### <a id="m-key"></a>Key

```csharp
public string Key { get; }
```

The text that names this audience in the host's list (player, team, relay and the sorted interest keys).

**Type:** `string`

### <a id="m-player"></a>Player

```csharp
public uint Player { get; init; }
```

The player id of the audience (0 for none).

**Type:** `uint`

### <a id="m-relay"></a>Relay

```csharp
public bool Relay { get; init; }
```

True for a relay audience, which sees what the entity's owner and team see.

**Type:** `bool`

### <a id="m-team"></a>Team

```csharp
public uint Team { get; init; }
```

The team id (0 for none).

**Type:** `uint`
