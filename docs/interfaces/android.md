# android: the Android backend, export and device harness

Owner: `src/OneGame.Platform/Android` (the backend), `build/Android.targets` and `build/AndroidEntry.cs` (the build), `src/OneGame.Cli/Verbs/ExportVerb.Android.cs` and `AndroidVerb.cs` (export and device commands). Events and window behaviour are in `platform.md` ("Mobile"); the Vulkan surface is `SurfaceSource.Android` in `gpu.md`.

## What runs

A game is a NativeAOT **shared library** that the platform's own `android.app.NativeActivity` loads. The app has no Java or Kotlin (`hasCode="false"`), and nothing in it but our code, the one* libraries and what NativeAOT emits (the Solitaire dev build is about 12 MB per ABI, 6 MB zipped; no runtime files travel with it).

- The library exports `ANativeActivity_onCreate` (`build/AndroidEntry.cs`, compiled into the game). It calls `AndroidActivityHost.Start(activity, args => Program.Main(args))`. **A game therefore needs a class `Program` with a `Main(string[])` returning `int`** (`onegame new` makes one; top-level statements cannot be called).
- The activity's callbacks run on the system's main thread and only queue commands. `Main` runs on its own thread, which owns the `PlatformApp`, reads the input queue (`AInputQueue` attached to that thread's `ALooper`) and applies the commands in `PumpEvents`. The one place the main thread waits is `onNativeWindowDestroyed`: it blocks (at most 1 s) until the game thread has handled `SurfaceLost`, so the swapchain is gone before the window is.
- Java is reached only through JNI function-table calls (`AndroidJni`): the launch intent's string extra, the window insets (`getRootWindowInsets`), and `KeyCharacterMap.get` for the character of a key. Everything else is the NDK's C API (`libandroid`, `liblog`).
- Arguments come from the launch intent: `adb shell am start -n <package>/android.app.NativeActivity -e onegame.args "--headless --frames 3"`. Standard output and error go to logcat (tag `onegame`) and to `files/onegame-stdout.txt` in the app's data; when `Main` returns its exit code is written to `files/onegame-exit.txt` and the activity finishes. That marker is how `onegame android run` knows a run ended.
- `@files` in an argument stands for the app's files folder in the CLI commands below.

## Export

```
onegame export --target android --json                      # arm64-v8a and x86_64, Release, no development tools
onegame export --target android --abi x86_64 --dev --json   # an emulator build agents can drive
```

Options: `--abi arm64-v8a,x86_64`, `--dev`, `--package com.example.game` (default `dev.onegame.<assembly>`), `--label`, `--orientation` (an `android:screenOrientation` value, default `unspecified`), `--sdk <dir>`, `--keystore <file>`, `--out`, `--project`. The result (`AndroidExportResult`) has the APK path, size, SHA-256, package, ABIs and each library's hash.

- **Needs** a Linux x86-64 host (the NDK's clang links the library), the Android SDK (`ANDROID_HOME`: build-tools, a platform, platform-tools) and an NDK (`ANDROID_NDK` or `$ANDROID_HOME/ndk/<newest>`), plus a JDK for `apksigner` and `keytool`. All of these are build-time tools; none ships.
- The .NET SDK does not list `linux-bionic` as a NativeAOT target, so `build/Android.targets` (imported by `build/Game.targets`) downloads the host compiler and the `Microsoft.NETCore.App.Runtime.NativeAOT.linux-bionic-*` pack at restore and points the compiler at them and at the NDK's clang and sysroot. A plain `dotnet publish -r linux-bionic-arm64` of a game that imports `Game.targets` does the same.
- The APK is signed with the Android debug key (`$ANDROID_USER_HOME/debug.keystore` or `~/.android/debug.keystore`, made with `keytool` when missing). Debug-signed means installable on any device with USB debugging and on emulators; it is not a store build. An AAB and release signing are not built (they need Play's bundletool and a key the owner holds).
- `--dev` adds the INTERNET permission (the live listener is a loopback socket), `android:debuggable` (so `run-as` can read the app's files) and the development tools. Without it the app has neither, and `--live`, `--scenario` and friends exit 5 `no_dev_tools`, like a desktop release.
- minSdk 26 (Vulkan 1.0 hardware), targetSdk 35, `android.hardware.vulkan.level` required. Libraries are linked with 16 KiB page alignment.

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

Emulator runs (x86_64, API 35, SwiftShader Vulkan) are in the unit's report. Not yet verified: arm64 on a device or emulator (built, not run), a real GPU driver on a phone, gamepads, a hardware keyboard, composed text, devices with a display cut-out, and Play-style 16 KiB-page devices.
