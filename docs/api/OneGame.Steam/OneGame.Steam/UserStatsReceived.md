[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# UserStatsReceived struct

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`ISteamCallbackData`](ISteamCallbackData-1.md)&lt;[`UserStatsReceived`](UserStatsReceived.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`UserStatsReceived`](UserStatsReceived.md)&gt;

```csharp
public readonly struct UserStatsReceived : ISteamCallbackData<UserStatsReceived>, IEquatable<UserStatsReceived>
```

The latest stats and achievements of a user arrived.

## Constructors

| Member | Description |
| --- | --- |
| [`UserStatsReceived(ulong, int, ulong)`](#m-ctor-ulong-int-ulong) | The latest stats and achievements of a user arrived. |

### <a id="m-ctor-ulong-int-ulong"></a>UserStatsReceived(ulong, int, ulong)

```csharp
public UserStatsReceived(ulong GameId, int Result, ulong UserId);
```

The latest stats and achievements of a user arrived.

**Parameters**

- `GameId` (`ulong`): The game the stats belong to.
- `Result` (`int`): Steam result code; 1 is success.
- `UserId` (`ulong`): The user whose stats arrived.

## Properties

| Member | Description |
| --- | --- |
| [`GameId`](#m-gameid) | The game the stats belong to. |
| [`Result`](#m-result) | Steam result code; 1 is success. |
| [`Succeeded`](#m-succeeded) | True when [`UserStatsReceived.Result`](UserStatsReceived.md#m-result) is the success code. |
| [`UserId`](#m-userid) | The user whose stats arrived. |

### <a id="m-gameid"></a>GameId

```csharp
public ulong GameId { get; init; }
```

The game the stats belong to.

**Type:** `ulong`

### <a id="m-result"></a>Result

```csharp
public int Result { get; init; }
```

Steam result code; 1 is success.

**Type:** `int`

### <a id="m-succeeded"></a>Succeeded

```csharp
public bool Succeeded { get; }
```

True when [`UserStatsReceived.Result`](UserStatsReceived.md#m-result) is the success code.

**Type:** `bool`

### <a id="m-userid"></a>UserId

```csharp
public ulong UserId { get; init; }
```

The user whose stats arrived.

**Type:** `ulong`
