[API reference](../index.md) › OneGame.Steam

# OneGame.Steam

Steamworks binding and the store-neutral services (achievements, stats, cloud saves, presence, overlay, ownership) that sit in front of it.

**Package:** `RecursiveAI.OneGame.Steam`

```sh
dotnet add package RecursiveAI.OneGame.Steam
```

## OneGame.Steam

| Type | Kind | Description |
| --- | --- | --- |
| [`GameOverlayActivated`](OneGame.Steam/GameOverlayActivated.md) | struct | The overlay opened or closed. |
| [`ISteamCallbackData<TSelf>`](OneGame.Steam/ISteamCallbackData-1.md) | interface | A callback or call-result struct Steam can deliver, decoded from its raw bytes. |
| [`NumberOfCurrentPlayers`](OneGame.Steam/NumberOfCurrentPlayers.md) | struct | Result of `GetNumberOfCurrentPlayers`. |
| [`SteamApiCallCompleted`](OneGame.Steam/SteamApiCallCompleted.md) | struct | A previously started async call finished. |
| [`SteamCallbacks`](OneGame.Steam/SteamCallbacks.md) | class | Delivers Steam's callbacks and async call results to typed handlers. |
| [`SteamField`](OneGame.Steam/SteamField.md) | enum | Primitive kinds a callback struct is made of. |
| [`SteamLayout`](OneGame.Steam/SteamLayout.md) | static class | The packing in effect on this operating system. |
| [`SteamModule`](OneGame.Steam/SteamModule.md) | static class | Identifies the Steam assembly; a placeholder until its first feature lands. |
| [`SteamOptions`](OneGame.Steam/SteamOptions.md) | class | Settings for connecting to Steam. |
| [`SteamPacking`](OneGame.Steam/SteamPacking.md) | enum | How the Steam library packs the callback structs it hands out. |
| [`SteamPlatform`](OneGame.Steam/SteamPlatform.md) | static class | Entry point: connects to Steam, or falls back to a store that does nothing. |
| [`SteamShutdown`](OneGame.Steam/SteamShutdown.md) | struct | Steam wants the game to shut down (the client is exiting). |
| [`SteamStorePlatform`](OneGame.Steam/SteamStorePlatform.md) | class | The connected Steam store. |
| [`UserAchievementStored`](OneGame.Steam/UserAchievementStored.md) | struct | An achievement was stored, or its progress was shown. |
| [`UserStatsReceived`](OneGame.Steam/UserStatsReceived.md) | struct | The latest stats and achievements of a user arrived. |
| [`UserStatsStored`](OneGame.Steam/UserStatsStored.md) | struct | Stats were (or were not) stored on the server. |

## OneGame.Steam.Input

| Type | Kind | Description |
| --- | --- | --- |
| [`ISteamInputDriver`](OneGame.Steam.Input/ISteamInputDriver.md) | interface | How the harness plays the part of a pad when no Steam is behind Steam Input. |
| [`SteamActionChange`](OneGame.Steam.Input/SteamActionChange.md) | struct | One change of an action's state, as [`SteamInputService.ActionChanged`](OneGame.Steam.Input/SteamInputService.md#m-actionchanged) reports it. |
| [`SteamActionKind`](OneGame.Steam.Input/SteamActionKind.md) | enum | The kind of a declared action. |
| [`SteamActionSource`](OneGame.Steam.Input/SteamActionSource.md) | class | Feeds Steam Input into the engine's [`ActionMap`](../OneGame.Platform/OneGame.Platform/ActionMap.md): an action of the map and a Steam action of the same name and kind (button, vector, one-axis) are one action, so game code reads `actions.Pressed("jump")` whether the player uses a keyboard, a pad or a Steam controller configured in Steam's own binding screen. |
| [`SteamActionValue`](OneGame.Steam.Input/SteamActionValue.md) | record | An action value, for the report. |
| [`SteamController`](OneGame.Steam.Input/SteamController.md) | record | A controller Steam Input reports. |
| [`SteamControllerInfo`](OneGame.Steam.Input/SteamControllerInfo.md) | record | A controller, for the report. |
| [`SteamDigitalAction`](OneGame.Steam.Input/SteamDigitalAction.md) | record | One digital action: a button that is down or up. |
| [`SteamGlyphSize`](OneGame.Steam.Input/SteamGlyphSize.md) | enum | Glyph picture sizes Steam offers. |
| [`SteamInputActions`](OneGame.Steam.Input/SteamInputActions.md) | class | What a game declares to Steam Input: action sets with their actions. |
| [`SteamInputBridge`](OneGame.Steam.Input/SteamInputBridge.md) | class | Joins [`SteamInputService`](OneGame.Steam.Input/SteamInputService.md) to the engine's input path so game code reads one path whether Steam is on or off. |
| [`SteamInputReport`](OneGame.Steam.Input/SteamInputReport.md) | record | What the harness can read about Steam Input. |
| [`SteamInputService`](OneGame.Steam.Input/SteamInputService.md) | class | Steam Input for a game: the declared action sets, the active set per screen, digital and analog action state, controller connect and disconnect, and the glyphs of the buttons the player actually has. |
| [`SteamInputSet`](OneGame.Steam.Input/SteamInputSet.md) | class | An action set: the actions that are live together on one kind of screen (a map, a trade window, a menu). |
| [`SteamInputSetInfo`](OneGame.Steam.Input/SteamInputSetInfo.md) | record | A declared set, for the report. |
| [`SteamKeyboardInfo`](OneGame.Steam.Input/SteamKeyboardInfo.md) | record | The on-screen keyboard, for the report. |
| [`SteamOrigins`](OneGame.Steam.Input/SteamOrigins.md) | static class | Maps Steam's action origins to the kit's gamepad glyph names. |
| [`SteamStick`](OneGame.Steam.Input/SteamStick.md) | enum | Which stick of the engine's virtual gamepad a two-axis action drives. |
| [`SteamStickAction`](OneGame.Steam.Input/SteamStickAction.md) | record | One analog action with two axes. |
| [`SteamStickMode`](OneGame.Steam.Input/SteamStickMode.md) | enum | How Steam interprets a two-axis action. |
| [`SteamTriggerAction`](OneGame.Steam.Input/SteamTriggerAction.md) | record | One analog action with a single axis, such as a trigger. |

## OneGame.Steam.Native

| Type | Kind | Description |
| --- | --- | --- |
| [`SteamInitStatus`](OneGame.Steam.Native/SteamInitStatus.md) | enum | Result of the library's init call. |

## OneGame.Stores

| Type | Kind | Description |
| --- | --- | --- |
| [`CloudFile`](OneGame.Stores/CloudFile.md) | struct | One cloud file. |
| [`CloudQuota`](OneGame.Stores/CloudQuota.md) | struct | Cloud storage quota in bytes. |
| [`IAchievements`](OneGame.Stores/IAchievements.md) | interface | Achievements, named by the id configured in the store's partner site. |
| [`ICloudSaves`](OneGame.Stores/ICloudSaves.md) | interface | Cloud-synced save files. |
| [`IOverlay`](OneGame.Stores/IOverlay.md) | interface | The store's in-game overlay. |
| [`IOwnership`](OneGame.Stores/IOwnership.md) | interface | Ownership of the app and its DLC. |
| [`IRichPresence`](OneGame.Stores/IRichPresence.md) | interface | Rich presence: key/value text friends see next to the player's name. |
| [`IStats`](OneGame.Stores/IStats.md) | interface | Numeric stats, named by the id configured in the store's partner site. |
| [`IStoreEnvironment`](OneGame.Stores/IStoreEnvironment.md) | interface | Language and device facts reported by the store. |
| [`IStoreIdentity`](OneGame.Stores/IStoreIdentity.md) | interface | The signed-in player. |
| [`IStorePlatform`](OneGame.Stores/IStorePlatform.md) | interface | A connected store (Steam today; others later) and the services it provides. |
| [`NullStorePlatform`](OneGame.Stores/NullStorePlatform.md) | class | The store used when none is connected: every service is present, reads return empty values and changes return [`StoreResult.Unavailable`](OneGame.Stores/StoreResult.md#m-unavailable). |
| [`OverlayPage`](OneGame.Stores/OverlayPage.md) | enum | Which overlay page to open. |
| [`StoreResult`](OneGame.Stores/StoreResult.md) | enum | Outcome of a store operation that changes something. |
