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

Fonts: put `.ttf`/`.otf` files in a `fonts/` folder next to the game (or override `DefineFonts`). With fonts and compiled shaders (`ONEGAME_SHADER_DIR`, until the shader build package is wired in) the 2D renderer draws the UI; without them a plain preview is drawn, enough to check layout. SVG art goes in `DefineArt`.

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
  - press: Tab             # keys, also: type: "text", nav: next, wheel, screen, fixture, snapshot, invoke
expect:
  - state: { entity: Counter, property: Count, equals: 3 }
  - ui: { id: count, text: "Count: 3" }       # or selector + count/contains/visible/enabled/focused
  - tick: 3
  - capture: { reference: refs/main.png, size: 320x200, tolerance: 2, max-diff: 0.001 }
```

`onegame test --json` runs them all (and the project's C# tests; `--no-unit` skips those). A failing expectation reports `expected` and `actual` with the line. Unknown keys are errors with a line number. Run one with `onegame test --scenario scenarios/x.yaml`. `onegame test --update-references` writes new or changed capture references; look at them before committing.

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
```

Sizes: `360p 720p 1080p 1440p 4k deck phone tablet` or `WxH`. Open the PNG and look at it: a hash that matches proves repeatability, not that the picture is right. The JSON says which renderer made it (`cpu` or the GPU's name). `ONEGAME_GPU_VALIDATION=1` turns on the Vulkan validation layer.

## 4. Record and replay

```
onegame run --headless -- --scenario scenarios/click-button.yaml --record run.ogrp --artifacts .
onegame replay run.ogrp --json      # {"ok":true,"identical":true,"ticks":2,"commands":2,"finalHash":"..."}
```

A replay that diverges exits 1 and names the first tick and the hashes. Recordings start from a snapshot, so a bug found in a live session can be replayed after the fix.

## 5. Drive a running game (MCP)

Debug builds listen on a local socket: `onegame run -- --live` (add `--realtime` to let it run on its own; otherwise it waits for `step`). Point an MCP client at the stdio bridge:

```json
{ "mcpServers": { "shop": { "command": "onegame", "args": ["mcp", "--project", "."] } } }
```

`--project` starts the game and bridges to it; `--socket <path>` attaches to a given one; with neither, the bridge attaches to the only live game. Tools: `info`, `hash`, `state_query`, `state_patch`, `commands_list`, `command_send`, `input_pointer`, `input_key`, `input_text`, `input_nav`, `step`, `ui_tree`, `ui_query`, `ui_invoke`, `ui_click`, `ui_hit`, `screen_open`, `fixture_load`, `reset`, `snapshot_save`, `snapshot_load`, `capture_frame`, `capture_sequence`, `quit`. A typical session: `info` → `state_query` → `ui_click` → `step 10` → `ui_tree` → `capture_frame`. Errors come back as `{"error":{"code","message"}}` with a stable code. Release builds contain none of this.

## Exit codes

0 ok, 1 a check failed (scenario, replay, tests), 2 bad command line, 3 build failed, 4 could not run, 5 the option needs a Debug build.
