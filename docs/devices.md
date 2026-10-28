# Running the samples on phones and in a browser

The two samples run on the Android emulator, the iOS Simulator and in Chrome with the same state hashes as on a computer. This page is what to do on real hardware, and which commands repeat the simulator checks. Interfaces: [android](interfaces/android.md), [ios](interfaces/ios.md), [audio](interfaces/audio.md), [exporting](exporting.md).

## One command per device

`tools/devices/` holds a script per kind of device. Each finds the device, builds what it needs, installs and runs the samples (Bricks too, once it is in the tree), compares every scenario's state hash with a run on the computer, takes a frame, checks sound through the `audio` operation, exercises touch or pad input where a script can, prints one verdict line and cleans up. Details of each step are in the script's header; per-step JSON lands in `$ONEGAME_ARTIFACTS/device-<kind>/`.

Cleanup runs on every way out of a script: finish, failure, Ctrl-C and SIGTERM. Each script stops exactly what it started (by process id, unit or package name it created, never by program name or port): the games and apps it launched, the packages it installed (unless `--keep`), port forwards to the live-control port that appeared during the run, ssh tunnels, transient units and scheduled tasks, and the commands it was waiting for. Settings it changed (Android's stay-awake) are put back. Steam on the Deck is never stopped or restarted. `ios-signing.sh` keeps its signing keychain on purpose; it is reused. Shared helpers for the bash scripts are in `tools/devices/teardown.sh`.

| Device | Command | Needs |
|---|---|---|
| Android phone | `tools/devices/android.sh [--serial S]` | USB debugging on, `adb`, the Android SDK and NDK the export uses; the phone on the same network as the computer for Lanterns (`--lan-host` if the guess is wrong) |
| iPhone or iPad | `python3 tools/devices/ios.py` on the Mac the device is plugged into | the device trusted and in Developer Mode, unlocked; an App Store Connect API key in `ASC_KEY_FILE`, `ASC_KEY_ID`, `ASC_ISSUER_ID` (the script registers the device and makes the profile; `asc.py` needs `pip install cryptography`), or ready profiles in `IOS_PROFILE_DIR` |
| Steam Deck | `tools/devices/deck.sh [--host H]` | ssh on, this computer's key in the Deck's `authorized_keys`; the Deck found by `steamdeck.local` or a scan of the network (`find-host.py`); desktop mode logged in for the windowed run |
| Windows computer | `tools/devices/windows.sh --host H --user U [--tests]` | ssh on (OpenSSH server), this computer's key authorized, the user logged in at the desktop; a Visual Studio install with the C++ tools for the NativeAOT export. A .NET SDK and the validation layer are installed under `%USERPROFILE%\onegame` only |
| Gamepad | `tools/devices/pad.sh [--expect NAME] [--rumble]` | the pad plugged in and `/dev/input/event*` readable; `--virtual xbox\|dualsense` runs the same check against a `/dev/uinput` stand-in |

The community controller mapping database is test data: `tools/devices/fetch-gamecontrollerdb.sh <file>` downloads a pinned copy and `pad.sh` does it on first use. What the pad check reports (device id, database entry, whether the layout was mapped, rumble, which buttons and axes moved) needs a person pressing things to cover the buttons; unattended it reports what the idle device says.

For a cluster whose servers run on the computer and whose client runs on a phone, set `ONEGAME_CLUSTER_BIND=0.0.0.0` ([cluster](interfaces/cluster.md)); the scripts do. A game's live control on a device the computer can reach only over the network is `--live tcp:*:PORT` ([control](interfaces/control.md)).

## A Windows computer

`tools/devices/windows.sh` builds the engine on the machine, runs each sample's scenarios on its GPU with the validation layer on and compares every state hash with a run here, opens Solitaire in the logged-in user's session (ssh has no desktop, so a one-shot scheduled task running as that user starts it) and drives it over live control through an ssh tunnel: touch, the WASAPI device with strict counters (frames written and played and the peak above 0, no underruns), a presented frame, background and back, and F3 for the debug overlay (the key is posted to the game's own window, so it never lands in whatever the person is typing in). The particles example, a Lanterns client against servers on this computer, and `export --target win` (hash equal to a development run, `--live` refused) follow. `--tests` also runs the GPU, window, audio and platform test apps there; `ONEGAME_WINDOWED_ADAPTER` names the GPU for the window tests, which otherwise insist on a software one. Every task, program and tunnel the script starts is removed on exit, however it ends.

The machine's GPU draws captures a little differently from the software references (edges differ by up to about 55 per channel on at most 0.2 % of pixels on Intel Iris Xe); the scenario files' own tolerances already cover that.

## A real Android phone

1. On the phone: Settings → About → tap *Build number* seven times; Developer options → *USB debugging*. Plug it in and accept the computer's key. `adb devices` lists it (`ANDROID_SERIAL` picks one when several are attached).
2. From `samples/solitaire`:
   ```
   onegame export --target android --abi arm64-v8a --dev --json
   onegame android install export/Solitaire-*-android-dev.apk
   onegame android test --scenario scenarios --json      # 14 of 14; state hashes equal the desktop's
   onegame android live                                  # prints tcp:127.0.0.1:N for `onegame mcp --socket`
   ```
   Over MCP: `audio` must report `backend: "aaudio"`, `deviceState: "running"` and a growing `framesPlayed`; draw a card (`ui_tap {id: stock}`), then `peakPlayed` is above 0. Touch with a finger, rotate, press Home and come back: `hash` is the same before and after.
3. Lanterns as a client of servers on the computer: build the servers into `bin/` (README of the sample), start `onegame cluster up --spec` with the servers only, and run the client with the computer's LAN address, not `10.0.2.2`:
   ```
   LANTERNS_HOST=<computer's address> onegame cluster test cluster/play-android.yaml --spec cluster-android.yaml --json
   ```
   The phone and the computer must be on one network; the servers listen on all addresses only when their spec says `listen` with a non-loopback address.

What only a phone shows: a real GPU driver's output (compare a capture with the references at the tolerances in the sample READMEs), real touch and keyboard, notches, sound from the speaker, thermals.

## A real iPhone

`tools/devices/ios.py` does the steps below, including the account side, when an App Store Connect API key is set (see the table above). By hand it needs an Apple Developer account, a development certificate in a keychain the shell can unlock (a locked login keychain fails with `errSecInternalComponent` over ssh; `tools/devices/ios-signing.sh` makes a keychain of its own with a certificate the API issues), a provisioning profile that lists the device, and the device in Developer Mode (Settings → Privacy & Security).

1. `onegame export --target ios --dev --bundle-id <an id the account owns> --json` from the sample. The bundle is unsigned and the command prints `signingNeeds`.
2. Copy the profile to `<Name>.app/embedded.mobileprovision`, put the profile's team id into the `.entitlements` file beside the bundle, then
   `codesign --force --sign "<identity>" --entitlements <Name>.entitlements --timestamp=none <Name>.app`
   `xcrun devicectl device install app --device <device id> <Name>.app`
3. Launch it from the phone's home screen, or `xcrun devicectl device process launch --device <id> <bundle id> --console -- --headless --scenario ...`. The `onegame ios` verbs address simulators only; on a device use `devicectl` and read `Documents/onegame-stdout.txt` with `devicectl device copy from`.
4. The same checks as on the simulator: the 14 scenarios give the desktop's hashes, the `audio` op reports `backend: "audiounit"` and `peakPlayed` above 0 after a draw. Lanterns connects to servers on a computer by that computer's LAN address (the first connection asks for the local-network permission).

What only a phone shows: finger touch, the real soft keyboard, the notch and home-indicator safe areas, the speaker and the silent switch (the audio session category is Ambient, so it follows the switch).

## A browser

```
onegame export --target web                        # a folder to host; any static server works
onegame web test --json --tolerance 2 --max-diff 0.02   # solitaire's 14 scenarios in headless Chrome
```

A page needs a browser with WebGPU (Chrome and Edge 113+; Safari and Firefox are not tested). Sound starts after the first click or key press, as browsers require; until then the `audio` op reports `awaiting_gesture`. To see the page present frames on a real GPU, start headed Chrome on a display with `--enable-unsafe-webgpu --ignore-gpu-blocklist --enable-features=Vulkan --ozone-platform=x11 --no-first-run --disable-extensions` (a virtual X server such as `Xvfb` is enough), open the exported page, and `navigator.gpu.requestAdapter()` names the GPU.

## Repeating the simulator checks

| Target | Check |
|---|---|
| Android emulator | `onegame android test --scenario scenarios --json` in `samples/solitaire`, then `onegame cluster test cluster/play-android.yaml --spec cluster-android.yaml --json` in `samples/lanterns` (README of each sample) |
| iOS Simulator | `onegame ios test --scenario scenarios --bundle dev.onegame.solitaire --json`; in `samples/lanterns`, `onegame ios test --scenario scenarios --json -- --capture-tolerance 4 --capture-max-diff 0.0025` and `onegame cluster test cluster/play-ios.yaml --spec cluster-ios.yaml --json` |
| Browser | `onegame web test --tolerance 2 --max-diff 0.02` in `samples/solitaire`; in `samples/lanterns`, `onegame web test --tolerance 4 --max-diff 0.01`; `tools/web/lanterns-test.mjs` for the WebSocket client |

## What the simulators showed

| | Solitaire (14 scenarios, hashes equal to the computer's) | Lanterns (8 scenarios, hashes equal) | Lanterns as a client of native island servers | Sound |
|---|---|---|---|---|
| Android emulator, arm64, API 35 | 14/14; captures within tolerance 48 | 8/8; 3D captures differ on at most 0.22 % of pixels by more than 4 | UDP: bot agrees with its island at tick 3600, zero drift | `aaudio`, `peakPlayed` 0.077 |
| iOS Simulator, Metal | 14/14; captures within the scenario files' tolerance | 8/8 with `--capture-max-diff 0.0025` (largest frame: 0.20 %) | UDP and WebSocket: agree at tick 3600, zero drift | `audiounit`, `peakPlayed` 0.077 |
| Chrome, WebGPU | 14/14 | 8/8; largest 3D frame differs on 0.4 % of pixels | WebSocket: replica view hash equals the server's at five ticks, zero drift | `webaudio`, silent until the first click |

On iOS and in a browser a capture compares against `name-mobile.png` beside `name.png` when that file exists (Bricks' title has no Quit button there, so `title-720p-mobile.png` and `title-1080p-mobile.png` are the same frames without it).

Frames are compared with references drawn on a software renderer; GPU drivers differ along edges, which the tolerances above cover. Headed Chrome on a desktop GPU (NVIDIA, through a virtual X server) presents both samples' frames to the canvas.
