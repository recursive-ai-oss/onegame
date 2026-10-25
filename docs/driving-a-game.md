# Driving a game

onegame has no editor. You build a game, run it, look at it, poke it and replay it through one set of operations that comes in three forms: the `onegame` command line, a C# test API, and a live connection (MCP) to a running Debug build. This page shows each, in the order you usually need them. The operation list and JSON shapes are in `docs/interfaces/control.md`.

## 1. Make and run a game

```
onegame new shop            # shop/: shop.csproj, Program.cs, Game.cs, scenarios/, README.md
cd shop
onegame build --json        # {"ok":true,"errors":0,"warnings":0,...}
onegame run --headless --frames 60 --json
#  {"ok":true,"game":"shop","frames":60,"tick":60,"hash":"...","width":1280,"height":720}
```

A game is a `GameDefinition` (entities, `Setup`, screens, fixtures) plus entity classes: synced state as partial properties, commands as methods, simulation in `OnTick`, views as `[View]` members. The scaffold shows all of it in 40 lines. Warnings are errors; a sim or view rule you break is a compile error with an `OG....` id and a message that says the fix.

Fonts: put `.ttf`/`.otf` files in a `fonts/` folder next to the game (or override `DefineFonts`). With fonts the 2D renderer draws the UI through the GPU, using the shaders the engine compiles during its own build; without fonts a plain preview is drawn, enough to check layout. SVG art goes in `DefineArt`.

## 2. Scenarios: tests as text

`scenarios/*.yaml`, one file per case:

```yaml
name: click-button
window: 320x200            # optional; also fixture, snapshot, seed
steps:
  - click: add             # an element id; or {selector: "Button"} or {x: 10, y: 20}
  - step: 1                # frames
  - command: { entity: Counter, name: Increment, args: [5] }
  - set: { entity: Counter, property: Count, value: 40 }
  - set: { table: lanes, row: 2, property: Heat, value: 7 }   # one cell of a [HotData] table
  - press: Tab             # keys, also: type: "text", nav: next, wheel, screen, fixture, snapshot, reset: 77, ui-scale: 1.5, invoke
  - nav: start             # a controller button: up down left right next previous activate cancel, or any button by name (start, back, west, north, x, y, left_stick...)
expect:
  - state: { entity: Counter, property: Count, equals: 3 }
  - state: { entity: Ship, property: Fitted.count, equals: 2 }   # a SyncList: count, [index], or the whole list as JSON
  - state: { table: lanes, row: 2, property: Heat, equals: 7 }
  - ui: { id: count, text: "Count: 3" }       # or selector + count/contains/visible/enabled/focused/pressed
  - tick: 3
  - capture: { reference: refs/main.png, size: 320x200, tolerance: 2, max-diff: 0.001 }
```

`onegame test --json` runs them all (and the project's C# tests; `--no-unit` skips those). A failing expectation reports `expected` and `actual` with the line. Unknown keys are errors with a line number. Run one with `onegame test --scenario scenarios/x.yaml`. `onegame test --update-references` creates missing capture references and rewrites changed ones; look at them before committing. With `--json`, the result has `references: {created: [...], updated: [...]}` (absolute paths) and each such expectation carries `referenceAction`.

**Addressing state.** `entity:` is a numeric id, a type name (the first of that type) or `Type#n` (the nth, from 1). `property:` is a path into that entity's JSON: a name (`Gold`), an index into a list (`Fitted[0]`), more names after it (`Fitted[0].Kind`), and on a list `count` for its length. A bare list name reads the whole list as JSON text. A hot table is addressed by its name (`[HotData(Name = "lanes")]`, default the struct's name) with `table:` instead of `entity:`; `row:` (from 0) picks a row and `property:` is then a column (without `row:`, a path into the whole table such as `[2].Heat` or `count`). `set:` writes one scalar (number, bool, string, `Fixed` like `12.5`, enum by number) of an entity property or of a table cell; lists and nested values cannot be set. `set:` changes state outside commands, so a running recording ends its segment and continues from the edited state, and replay reproduces the hashes.

From C#:

```csharp
using var cluster = TestCluster.Start(new ShopGame(), new TestOptions { Width = 320, Height = 200 });
cluster.Client.Click("buy").Step(2);
Assert.Equal("2", cluster.Client.Get("Shop", "Stock"));
Determinism.RunTwice(() => new ShopGame(), drive, 120).ThrowIfDifferent();
Determinism.SnapshotRestore(() => new ShopGame(), drive, 120, splitFrame: 50).ThrowIfDifferent();
Determinism.Replay(() => new ShopGame(), drive, 120).ThrowIfDifferent();
```

## 3. Look at it

```
onegame capture --out shots/main.png --size 1080p --scale 2 --after 30 --json
onegame capture --sequence shots/run --count 60 --every 2        # frame-0000.png ... + manifest.json
onegame lint-theme --json                                    # WCAG AA contrast of the UI theme; exit 1 and the failing token pairs when one fails
```

Sizes: `360p 720p 1080p 1440p 4k deck phone tablet` or `WxH`. Open the PNG and look at it: a hash that matches proves repeatability, not that the picture is right. The JSON says which renderer made it (`cpu` or the GPU's name). `ONEGAME_GPU_VALIDATION=1` turns on the Vulkan validation layer.

## 4. Record and replay

```
onegame run --headless -- --scenario scenarios/click-button.yaml --record run.ogrp --artifacts .
onegame replay run.ogrp --json      # {"ok":true,"identical":true,"ticks":2,"commands":2,"finalHash":"..."}
```

A replay that diverges exits 1 and names the first tick and the hashes. Recordings start from a snapshot, so a bug found in a live session can be replayed after the fix. A recording is a session: one segment per world the run used. A game that starts a new campaign (`Game.Reset(seed)`), a scenario `reset:`, `fixture:` or `snapshot:`, and a `set:` or `state_patch` (a change outside commands) each end a segment and start the next from the new state, seed included, so replay reproduces the run exactly; the replay summary reports `segments`, and a divergence names the `segment`. In code: `game.StartSessionRecording()`, `RecordingFile.Save(session, path)`, `game.Replay(session)`. A single-segment session is the plain recording file older tools read. A `set:` while commands are queued and a recording runs is rejected (step first). The scenario's own start state counts, so a scenario `fixture:`/`seed:`/`snapshot:` is recorded too. A replay of a recording with no ticks and no commands exits 1 (`identical:false`) because nothing was compared; `--record` itself fails with `record_empty` when a scenario ran ticks but the recording holds none.

Ids may contain `/` (the kit makes ids like `qty/track`) and, in a selector, any other character escaped with a backslash (`#a\.b`). A selector that does not parse is a failed step or expectation with its line and reason (`bad_args: Bad selector ...`), never a crash.

## 5. Drive a running game (MCP)

Debug builds listen on a local socket: `onegame run -- --live` (add `--realtime` to let it run on its own; otherwise it waits for `step`). Point an MCP client at the stdio bridge:

```json
{ "mcpServers": { "shop": { "command": "onegame", "args": ["mcp", "--project", "."] } } }
```

`--project` starts the game and bridges to it; `--socket <path>` attaches to a given one; with neither, the bridge attaches to the only live game. Tools: `info`, `hash`, `state_query`, `state_patch`, `commands_list`, `command_send`, `input_pointer`, `input_key`, `input_text`, `input_composition`, `clipboard_get`, `clipboard_set`, `input_nav`, `step`, `ui_tree`, `ui_query`, `ui_invoke`, `ui_click`, `ui_hit`, `screen_open`, `fixture_load`, `reset`, `snapshot_save`, `snapshot_load`, `capture_frame`, `capture_sequence`, `shaders`, `quit`. A typical session: `info` → `state_query` → `ui_click` → `step 10` → `ui_tree` → `capture_frame`. Errors come back as `{"error":{"code","message"}}` with a stable code. Release builds contain none of this.

## 6. Run it in a window

`onegame run` without `--headless` opens a real window on Wayland or X11 (`--backend wayland|x11`, `--display NAME`, `--present vsync|lowlatency|immediate`). Add `--live` and an agent can drive it exactly like a headless one while it presents:

```
onegame run -- --live /tmp/shop.sock --realtime --backend x11 --display :88
onegame mcp --socket /tmp/shop.sock     # then: info, ui_click, input_text, clipboard_set, capture_frame {"presented":true}
```

`capture_frame` renders offscreen at any size; with `"presented": true` it returns the frame the window actually shows. `input_*` operations go through the same queue as the keyboard and mouse, so a text field cannot tell them apart; `input_composition` plays an input method's pre-edit text and `clipboard_get`/`clipboard_set` read and write the system clipboard (paste with `input_key` `v` + `Control`). With no display (a CI machine) run headless, or start `Xvfb :88 -noreset` for a window.

### Shader hot reload

In a development build, a game that registered its material shaders (`docs/interfaces/render2d.md`) picks up edits to the `.hlsl` file while it runs; no restart, and the tick and frame counts carry on. `capture_frame` and `shaders` check the files first, so edit, then capture, and the frame shows the change. The `shaders` result lists each shader with `state`:

```
{"shaders":[{"name":"glow","path":"Shaders/Glow.hlsl","state":"ok","reloads":2}],"changed":["glow"]}
{"shaders":[{"name":"glow","state":"incompatible","message":"The shader's interface changed, so it cannot replace the running one: Params.P changed array length from 2 to 3. Rebuild the project to use it."}]}
{"shaders":[{"name":"glow","state":"compile_error","problems":[{"file":"Shaders/Glow.hlsl","line":21,"column":48,"code":"OSH0200","message":"Expected ';' ..."}]}]}
```

On `incompatible` or `compile_error` the previous shader keeps drawing; fix the file and the next check recovers. A changed cbuffer layout, vertex input or binding needs a rebuild. Exports and Release builds have none of this.

## 7. Servers and clusters

A game with a dedicated server (`onegame new shop --server` adds `server/`, a small project that compiles the game's `Sim.cs`) runs as separate processes on a real network:

```
onegame export --server --project server --out ./srv     # NativeAOT linux-x64 server, same JSON report as a game export
onegame run -- --connect udp://127.0.0.1:7777 --headless --live /tmp/p1.sock --realtime   # a client on the server's world
```

A cluster file names the processes; `onegame cluster` starts, inspects and stops them, and every node has its own live socket:

```yaml
name: shop
nodes:
  - { name: arena, role: server, exe: srv/shop-server-1.0.0-linux-x64/shopServer, listen: udp+ws, scopes: [{ id: 1, seed: 7 }] }
  - { name: p1, role: client, exe: bin/shop, connect: arena, transport: udp, net: { latencyMs: 100, lossRate: 0.02 } }
  - { name: p2, role: client, exe: bin/shop, connect: arena, transport: ws }
```

```
onegame cluster up --spec cluster.yaml --json      # pids, sockets, bound ports
onegame cluster call p1 command_send '{"entity":"Counter","command":"Increment","args":["1"]}'
onegame cluster hashes --json                      # every replica against its server at the same tick; exit 1 on a mismatch or drift
onegame cluster test scenarios/two-players.yaml --spec cluster.yaml   # up, run, down
onegame cluster down
onegame mcp --cluster .onegame-cluster             # one MCP server for all nodes; every tool takes a node argument
```

In C#: `TestCluster.Launch(spec)` returns a `ProcessCluster` (`Call`, `WaitForTick`, `Hashes`, `Run(scenario)`, `Down`). A cluster scenario aims steps and expectations at nodes with `node:`; `wait`, `agree` and `drift` are its own verbs. `docs/interfaces/cluster.md` has the details.

## 8. Ship it

```
onegame export --target linux-x64 --steam --json     # a folder and a .tar.gz under ./export, nothing else in them
onegame export --target win-x64 --json              # a folder and a .zip
```

The JSON lists every file with its size and hash, and the warnings worth acting on. Release builds only; see `docs/exporting.md`.

## Exit codes

0 ok, 1 a check failed (scenario, replay, tests), 2 bad command line, 3 build failed, 4 could not run, 5 the option needs a Debug build.
