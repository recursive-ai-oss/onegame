[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# UserStatsStored struct

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`ISteamCallbackData`](ISteamCallbackData-1.md)&lt;[`UserStatsStored`](UserStatsStored.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`UserStatsStored`](UserStatsStored.md)&gt;

```csharp
public readonly struct UserStatsStored : ISteamCallbackData<UserStatsStored>, IEquatable<UserStatsStored>
```

Stats were (or were not) stored on the server.

## Constructors

| Member | Description |
| --- | --- |
| [`UserStatsStored(ulong, int)`](#m-ctor-ulong-int) | Stats were (or were not) stored on the server. |

### <a id="m-ctor-ulong-int"></a>UserStatsStored(ulong, int)

```csharp
public UserStatsStored(ulong GameId, int Result);
```

Stats were (or were not) stored on the server.

**Parameters**

- `GameId` (`ulong`): The game the stats belong to.
- `Result` (`int`): Steam result code; 1 is success, 8 means the server rejected a value and sent corrected ones.

## Properties

| Member | Description |
| --- | --- |
| [`GameId`](#m-gameid) | The game the stats belong to. |
| [`Result`](#m-result) | Steam result code; 1 is success, 8 means the server rejected a value and sent corrected ones. |
| [`Succeeded`](#m-succeeded) | True when [`UserStatsStored.Result`](UserStatsStored.md#m-result) is the success code. |

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

Steam result code; 1 is success, 8 means the server rejected a value and sent corrected ones.

**Type:** `int`

### <a id="m-succeeded"></a>Succeeded

```csharp
public bool Succeeded { get; }
```

True when [`UserStatsStored.Result`](UserStatsStored.md#m-result) is the success code.

**Type:** `bool`
