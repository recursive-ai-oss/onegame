[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# SteamPlatform static class

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public static class SteamPlatform
```

Entry point: connects to Steam, or falls back to a store that does nothing.

Call the store services from view code and host hooks (a button, a command's result), never from sim code: they do I/O and their answers differ between machines. Real calls need the Steam client running and the game's app id.

**Example**

```csharp
IStorePlatform store = SteamPlatform.Start(new SteamOptions { AppId = 480, RelaunchThroughSteam = true });
if (store.RelaunchRequested) { return; }              // Steam is starting the game again; exit before opening a window
// Once per frame on the game thread: store.Pump();
store.Achievements.Unlock("FIRST_WIN");               // StoreResult.Unavailable, not an exception, when Steam is not there
```

## Methods

| Member | Description |
| --- | --- |
| [`Start(SteamOptions?)`](#m-start-steamoptions) | Connects to Steam. |

### <a id="m-start-steamoptions"></a>Start(SteamOptions?)

```csharp
public static IStorePlatform Start(SteamOptions? options = null);
```

Connects to Steam. Never throws for an absent Steam: when the library or client is missing, or Steam refuses the app, the result is a [`NullStorePlatform`](../OneGame.Stores/NullStorePlatform.md) whose [`IStorePlatform.UnavailableReason`](../OneGame.Stores/IStorePlatform.md#m-unavailablereason) says why.

**Parameters**

- `options` ([`SteamOptions`](SteamOptions.md)?): Connection settings, or null for the defaults.

**Returns:** [`IStorePlatform`](../OneGame.Stores/IStorePlatform.md)
