# steam — store services and the Steamworks binding

`OneGame.Steam` is onegame's own binding to Steam's flat C API (`SteamAPI_*` exports of Valve's redistributable), written from Valve's public API reference. In front of it sit store-neutral services in the `OneGame.Stores` namespace (same assembly for now), so game code never mentions Steam and a later StoreKit or Play Billing binding can offer the same interfaces. All `unsafe` and `LibraryImport` code is in `OneGame.Steam/Native/`.

## Using it from a game

```csharp
using IStorePlatform store = SteamPlatform.Start(new SteamOptions { AppId = 480, RelaunchThroughSteam = true });
if (store.RelaunchRequested) return;          // started outside Steam; Steam is starting the game again

// every frame, same thread:
store.Pump();

store.Achievements.Unlock("ACH_WIN");          // StoreResult.Ok / Unavailable / Rejected / InvalidArgument …
store.Stats.SetInt("wins", 12); store.Stats.Commit();
store.Cloud.Write("saves/slot1.bin", bytes);   // store.Cloud.Read("…", out var data, maxBytes: 1 << 20)
store.Presence.Update("status", "In the garden");
store.Overlay.ActiveChanged += active => paused = active;
```

- `IStorePlatform` has `Identity`, `Achievements`, `Stats`, `Cloud`, `Presence`, `Overlay`, `Ownership` and `Environment` (language, `IsHandheld` for Steam Deck, `IsCouchMode` for Big Picture). They are never null.
- `SteamPlatform.Start` never throws for an absent Steam. If the library is missing, no client is running, or Steam refuses the app, it returns a `NullStorePlatform`: `IsAvailable` is false, `UnavailableReason` says why (log it), reads return empty values, changes return `StoreResult.Unavailable`. The game runs unchanged.
- Failures are values (`StoreResult`), not exceptions. Arguments that Steam documents as limited (names 128 bytes, cloud paths 260, presence keys 64 / values 256, files 100 MiB) are checked before any call. `Cloud.Read` checks the file size against `maxBytes` before allocating.
- Events (`Achievements.Stored`, `Stats.Committed`, `Overlay.ActiveChanged`) and async results are raised **only from `Pump()`**. The binding starts no threads; it uses the library's manual dispatch (`SteamAPI_ManualDispatch_*`), so the game loop decides when handlers run.
- Steam-specific: cast to `SteamStorePlatform` for `Callbacks.Subscribe<T>(handler)` and `Callbacks.Track<T>(call, handler)` (typed callbacks and call results; `RequestNumberOfCurrentPlayers` is the worked example).

## What sits next to the executable

| Platform | File | Notes |
|---|---|---|
| Windows | `steam_api64.dll` | from the Steamworks SDK `redistributable_bin/win64` |
| Linux (incl. Steam Deck) | `libsteam_api.so` | from `redistributable_bin/linux64` |
| macOS | `libsteam_api.dylib` | universal (x64 + arm64) |

The library is Valve's platform binary: it is copied in by the export step from the SDK the developer downloaded, never committed to this repo and never built by it. Lookup order: `ONEGAME_STEAM_LIBRARY` (full file path), the executable's directory, the current directory, `ONEGAME_STEAM_REDIST/<win-x64|linux-x64|osx>/<file>` (development and tests), then the OS library path. Only 64-bit targets are supported.

## App id in development

Steam learns the app id from how the game was launched. When run directly (debugger, `dotnet run`), put `steam_appid.txt` containing just the id (Valve's test app is `480`) in the **current working directory**, with the Steam client running and signed in. Do not ship that file: with it present, `RestartAppIfNecessary` returns false and the game would run unlaunched-by-Steam. Set `RelaunchThroughSteam = true` with `AppId` in release builds only.

## Behaviour when Steam is absent

| Situation | Result |
|---|---|
| library file not found / wrong architecture / too old (missing export) | `NullStorePlatform`, reason names the file and where it looked |
| client not running | `NullStorePlatform`, reason `NoSteamClient: Cannot create IPC pipe to Steam client process. Steam is probably not running.` |
| app id unknown, no licence | `NullStorePlatform`, reason `FailedGeneric: …` with Steam's message |
| client older than the library | `NullStorePlatform`, reason `VersionMismatch: …` |
| started outside Steam with `RelaunchThroughSteam` | `NullStorePlatform`, `RelaunchRequested` true |

## Struct packing

Callback structs are packed by the C++ headers at 4 bytes on Linux/macOS and 8 on Windows. That moves a 64-bit field following an odd count of 32-bit ones (`UserStatsReceived` is 20 bytes on Linux, 24 on Windows). Each struct declares its fields once; `SteamLayout.Compute` derives offsets and size for either packing, `SteamLayout.Current` picks the one for the running OS, and decoders reject data shorter than the struct.

## Not covered yet

Networking, matchmaking, lobbies, Workshop/UGC, leaderboards, and Steam Input (action sets, glyphs). The Steam Input accessor `SteamAPI_SteamInput_v006` is exported by the library but not bound. The reference pages do not state callback ids or the packing rule. A run against a signed-in Linux client with Valve's test app 480 confirmed them on Linux:
- `UserStatsReceived` decoded with game id 480, result OK and the signed-in user's SteamID;
- `UserStatsStored`, `UserAchievementStored` and the `NumberOfCurrentPlayers` call result arrived and decoded;
- cloud write/read/list/delete and rich presence worked.

Still unconfirmed:
- the 8-byte Windows packing (no Windows client run yet);
- `GameOverlayActivated` (the overlay is off for a game not launched through Steam).
