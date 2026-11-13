# Control layer: one operation set, three faces

Source: `src/OneGame.Control` (operations), `src/OneGame.Host` (the game being driven), `src/OneGame.Testing` (test API, scenarios), `src/OneGame.Live` (development tools: the listener and the dev flags), `src/OneGame.Cli` (`onegame`). Builds on `sync.md` (cluster, recording) and `ui-input-inspect.md`.

## The game host

`GameInstance.Create(GameDefinition, GameOptions)` runs one game: a single-player `Cluster` (an authority node holding `World`, plus a local replica `Replica`), the `UiTree`, a platform window (virtual when headless) and an `IRenderStage`. `Step(n)` runs n frames; each frame drains input, runs one cluster tick (`Cluster.Step(1)`, which steps the authority and publishes to the replica) and updates the UI. `RunRealtime` does this at 60 Hz for a window (see "Windowed runs"). `TickCompleted` fires after every tick; `DriftReports` lists any disagreement between replica and authority (it must stay empty and shows as `drift` in `info`).

- `World` is the authority world; state queries, patches, hashes and snapshots act on it. Replacing the world (`Reset`, `LoadFixture`, `LoadSnapshot`) rebuilds the cluster.
- `SaveSnapshot()`/`LoadSnapshot()` are `SaveFile` bytes (header, hashes, versioned snapshot).
- `StartRecording()` returns the sync layer's `Recorder` (checkpoint every tick by default); `Replay(recording)` runs it on a fresh world built like the game's (with the recording's seed) and returns `ReplayResult`. `RecordingFile.Save/Load` handle files (`OGRP`).
- `StartSessionRecording()` returns a `SessionRecorder`: it follows the game across world replacements (`Reset`, `LoadFixture`, `LoadSnapshot`) and `EditWorld` changes (the `state_patch` operation), keeping one `Recording` per world in a `SessionRecording`. `Replay(session)` replays every segment and stops at the first divergence (`SessionReplayResult.Segment`). `RecordingFile.Save(session, path)` writes a plain recording for one segment and an `OGRS` container (version, count, length-prefixed segments) for several; `LoadSession(path)` reads both and bounds every count and length before allocating. The command line's `--record` and `--replay` use sessions.
- `EditWorld(Func<bool>)` runs a change that bypasses commands and ticks and tells recorders; it throws `InvalidOperationException` while a recording runs and commands are queued (`state_patch` reports that as `rejected`).
- `UiScaleOverride` (a `float?`, 0 < v <= 16) replaces the window's scale for layout, hit-testing, drawing and captures that name no scale; null follows the window again (`WindowScale` is the window's own). It is a view setting, not simulation state, and is not recorded. The `ui_scale` operation reads and sets it; `info` reports `scale` (in effect) and `scaleOverride`.
- A `GameDefinition` declares entities, `Setup`, `RegisterSystems`, `DefineScreens`, `DefineFixtures`, `Style`, `OnInput`, `DefineFonts` (default: `fonts/*.ttf|otf`), `DefineArt`, and optionally `CreateRenderStage(HostResources)`. The default stage is `Render2DStage` (the 2D renderer, sRGB targets) when the game has fonts (the shaders are the renderer's own, compiled during the build; `GameOptions.Shaders` replaces them); otherwise `StubStage`, a GPU clear plus a CPU raster of the display list. A GPU-less capture always uses the CPU raster.
- `GameApp.Run(definition, args)` is the whole `Main` of a game: `--headless --frames N --size WxH --scale S --seed N --gpu auto|off|required --backend wayland|x11 --display NAME --present vsync|lowlatency|immediate --json`. Any other flag needs the development tools (`Dev.Enable()`, called by a module initializer that exists only in Debug builds); without them the game exits 5 `no_dev_tools`.

## Windowed runs

Without `--headless` the game opens a window (Wayland when `WAYLAND_DISPLAY` is set, else X11; `--backend` and `--display` choose, `GameOptions.Backend`/`DisplayName` in code) and each frame: drains platform input into the UI, runs the ticks that are due (fixed 60 Hz, up to 5 caught up), updates the UI and draws into a swapchain with the render stage. Details worth knowing:

- **Swapchain.** Made on the first draw from `Window.NativeHandle`, sRGB when the stage asks for it (the 2D renderer does), `--present` picks the mode (vsync is the default and always exists). A `WindowResized` event releases the stage's per-target caches and resizes the swapchain; a minimized window (or zero area) skips drawing but not ticks; `PresentedFrames` counts frames handed to the display.
- **Pacing.** A frame that presented is paced by the display; one that could not draw waits for the next tick; a presenter that returns at once (a software rasterizer) is held to at least 4 ms per frame unless the mode is `immediate`.
- **Input.** Real and injected events share one queue and one adapter: pointer, wheel, keys (physical `Key` to DOM key names), text, pre-edit text, gamepad buttons (d-pad and shoulders navigate, South activates, East cancels) and the left stick (a push past 0.6 moves focus once, holding repeats after 24 frames every 6). The scale the OS reports (`Window.Scale`) is the UI scale and follows changes.
- **Text fields.** When the focused element has `AcceptsText`, the host calls `Window.SetTextInput(true, caret rectangle)`, so an input method can follow the caret; `GameInstance.Clipboard` is the system clipboard for copy and paste.
- **Capture.** `capture_frame` renders offscreen with the same stage and display list at the size asked for. `capture_frame` with `presented: true` instead reads back the swapchain image itself, drawn one more time at the window's size and scale (png, `renderer` names the adapter); it is an error for a headless game or one with no device.
- **Live control.** `--live` works unchanged: requests run on the game thread between frames, injected input goes through the same queue as real input, and the window keeps presenting while requests are served (paused games redraw every 16 ms).

## Operations

`ControlOps.All` is the table; `ControlSurface.Invoke(op, argsJson)` runs one and returns `ControlResult(Json, Png?)`; `Call(op, argsJson)` returns the JSON text. Failures are `ControlException(code, message)`; codes are stable (`unknown_op`, `bad_args`, `not_found`, `rejected`, `path_outside`, `too_large`, ...). Names use underscores so they are valid MCP tool names. Every operation that reads or writes a file (`snapshot_save`, `snapshot_load`, `capture_frame`, `capture_sequence`, `audio_capture`) takes a path relative to the output folder: the game's `--artifacts` folder, or `artifacts/` in the working directory when the option is not given (`ControlSurface`'s own default is the working directory; the runner passes the folder). Relative paths may have subfolders (`shots/a.png`) and are created as needed. Anything that would leave the folder is refused with `path_outside`: `..`, an absolute path (even one inside the folder), or a link inside the folder that points out of it. An empty path or one with a NUL is `bad_args`. The result's `path` is the full path written.

| Group | Operations |
|---|---|
| Read | `info` (game, tick, frame, hash, size, screen, screens, fixtures, stage, adapter, drift, backend, presented), `hash`, `state_query`, `commands_list` |
| Change state | `state_patch`, `command_send`, `reset`, `fixture_load`, `snapshot_load`, `step` (frames) |
| Input | `input_pointer`, `input_key`, `input_text`, `input_composition`, `input_nav` (menu actions, or any `GamepadButton` by name: see `NavActions`), `ui_click`, `ui_invoke` |
| Window | `window` (read or set the window's placement: position, size, normal/maximized/fullscreen; `docs/interfaces/platform.md`) |
| Clipboard | `clipboard_get`, `clipboard_set` (`selection`: `clipboard` or `primary`) |
| UI | `ui_tree`, `ui_query`, `ui_hit`, `screen_open`, `ui_scale` (override the UI scale; `scale: null` or `reset: true` follows the window) |
| Output | `snapshot_save`, `capture_frame`, `capture_sequence` |
| Theme | `theme_lint` (WCAG AA contrast of the kit theme in effect, as a report; `docs/interfaces/ui-kit.md`) |
| Shaders | `shaders` (hot reload status; recompiles changed shader files first, see `docs/interfaces/render2d.md`) |
| Code hot reload | `hot_reload` (edits applied by `onegame run`, the last edit's state and message, restarts, `replaySafe`; see `docs/hot-reload.md`) |
| Rendering | `render_graph` (passes, resources and per-pass GPU time of the last frame; see `render-graph.md`) |
| 3D | `camera` (read or override the camera), `pick` (entity or labelled model under a pixel or along a ray), `project` (world point to pixel), `scene_models` (models drawn, with labels, opacity and triangle counts), for a `Render3DStage` with a view or one filled by hand: `scene3d.md`; `quality` (read or set the renderer's level, SSAO) and `environment` (distance fog, environment crossfade weight, light intensity and yaw): `render3d.md` |
| 2D | `draw_view` (what the 2D happy path drew last frame, its camera, screen/world conversion): `render2d.md` |
| Debug overlay | `overlay` (`visible`, `toggle`, `masked`, `key`, `chord`; returns `visible`, `key`, `chord`, `masked`, `lines`, and `timing` while the panel is visible: CPU frame, simulation and draw milliseconds with average and maximum over the last 60 frames, `cpuHistory`, and GPU milliseconds per render pass or `gpu: "unsupported"` on Metal and WebGPU; `info` carries the same `timing`): the on-screen debug panel, F3 or Back+Start; view-only, drawn into captures; scenario step `overlay: on\|off\|toggle`: `driving-a-game.md` |
| Particles | `particles` (emitters shown in the last drawn frame: name, live, capacity, emitting, spawned, dropped, blend, space, position; `name` filters; draws a frame first when none was drawn at this tick): `particles.md` |
| Saves, settings | `saves` (list, save, load, quick-save, quick-load, autosave, load-latest, delete, settings-load, settings-save, write-file, delete-file): `storage.md` |
| Purchases | `store` (describe, connect, load, buy, finish, restore; script, resolve, revoke, disconnect against the fake store): `store.md` |
| Audio | `audio` (buses, filters, reverb, limiter, loudness LUFS and dBTP, per-bus loudness, music position, and the `device` section: backend, device state, underruns, frames written and played, peak played; `unlock`, `interrupt` and `suspend` arguments drive the device), `audio_events` (the sounds the game played: sequence, frame, clip name, bus; `since`, `clear`), `audio_capture` (render capture-mode audio, report peak, output and per-bus loudness, write a WAV; see `audio.md`) |
| Steam Input | `steam_input` (enabled or why not, action sets and their screens, active set, controllers, latest action values, on-screen keyboard; with the stand-in controller also connect, disconnect, press, release, analog; see `steam.md`) |
| Lifecycle | `quit` |

This table lists the core operations. `tools/list` over MCP is the complete, current list (name, description, JSON schema for each), including operations added by engine packages and by the game. Entity addressing everywhere: numeric id, `Type` (first of that type) or `Type#n`.

Commands go through the entities' generated `SendFromText(name, args[])` (arguments typed by `CommandSignatures`: int, bool, string, enum, `Fixed`, `EntityId`). A command is queued and runs at the start of the next tick.

## Faces

- **C# test API** (`OneGame.Testing`): `TestCluster.Start(game, options)` → `Client` (`Send`, `Step`, `Call`, `Hash`, `Game`), `Determinism.RunTwice/SnapshotRestore/Replay`, `ScenarioParser` and `ScenarioRunner` (below). Everything goes through `ControlSurface`, so tests and live control exercise the same path.
- **CLI** (`onegame`): verbs `new build run test capture lint-theme replay mcp export view asset keys server cluster android ios web` (`export` is described below; `onegame help` lists the options of each). `--json` prints one object. Errors are `{"ok":false,"error":{"code","message"}}`. Exit codes: 0 ok, 1 a check failed, 2 usage, 3 build failed, 4 could not run, 5 needs a development build.
- **MCP**: MCP revision 2025-06-18, JSON-RPC 2.0, tools only (`initialize`, `tools/list`, `tools/call`; captures return `image` content plus text). A Debug game listens on a Unix domain socket (`--live [path]`) and speaks one JSON-RPC message per line; `--live tcp:PORT` listens on the loopback instead (for `adb forward`), and `--live tcp:*:PORT` on every interface, IPv4 and IPv6, for a device the computer reaches only over the network (an iPad on a cable, through its tunnel address; nothing authenticates it, so test devices only); requests are handled on the game thread between frames, so the game stays single-threaded. `onegame mcp` is the stdio bridge MCP hosts spawn: `--socket <path>`, or `--project <dir>` to start the game and bridge it, or no option to attach to the only live game. The game starts paused (the harness steps it) unless `--realtime`.

Release builds do not reference `OneGame.Live`, `OneGame.Control`, `OneGame.Testing` or OneYaml and do not compile the start-up hook (`build/Game.targets`, `OneGameDev`), so there is no listener, no operation table and no scenario parser in the binary.

## `onegame export --json`

`onegame export --target linux-x64|win-x64 [--steam] [--steam-redist <dir>] [--out <dir>] [--project <dir>] [--no-archive] [--no-aot] --json` prints one object (guide: `docs/exporting.md`):

```json
{"ok":true,"game":"demo","gameVersion":"1.0.0","engineVersion":"1.25.1","target":"linux-x64","configuration":"Release",
 "runtime":"nativeaot","steam":true,"devTools":false,"renderer":"2d",
 "directory":"/abs/export/demo-1.0.0-linux-x64","executable":"Demo",
 "archive":{"path":"/abs/export/demo-1.0.0-linux-x64.tar.gz","format":"tar.gz","size":3391682,"sha256":"…"},
 "files":[{"path":"Demo","size":6181872,"sha256":"…"}],
 "skipped":[{"path":"saves/","reason":"…"}],
 "warnings":[{"code":"glibc_too_new","message":"…"}],"seconds":10.2}
```

`runtime` is `nativeaot` or `coreclr-self-contained`; `renderer` is `2d`; `archive` is absent with `--no-archive`; `files` is every file in `directory` (paths relative, `/`-separated, sorted). Warning codes: `not_native_aot`, `glibc_too_new`, `font_licence_missing`, `large_file`. Failure codes: `bad_target` (exit 2), `steam_redist_missing` and `dev_tools_present` (exit 4), `publish_failed`, `bad_project` and `no_executable` (exit 3), plus `no_project` and `ambiguous_project` as for the other verbs.

## Playing on a server

`--connect udp://host:port|ws://host:port` (with `--scope`, `--name`, `--net latency=100,loss=0.02`) makes the game a client: its world is a replica, `step` pumps the connection, commands go to the server, and `net_status`, `net_wait` and `net_diverge` are added to the table. `onegame cluster`, `onegame mcp --cluster` and cluster scenarios drive many such processes at once: `cluster.md`.

## Scenarios

YAML read through OneYaml's document model; unknown keys are errors with the line. Top level: `name`, `description`, `fixture`, `snapshot`, `window` (`WxH`), `seed`, `steps`, `expect`.

Steps: `click` (an id, `{selector}` or `{x, y}`), `invoke`, `press`, `type`, `nav`, `command {entity, name, args}`, `set {entity, property, value}`, `screen`, `fixture`, `snapshot`, `step: N`, `wheel`, `capture`, `camera` (3D), `store` (purchases, `store.md`), `saves` (slots and settings, `storage.md`), `expect`.

Expectations: `state {entity, property, equals|min|max}`, `ui {id|selector, text|contains|count|visible|enabled|focused}`, `tick`, `hash`, `capture {reference, size, scale, tolerance, max-diff}`, `pick {x, y, size, entity}` (3D). A failing expectation reports `{kind, line, message, expected, actual}`. Capture compare allows per-channel `tolerance` and a fraction of differing pixels (`max-diff`); a mismatch writes the actual and a diff image into the artifacts folder. `ONEGAME_UPDATE_REFERENCES=1` (or `--update-references`) rewrites references. `--capture-tolerance N` and `--capture-max-diff F` on the game loosen every capture check to at least those values; they are for running references on a different GPU driver than the one that drew them (the Android emulator, for instance).

`ui.visible` is effective visibility: the element and every ancestor must have a true visible flag/binding and neither `display: none` nor `visibility: hidden`. `ui_query` and `ui_tree` return the same meaning through the `hidden` state. This does not measure opacity, clipping, occlusion or intersection with the viewport.

Before each scenario the runner calls `GameDefinition.OnScenarioStarting(game)`, then rebuilds the current screen before applying the scenario's setup and steps. Override the hook to restore defaults for view-side fields and services (settings, selections, animation clocks); its default does nothing. The host reuses the game and graphics resources, and cannot infer defaults for game-owned fields. An in-scenario `reset:` does not call this hook. See [driving a game](../driving-a-game.md#2-scenarios-tests-as-text).

## Capture

Before drawing at a new size or scale the UI is framed until its display list stops changing (at most 9 frames), so bindings that read element bounds see the layout of the capture size; a capture at size S equals a fresh run at S. The previous size is restored the same way afterwards.


PNG at a named size (`360p 720p 1080p 1440p 4k deck phone tablet` or `WxH`) and UI scale. With a graphics device the render stage draws on it and the frame is read back; otherwise the stage's CPU path draws. `CaptureResult.Renderer` says which (`cpu` or the adapter name). A sequence is `frame-NNNN.png` plus `manifest.json` (fps, ticks per frame, per frame tick, state hash and sha256); video is that sequence, with no codec. `ONEGPU_VALIDATION=1` turns on the Vulkan validation layer; `GameInstance.TakeGpuValidationMessages()` returns what it said.

### Sequence audio and video

`capture_sequence` takes `dir`, `count` (1–3600), `ticksPerFrame` (default 1), `fps` (1–1000, default tick rate / ticks per frame), `size` and `scale`. It keeps the existing PNGs and `manifest.json`, and also writes `audio.wav`: the mixed output, including bus effects, gains and limiting, as interleaved 32-bit float WAV at the service's sample rate and channel count. Without an audio service it writes 48 kHz stereo silence. Silent spans are always retained.

Recording starts at the first captured frame, from the mixer's current position. For frame `i` it renders samples from `floor(i * sampleRate / fps)` to `floor((i + 1) * sampleRate / fps)`; total length is `floor(count * sampleRate / fps)` samples (less than one sample short when the duration is not exactly representable). The mixer is manually driven and the device receives silence until recording finishes, so no device clock can race the recording. Repeating the same initial mixer/game state and commands produces byte-identical WAVs. Use `fps` matching the tick cadence for real-time playback; an explicit different fps changes playback duration without changing simulation steps. The sample limit is ten minutes at 48 kHz. Capture advances the existing mixer; it does not rewind voices or record microphone input.

The reply adds `wav`, and either `mp4` or `mp4SkipReason`. If an external `ffmpeg` is on PATH, it encodes `video.mp4` with H.264 video and AAC audio (odd image sizes are padded to even dimensions in the video only). It is never bundled or required; unavailable platforms, a missing executable, encoding errors or the two-minute encoding timeout leave the PNGs, manifest and WAV usable and return a skip reason. MP4 encoding is not a byte-determinism contract. MCP exposes the same reply. The development `--sequence <dir>` option prints it unchanged.
