# Input: actions and rebinding

Games read **actions** ("jump", "move"), not keys. One declaration answers the keyboard, the mouse, gamepads, touch, Steam Input and the test harness, and players can rebind it. Actions are view state: you read them in view code and send commands; they never write synced state, so recordings and replays see commands only.

## Declare, read, send

```csharp
using OneGame.Platform;

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

- **Kinds.** `Button` (`Pressed`, `Held`, `Released`, `Value`), `Axis` (-1 to 1; the wheel gives notches), `Vector` (`X`, `Y`; diagonals from keys have length 1). Axes and vectors also have `Held`, true once the magnitude passes a threshold, so a stick can act as a button.
- **Bindings** (`Bind.*`): `Key`, `Mouse`, `Pad`, `PadAxis`, `Wheel`, `Touch`, composites `Pair`, `Quad`, and the ready-made `Wasd`, `Arrows`, `DPad`, `LeftStick`, `RightStick`. Give an action several; the strongest wins. A wrong shape (a stick on a button) throws when declared, and so does asking for an action that was never declared.
- **Tuning:** `c.Tune("move", deadZone: 0.25f, threshold: 0.6f, title: "Move")`. The title is what the rebinding list shows.
- **A press shorter than a frame** still reads `Pressed` and `Released` in the next frame. Keys are ignored while a text field has the keyboard.

## Contexts

A context groups actions. `OnScreens` makes it active only while those screens are open, so menu and play bindings do not mix:

```csharp
actions.Context("menu", c => c.OnScreens("menu").Button("back", Bind.Key(Key.Escape)));
```

Inactive actions read zero. `SetActive`, `Activate` and `Deactivate` switch them by hand.

## Rebinding and saved bindings

```csharp
game.Actions.BeginRebind("jump");                  // waits for the next key, button or stick push; Escape cancels
game.Actions.ResetBindings();                      // back to the declared defaults

game.Actions.Save(ActionMap.DefaultPath("mygame")); // only what differs from the defaults, as text
var result = game.Actions.Load(ActionMap.DefaultPath("mygame")); // a missing file means defaults
foreach (var problem in result.Problems) Console.WriteLine(problem);
```

The engine does not load saved bindings on its own: call `Load` at start-up from view code and `Save` when the player leaves the settings screen. A bad line in the file is skipped and reported in `Problems`; the rest still applies. The file reads like `jump = key:J | pad:South`.

For the UI, `ActionUi.BindingList(game)` returns a ready element listing every action with its current bindings, a Rebind button (for buttons and single-source axes) and Reset:

```csharp
screens.Add("settings", game => Ui.Column(ActionUi.BindingList(game)).Padding(Length.Px(8)));
```

`ActionUi.SyncGlyphs(game.Actions, glyphs)` points an `InputGlyphs` table at the current bindings, so `UiKit.InputGlyph(glyphs, "jump")` shows the key or button the player actually uses.

## Steam Input

Declare the actions once. The Steam actions file is generated from the same map, and Steam controllers then feed the same-named actions:

```csharp
using var service = SteamInputService.StartFake(SteamActionSource.CreateActions(map));
using var steam = new SteamActionSource(service, map);
```

(`StartFake` is the stand-in controller for tests; a shipped game starts the real `SteamInputService`.) Steam's stick y is flipped to the action's y-down. Steam keeps one action set active: the context that names the open screen, else the first.

## Testing it

Scenario steps drive the same actions a player would:

```yaml
steps:
  - action: jump                                   # a one-frame tap
  - action: { name: move, state: down, x: 1, y: 0 }
  - step: 3
  - expect: { action: { name: move, held: true, x: 1 } }
  - action: { name: move, state: up }
  - pad: { button: south }                         # the harness's virtual gamepad
  - pad: { axis: leftx, value: 1 }
  - press: { key: d, state: down }                 # a raw key, for code that reads keys
```

Changes show after the next `step:`. Expectations also take `pressed`, `released` and `y`. The control operation `actions` lists every action with its state and bindings, and can drive one (`name`, `state` of `tap`, `down` or `up`) or switch `contexts`; `input_pad` presses buttons and moves axes on the virtual pad.

## Reference

The input types are in the API reference: [OneGame.Platform](api/OneGame.Platform/index.md) (`ActionMap` and the devices).
