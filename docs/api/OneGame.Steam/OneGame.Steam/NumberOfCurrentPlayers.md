[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# NumberOfCurrentPlayers struct

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`ISteamCallbackData`](ISteamCallbackData-1.md)&lt;[`NumberOfCurrentPlayers`](NumberOfCurrentPlayers.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NumberOfCurrentPlayers`](NumberOfCurrentPlayers.md)&gt;

```csharp
public readonly struct NumberOfCurrentPlayers : ISteamCallbackData<NumberOfCurrentPlayers>, IEquatable<NumberOfCurrentPlayers>
```

Result of `GetNumberOfCurrentPlayers`.

## Constructors

| Member | Description |
| --- | --- |
| [`NumberOfCurrentPlayers(bool, int)`](#m-ctor-bool-int) | Result of `GetNumberOfCurrentPlayers`. |

### <a id="m-ctor-bool-int"></a>NumberOfCurrentPlayers(bool, int)

```csharp
public NumberOfCurrentPlayers(bool Success, int Players);
```

Result of `GetNumberOfCurrentPlayers`.

**Parameters**

- `Success` (`bool`): Whether the call worked.
- `Players` (`int`): Players currently in the game.

## Properties

| Member | Description |
| --- | --- |
| [`Players`](#m-players) | Players currently in the game. |
| [`Success`](#m-success) | Whether the call worked. |

### <a id="m-players"></a>Players

```csharp
public int Players { get; init; }
```

Players currently in the game.

**Type:** `int`

### <a id="m-success"></a>Success

```csharp
public bool Success { get; init; }
```

Whether the call worked.

**Type:** `bool`
