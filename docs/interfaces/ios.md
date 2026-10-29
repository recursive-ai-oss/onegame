# ios: the iOS backend, export and simulator harness

Owner: `src/OneGame.Platform/Ios` (the backend), `build/iOS.targets` and `build/IosEntry.cs` (the build), `src/OneGame.Cli/Verbs/ExportVerb.Ios.cs` and `IosVerb.cs` (export and simulator commands). Events and window behaviour are in `platform.md` ("Mobile"); the Metal surface is `SurfaceSource.MetalLayer` in `gpu.md`. The device (real iPhone) path is built but not run: see "What is verified where".

## What runs

A game is a NativeAOT **executable** (arm64 Mach-O, the runtime and the game in one file) inside an app bundle. Nothing ships but our code, the one* libraries, the runtime and Apple's own frameworks; no Xamarin, MAUI or iOS workload.

- `build/IosEntry.cs` is compiled into the game and made its entry point (`StartupObject`). It calls `IosAppHost.Run(args, game.Main)`. **A game therefore needs a class `Program` with a `Main(string[])` returning `int`** (`onegame new` makes one; top-level statements cannot be called).
- UIKit owns the main thread. `IosAppHost` runs `UIApplicationMain`; the **scene delegate** (`UIWindowSceneDelegate`, named in `Info.plist`) makes the window, a view controller and the game's view; once the scene has connected the game's `Main` runs on a thread of its own (16 MiB stack). That thread owns the `PlatformApp`. The classes (`OneGameAppDelegate`, `OneGameSceneDelegate`, `OneGameViewController`, `OneGameView`, `OneGameTextView`) are registered with the Objective-C runtime at start-up, with managed `UnmanagedCallersOnly` functions as their methods. Nothing uses reflection.
- Main-thread callbacks only queue events; `IosBackend.Pump` on the game thread turns them into platform events. The few calls that go the other way (keyboard on/off, clipboard, rotation requests) hop to the main thread with `dispatch_async_f` / `dispatch_sync_f`.
- The view is backed by a `CAMetalLayer` (`+layerClass`), handed to the GPU layer as `NativeWindowHandle(MetalLayer, 0, layer)`. The layer lives as long as the app, so there is no `SurfaceLost` on iOS.
- **Arguments** are the process arguments: `xcrun simctl launch <device> <bundle id> --headless --frames 3`. Output goes to the console and to `Documents/onegame-stdout.txt` in the app's data folder; when `Main` returns its exit code is written to `Documents/onegame-exit.txt` and the process exits. That marker is how `onegame ios run` knows a run ended. The working directory is `Documents`; relative output paths (`artifacts/`) land there.

## Window, scale, insets

`Window.Width/Height` are drawable pixels (`bounds * contentScaleFactor`), `Scale` is the points-to-pixels factor, so one logical unit is one **point**. `SafeArea` is `safeAreaInsets` in pixels and follows rotation; `WindowResized`, `WindowScaleChanged` and `SafeAreaChanged` come from `layoutSubviews` and `safeAreaInsetsDidChange`. The status bar and home indicator are hidden and the screen edges defer system gestures, so a swipe from an edge needs a second swipe (games own the screen). Orientations come from `Info.plist` (`--orientation`); rotation is the system's: the view re-lays out and the game rebuilds its swapchain on `WindowResized`.

## Lifecycle

| UIKit scene callback | Platform event | Notes |
|---|---|---|
| `sceneWillEnterForeground` | `AppLifecycle(Resumed)` | `Window.IsMinimized` false |
| `sceneDidBecomeActive` / `sceneWillResignActive` | `WindowFocusChanged(true/false)` | releases held keys and buttons |
| `sceneDidEnterBackground` | `AppLifecycle(Paused)` | `IsMinimized` true. The callback **waits (at most 1 s) until the game thread has handled the event twice**, so the game has stopped submitting frames before the system stops the GPU |
| `applicationDidReceiveMemoryWarning` | `AppLifecycle(LowMemory)` | |

## Input

- **Touch:** `touchesBegan/Moved/Ended/Cancelled` become `TouchDown/Move/Up/Cancel` with the lowest free finger number as `TouchId`, positions in pixels, `Pressure` from `force / maximumPossibleForce` (1 where the device has no force). The shared pointer emulation in `platform.md` applies unchanged.
- **Hardware keyboard:** `pressesBegan/Ended` (UIPress, UIKey). The key is the USB HID usage (`IosKeys`), the modifiers `UIKeyModifierFlags`; typing keys also post `TextInput` with `UIKey.characters`. The game's view is first responder, which does not raise the soft keyboard.
- **Soft keyboard and text:** `window.SetTextInput(true, ...)` makes the hidden `OneGameTextView` first responder. It implements `UIKeyInput` and `UITextInput`: `insertText:` posts `TextInput` (a `\n` posts an Enter key press), `deleteBackward` posts Backspace, `setMarkedText:selectedRange:` posts `TextComposition` (text and cursor range), `unmarkText` ends the composition and commits it as `TextInput`. The text object's "document" is just the marked text, so positions and ranges (UITextPosition/UITextRange subclasses) index into it. Autocorrect, spell checking, smart quotes and capitalisation are off: a game field wants what was typed. `caretRectForPosition:` answers with the rectangle given to `SetTextInput`, so candidate lists sit beside the caret.
- **Gamepads:** the GameController framework, polled on the game thread each pump (`GCController.controllers`, extended profile). A south/east/west/north = A/B/X/Y, shoulders, menu = Start, options = Back, home = Guide, thumbstick clicks, the d-pad; sticks are y-inverted to the platform's down-positive standard and use `StickDeadZone`; triggers `TriggerDeadZone`.
- **Clipboard:** `UIPasteboard.general` string. iOS shows its own paste confirmation when another app's content is read.

## Export

```
onegame export --target ios-simulator --dev --json     # ad-hoc signed, runs in the Simulator
onegame export --target ios --json                     # arm64 device build, unsigned
```

Options: `--dev` (live control and scenario options; without it they exit 5 `no_dev_tools`, like a desktop release), `--bundle-id` (default `dev.onegame.<assembly>`; the project may set `<IosBundleId>`), `--label` (`<IosLabel>`), `--orientation all|landscape|portrait` (`<IosOrientation>`, default `all`), `--symbols <dir>` (keep the dSYM), `--out`, `--project`. The result (`IosExportResult`) lists the bundle's files with sizes and hashes, `signing` (`ad-hoc` or `unsigned`), `signingNeeds` and the entitlements file.

**Needs** a Mac with Xcode (`xcrun` finding the `iphonesimulator` or `iphoneos` SDK) and arm64 (Apple silicon) for the simulator. All are build-time tools; none ships. The .NET SDK lists iOS as a NativeAOT target only with the iOS workload, so `build/iOS.targets` adds `ios-arm64` and `iossimulator-arm64` to the compiler pack's identifiers; the compiler (`runtime.osx-arm64.Microsoft.DotNet.ILCompiler`) and the runtime pack (`Microsoft.NETCore.App.Runtime.NativeAOT.<rid>`) are ordinary NuGet packages and the linker is Xcode's. No workload is installed.

The bundle is `<Name>.app/` with the executable, `Info.plist` and `PkgInfo`, and nothing else: art, fonts and shaders are inside the executable. `Info.plist` sets `UILaunchScreen` to an empty dictionary (without it iOS runs the app letter-boxed), the scene manifest naming `OneGameSceneDelegate`, `UIRequiresFullScreen`, the hidden status bar, `UIRequiredDeviceCapabilities` (`arm64`, `metal`), the orientations and `MinimumOSVersion` 15.0; a `--dev` build adds `NSLocalNetworkUsageDescription` and `NSAllowsLocalNetworking` for the live listener. The Solitaire dev build is 12.3 MB (simulator) and the release build for a device 7.4 MB.

### Signing for a device (the owner's step)

The device bundle is **unsigned**: iOS will not run it until it is signed. What that needs, also printed as `signingNeeds`:

1. An Apple Developer account and a signing certificate (Apple Development for a test device, Apple Distribution for the store) in the Mac's keychain.
2. A provisioning profile for the bundle id that lists the device; copy it to `<Name>.app/embedded.mobileprovision`. If the id is taken or belongs to another team, export again with `--bundle-id`.
3. The entitlements file written next to the bundle (`<Name>.entitlements`, with `application-identifier`, `com.apple.developer.team-identifier` and `get-task-allow`): replace `TEAMID` with the profile's team identifier.
4. `codesign --force --sign "<identity>" --entitlements <Name>.entitlements --timestamp=none <Name>.app`
5. `xcrun devicectl device install app --device <device id> <Name>.app`

An App Store package (an `.ipa`, a distribution profile, an upload) is not built here.

## Simulator commands

All print JSON with `--json` and run on a Mac with Xcode and an iOS runtime (`xcodebuild -downloadPlatform iOS`). `--device` takes a simulator's identifier or name (or `ONEGAME_IOS_DEVICE`); the default is the booted one, else the first available, which the command boots.

| Command | Does |
|---|---|
| `onegame ios devices` | lists simulators with state and runtime |
| `onegame ios install <app>` | boots the device if needed, installs, prints the bundle id |
| `onegame ios run --bundle B [--app F] [--timeout S] [--push local[=dest]] [--pull remote[=local]] -- <game options>` | stops the app, clears the last run's output, copies files into its `Documents`, launches it with the options, waits for the exit marker, returns exit code, the game's last JSON line (`result`), all its output and the files pulled from `Documents/<remote>`. A process that vanishes without a marker is `crashed` and the newest crash report's head is in `output` |
| `onegame ios test [--scenario dir\|file] [--windowed] [--app F]` | `run` with the scenario folder in `Documents/scenarios` and `--headless --scenario ... --json`; artifacts come back to `./artifacts/artifacts` |
| `onegame ios live [--port N] [-- game options]` | starts the dev build with live control and waits until the port accepts; prints `tcp:127.0.0.1:<port>` for `onegame mcp --socket` (the simulator shares the Mac's network) |
| `onegame ios background` / `foreground` | opens Settings over the app / brings the app back, as the home button and the app switcher do |
| `onegame ios screenshot --out F` | `simctl io screenshot` |
| `onegame ios stop`, `logs`, `shutdown` | terminate the app, print `onegame-stdout.txt`, shut the device down |

The control operation **`platform_native`** reaches what only this backend has: `describe` (window, scale, safe area, orientation, application state, keyboard up, gamepads, pause/resume counts), `orientation` (`portrait`, `landscape-left`, `landscape-right`, `landscape`, `all`: asks the system to rotate the scene; only orientations in `Info.plist` can be reached) and `text_insert`, `text_mark`, `text_unmark`, `text_delete`, which call the text object's own `insertText:`, `setMarkedText:selectedRange:` and the like through the Objective-C runtime, on the main thread, the way the keyboard does.

## Runtime libraries on iOS NativeAOT

Checked in the simulator with a throwaway NativeAOT program built on the engine's own `OneGame.Net`: SHA-256, SHA-1, HMAC-SHA256, `RandomNumberGenerator`, AES-GCM and ECDSA work (the runtime links Apple's Security and CryptoKit frameworks, so there is no Android-style missing crypto library); files and `File.SetUnixFileMode`, threads with a chosen stack size, tasks, TCP and UDP loopback (IPv4 and IPv6), the engine's reliable-UDP handshake (HMAC cookie) and WebSocket handshake (SHA-1) all pass. `Process.Start`, `AssemblyLoadContext` plugins and `System.Reflection.Emit` do not exist on iOS and are not used by games (the plugin host is a server feature). Globalization is invariant. The app can write only below its data folder (`Documents`, `Library`, `tmp`): `HOME` and the working directory are inside it.

## What is verified where

On the iOS 27.1 Simulator (arm64, device "iPhone Duo", 466x678 points at scale 1; Xcode 27.1, macOS 27, .NET SDK 10.0.400):

- **Scenarios:** `onegame ios test --scenario scenarios --json` ran Solitaire's 14 of 14 headless at 1.0.0 (15 scenarios since; not re-run), with the same state hashes as `onegame test` on Linux (lavapipe), scenario by scenario. The seven capture scenarios compare 14 captures with the references (drawn by lavapipe) at the scenario files' own tolerance (2 per channel, 0.1 % of pixels): at most 358 pixels differ by more than 2 in any frame (0.04 %), with a largest channel difference of 9 on Metal.
- **Windowed:** `onegame ios live` starts the dev build with a real UIKit window; over MCP, `capture_frame presented=true` returns the frame the window shows (Solitaire's menu and a table, looked at); `platform_native describe` reports size, scale, safe area, orientation, application state.
- **Touch:** `ui_tap` and `input_touch` (through the platform event queue, the same path real touches take after the view's callbacks) draw a card and play it onto a column (`Moves` 0, 1, 2), and a second finger down and up leaves `Moves` at 2. The UIKit touch handler itself (`touchesBegan:`) was not driven by a real finger: `simctl` cannot inject touches.
- **Text:** with `AndroidTextGame`'s text field tapped (`textInputActive` true, the text object first responder), `text_insert "héllo"` reaches the field and the label, `text_mark` shows a composition, `text_unmark` commits it, `text_delete` removes one character, a `\n` is an Enter key.
- **Rotation:** `orientation` to landscape-left, landscape-right and back to portrait changes the window between 466x678 and 678x466, swaps the safe area's side, and presents frames at each size (presented captures looked at).
- **Background and foreground:** `onegame ios background` / `foreground` pause and resume the game; the process is suspended by the system in between, and the table state is the same afterwards.
- **Audio:** `AudioService` plays through an Audio Unit (RemoteIO) behind an `AVAudioSession` (`Ambient`); the design is in `audio.md`. On the simulator, Solitaire's card sounds reach the unit: the `audio` operation's `device` section shows `backend: audiounit`, `deviceState: started`, `framesPlayed` equal to the frames the unit asked for, `peakPlayed` 0.0768 after a sound (0 before) and `underruns: 0`, with the render callback allocating 0 bytes. The scene lifecycle (`sceneDidEnterBackground`/`WillEnterForeground`) suspends and resumes the output (`suspensions: 1`, the mixer clock held while away). `audio {"interrupt":"begin"}` posts `AVAudioSessionInterruptionNotification`: `deviceState` becomes `interrupted`, the mixer clock and the frames played stand still, and `{"interrupt":"end"}` returns it to `started` where it left off.

Run on a device: an iPad (6th generation, iPadOS 17.7) at 1.0.0, where Solitaire, Bricks and Lanterns gave the desktop's state hashes, a Lanterns client on the local network agreed with its island at zero drift, and the audio counters showed frames played. Not verified: an iPhone; the real soft keyboard (the simulator was driven headless, no keyboard UI; autocorrect, the input delegate and the candidate bar were not exercised); a hardware keyboard and gamepads (code written against the documentation; the GameController list was empty); `touchesBegan:` with a finger; display cut-outs on real phones; a real interruption (a call or Siri; the simulator was driven with the same notification posted from the harness), a Bluetooth or headphone route change, the silent switch; store bindings.
