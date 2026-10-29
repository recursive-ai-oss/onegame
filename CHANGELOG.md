# Changelog

All notable changes to onegame are listed here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/) from 1.0.0.

## [Unreleased]

### Fixed

- An entity removed during a tick no longer appears in `World.Entities` for the rest of that tick. The world keeps it until the tick ends, so a second command or a later loop in the same tick used to find it again and could apply the same change twice (for example, adding a leaving player's holdings to a total once per queued request). `World.Entities` keeps its type and returns a live view of the entities in id order. Saves, snapshots, deltas and hashes are unchanged, because they are produced between ticks.

## [1.15.0] - 2026-10-29

`RecursiveAI.OneGame.Physics` joins as a **preview** (`1.15.0-preview.1`): it is built on OnePhysics 0.1.0-preview.5, itself a preview, so its API may change. The other 31 packages are stable at 1.15.0 and never depend on it.

### Added

- `RecursiveAI.OneGame.Physics` (preview, built on OnePhysics 0.1.0-preview.5): 2D rigid bodies, characters, triggers and ray casts for sim code on `Fixed` positions. `PhysicsSpace2D` is a hidden hot table of the world plus a system that steps it once per tick, so the physics world is saved, restored, replayed and hashed with the world; `BodyRef` and `CharacterRef` are synced values; events reach sim code in a fixed order; `PhysicsDebugShapes` draws the world on the debug overlay; `Warmup()` and capacity options make steps, ray casts and the space's hash allocation-free from the first step. A save the library refuses (damaged, or from another format) is refused as `SyncError.InvalidValue` and leaves the world unchanged. See `docs/physics.md`.
- Example `examples/physics` in the gallery: a character that walks, jumps between platforms and pushes crates into a goal zone.
- CI: a physics scene must end in the same hashes and positions on Linux, Windows and WebAssembly (`tools/ci/physics-probe.sh`).

### Changed

- `tools/ci/check-packages.py` allows exactly one prerelease package, `RecursiveAI.OneGame.Physics` at `<engine version>-preview.N`, and still refuses any other.

## [1.14.0] - 2026-10-28

`RecursiveAI.OneGame.Navigation` is stable, on OnePath 1.0.0: every package now ships at the engine version.

### Changed

- `RecursiveAI.OneGame.Navigation` is stable: it is built on OnePath 1.0.0 and ships at the engine version like every other package, no longer as a prerelease. `tools/ci/check-packages.py` now refuses any prerelease package. `NavGrid.Stamp` reads OnePath's cached `Grid.ContentHash` directly.
- `OneGame.Navigation` moves to OnePath 1.0.0. Search and crowd results and saved bytes are unchanged. `NavCrowd` reads agents with `Crowd.CopyAgents(Span<Agent>)`, treats any `OnePathFormatException` from a bad crowd state as a refused snapshot, and `TryAdd` snaps to the nearest walkable point of the whole map (it used to refuse a spot too far from ground).

### Fixed

- Lanterns sample: a player who reconnects to an island that restarted from its save keeps their player. Before, the island could remove them as "left" just after they came back.

## [1.13.0] - 2026-10-28

Crowds as synced state in `RecursiveAI.OneGame.Navigation` (1.13.0-preview.1, still a preview on a preview of OnePath). The other packages have no API changes.

### Added

- `OneGame.Navigation`: crowds as synced state. `NavCrowd` is OnePath's crowd (avoidance of walls and of each other) on a mesh built from a `NavGrid`, kept in the world as the hot table `og.nav.crowd`: stepped once per tick, saved, hashed, replayed and replicated with the world, 0 B per step; sim code sees `Fixed`/`FVec2` only (`NavAgent`, `NavAgentOptions`, `NavCrowdOptions`). See `docs/navigation.md` and `examples/crowd`.

### Changed

- `OneGame.Navigation` moves to OnePath 0.5.0-preview.1: searches run on `GridSearch`, path costs are OnePath's exact Q31.32 values, and `NavGrid.Stamp` is `Grid.ContentHash`. A `NavState` saved by 1.12 still loads; its map stamp differs, so a path or search from such a save is found again from where the unit stands.
- CI checks that root motion bakes to the same fixed-point numbers on Linux, Windows and WebAssembly, and runs the hot reload check on Windows as well as Linux.

### Fixed

- Web runs (`onegame web test`, the web export check) start headless Chrome reliably on a busy machine: Chrome picks its own debugging port, start-up waits longer and retries, and a failed start prints Chrome's own output.

## [1.12.0] - 2026-10-28

A 3D animation layer for characters: blend spaces, a state machine animator, root motion and two-bone IK on glTF clips. `RecursiveAI.OneGame.Navigation` moves to 1.12.0-preview.1.

### Added

- 3D animation layer in `OneGame.Gltf`: `Rig`, pose blending with masks (`PoseBlend`, `PoseMask`), `Crossfade`, 1D and 2D blend spaces, `Motion`s, two-bone IK (`TwoBoneIk`), and an `Animator` with parameters, triggers, states and transitions (conditions, exit time, crossfade). `SceneWriter.Model(name, transform, pose)` draws a model in an animated pose. See `docs/animation.md` and `examples/character`.
- Root motion: `RootMotion.Bake` turns a clip's root bone movement into a `RootMotionTrack` (new, in `OneGame.Core`), fixed-point steps per tick that sim code applies deterministically.
- Analyzer rule OG0023: animation types (`OneGame.Gltf`) cannot be used in sim code.

### Changed

- `OneGame.Gltf` now references `OneGame.Core` (for the root motion bake). No new package.

## [1.11.0] - 2026-10-28

Pathfinding for deterministic simulations, as a preview package.

### Added

- `RecursiveAI.OneGame.Navigation`: grid pathfinding on `Fixed` positions for sim code, built on OnePath 0.4. Published as a prerelease (`<engine version>-preview.N`), so no stable package depends on it. `NavGrid`, one-shot `FindPath`, and `NavState` (a synced value) with `NavSearcher` for searches spread over ticks, saved and restored with the world and covered by the state hash; walking with `NavState.Follow`; `NavRangeException` for positions beyond +-2^21. See `docs/navigation.md` and `examples/pathing`.
- `Draw2D.DebugVisible`: true while the development overlay is shown, so views can draw debug geometry such as navigation paths.
- Example `examples/pathing` in the gallery.
- A package check (`tools/ci/check-packages.py`) in the package scripts: every stable package is at the engine version and none depends on a prerelease.

### Changed

- The API reference shows `--prerelease` in the install line of a prerelease package.

## [1.10.0] - 2026-10-28

Encrypted, authenticated network links: every UDP and WebSocket connection is encrypted and the server is authenticated by its key, and a player keeps their identity across reconnects.

**Networking in 1.10 does not interoperate with 1.9 or earlier.** Every transport now needs a `Security` setting; use `NetSecurity.Insecure` only for tests and local tools.

### Added

- Encrypted, authenticated UDP and WebSocket links built on the Noise Protocol Framework (`Noise_IK_25519_ChaChaPoly_SHA256`) inside the existing cookie handshake, with ChaCha20-Poly1305 packets and records, authenticated disconnects, handshake rate limits per source and replay protection. X25519, ChaCha20-Poly1305 and HKDF are managed code in `OneGame.Core`, so links work everywhere the transport runs, including Android, iOS and the browser. Each UDP packet grows by 16 bytes. Design and threat model: [docs/interfaces/transport-security.md](docs/interfaces/transport-security.md).
- `NetSecurity` (`ForServer`, `ForClient`, `Insecure`, `AlsoTrusting`) on `UdpOptions.Security`, `WebSocketOptions.Security` and `ConnectOptions.Security`; `NetKeyPair`, `PeerKey`, `PeerIdentity`, `INetConnection.ChannelBinding` (for binding a login ticket to the session) and `PlayerRegistry`, which maps a client key to a player id that survives reconnects.
- `onegame keys` makes a key pair (`.key` private, `.pub` public). Server flags `--key` (repeatable, for rotation) and `--game-id`; client flags `--server-key` (several allowed), `--identity`, `--server-id` and `--game-id`. Both accept `--insecure`, which needs a development run (`ONEGAME_DEV=1`).
- Development runs need no setup: `onegame run --connect`, `onegame server run`, `onegame cluster`, test clusters and the samples make development keys and pass them on. Release builds refuse development keys and insecure links.
- A client's key is derived per server, so servers cannot link a player by key. `NetSecurity.ServerId` keeps a player's identity on a server across that server's key rotation.
- Fuzz targets for the handshake, AEAD open, encrypted UDP datagrams and secure WebSocket records. Test vectors from RFC 7748, 8439 and 5869, Project Wycheproof and the Noise vectors.

### Changed

- `WebSocketTransport.Listen` and `Connect` return interfaces.
- `onegame export` refuses to build when a content folder holds a private key file.
- Lanterns runs encrypted over UDP and WebSocket, and identifies players by key.
- OneRender, OneRender.Image and OneRender.Fonts 1.3.1 (with OneHtml 1.5.1). Settings slider thumbs have hover and active states, and pointer moves over a page allocate nothing.

## [1.9.0] - 2026-10-28

Hot reload of game code: edit C# while the game runs and keep the world.

### Added

- `onegame run` applies C# edits to a running Debug game through .NET hot reload (`dotnet watch` metadata deltas): view, sim and screen-builder method bodies, new methods and new synced members go live with the world kept, and open screens are rebuilt. An edit that cannot be applied live restarts the game and loads its world back; a compile error leaves the game on the last good code. `--no-hot` turns it off. See [docs/hot-reload.md](docs/hot-reload.md), including what a live sim edit means for replays and cluster hashes.
- `hot_reload` live-control operation (applied edits, last error, restarts, whether the run is still replay-safe) and `GameInstance.RefreshScreens()`.
- `tools/hot-reload/check.py`, an end-to-end check of hot reload that runs in the Linux package smoke.

### Changed

- OneRender, OneRender.Image and OneRender.Fonts 1.3.0 (with OneHtml 1.5.0).
- Solitaire's settings sliders are styled like the kit: a gold fill drawn by a value-driven gradient on the track, and a white thumb.

Releases and exports contain none of the hot-reload code.

## [1.8.0] - 2026-10-28

An API reference for every package, generated from the code and its documentation.

### Added

- API reference: [`docs/api/`](docs/api/index.md) documents the public API of every package (types, members, C# signatures, docs, examples and cross-links), generated from the built assemblies and their XML docs by `tools/OneGame.ApiDocs`. CI regenerates it and fails when it is out of date. README, the first-game guide and the other guides link to it.
- [`docs/api/sdk.md`](docs/api/sdk.md): every MSBuild property and item a game project can set on `RecursiveAI.OneGame.Sdk`, with defaults and effects.

### Changed

- XML documentation rewritten for the reference across the game-facing runtime (`GameDefinition`, `GameApp`, `GameInstance`, `Entity`, `World`, `Ui`, `UiKit`, `Draw2D`, `SpriteSheet`, `Camera2DRig`, `ActionMap`, `AudioService`, `Cluster` and more), the test API (`TestCluster`, `TestClient`, scenarios, `ProcessCluster`, `TestServer`), the control and live layers, the server host and plugin API, the 3D renderer, glTF loading and the GPU device. Entry points gain compiled examples; parameters, units, ranges, exceptions and the sim-versus-view rules are stated.

### Fixed

- Process clusters (`OneGame.Testing`) no longer fail when a server's port is taken between choosing it and binding it, or when a long test process runs out of fresh port numbers: a start that loses the race or finds no free port is retried on new ports, a restarted node retries its own port, and a cluster gives its ports back when it goes down. `onegame web` picks another port when its listener's port is taken.

## [1.7.0] - 2026-10-28

Every export works from packages: a game made by `onegame new` from the installed tool exports for the web, Android (with Google Play Billing), iOS and the plugin-capable server with no engine checkout.

### Added

- `onegame new --server` also writes `plugin/`, a server plugin project (the SDK's new `OneGameKind` `Plugin`) and a configuration that loads it.
- CI jobs `packages-web`, `packages-android`, `packages-ios` and `packages-server` build each export from packages alone; the web page and the iOS Simulator app end in the same world hashes as the desktop run, and an exported client agrees with the exported plugin host.

### Changed

- The `onegame` tool carries the web page and script files, the Play Billing Java wrapper and the plugin-capable server host (`server-host/`), so `onegame export --target web`, `onegame web test`, `export --target android` with `AndroidPlayBilling`, `onegame server run` and `export --server --plugins` no longer need an engine checkout.
- Web, Android and iOS exports pack the game's `fonts/`, `art/` and `data/` folders with the same filter as the desktop export, and the game reads them as loose files (the web page and the APK unpack them at start), so games that load files from disk run unchanged on every target.
- Device scripts in `tools/devices/` tear down everything they start on finish, failure, Ctrl-C and SIGTERM (apps, installed packages, port forwards, tunnels, transient units, background commands); `android.sh` no longer removes every forward on the adb server and restores the stay-awake setting.
- The package smoke scripts install the tool from the local feed explicitly.
- OneRender, OneRender.Image and OneRender.Fonts 1.2.0 (with OneHtml 1.4.0). Solitaire's settings controls take their colours from `color-scheme: dark` and `accent-color` instead of a CSS filter.

## [1.6.1] - 2026-10-28

### Changed

- Solitaire's settings page styles its native controls dark and gold like the kit (dark tracks with a gold fill, gold rings and dots), with CSS on the page only.

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
