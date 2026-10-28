[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# SteamStorePlatform class

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IStorePlatform`](../OneGame.Stores/IStorePlatform.md), [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class SteamStorePlatform : IStorePlatform, IDisposable
```

The connected Steam store. Also exposes the typed callback and call-result channel for Steam features the neutral services do not cover.

## Properties

| Member | Description |
| --- | --- |
| [`Achievements`](#m-achievements) | Achievements. |
| [`Callbacks`](#m-callbacks) | Typed Steam callbacks and call results, delivered from [`SteamStorePlatform.Pump()`](SteamStorePlatform.md#m-pump). |
| [`Cloud`](#m-cloud) | Cloud-synced save files. |
| [`Environment`](#m-environment) | Language and device facts the store knows. |
| [`Identity`](#m-identity) | The signed-in player. |
| [`IsAvailable`](#m-isavailable) | True when a store is connected and the services talk to it. |
| [`Name`](#m-name) | Short store name, such as `steam`, or `none` when no store is connected. |
| [`Overlay`](#m-overlay) | The store's in-game overlay. |
| [`Ownership`](#m-ownership) | Which app and DLC the player owns. |
| [`Presence`](#m-presence) | What the player is doing, shown to friends. |
| [`RelaunchRequested`](#m-relaunchrequested) | True when the store asked for the game to be started again through the store's own launcher. |
| [`Stats`](#m-stats) | Numeric stats. |
| [`UnavailableReason`](#m-unavailablereason) | Why the store is not connected, in plain words for logs. |

### <a id="m-achievements"></a>Achievements

```csharp
public IAchievements Achievements { get; }
```

Achievements.

**Type:** [`IAchievements`](../OneGame.Stores/IAchievements.md)

### <a id="m-callbacks"></a>Callbacks

```csharp
public SteamCallbacks Callbacks { get; }
```

Typed Steam callbacks and call results, delivered from [`SteamStorePlatform.Pump()`](SteamStorePlatform.md#m-pump).

**Type:** [`SteamCallbacks`](SteamCallbacks.md)

### <a id="m-cloud"></a>Cloud

```csharp
public ICloudSaves Cloud { get; }
```

Cloud-synced save files.

**Type:** [`ICloudSaves`](../OneGame.Stores/ICloudSaves.md)

### <a id="m-environment"></a>Environment

```csharp
public IStoreEnvironment Environment { get; }
```

Language and device facts the store knows.

**Type:** [`IStoreEnvironment`](../OneGame.Stores/IStoreEnvironment.md)

### <a id="m-identity"></a>Identity

```csharp
public IStoreIdentity Identity { get; }
```

The signed-in player.

**Type:** [`IStoreIdentity`](../OneGame.Stores/IStoreIdentity.md)

### <a id="m-isavailable"></a>IsAvailable

```csharp
public bool IsAvailable { get; }
```

True when a store is connected and the services talk to it.

**Type:** `bool`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

Short store name, such as `steam`, or `none` when no store is connected.

**Type:** `string`

### <a id="m-overlay"></a>Overlay

```csharp
public IOverlay Overlay { get; }
```

The store's in-game overlay.

**Type:** [`IOverlay`](../OneGame.Stores/IOverlay.md)

### <a id="m-ownership"></a>Ownership

```csharp
public IOwnership Ownership { get; }
```

Which app and DLC the player owns.

**Type:** [`IOwnership`](../OneGame.Stores/IOwnership.md)

### <a id="m-presence"></a>Presence

```csharp
public IRichPresence Presence { get; }
```

What the player is doing, shown to friends.

**Type:** [`IRichPresence`](../OneGame.Stores/IRichPresence.md)

### <a id="m-relaunchrequested"></a>RelaunchRequested

```csharp
public bool RelaunchRequested { get; }
```

True when the store asked for the game to be started again through the store's own launcher. The game should exit at once, before opening a window.

**Type:** `bool`

### <a id="m-stats"></a>Stats

```csharp
public IStats Stats { get; }
```

Numeric stats.

**Type:** [`IStats`](../OneGame.Stores/IStats.md)

### <a id="m-unavailablereason"></a>UnavailableReason

```csharp
public string? UnavailableReason { get; }
```

Why the store is not connected, in plain words for logs. Null when [`IStorePlatform.IsAvailable`](../OneGame.Stores/IStorePlatform.md#m-isavailable).

**Type:** `string?`

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) |  |
| [`Pump()`](#m-pump) | Delivers store events and async results. |
| [`RequestNumberOfCurrentPlayers(Action<NumberOfCurrentPlayers, bool>)`](#m-requestnumberofcurrentplayers-action-numberofcurrentplayers-bool) | Asks Steam how many players the game has now. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-pump"></a>Pump()

```csharp
public void Pump();
```

Delivers store events and async results. Call once per frame from the game loop on the same thread every time. No threads are started on the game's behalf. Cheap when nothing is pending.

### <a id="m-requestnumberofcurrentplayers-action-numberofcurrentplayers-bool"></a>RequestNumberOfCurrentPlayers(Action&lt;NumberOfCurrentPlayers, bool&gt;)

```csharp
public void RequestNumberOfCurrentPlayers(Action<NumberOfCurrentPlayers, bool> handler);
```

Asks Steam how many players the game has now. The handler runs from [`SteamStorePlatform.Pump()`](SteamStorePlatform.md#m-pump).

**Parameters**

- `handler` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)&lt;[`NumberOfCurrentPlayers`](NumberOfCurrentPlayers.md)`, bool>`)
