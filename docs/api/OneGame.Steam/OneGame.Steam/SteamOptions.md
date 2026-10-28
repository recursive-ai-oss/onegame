[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# SteamOptions class

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public sealed class SteamOptions
```

Settings for connecting to Steam.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>SteamOptions()

```csharp
public SteamOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`AppId`](#m-appid) | The game's Steam app id. |
| [`RelaunchThroughSteam`](#m-relaunchthroughsteam) | When true and the game was started outside Steam, ask Steam to start it again through the client instead of running without a connection. |

### <a id="m-appid"></a>AppId

```csharp
public uint AppId { get; init; }
```

The game's Steam app id. Needed for [`SteamOptions.RelaunchThroughSteam`](SteamOptions.md#m-relaunchthroughsteam); otherwise Steam learns it from how the game was launched, or in development from a `steam_appid.txt` file next to the executable (see the interface notes).

**Type:** `uint`

### <a id="m-relaunchthroughsteam"></a>RelaunchThroughSteam

```csharp
public bool RelaunchThroughSteam { get; init; }
```

When true and the game was started outside Steam, ask Steam to start it again through the client instead of running without a connection. [`IStorePlatform.RelaunchRequested`](../OneGame.Stores/IStorePlatform.md#m-relaunchrequested) is then true and the game should exit at once. A `steam_appid.txt` file next to the executable turns this off, so development builds keep running.

**Type:** `bool`
