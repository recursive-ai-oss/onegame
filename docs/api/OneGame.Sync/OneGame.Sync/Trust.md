[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Trust struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Trust`](Trust.md)&gt;

```csharp
public readonly struct Trust : IEquatable<Trust>
```

What a connection may do and who it is. [`Trust.Read`](Trust.md#m-read) decides which scopes it may subscribe to, [`Trust.Command`](Trust.md#m-command) which it may send commands to, and [`Trust.Own`](Trust.md#m-own) in which it may send `OwnerOnly` commands. [`Trust.Who`](Trust.md#m-who) is the player and team it speaks for, which decides what owner- and team-visible data it receives and which entities it owns. The authority enforces all of it on every subscription, command and frame, and it can change at any time (see [`Connection.Trust`](Connection.md#m-trust)).

## Constructors

| Member | Description |
| --- | --- |
| [`Trust(ScopeSet, ScopeSet, ScopeSet, Audience)`](#m-ctor-scopeset-scopeset-scopeset-audience) | What a connection may do and who it is. |

### <a id="m-ctor-scopeset-scopeset-scopeset-audience"></a>Trust(ScopeSet, ScopeSet, ScopeSet, Audience)

```csharp
public Trust(ScopeSet Read, ScopeSet Command, ScopeSet Own, Audience Who = default);
```

What a connection may do and who it is. [`Trust.Read`](Trust.md#m-read) decides which scopes it may subscribe to, [`Trust.Command`](Trust.md#m-command) which it may send commands to, and [`Trust.Own`](Trust.md#m-own) in which it may send `OwnerOnly` commands. [`Trust.Who`](Trust.md#m-who) is the player and team it speaks for, which decides what owner- and team-visible data it receives and which entities it owns. The authority enforces all of it on every subscription, command and frame, and it can change at any time (see [`Connection.Trust`](Connection.md#m-trust)).

**Parameters**

- `Read` ([`ScopeSet`](ScopeSet.md)): Scopes the peer may subscribe to.
- `Command` ([`ScopeSet`](ScopeSet.md)): Scopes the peer may send commands to.
- `Own` ([`ScopeSet`](ScopeSet.md)): Scopes in which the peer may send `OwnerOnly` commands for entities it owns. Without it those commands are refused, even for the owner.
- `Who` ([`Audience`](../../OneGame.Core/OneGame.Core/Audience.md)): The player and team the peer speaks for; [`Audience.Anonymous`](../../OneGame.Core/OneGame.Core/Audience.md#m-anonymous) by default.

## Properties

| Member | Description |
| --- | --- |
| [`Command`](#m-command) | Scopes the peer may send commands to. |
| [`Full`](#m-full) | May read, command and own everything. |
| [`None`](#m-none) | May do nothing. |
| [`Own`](#m-own) | Scopes in which the peer may send `OwnerOnly` commands for entities it owns. |
| [`Read`](#m-read) | Scopes the peer may subscribe to. |
| [`Who`](#m-who) | The player and team the peer speaks for; [`Audience.Anonymous`](../../OneGame.Core/OneGame.Core/Audience.md#m-anonymous) by default. |

### <a id="m-command"></a>Command

```csharp
public ScopeSet Command { get; init; }
```

Scopes the peer may send commands to.

**Type:** [`ScopeSet`](ScopeSet.md)

### <a id="m-full"></a>Full

```csharp
public static Trust Full { get; }
```

May read, command and own everything. For the local player of a single-player game and for tests.

**Type:** [`Trust`](Trust.md)

### <a id="m-none"></a>None

```csharp
public static Trust None { get; }
```

May do nothing.

**Type:** [`Trust`](Trust.md)

### <a id="m-own"></a>Own

```csharp
public ScopeSet Own { get; init; }
```

Scopes in which the peer may send `OwnerOnly` commands for entities it owns. Without it those commands are refused, even for the owner.

**Type:** [`ScopeSet`](ScopeSet.md)

### <a id="m-read"></a>Read

```csharp
public ScopeSet Read { get; init; }
```

Scopes the peer may subscribe to.

**Type:** [`ScopeSet`](ScopeSet.md)

### <a id="m-who"></a>Who

```csharp
public Audience Who { get; init; }
```

The player and team the peer speaks for; [`Audience.Anonymous`](../../OneGame.Core/OneGame.Core/Audience.md#m-anonymous) by default.

**Type:** [`Audience`](../../OneGame.Core/OneGame.Core/Audience.md)

## Methods

| Member | Description |
| --- | --- |
| [`As(uint, uint)`](#m-as-uint-uint) | The same trust, speaking for a player and a team: they decide which owner- and team-visible data the peer receives. |
| [`AsRelay()`](#m-asrelay) | The same trust, for a relay node: the authority sends it every member except [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden) ones, because the relay filters them again for its own subscribers. |
| [`Owner(ScopeId[])`](#m-owner-scopeid) | May read, command and own the listed scopes (and so send `OwnerOnly` commands there for entities it owns). |
| [`Player(ScopeId[])`](#m-player-scopeid) | May read and command the listed scopes. |
| [`Reader(ScopeId[])`](#m-reader-scopeid) | May read the listed scopes and nothing else. |

### <a id="m-as-uint-uint"></a>As(uint, uint)

```csharp
public Trust As(uint player, uint team = 0);
```

The same trust, speaking for a player and a team: they decide which owner- and team-visible data the peer receives.

**Parameters**

- `player` (`uint`): The player id; 0 for none.
- `team` (`uint`): The team id; 0 for none.

**Returns:** [`Trust`](Trust.md)

### <a id="m-asrelay"></a>AsRelay()

```csharp
public Trust AsRelay();
```

The same trust, for a relay node: the authority sends it every member except [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden) ones, because the relay filters them again for its own subscribers. Only grant this to a node you trust with all the data of the scope.

**Returns:** [`Trust`](Trust.md)

### <a id="m-owner-scopeid"></a>Owner(ScopeId\[\])

```csharp
public static Trust Owner(params ScopeId[] scopes);
```

May read, command and own the listed scopes (and so send `OwnerOnly` commands there for entities it owns).

**Parameters**

- `scopes` ([`ScopeId`](ScopeId.md)\[\])

**Returns:** [`Trust`](Trust.md)

### <a id="m-player-scopeid"></a>Player(ScopeId\[\])

```csharp
public static Trust Player(params ScopeId[] scopes);
```

May read and command the listed scopes.

**Parameters**

- `scopes` ([`ScopeId`](ScopeId.md)\[\])

**Returns:** [`Trust`](Trust.md)

### <a id="m-reader-scopeid"></a>Reader(ScopeId\[\])

```csharp
public static Trust Reader(params ScopeId[] scopes);
```

May read the listed scopes and nothing else.

**Parameters**

- `scopes` ([`ScopeId`](ScopeId.md)\[\])

**Returns:** [`Trust`](Trust.md)
