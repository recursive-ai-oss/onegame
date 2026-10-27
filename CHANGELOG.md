# Changelog

All notable changes to onegame are listed here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/) from 1.0.0.

## [Unreleased]

## [1.2.0] - 2026-10-27

Happy-path layers over the low-level API: a first game now has a moving sprite, action-mapped input and a hot-reloaded asset in about 90 lines.

### Added

- 2D drawing from view code: entities implementing `IView2D` are drawn by the default `Render2DStage` through `Draw2D` (sprites, sprite-sheet frames and clips, rectangles, lines, circles, text), batched onto the existing `Scene2D`. `SpriteSheet`, `SpriteClip` (frame animation on view time) and `Camera2DRig` (follow, smoothing, bounds, zoom, screen and world conversion). `UiCanvas.Text` and `TextQuadBuilder.BuildLine` draw text outside a UI list. Control operation `draw_view`.
- `Draw2D.Texture(path)` and `d.Sprite("art/ship.png", ...)` draw from an asset path; a grid `SpriteSheet` over an asset texture is cut again when a reload changes the image size.
- Input actions: `ActionMap` with contexts, bindings, composites (`Wasd`, `Arrows`, sticks, pairs) and queries, declared in `GameDefinition.DefineActions` and read from `GameInstance.Actions`; rebinding with a bindings file and `ActionUi`; `SteamActionSource`. Control operations `actions` and `input_pad`, scenario steps `action:` and `pad:`, `press:` with `state`, and the `action:` expectation.
- `Assets`: cached, reference-counted handles for textures, SVG, YAML data, sounds, models and fonts, background loads and size limits; `GameDefinition.CreateAssetSource`; development hot reload (textures, sounds and models swap in at once; YAML data is staged and applied at a boundary); the `assets` operation, `assets:` step and `asset:` expectation; `Renderer2D.ReplaceTexture`.
- `onegame new` writes a ship game: a `Ship` entity with fixed-point position drawn from `art/ship.png`, `DefineActions` for move and boost, a scenario using `action:`, and a capture scenario with its reference. `--server` keeps the counter scaffold.

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
