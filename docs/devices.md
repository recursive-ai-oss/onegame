# Running the samples on phones and in a browser

The two samples run on the Android emulator, the iOS Simulator and in Chrome with the same state hashes as on a computer. This page is what to do on real hardware, and which commands repeat the simulator checks. Interfaces: [android](interfaces/android.md), [ios](interfaces/ios.md), [audio](interfaces/audio.md), [exporting](exporting.md).

## A real Android phone

1. On the phone: Settings → About → tap *Build number* seven times; Developer options → *USB debugging*. Plug it in and accept the computer's key. `adb devices` lists it (`ANDROID_SERIAL` picks one when several are attached).
2. From `samples/solitaire`:
   ```
   onegame export --target android --abi arm64-v8a --dev --json
   onegame android install export/Solitaire-*-android-dev.apk
   onegame android test --scenario scenarios --json      # 14 of 14; state hashes equal the desktop's
   onegame android live                                  # prints tcp:127.0.0.1:N for `onegame mcp --socket`
   ```
   Over MCP: `audio` must report `backend: "aaudio"`, `deviceState: "started"` and a growing `framesPlayed`; draw a card (`ui_tap {id: stock}`), then `peakPlayed` is above 0. Touch with a finger, rotate, press Home and come back: `hash` is the same before and after.
3. Lanterns as a client of servers on the computer: build the servers into `bin/` (README of the sample), start `onegame cluster up --spec` with the servers only, and run the client with the computer's LAN address, not `10.0.2.2`:
   ```
   LANTERNS_HOST=<computer's address> onegame cluster test cluster/play-android.yaml --spec cluster-android.yaml --json
   ```
   The phone and the computer must be on one network; the servers listen on all addresses only when their spec says `listen` with a non-loopback address.

What only a phone shows: a real GPU driver's output (compare a capture with the references at the tolerances in the sample READMEs), real touch and keyboard, notches, sound from the speaker, thermals.

## A real iPhone

Needs the owner's Apple Developer account, a development certificate in the Mac's keychain, a provisioning profile that lists the phone, and the phone in Developer Mode (Settings → Privacy & Security).

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

Frames are compared with references drawn on a software renderer; GPU drivers differ along edges, which the tolerances above cover. Headed Chrome on a desktop GPU (NVIDIA, through a virtual X server) presents both samples' frames to the canvas.
