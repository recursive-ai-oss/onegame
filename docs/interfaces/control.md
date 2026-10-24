# Control layer: one operation set, three faces

Source: `src/OneGame.Control` (operations), `src/OneGame.Host` (the game being driven), `src/OneGame.Testing` (test API, scenarios), `src/OneGame.Live` (development tools: the listener and the dev flags), `src/OneGame.Cli` (`onegame`). Builds on `sync.md` (cluster, recording) and `ui-input-inspect.md`.

## The game host

`GameInstance.Create(GameDefinition, GameOptions)` runs one game: a single-player `Cluster` (an authority node holding `World`, plus a local replica `Replica`), the `UiTree`, a platform window (virtual when headless) and an `IRenderStage`. `Step(n)` runs n frames; each frame drains input, runs one cluster tick (`Cluster.Step(1)`, which steps the authority and publishes to the replica) and updates the UI. `RunRealtime` does this at 60 Hz for a window. `TickCompleted` fires after every tick; `DriftReports` lists any disagreement between replica and authority (it must stay empty and shows as `drift` in `info`).

- `World` is the authority world; state queries, patches, hashes and snapshots act on it. Replacing the world (`Reset`, `LoadFixture`, `LoadSnapshot`) rebuilds the cluster.
- `SaveSnapshot()`/`LoadSnapshot()` are `SaveFile` bytes (header, hashes, versioned snapshot).
- `StartRecording()` returns u6's `Recorder` (checkpoint every tick by default); `Replay(recording)` runs it on a fresh world built like the game's and returns `ReplayResult`. `RecordingFile.Save/Load` handle files (`OGRP`).
- A `GameDefinition` declares entities, `Setup`, `RegisterSystems`, `DefineScreens`, `DefineFixtures`, `Style`, `OnInput`, `DefineFonts` (default: `fonts/*.ttf|otf`), `DefineArt`, and optionally `CreateRenderStage(HostResources)`. The default stage is `Render2DStage` (the 2D renderer, sRGB targets) when the game has fonts and shaders are available (`GameOptions.Shaders`, or `ONEGAME_SHADER_DIR` holding `Program.Entry.spv` files until the shader build package is wired in); otherwise `StubStage`, a GPU clear plus a CPU raster of the display list. A GPU-less capture always uses the CPU raster.
- `GameApp.Run(definition, args)` is the whole `Main` of a game: `--headless --frames N --size WxH --scale S --seed N --gpu auto|off|required --json`. Any other flag needs the development tools (`Dev.Enable()`, called by a module initializer that exists only in Debug builds); without them the game exits 5 `no_dev_tools`.

## Operations

`ControlOps.All` is the table; `ControlSurface.Invoke(op, argsJson)` runs one and returns `ControlResult(Json, Png?)`; `Call(op, argsJson)` returns the JSON text. Failures are `ControlException(code, message)`; codes are stable (`unknown_op`, `bad_args`, `not_found`, `rejected`, `path_outside`, `too_large`, ...). Names use underscores so they are valid MCP tool names. Paths are relative to the output folder and cannot leave it.

| Group | Operations |
|---|---|
| Read | `info` (game, tick, frame, hash, size, screen, screens, fixtures, stage, adapter, drift), `hash`, `state_query`, `commands_list` |
| Change state | `state_patch`, `command_send`, `reset`, `fixture_load`, `snapshot_load`, `step` (frames) |
| Input | `input_pointer`, `input_key`, `input_text`, `input_nav`, `ui_click`, `ui_invoke` |
| UI | `ui_tree`, `ui_query`, `ui_hit`, `screen_open` |
| Output | `snapshot_save`, `capture_frame`, `capture_sequence` |
| Lifecycle | `quit` |

`tools/list` over MCP returns exactly this table (name, description, JSON schema). Entity addressing everywhere: numeric id, `Type` (first of that type) or `Type#n`.

Commands go through the entities' generated `SendFromText(name, args[])` (arguments typed by `CommandSignatures`: int, bool, string, enum, `Fixed`, `EntityId`). A command is queued and runs at the start of the next tick.

## Faces

- **C# test API** (`OneGame.Testing`): `TestCluster.Start(game, options)` → `Client` (`Send`, `Step`, `Call`, `Hash`, `Game`), `Determinism.RunTwice/SnapshotRestore/Replay`, `ScenarioParser` and `ScenarioRunner` (below). Everything goes through `ControlSurface`, so a test does what an agent does.
- **CLI** (`onegame`): verbs `new build run test capture replay mcp export` (`export` is described below). `--json` prints one object. Errors are `{"ok":false,"error":{"code","message"}}`. Exit codes: 0 ok, 1 a check failed, 2 usage, 3 build failed, 4 could not run, 5 needs a development build.
- **MCP**: MCP revision 2025-06-18, JSON-RPC 2.0, tools only (`initialize`, `tools/list`, `tools/call`; captures return `image` content plus text). A Debug game listens on a Unix domain socket (`--live [path]`) and speaks one JSON-RPC message per line; requests are handled on the game thread between frames, so the game stays single-threaded. `onegame mcp` is the stdio bridge agent hosts spawn: `--socket <path>`, or `--project <dir>` to start the game and bridge it, or no option to attach to the only live game. The game starts paused (an agent steps it) unless `--realtime`.

Release builds do not reference `OneGame.Live`, `OneGame.Control`, `OneGame.Testing` or OneYaml and do not compile the start-up hook (`build/Game.targets`, `OneGameDev`), so there is no listener, no operation table and no scenario parser in the binary.

## `onegame export --json`

`onegame export --target linux-x64|win-x64 [--steam] [--steam-redist <dir>] [--out <dir>] [--project <dir>] [--no-archive] [--no-aot] --json` prints one object (guide: `docs/exporting.md`):

```json
{"ok":true,"game":"demo","gameVersion":"1.0.0","engineVersion":"0.1.0","target":"linux-x64","configuration":"Release",
 "runtime":"nativeaot","steam":true,"devTools":false,"renderer":"ui-preview",
 "directory":"/abs/export/demo-1.0.0-linux-x64","executable":"Demo",
 "archive":{"path":"/abs/export/demo-1.0.0-linux-x64.tar.gz","format":"tar.gz","size":3391682,"sha256":"…"},
 "files":[{"path":"Demo","size":6181872,"sha256":"…"}],
 "skipped":[{"path":"shaders/","reason":"…"}],
 "warnings":[{"code":"shaders_not_included","message":"…"}],"seconds":10.2}
```

`runtime` is `nativeaot` or `coreclr-self-contained`; `renderer` is `ui-preview` until the shader package ships, then `2d`; `archive` is absent with `--no-archive`; `files` is every file in `directory` (paths relative, `/`-separated, sorted). Warning codes: `not_native_aot`, `shaders_not_included`, `glibc_too_new`, `font_licence_missing`, `large_file`. Failure codes: `bad_target` (exit 2), `steam_redist_missing` and `dev_tools_present` (exit 4), `publish_failed`, `bad_project` and `no_executable` (exit 3), plus `no_project` and `ambiguous_project` as for the other verbs.

## Scenarios

YAML read through OneYaml's document model; unknown keys are errors with the line. Top level: `name`, `description`, `fixture`, `snapshot`, `window` (`WxH`), `seed`, `steps`, `expect`.

Steps: `click` (an id, `{selector}` or `{x, y}`), `invoke`, `press`, `type`, `nav`, `command {entity, name, args}`, `set {entity, property, value}`, `screen`, `fixture`, `snapshot`, `step: N`, `wheel`, `capture`, `expect`.

Expectations: `state {entity, property, equals|min|max}`, `ui {id|selector, text|contains|count|visible|enabled|focused}`, `tick`, `hash`, `capture {reference, size, scale, tolerance, max-diff}`. A failing expectation reports `{kind, line, message, expected, actual}`. Capture compare allows per-channel `tolerance` and a fraction of differing pixels (`max-diff`); a mismatch writes the actual and a diff image into the artifacts folder. `ONEGAME_UPDATE_REFERENCES=1` (or `--update-references`) rewrites references.

## Capture

PNG at a named size (`360p 720p 1080p 1440p 4k deck phone tablet` or `WxH`) and UI scale. With a graphics device the render stage draws on it and the frame is read back; otherwise the stage's CPU path draws. `CaptureResult.Renderer` says which (`cpu` or the adapter name). A sequence is `frame-NNNN.png` plus `manifest.json` (fps, ticks per frame, per frame tick, state hash and sha256); video is that sequence, with no codec. `ONEGAME_GPU_VALIDATION=1` turns on the Vulkan validation layer; `GameInstance.TakeGpuValidationMessages()` returns what it said.
