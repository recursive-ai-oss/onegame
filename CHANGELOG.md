# Changelog

All notable changes to onegame are listed here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/) from 1.0.0.

## [Unreleased]

## [1.6.0] - 2026-10-28

Pages: a game screen can be an HTML/CSS page or a style-tree page from OneRender, drawn through the 2D renderer.

### Added

- `OneGame.Pages` (a new package): OneRender pages (`PageScreen.FromHtml` / `FromTree`, `game.Pages`) drawn through the render graph. Images, SVG (through the art atlas) and glyphs (through the glyph atlas, sharing faces with the game's text) go through the engine's atlases; host scopes place a page under or over the 2D pass; filters, masks, backdrop filters and blend groups run as render-graph passes. Input (pointer, keyboard, wheel, controller focus) goes to a focused page first, and CSS transitions run on view time. No managed allocation per steady frame.
- Harness: `pages` control operation; `ui_query`, `ui_click` and `ui_invoke`, and the scenario steps `click:`, `type:` and `ui:`, find page elements by name.
- Analyzers: lambdas, local functions and method groups registered as OneRender DOM event listeners are checked as view code (OG0008).
- `GameInstance.Fonts`; `Renderer2D.FrameNumber` is public.
- User guide: `docs/pages.md`.

### Changed

- Solitaire's settings screen is an HTML page, with live slider readouts; it runs on desktop and in the browser.
- OneRender 1.1.0 (`SetTextContent` without allocation; class and attribute edits).

## [1.5.2] - 2026-10-27

Android audio on the device package, and fixes from real Windows hardware.

### Changed

- Android audio output runs on `RecursiveAI.OneAudio.Devices` 1.3.0's AAudio backend: data-callback streams, device-clock frame counters, reopen on route change. `AAudioBackend` stays as a name for it; the engine's own AAudio interop is removed. On Android `device.deviceState` reads `running` (was `started`).
- `RecursiveAI.OneAudio` and `RecursiveAI.OneAudio.Devices` updated to 1.3.0.

### Fixed

- Render3D: a scene with no point or spot lights read light counts from a buffer nobody wrote, or from an earlier frame. On some drivers that looped over garbage and hung the frame; with stale counts it lit the scene with lights that were gone. The shader now ignores the buffer unless the cluster pass ran.
- Vulkan: pooled command encoders are pre-sized for a busy frame, so steady frames no longer allocate a few hundred bytes to 2 KB now and then on slower GPUs.
- Tests run on Windows hardware: an edge-sharpness check accepts a sharper result than the reference, and the cluster-kill test no longer needs `sleep`.

### Added

- Device checks for Windows (`tools/devices/windows.sh`); the live check is strict about device counters on every backend, WASAPI included.

## [1.5.1] - 2026-10-27

Fixes from building a game from the packages alone.

### Changed

- Linux NativeAOT exports link against glibc 2.31 by default (a build sysroot is fetched once, checked against a pinned hash and cached), so they run on SteamOS, the Steam Linux Runtime and the Steam Deck when built on a current system. `--host-glibc` links against the host instead; `--sysroot <dir>` uses your own.
- An export that needs a newer glibc than 2.31 now fails with `glibc_too_new` instead of warning.
- Games using NuGet central package management (`Directory.Packages.props`) build with the SDK: the engine packages are versioned with `VersionOverride`.

### Added

- `ArtAtlas.Preload`, `PreloadFitted`, `PumpPreload`, `CompletePreload`, `ArtPreload`, `BackgroundThreads`, `PreloadFrameBudget` and the `SyncRasterizations`/`PreloadRasterizations` counters: art can be decoded and rasterized before its first draw, off the frame thread (spread over frames where there are no threads). `GameInstance.Art` exposes the game's atlas.
- Docs: detecting the Steam Deck, and the deprecation of `IsSteamRunningOnSteamDeck`.

## [1.5.0] - 2026-10-27

An examples gallery and a debug overlay.

### Added

- `examples/`: six runnable one-feature games (sprite, input actions, tween, particles, saves, a networked counter) with scenarios, built with the solution and run by CI through `tools/ci/examples.sh`.
- An on-screen debug overlay in development builds, toggled with F3 or Back+Start: frame time, tick and entities, net transport, round trip, bytes per second and drift, audio device counters. `overlay` control operation and `overlay: on|off|toggle` scenario step; `ONEGAME_OVERLAY`, `ONEGAME_OVERLAY_KEY` and `ONEGAME_OVERLAY_CHORD` configure it.

### Changed

- Exports fail with `dev_tools_present` if overlay code is linked in; exports and release builds do not contain it.

## [1.4.0] - 2026-10-27

Game feel and persistence: tweens and style transitions, particles in 2D and 3D, and a storage service for save slots, settings and files, in the browser too.

### Added

- Tweens: `OneGame.Tweens` with `Easing` (the CSS timing functions, cubic-bezier and steps, plus named in/out curves), `TweenSet`/`Tween` with sequences, parallel groups, delay, repeat and yoyo, callbacks and cancel; no allocation per frame. `game.Tweens` runs them on view time, so the simulation and replays are unaffected.
- The style `transition` property now animates, following CSS Transitions.
- Particles: `OneGame.Particles` emitters (a new package) with `Draw2D.Particles` and `SceneWriter.Particles`, built on the new `Renderer2D.AdditiveMaterial` and 3D camera-facing billboards (`BillboardBatch3D`); seedable, view-only, no allocation per frame.
- Storage: `game.Storage` gives named save slots built on world saves (label, kind, time and game version for a load screen), quick-save, rotating autosave, typed settings (`SettingsSchema<T>`) and plain files (`IByteStore`). Writes are atomic; readers check limits before allocating.
- Browser persistence through IndexedDB for saves, settings and files.
- Harness: `tweens`, `particles` and `saves` control operations; scenario steps `tweens:` and `saves:` and expectations `tweens:`, `particles:` and `saves:`; `--data` and `--persist-data` arguments; `onegame web test --profile --port`.
- User guides: `docs/tweens.md`, `docs/particles.md`, `docs/saves.md`.

### Changed

- OneShader 1.3.0. Layouts come from `ToBindings`, `UnionOptions` and vertex stream splitting alone (`ShaderLayouts.Bindings(..., options)`, `ShaderLayouts.VertexStreams`); the IBL passes share one layout and the 3D instance vertex layout is derived from the shader.
- The solitaire and bricks samples use the engine storage service in place of their own file stores.

## [1.3.0] - 2026-10-27

Audio devices from the one* audio library, shader layouts from the one* shader library, and user guides for the 1.2 happy paths.

### Changed

- Audio output on Linux, Windows, macOS and iOS runs on `RecursiveAI.OneAudio.Devices` 1.2.1: device-clock frame counters, default-device following, reopen after a lost device. The browser backend plugs into the same device interface as a manually driven device (no threads). `AlsaBackend`, `WasapiBackend` and `AudioUnitBackend` remain as names for the package backends; `device.deviceState` reads `running` or `suspended`. Android keeps its own AAudio path for now.
- The harness's audio capture runs on the package's manual device; its device report now shows real frames played.
- OneShader 1.2.0. `ShaderLayouts.Bindings` builds one layout over several entry points with set widening and dynamic bindings, and `ShaderLayouts.Vertex` derives vertex layouts with format overrides; the 2D renderer's binding and vertex tables and the 3D mesh vertex layout are now derived from the shaders.

### Removed

- The engine's own ALSA, WASAPI and Audio Unit/Objective-C output code.

### Added

- User guides: `docs/2d.md`, `docs/input.md`, `docs/assets.md`.

## [1.2.0] - 2026-10-27

Happy-path layers over the low-level API: a first game now has a moving sprite, action-mapped input and a hot-reloaded asset in about 90 lines.

### Added

- 2D drawing from view code: entities implementing `IView2D` are drawn by the default `Render2DStage` through `Draw2D` (sprites, sprite-sheet frames and clips, rectangles, lines, circles, text), batched onto the existing `Scene2D`. `SpriteSheet`, `SpriteClip` (frame animation on view time) and `Camera2DRig` (follow, smoothing, bounds, zoom, screen and world conversion). `UiCanvas.Text` and `TextQuadBuilder.BuildLine` draw text outside a UI list. Control operation `draw_view`.
- `Draw2D.Texture(path)` and `d.Sprite("art/ship.png", ...)` draw from an asset path; a grid `SpriteSheet` over an asset texture is cut again when a reload changes the image size.
- Input actions: `ActionMap` with contexts, bindings, composites (`Wasd`, `Arrows`, sticks, pairs) and queries, declared in `GameDefinition.DefineActions` and read from `GameInstance.Actions`; rebinding with a bindings file and `ActionUi`; `SteamActionSource`. Control operations `actions` and `input_pad`, scenario steps `action:` and `pad:`, `press:` with `state`, and the `action:` expectation.
- `Assets`: cached, reference-counted handles for textures, SVG, YAML data, sounds, models and fonts, background loads and size limits; `GameDefinition.CreateAssetSource`; development hot reload (textures, sounds and models swap in at once; YAML data is staged and applied at a boundary); the `assets` operation, `assets:` step and `asset:` expectation; `Renderer2D.ReplaceTexture`.
- `onegame new` writes a ship game: a `Ship` entity with fixed-point position drawn from `art/ship.png`, `DefineActions` for move and boost, a scenario using `action:`, and a capture scenario with its reference. `--server` keeps the counter scaffold.
- `tools/OneGame.Corpus` (`onegame-corpus`): a seeded, deterministic corpus of real frames, snapshots, saves, recordings and transfer bundles from Lanterns, Bricks and Solitaire, for compression work.

### Changed

- OneShader 1.1.0. `ShaderSource.Layout` replaces the `Bindings`, `ComputeGroup*`, `Msl*` and `Wgsl*` members (breaking for a hand-built `ShaderSource`); `ShaderLayouts.Bindings` and `ShaderLayouts.PushConstants` expose the generated layouts.

## [1.1.2] - 2026-10-27

### Added

- `OneGame.Platform.RunningOn` (`Android`, `IOS`, `Browser`, `Phone`, `Mobile`, `Desktop`): one way for a game to ask what it runs on.

### Fixed

- Android builds reported themselves as Linux to platform checks (`OperatingSystem.IsAndroid()` is false in a NativeAOT Android app); the engine now asks `RunningOn`, and Bricks hides Quit on Android. Scenario captures on Android use `name-mobile.png` references like iOS and the browser.

## [1.1.1] - 2026-10-26

### Fixed

- Linux audio output: the ALSA sink never reported its device side, so the `audio` report showed `framesWritten`, `framesPlayed` and `peakPlayed` as 0 on every ALSA device even while sound was playing, and a device that really took nothing could not be told apart. It now reports frames written, frames the device has consumed (from `snd_pcm_delay`), the peak of consumed frames, its PCM state and underrun count.
- The device checks in `tools/devices` now fail unless `framesWritten` and `framesPlayed` are above 0, not on a `Running` state alone.

## [1.1.0] - 2026-10-26

A game builds from nuget.org alone, with no engine checkout.

### Added

- `RecursiveAI.OneGame.Cli`: the `onegame` command as a framework-dependent dotnet tool (`dotnet tool install -g RecursiveAI.OneGame.Cli`).
- `RecursiveAI.OneGame.Sdk`: an MSBuild SDK for games. It sets the target framework and the rules, references the engine libraries at its own version, adds the development tools in Debug builds, and carries the export targets and entry files. `onegame new` writes a project on it (`<Project Sdk="RecursiveAI.OneGame.Sdk/1.1.0">`), and with `--server` a dedicated-server project (`<OneGameKind>Server</OneGameKind>`).
- `onegame export --target linux|win` as short names for `linux-x64` and `win-x64`.
- `tools/ci/package-smoke.sh` and a CI job that pack the engine into a local feed, install the tool from it and run new, run, test and a NativeAOT export on Linux and Windows.

### Changed

- Games inside an engine checkout (`--engine`, `ONEGAME_ROOT`, the samples) build against project references as before.
- Not yet from packages: the web export, Play Billing in Android builds and plugin servers need a game created with `--engine`.

## [1.0.0] - 2026-10-25

The first release: a C# game engine with no editor, driven from a terminal.

### Programming model

- Entities are partial classes holding synced state, commands, deterministic simulation and `[View]` members. Source generators emit change tracking, binary codecs, deltas, replica types, versioned snapshots and command stubs.
- Analyzers make the rules compile errors: no floats, wall-clock reads, threads, I/O or hash-order iteration in simulation code; views read state and send commands but never write it.
- Fixed-point numbers, named random streams and `Timed<T>` interpolation values.

### Sync and networking

- Nodes, scopes, authorities and subscriptions; per-connection visibility (everyone, owner, team, hidden) enforced at the byte level; drift hashes always on.
- Prediction with rollback, atomic authority transfer, relaying and escrow for changes that span scopes.
- Reliable UDP and WebSocket transports, reconnection, dedicated server hosts and hot-reloadable server plugins.
- Single-player is a one-node cluster: saves are snapshots and the action log is the command log, so replays are bit-exact.

### Rendering

- A GPU layer over Vulkan (Windows, Linux, Android), Metal (macOS, iOS) and WebGPU (browsers), with a render graph.
- A 2D renderer (sprites, tilemaps, parallax, custom materials) and a 3D renderer (glTF 2.0 metal/rough PBR, clustered forward lighting, image-based lighting, cascaded shadows, SSAO, bloom, tone mapping, instancing, a quality ladder).
- HLSL shaders compiled during the build with generated C# bindings.
- glTF 2.0 import with limits checked before allocating.

### UI

- A retained UI tree styled on the CSS model, flexbox and anchor layout, controller focus navigation, input glyphs, world-anchored UI and tweens.
- A component kit: table, slider, tabs, tooltip, list, card, nine-slice frame and text input.
- Text shaping through OneFont; SVG and images through OneImage.

### Platforms

- Windows (Win32, XInput, WASAPI), Linux (Wayland and X11, evdev pads, ALSA), macOS (Cocoa), iOS (UIKit), Android (native activity) and the browser (.NET on WebAssembly).
- Steamworks and Steam Input bindings, StoreKit and Google Play Billing bindings, and a scriptable fake store.
- `onegame export` for Windows, Linux, macOS, iOS, Android and the web: NativeAOT where the host allows it.

### Harness

- The `onegame` command (`new`, `build`, `run`, `test`, `capture`, `replay`, `export`, `cluster`, and more) with JSON output.
- A C# test API (`TestCluster`, scenarios, captures, recordings) and live control over MCP in development builds; release builds leave the listener out.

### Samples

- Solitaire: a UI-heavy 2D game with scenarios, replays and captures.
- Lanterns: two island servers and a master, entity hand-off, escrowed trades, predicted movement, owner-only inventories, in 2D and 3D.
