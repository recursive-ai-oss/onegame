# steam — store services and the Steamworks binding

`OneGame.Steam` is onegame's own binding to Steam's flat C API (`SteamAPI_*` exports of Valve's redistributable), written from Valve's public API reference. In front of it sit store-neutral services in the `OneGame.Stores` namespace (same assembly), so game code never mentions Steam. In-app purchases (StoreKit, Play Billing) are a separate API in `OneGame.Purchases` (`store.md`). All `unsafe` and `LibraryImport` code is in `OneGame.Steam/Native/`.

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

## Steam Input (`OneGame.Steam.Input`)

Bound from Valve's public ISteamInput and ISteamUtils pages (flat API: `SteamAPI_ISteamInput_*`, accessor `SteamAPI_SteamInput_v006`). A game declares its action sets once; the same declaration drives the code and writes the file Steam needs.

```csharp
var actions = new SteamInputActions { MajorRevision = 1 }
    .Set("Map", "Map screen", s => s
        .Stick("map_move", "Move", SteamStickMode.JoystickMove, SteamStick.Left)   // also drives the virtual pad's left stick
        .Button("map_select", "Select", GamepadButton.South)                       // also presses South
        .OnScreens("map"))                                                          // active while screen "map" is open
    .Set("Menu", "Menus", s => s.Button("menu_accept", "Accept", GamepadButton.South).OnScreens("menu"));

actions.WriteVdf("game_actions_480.vdf");            // or assert actions.Matches(File.ReadAllText(path), out var diff) in a test

using var input = SteamInputService.Start(store, actions);   // never null, never throws; disabled when Steam is absent
using var bridge = new SteamInputBridge(input, game.Platform, game.Ui, () => game.UiScale, glyphs);
// GameDefinition.OnEachFrame(game): bridge.Update(game.CurrentScreen);
```

- **The file.** `ToVdf()` writes Valve's in-game actions format (`"In Game Actions"` with `actions` per set: `StickPadGyro`, `AnalogTrigger`, `Button`; titles as `#Set_*`/`#Action_*` tokens; an English `localization` block from the titles; extra languages in `Localization`; `major_revision`/`minor_revision` when set). Output is deterministic. Name it `game_actions_<appid>.vdf` and put it where Steam reads it (`controller_config` in the Steam folder while developing; upload it with the app's Steam Input settings). Action names are unique across sets (Steam finds actions by name alone; the declaration rejects duplicates) and are letters, digits and underscores.
- **Action sets per screen.** `ActivateForScreen(screen)` (the bridge does it when the open screen changes) activates the set that lists the screen in `OnScreens`, else the first set. `ActivateSet(name)` returns `StoreResult` (`NotFound`, `Unavailable`, `Ok`). The set is re-asserted on controllers that connect later or that Steam resets. Actions outside the active set read as inactive and are reported released.
- **One input path.** The bridge makes each Steam controller a virtual gamepad on the platform layer. An action with a button target presses that button, a `Stick` action with a stick target moves it (Steam's Y is up-positive; the pad's Y is down-positive, so it is negated), a `Trigger` action moves its trigger. Menu navigation, `OnInput` and `InputState` see ordinary gamepad events, whether Steam is on or off. For actions with no target, poll `IsDown(action)`, `WasPressed(action)` and `GetAnalog(action, out x, out y)`, or subscribe to `ActionChanged`. `ControllerConnected`/`ControllerDisconnected` and `Controllers` (stable 1-based index, Steam input type name) report pads. `Update()` runs `ISteamInput::RunFrame` (Steam Input is started with explicit RunFrame) and must be called once per frame, after `store.Pump()`; the bridge's `Update` does it.
- **Glyphs.** `GetOrigins(action)` returns Steam's origin numbers, `GetGlyphPath(action, size)` Steam's own PNG for the player's controller style (like `GetGlyphForActionOrigin`), `GetGlyphName(action)` the kit's gamepad glyph name: the origin is translated to the Xbox One layout (`TranslateActionOrigin`) and mapped by `SteamOrigins.KitGlyphName` (origins 114 to 147). `BindGlyphs(inputGlyphs)` (the bridge does it when given the table) keeps each action's gamepad glyph on the player's real button via `InputGlyphs.SetGamepad`, leaving keyboard and mouse names alone; it re-checks twice a second, so rebinding in Steam updates the hints.
- **On-screen keyboard.** While a kit text input has focus, a controller is connected and the player is on a handheld (Deck) or last used the pad (`InputGlyphs.Device`), the bridge calls `ShowFloatingGamepadTextInput` with the field's box in window pixels and dismisses it on blur. Steam sends typed keys to the game as ordinary keyboard input, so text arrives through the normal text-input path; nothing is read back. `ShowKeyboard` returns `Failed` when Steam refuses. The dismissed callback is not bound (its id is not on the reference page); the bridge dismisses by focus.
- **Harness.** Control op `steam_input` reports `{enabled, reason, fake, activeSet, sets, controllers, values, keyboard, missing}` (`missing` lists declared actions Steam does not know, i.e. the uploaded file differs from the code). Scenario step `steam-input: {connect: 3 | disconnect: 1 | press: a | release: a | analog: move, x: 0, y: 1, controller: 1}` and expectation `steam-input: {set, controllers, down, up, keyboard}` (`docs/driving-a-game.md`). Driving is only possible on the stand-in controller, used when Steam is not running and the process has `ONEGAME_STEAM_INPUT=fake` (or `SteamInputService.StartFake` in tests); with real Steam the driving arguments fail with `not_fake`.
- **Off.** Without Steam, or with a library lacking the interface, the service is disabled (`IsEnabled` false, `UnavailableReason`), every call is a no-op and the platform gamepad path is untouched.

- **Handles arrive late.** Right after `Init`, Steam answers 0 for every set and action handle; it adopts the app's configuration about a second later (and again when a controller attaches or the player changes configuration). The service asks again for any unresolved name every 30 frames and immediately when a controller connects, so `Describe().Missing` empties by itself. A name that stays in `Missing` is really absent from the uploaded actions file. Handles are looked up by name alone and case-insensitively; whether an action reads as active depends on the set Steam has active on the controller.
- **What makes the client route a pad to the game.** Steam sends a controller to the app whose window has focus; otherwise it applies the desktop configuration and the game sees no controller (`GetConnectedControllers` stays empty, the client log shows the pad bound to its desktop app). Measured against a signed-in Linux client on a Wayland desktop: the game needs a **focused X11 window** (Steam is an X11 client, so a native Wayland window or no window is not seen), the app id from `steam_appid.txt` or a Steam launch, and `store.Pump()` plus `SteamInputService.Update()` running. Launched that way the controller was connected 1.5 s after start. Under a Wayland session set `PlatformOptions.Backend = PlatformBackendKind.X11` (runs through XWayland), or launch through Steam. Headless test processes get no controller from a real client; use the stand-in.
- **Verified against app 480 with a virtual Xbox 360 pad and a virtual PS4 pad (uinput)**, using the app's own action names (`ship_controls`: `Fire_Lasers`, `Forward_Thrust`, `Backward_Thrust`, `Turn_Left`, `Turn_Right`, `pause_menu` and the stick `Analog_Controls`; `menu_controls`: `menu_up|down|left|right|select|cancel`):
  - by-value `GetDigitalActionData` and `GetAnalogActionData` decode correctly: a button reads down then up; a full stick deflection reads 1.0, a half deflection 0.501;
  - **Y is up-positive**: stick pushed up gives `Y = +1` in `SteamActionChange`, and the bridge delivers `LeftY = -1`, which equals the platform's own evdev gamepad for the same input;
  - origins and glyphs work for both pads: Xbox 360 origin 153 is `a`, a PS4 pad reports input type 5 with origin 50, `GetGlyphName` gives `a` (translated to the Xbox layout) and `GetGlyphPath` gives Steam's PS4 cross PNG;
  - `ShowKeyboard` returned `Failed` on a desktop client (no on-screen keyboard there); success is a Steam Deck / Big Picture check.

Unverified against a live client: origin numbers for pads other than Xbox 360 and PS4, Steam Deck and Big Picture (on-screen keyboard shown, `IsHandheld`), real hardware, and the PS4 pad's button mapping through uinput (cross did not read as `menu_select`).

## Not covered yet

Networking, matchmaking, lobbies, Workshop/UGC, leaderboards, haptics and motion in Steam Input, action set layers. The reference pages do not state callback ids or the packing rule. A run against a signed-in Linux client with Valve's test app 480 confirmed them on Linux:
- `UserStatsReceived` decoded with game id 480, result OK and the signed-in user's SteamID;
- `UserStatsStored`, `UserAchievementStored` and the `NumberOfCurrentPlayers` call result arrived and decoded;
- cloud write/read/list/delete and rich presence worked.

Still unconfirmed:
- the 8-byte Windows packing (no Windows client run yet);
- `GameOverlayActivated` (the overlay is off for a game not launched through Steam).

## Detecting the Steam Deck

`store.Environment.IsHandheld` is the flat call `ISteamUtils::IsSteamRunningOnSteamDeck`, nothing more. Valve's reference marks that call **deprecated as of Steamworks SDK 1.65**, in favour of `IsRunningOnSteamHardware` (returns `ESteamHardwareType`: Deck 1, Machine 2, Frame 3) and `GetSteamHardwareDefaultConfig`. The redistributable the engine is built and tested with (`SteamUtils010`) has no export for the new calls, so the binding stays on the old one.

Checked on a Steam Deck (SteamOS 3.8, Desktop Mode, KDE Plasma) with app 480, from a plain program started over ssh and again as a user unit in the desktop session:

| Steam client | `IsHandheld` | `IsCouchMode` |
|---|---|---|
| started by the session with `-steamdeck` (the default) | true | false |
| restarted without `-steamdeck` | true | false |

So on a real Deck the call is true in Desktop Mode, and it does not depend on the client's `-steamdeck` flag; `IsCouchMode` (Big Picture) is a separate question and is false in Desktop Mode. A `false` seen elsewhere came from something other than the Deck being in Desktop Mode: another machine's client, or a newer redistributable where the deprecated call no longer answers. (Game Mode was not tried: it would have taken the Deck's session over.)

A game that must know whether it runs on a Deck should combine signals and not rely on one:

1. `store.Environment.IsHandheld`, when Steam is available.
2. The `SteamDeck=1` environment variable, which a client started with `-steamdeck` sets for the games it launches (a public report in Valve's client tracker shows it being forced on non-Deck hardware that runs the client with that flag, so it means "Deck-style client", not "Deck hardware").
3. Without Steam (a program started by hand, or a non-Steam build): `ID=steamos` and `VARIANT_ID=steamdeck` in `/etc/os-release`, or DMI `sys_vendor` `Valve` with `product_name` `Jupiter` (LCD) or `Galileo` (OLED; only Jupiter was checked here), read from `/sys/devices/virtual/dmi/id/`.

Treat "is a Deck" as a hint for defaults (glyphs, UI scale, quality); keep every feature reachable without it.
