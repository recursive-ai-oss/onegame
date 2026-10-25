# Changelog

All notable changes to onegame are listed here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/) from 1.0.0.

## [Unreleased]

## [1.0.0] - Unreleased

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
