[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# UserAchievementStored struct

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`ISteamCallbackData`](ISteamCallbackData-1.md)&lt;[`UserAchievementStored`](UserAchievementStored.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`UserAchievementStored`](UserAchievementStored.md)&gt;

```csharp
public readonly struct UserAchievementStored : ISteamCallbackData<UserAchievementStored>, IEquatable<UserAchievementStored>
```

An achievement was stored, or its progress was shown. When both progress numbers are zero it is fully unlocked.

## Constructors

| Member | Description |
| --- | --- |
| [`UserAchievementStored(ulong, bool, string, uint, uint)`](#m-ctor-ulong-bool-string-uint-uint) | An achievement was stored, or its progress was shown. |

### <a id="m-ctor-ulong-bool-string-uint-uint"></a>UserAchievementStored(ulong, bool, string, uint, uint)

```csharp
public UserAchievementStored(ulong GameId, bool GroupAchievement, string Name, uint CurrentProgress, uint MaxProgress);
```

An achievement was stored, or its progress was shown. When both progress numbers are zero it is fully unlocked.

**Parameters**

- `GameId` (`ulong`): The game the achievement belongs to.
- `GroupAchievement` (`bool`): Unused by Steam.
- `Name` (`string`): The achievement's API name.
- `CurrentProgress` (`uint`): Progress so far.
- `MaxProgress` (`uint`): Progress needed to unlock.

## Properties

| Member | Description |
| --- | --- |
| [`CurrentProgress`](#m-currentprogress) | Progress so far. |
| [`GameId`](#m-gameid) | The game the achievement belongs to. |
| [`GroupAchievement`](#m-groupachievement) | Unused by Steam. |
| [`MaxProgress`](#m-maxprogress) | Progress needed to unlock. |
| [`Name`](#m-name) | The achievement's API name. |

### <a id="m-currentprogress"></a>CurrentProgress

```csharp
public uint CurrentProgress { get; init; }
```

Progress so far.

**Type:** `uint`

### <a id="m-gameid"></a>GameId

```csharp
public ulong GameId { get; init; }
```

The game the achievement belongs to.

**Type:** `ulong`

### <a id="m-groupachievement"></a>GroupAchievement

```csharp
public bool GroupAchievement { get; init; }
```

Unused by Steam.

**Type:** `bool`

### <a id="m-maxprogress"></a>MaxProgress

```csharp
public uint MaxProgress { get; init; }
```

Progress needed to unlock.

**Type:** `uint`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The achievement's API name.

**Type:** `string`
