[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# Audience struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Audience`](Audience.md)&gt;

```csharp
public readonly struct Audience : IEquatable<Audience>
```

Who a subscriber is, for visibility: the player it speaks for and the team it plays on. The authority matches it against the [`OwnerKeyAttribute`](OwnerKeyAttribute.md) and [`TeamKeyAttribute`](TeamKeyAttribute.md) values of each entity to decide what to send. Zero means "nobody" and never matches, so [`Audience.Anonymous`](Audience.md#m-anonymous) sees only what is visible to everyone.

## Constructors

| Member | Description |
| --- | --- |
| [`Audience(uint, uint, bool)`](#m-ctor-uint-uint-bool) | Who a subscriber is, for visibility: the player it speaks for and the team it plays on. |

### <a id="m-ctor-uint-uint-bool"></a>Audience(uint, uint, bool)

```csharp
public Audience(uint Player, uint Team = 0, bool Relay = false);
```

Who a subscriber is, for visibility: the player it speaks for and the team it plays on. The authority matches it against the [`OwnerKeyAttribute`](OwnerKeyAttribute.md) and [`TeamKeyAttribute`](TeamKeyAttribute.md) values of each entity to decide what to send. Zero means "nobody" and never matches, so [`Audience.Anonymous`](Audience.md#m-anonymous) sees only what is visible to everyone.

**Parameters**

- `Player` (`uint`): The player id the connection owns entities as; 0 for none.
- `Team` (`uint`): The team id; 0 for none.
- `Relay` (`bool`): True for a trusted relay node: it receives every member that is not [`Visibility.Hidden`](Visibility.md#m-hidden) (as if it were each entity's owner), so it can filter them again for its own subscribers.

## Properties

| Member | Description |
| --- | --- |
| [`Anonymous`](#m-anonymous) | No player and no team: sees only what everyone sees. |
| [`Player`](#m-player) | The player id the connection owns entities as; 0 for none. |
| [`Relay`](#m-relay) | True for a trusted relay node: it receives every member that is not [`Visibility.Hidden`](Visibility.md#m-hidden) (as if it were each entity's owner), so it can filter them again for its own subscribers. |
| [`Team`](#m-team) | The team id; 0 for none. |

### <a id="m-anonymous"></a>Anonymous

```csharp
public static Audience Anonymous { get; }
```

No player and no team: sees only what everyone sees.

**Type:** [`Audience`](Audience.md)

### <a id="m-player"></a>Player

```csharp
public uint Player { get; init; }
```

The player id the connection owns entities as; 0 for none.

**Type:** `uint`

### <a id="m-relay"></a>Relay

```csharp
public bool Relay { get; init; }
```

True for a trusted relay node: it receives every member that is not [`Visibility.Hidden`](Visibility.md#m-hidden) (as if it were each entity's owner), so it can filter them again for its own subscribers.

**Type:** `bool`

### <a id="m-team"></a>Team

```csharp
public uint Team { get; init; }
```

The team id; 0 for none.

**Type:** `uint`

## Methods

| Member | Description |
| --- | --- |
| [`IsOwnerOf(ulong)`](#m-isownerof-ulong) | True when this audience is the owner for an entity whose owner key is `ownerKey`. |
| [`IsTeamOf(ulong, ulong)`](#m-isteamof-ulong-ulong) | True when this audience is the owner of, or on the same team as, an entity with these keys. |

### <a id="m-isownerof-ulong"></a>IsOwnerOf(ulong)

```csharp
public bool IsOwnerOf(ulong ownerKey);
```

True when this audience is the owner for an entity whose owner key is `ownerKey`.

**Parameters**

- `ownerKey` (`ulong`)

**Returns:** `bool`

### <a id="m-isteamof-ulong-ulong"></a>IsTeamOf(ulong, ulong)

```csharp
public bool IsTeamOf(ulong ownerKey, ulong teamKey);
```

True when this audience is the owner of, or on the same team as, an entity with these keys.

**Parameters**

- `ownerKey` (`ulong`)
- `teamKey` (`ulong`)

**Returns:** `bool`
