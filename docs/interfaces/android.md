# android: the Android backend, export and device harness

Owner: `src/OneGame.Platform/Android` (the backend), `build/Android.targets` and `build/AndroidEntry.cs` (the build), `src/OneGame.Cli/Verbs/ExportVerb.Android.cs` and `AndroidVerb.cs` (export and device commands). Events and window behaviour are in `platform.md` ("Mobile"); the Vulkan surface is `SurfaceSource.Android` in `gpu.md`.

## What runs

A game is a NativeAOT **shared library** that the platform's own `android.app.NativeActivity` loads. The app has no Java or Kotlin (`hasCode="false"`) unless the game sells through Google Play (`AndroidPlayBilling`, see `store.md`), and nothing in it but our code, the one* libraries and what NativeAOT emits (the Solitaire dev build is about 12 MB per ABI, 6 MB zipped; no runtime files travel with it).

- The library exports `ANativeActivity_onCreate` (`build/AndroidEntry.cs`, compiled into the game). It calls `AndroidActivityHost.Start(activity, args => Program.Main(args))`. **A game therefore needs a class `Program` with a `Main(string[])` returning `int`** (`onegame new` makes one; top-level statements cannot be called).
- The activity's callbacks run on the system's main thread and only queue commands. `Main` runs on its own thread, which owns the `PlatformApp`, reads the input queue (`AInputQueue` attached to that thread's `ALooper`) and applies the commands in `PumpEvents`. The one place the main thread waits is `onNativeWindowDestroyed`: it blocks (at most 1 s) until the game thread has handled `SurfaceLost`, so the swapchain is gone before the window is.
- Java is reached only through JNI function-table calls (`AndroidJni`): the launch intent's string extra, the window insets (`getRootWindowInsets`), and `KeyCharacterMap.get` for the character of a key. Everything else is the NDK's C API (`libandroid`, `liblog`).
- Arguments come from the launch intent: `adb shell am start -n <package>/android.app.NativeActivity -e onegame.args "--headless --frames 3"`. Standard output and error go to logcat (tag `onegame`) and to `files/onegame-stdout.txt` in the app's data; when `Main` returns its exit code is written to `files/onegame-exit.txt` and the activity finishes. That marker is how `onegame android run` knows a run ended.
- `@files` in an argument stands for the app's files folder in the CLI commands below.

## Export

```
onegame export --target android --json                      # arm64-v8a and x86_64, Release, no development tools
onegame export --target android --abi x86_64 --dev --json   # a build for an emulator, with live control
```

Options: `--abi arm64-v8a,x86_64`, `--dev`, `--package com.example.game` (default `dev.onegame.<assembly>`), `--label`, `--orientation` (an `android:screenOrientation` value, default `unspecified`), `--sdk <dir>`, `--symbols <dir>` (keep the debug files for crash analysis), `--out`, `--project`, and the release options below. The result (`AndroidExportResult`) has the APK path, size, SHA-256, package, ABIs and each library's hash, and for releases the version, target SDK, signing certificate and bundle.

### Release: bundle and signing

```
export ONEGAME_ANDROID_STORE_PASSWORD=...                 # and ONEGAME_ANDROID_KEY_PASSWORD if the key has its own
onegame export --target android --aab --keystore upload.p12 --key-alias upload --version-code 42 --json
```

This writes `<Game>-<version>-android.aab` (the Android App Bundle Google Play takes) and `<Game>-<version>-android.apk` (the same build signed with the same key, for side-loading and tests).

| Option | Property | Meaning |
|---|---|---|
| `--aab` | | also write the bundle |
| `--keystore <file>` | env `ONEGAME_ANDROID_KEYSTORE` | the game's own keystore, PKCS12 or JKS |
| `--key-alias <name>` | env `ONEGAME_ANDROID_KEY_ALIAS` | the key inside it |
| | env `ONEGAME_ANDROID_STORE_PASSWORD`, `ONEGAME_ANDROID_KEY_PASSWORD` | passwords; the key password defaults to the store password. They are never taken from the command line and are handed to `apksigner` and `jarsigner` through the environment, so they appear in no argument list, log or result |
| `--debug-key` | | sign with the debug key on purpose (the export warns `debug_signed`: Play refuses it) |
| `--version-code <n>` | `AndroidVersionCode` | 1 to 2100000000; default derived from the version (1.2.3 is 10203). Play refuses a repeated code, so pass a new one for every upload |
| `--version-name <text>` | `AndroidVersionName` | default: the project's `Version` |
| `--target-sdk <level>` | `AndroidTargetSdk` | default 36 |
| `--bundletool <jar>` | env `ONEGAME_BUNDLETOOL` | Google's bundletool; see below |

A release export (no `--dev`) with no keystore and no `--debug-key` exits 2 `keystore_required`. A `--dev` export keeps the debug key unless a keystore is given. Play App Signing re-signs what Play serves, so the game's upload key is all the bundle needs.

The bundle has one base module with protobuf resources, a split per ABI, and native libraries stored uncompressed and 16 KiB aligned. It is built with Google's `bundletool` (what Play itself uses), found as `--bundletool`, `ONEGAME_BUNDLETOOL`, `BUNDLETOOL`, a `bundletool*.jar` in the SDK folder, or the pinned release the tool downloads once into its cache (checked against a SHA-256) and needs a JDK to run. It is a build tool; nothing from it ships. With `--symbols <dir>` and `--aab`, `<dir>/native-debug-symbols.zip` holds `<abi>/lib<name>.so.dbg` for Play's crash reports.

Result fields added for releases: `versionCode`, `versionName`, `targetSdk`, `signing` (`debug-key` or `release-key`), `signingCertSha256`, `bundle`, `bundleSize`, `bundleSha256`, `symbolsArchive`.

- **Needs** a Linux x86-64 host (the NDK's clang links the library), the Android SDK (`ANDROID_HOME`: build-tools, a platform, platform-tools) and an NDK (`ANDROID_NDK` or `$ANDROID_HOME/ndk/<newest>`), plus a JDK for `apksigner` and `keytool`. All of these are build-time tools; none ships.
- The .NET SDK does not list `linux-bionic` as a NativeAOT target, so `build/Android.targets` (imported by `build/Game.targets`) downloads the host compiler and the `Microsoft.NETCore.App.Runtime.NativeAOT.linux-bionic-*` pack at restore and points the compiler at them and at the NDK's clang and sysroot. A plain `dotnet publish -r linux-bionic-arm64` of a game that imports `Game.targets` does the same.
- A `--dev` (or `--debug-key`) export is signed with the Android debug key (`$ANDROID_USER_HOME/debug.keystore` or `~/.android/debug.keystore`, made with `keytool` when missing): installable on any device with USB debugging and on emulators, not accepted by a store. A release export is signed with the game's own key (see Release, above).
- `--dev` adds the INTERNET permission (the live listener is a loopback socket), `android:debuggable` (so `run-as` can read the app's files) and the development tools. Without it the app has neither, and `--live`, `--scenario` and friends exit 5 `no_dev_tools`, like a desktop release.
- **Back** stays a key: the activity sets `android:enableOnBackInvokedCallback="false"`. At targetSdk 36 the platform otherwise registers its own back callback, which finishes the activity in addition to delivering the key, so the game could not keep the app open. With it off, `KEYCODE_BACK` arrives on the input queue for the button and the gesture (gestural and three-button navigation) on API 26 through 36, and the game's `OnBack` decides (see `platform.md`, "Back"). This is the manifest opt-out Android documents for apps that are not ready for predictive back animations; a future Android that removes it would need a Java `OnBackInvokedCallback`, which means a `classes.dex` in every export.
- **Edge claims** map to `View.setSystemGestureExclusionRects` (API 29+; ignored below) on the activity's content view. The Java call must run on the main thread, which has its own `ALooper`: the game thread writes to a pipe that the main looper watches (`AndroidActivityHost`), so no Java class of ours is involved and `hasCode` stays false.
- **Safe area** comes from `WindowInsets.getInsets(statusBars | navigationBars | captionBar | mandatorySystemGestures | displayCutout)` (API 30+; older releases use `getSystemWindowInset*`). The deprecated getters report zero under the edge-to-edge window targetSdk 36 enforces, even though the cut-out and bars are there. Hidden bars (a fullscreen theme) are not counted, the cut-out and the system-gesture bands are.
- minSdk 26 (Vulkan 1.0 hardware), targetSdk 36 (`--target-sdk` or `AndroidTargetSdk` to change; below 36 warns because Play refuses it), `android.hardware.vulkan.level` required. Libraries are linked with 16 KiB page alignment: the export reads each library's loadable segments and warns `page_size_4k` if one is below 16384, and zip-aligns with `-P 16`. A game with `AndroidPlayBilling` also gets the Billing Library's own `com.google.android.play.billingclient.version` meta-data in the manifest, read from the library's manifest at export time (Play reads the library version from it).

## Device commands

All take `--serial` (or `ANDROID_SERIAL`), print JSON with `--json`, and need `adb` from the SDK.

| Command | Does |
|---|---|
| `onegame android devices` | lists devices with state, model and ABI |
| `onegame android install <apk>` | `adb install -r -t`; prints the package |
| `onegame android run --package P [--apk F] [--timeout S] [--push local[=dest]] [--pull remote[=local]] -- <game options>` | stops the app, clears the last run's output, pushes files into the app's `files/`, starts it with the options, waits for the exit marker, returns exit code, the game's last JSON line (`result`), all its output and the files pulled from `files/<remote>` |
| `onegame android test [--scenario dir\|file] [--windowed] [--apk F]` | `run` with the scenario folder pushed to `files/scenarios`, `--headless --scenario ... --artifacts @files/artifacts --json`, and the artifacts pulled to `./artifacts/artifacts`. The result is the scenario runner's own JSON, same as `onegame test --json` prints for the scenarios part. With `--windowed` the game opens its window and presents through Vulkan while the scenarios run |
| `onegame android live [--port 7878]` | starts a dev build with `--live tcp:7878 --realtime`, `adb forward tcp:0 tcp:7878`, prints the local endpoint `tcp:127.0.0.1:N`; then `onegame mcp --socket tcp:127.0.0.1:N` speaks MCP to the phone: state, input (including `input_touch`), `step`, captures, `app_lifecycle` |
| `onegame android stop` | `am force-stop`, drops forwards |
| `onegame android logs` | recent logcat lines of the game and crashes |

Pulled files are only written inside the folder asked for; names a device sends that climb out of it are skipped.

The live listener on a phone is a loopback TCP port in a debuggable app. Any app on the same device can connect to it, so use it on test devices only; release builds do not contain it.

## What is verified where

**Back and edge claims** (`tests/AndroidTextGame` logs every back, touch, safe-area and claim line as `probe:` in logcat; a file named `noclaim` in its files folder turns the claim off): x86_64 emulators at API 30, 34, 35 and 36 (Google APIs images at 35 and 36), targetSdk 35 and 36, and API 26 for the back key and the missing-API paths. `input keyevent KEYCODE_BACK`, a gestural-navigation swipe from the left and right edges, and the three-button bar's back each give one `BackRequested` with the activity still resumed; a third, unhandled back leaves the app. A right-edge swipe inside the claimed strip gives touch down, moves and up and no back; the same swipe outside it is back (plus a `TouchCancel` on API 30, 34 and 36; on API 35 the touches continue). Rotation re-lays the claim. Edge-to-edge safe area at API 36: top 160, bottom 84 px on a 1080x2400 phone with a cut-out, rotating to right 128 in landscape.

On an x86_64 emulator (API 35, Google APIs image, SwiftShader Vulkan, 1080x2400 at 420 dpi), with Solitaire and `tests/AndroidTextGame`:

- **Scenarios:** `onegame android test --package dev.onegame.solitaire --scenario scenarios --capture-tolerance 48 --capture-max-diff 0.001 --json` passed 14 of 14 when measured at 1.0.0, before the 15th scenario was added, with the same state hashes as a desktop headless run. The references were drawn by lavapipe; SwiftShader places anti-aliased edge pixels differently (without the override, 4 captures differ by at most 41 per channel in 0.2 to 1 % of pixels, all on card, text and glyph edges, nothing else). The override keeps the stricter 0.1 % pixel budget and loosens only the channel tolerance. `--windowed` runs the scenarios with a real window, but the display's own size replaces the scenario's `window:` size, so scenarios that tap by layout fail there: use it to check presenting, not to run suites.
- **Touch:** `adb shell input tap` on the stock pile draws a card (`Waste` goes from empty to one card, `Moves` 0 to 1); scenario `06-touch-only` passes on the device.
- **Lifecycle:** after `KEYCODE_HOME` the process lives, the tick and the presented-frame counter stop, and after `am start` the table state is equal to before and frames present again. Rotation (`adb emu rotate`, `fullSensor`): the window flips between 1080x2400 and 2400x1080, frames keep presenting on each new surface, state and text field contents are kept.
- **Keyboard:** tapping a kit text field shows the system keyboard (`mInputShown=true`); `adb shell input text` characters arrive as key events plus `TextInput` and reach the field. The platform's content view is made focusable when the window is created: without that the system logs `showSoftInput() ... is not served` and shows nothing.
- **Networking:** the Lanterns client (`samples/lanterns`, `AndroidInternet` set so the release manifest has INTERNET) runs on the emulator as a client of island servers on the computer that hosts the emulator, reaching it as `10.0.2.2`. `samples/lanterns/cluster/android-client.sh` makes the device one node of a process cluster: `onegame cluster test cluster/play-android.yaml --spec cluster-android.yaml --json` plays to tick 3600 with the Android bot on island A over UDP (100 ms latency, 2 % loss, from the app's own conditioner) and a desktop bot on island B over a WebSocket. The Android bot's view hash equals island A's at tick 3600, there are 0 drift reports on 5 nodes, 6 bridge arrivals, 21 lanterns collected and 8 swaps settled through the master. The same scenario with the Android bot over a WebSocket (`transport: ws`) passes too (5 arrivals, 19 collected, 11 swaps). The app needs the dev build only so `onegame android run` can read its output.
- **Audio:** the app opens AAudio (`onegame android live`, then the `audio` operation: `backend aaudio`, `deviceState started`, 0 underruns, 0 bytes allocated on the audio thread). After Solitaire's stock taps `peakPlayed` is 0.0768 and `framesPlayed` advances with the frames written. `KEYCODE_HOME` then `am start` gives `suspensions 1` and the mixer clock stops while in the background. The emulator's host sound device is absent, so this proves the stream and its read position, not audible output or route changes.
- **Crash reports:** `onegame export --symbols <dir>` keeps the `.dbg` file; `llvm-symbolizer -e <dir>/<abi>/<Game>.so.dbg <pc>` names the frames from a tombstone.

arm64: Solitaire and Lanterns run on an arm64 emulator (Apple silicon, API 35, software GPU): 14 of 14 and 8 of 8 scenarios with the desktop state hashes, the `audio` op reports `aaudio` with `peakPlayed` above 0 after a draw, and a Lanterns bot on it agrees with its island server at tick 3600 with no drift (`docs/devices.md`).

Run on a phone: a Pixel 8 Pro (Android 17, arm64, its own GPU driver) at 1.0.0, where Solitaire, Bricks and Lanterns gave the desktop's state hashes, a Lanterns client on the local network agreed with its island at zero drift, and the audio counters showed frames played; the platform detection fix was checked on it at 1.1.2, and audio output on OneAudio.Devices 1.3.0 at 1.5.2. Not verified: gamepads, a hardware keyboard, composed text (needs a Java input connection), display cut-out handling, Play-style 16 KiB-page devices, whether the sound is audible (counters only), and a headphone or Bluetooth route change. The runtime's hash and random providers need a system crypto library the app does not have, so `System.Security.Cryptography` must not be used in a game or in the engine's runtime paths that run on a phone. The control layer and `OneGame.Net` use the engine's own SHA-256, HMAC-SHA-256 and SHA-1 and read random bytes through `OneGame.Core.Entropy` (`/dev/urandom` here; see `transport.md`).
