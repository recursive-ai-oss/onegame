# input: actions and rebinding

Owner: `src/OneGame.Platform/Actions` (namespace `OneGame.Platform`), the host wiring in `src/OneGame.Host` (`GameInstance.Actions`, `ActionUi`), Steam as a source in `src/OneGame.Steam/Input/SteamActionSource.cs`, harness in `OneGame.Control` (`actions`, `input_pad`) and `OneGame.Testing` (scenario steps). Raw events (`platform.md`) and `SteamInputService` stay usable directly; actions are built on them.

## Declare, read

```csharp
public override void DefineActions(ActionMap actions) =>
    actions.Context("play", c => c
        .Button("jump", Bind.Key(Key.Space), Bind.Pad(GamepadButton.South))
        .Vector("move", Bind.Wasd, Bind.LeftStick, Bind.DPad)
        .Axis("zoom", Bind.Wheel(), Bind.Pair(Key.Q, Key.E)));

public override void OnEachFrame(GameInstance game)          // view code: read actions, send commands
{
    if (game.Actions.Pressed("jump")) player.Send.Jump();
    var m = game.Actions.Vector("move");                     // x right, y DOWN, length <= 1
    if (m.Length > 0.5f) player.Send.Step((int)MathF.Round(m.X), (int)MathF.Round(m.Y));
}
```

- **Kinds.** `Button` (`Pressed`/`Held`/`Released`, `Value` = analog strength), `Axis` (-1..1; wheel sources give notches), `Vector` (`X`,`Y`; keys give length 1 on diagonals). Axes and vectors also have `Held` (magnitude past `Threshold`, default 0.5) so a stick can act as a button.
- **Sources** (`Bind.*`): `Key`, `Mouse`, `Pad` (any connected pad), `PadAxis(axis, sign)`, `Wheel`, `Touch(region)` (a finger in a half of the window), composites `Pair(neg, pos)`, `Quad(l, r, u, d)` / `Keys` / `Pads`, `Wasd`, `Arrows`, `DPad`, `LeftStick`, `RightStick`. Several bindings per action; the strongest wins. Buttons/axes take single sources (axes also pairs), vectors take quads and sticks; the wrong shape throws at declaration. Names are letters, digits, underscores (the same rule as Steam Input), unique across the map; an unknown name in a query throws.
- **Tuning.** `c.Tune("move", deadZone: 0.25f, threshold: 0.6f, title: "Move")`. Stick/axis dead zone is radial and rescaled (default 0.15, on top of the platform's own 0.1 raw zone). Pad values are the platform's standard layout, so SDL-mapped devices need nothing here.
- **Edges.** The host updates the map once per frame, before `OnEachFrame`. A press shorter than a frame (injected `KeyPress`, a tap) still reads `Pressed` and `Released` in the next frame. Keys are ignored while a text field has the keyboard (`KeyboardCaptured`).
- **Contexts.** `Context(name, ...)` groups actions; `OnScreens("menu")` makes a context active only while those screens are open (contexts naming no screen stay on). Manual: `SetActive`, `Activate`, `Deactivate`. Inactive actions read zero and release what they held.
- **Determinism.** Actions are view state. They never write synced state; the game sends commands from what it reads, so recordings, replays and hashes see commands only.
- Outside the host: call `map.Observe(event)` for every platform event and `map.Update(app.Input, width, height)` once per frame. `Update` allocates nothing.

## Other sources

`map.CreateSource(name)` returns an `ActionSource` (`Set(action, down)`, `Set(action, x, y)`, `Pulse(action)` = held for exactly one frame, `Release`, `Clear`); the strongest source wins. `map.Harness` is the one scenarios and live control drive.

**Steam Input.** `SteamActionSource.CreateActions(map)` builds the `SteamInputActions` declaration (one set per context, titles and screens from the map) so the map is declared once and the actions file is generated from it. `new SteamActionSource(service, map)` then feeds same-named, same-kind actions (button, vector, one-axis) each `Update` from what `SteamInputService.Update` last saw (call the bridge or service in `OnEachFrame` as before). Steam's stick y is up-positive and is flipped to the action's down-positive y. Steam keeps one set active (the context naming the open screen, else the first). The stand-in controller (`ONEGAME_STEAM_INPUT=fake` / `StartFake`, `steam-input:` step) drives it in tests.

## Rebinding

- `map.BeginRebind(action, slot = 0, part = 0, ConflictPolicy.Allow|Replace)` waits for the next key, mouse button, pad button, pad axis push (>= 0.6) or wheel turn; Escape cancels. While waiting `Observe` swallows input events (the host does not pass them on to the UI). `Rebinding` tells what is pending; `RebindFinished` gives a `RebindResult` (source, cancelled, the other actions in the same context that already used it). A slot equal to the binding count adds one; `part` picks a source of a pair/quad (left, right, up, down). A stick slot takes only a stick axis.
- `SetBinding`, `RemoveBinding`, `ResetBindings(action?)`, `FindConflicts`, `IsDefault`, `Version` (bumps on any change).
- **Save/load.** `ToText()` writes only the actions that differ from the declared bindings, one line each (`onegame-bindings 1` header, then `jump = key:J | pad:South`, `move = quad(key:A,key:D,key:W,key:S) | stick:left`). `LoadText` first resets everything, then applies the lines; a bad line (unknown action, wrong shape for the kind, unknown control, over 16 bindings, over 1 KiB) is skipped and listed in `Problems`; text over 64 KiB is rejected whole. `Save(path)` writes through a temporary file, `Load(path)` treats a missing file as "defaults", `ActionMap.DefaultPath(gameName)` is `<local app data>/<game>/bindings.txt`. Loading is the game's call (for example in `Setup`-time view code or on startup); the engine does not load on its own.
- **UI.** `ActionUi.BindingList(game, glyphs?, context?)` lists actions with title, glyph, current bindings, Rebind (Button and single-source Axis actions) and Reset; ids `action-bindings`, `rebind-NAME`, `reset-NAME`, `binding-NAME`. `ActionUi.SyncGlyphs(map, glyphs)` points an `InputGlyphs` table at the current bindings, so `UiKit.InputGlyph(glyphs, "jump")` shows the key or button the player uses.

## Harness

- Control operation `actions`: no arguments reports contexts and every action (`kind`, `context`, `active`, `held`, `pressed`, `released`, `x`, `y`, `default`, `bindings` in the file spelling). `name` drives the action as the harness source: `state` `tap` (default; a button is held and pressed for exactly one frame), `down` (held until `up`), `up`; axes/vectors take `x`/`y`. `contexts: [...]` switches contexts, `reset: true` restores bindings. Errors: `no_actions`, `unknown_action`, `bad_args`.
- Control operation `input_pad`: `button` (names of `input_nav`) with `state` press/down/up, or `axis` (`leftx lefty rightx righty lefttrigger righttrigger`) with `value`, on the harness's virtual pad.
- Scenario steps: `action: jump`, `action: { name: move, x: 1, y: 0 }`, `action: { name: move, state: up }`; `pad: { button: south }`, `pad: { axis: leftx, value: 1 }`; `press: { key: d, state: down }` (and `up`). Expectation: `action: { name: move, held: true, x: 1 }` (also `pressed`, `released`, `y`). Existing `press:`, `nav:`, `click:` steps are unchanged. Changes show after the next `step:`.
