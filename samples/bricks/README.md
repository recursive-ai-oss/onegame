# Bricks: a brick-breaker

The third onegame sample and its first real-time action game: a paddle, a ball, ten levels of bricks (plain, two-hit, three-hit and unbreakable), two power-ups (wide paddle, multi-ball), lives, score, a title screen, settings (sound volume, UI size), a pause dialog and a saved high-score table. Input from keyboard, mouse, touch and gamepad.

```
onegame test --json                 # 16 scenarios (with captures) plus the C# tests
onegame run                         # a window
onegame export --target linux-x64   # NativeAOT
onegame export --target web --dev && onegame web test --tolerance 48 --max-diff 0.02
```

- `Sim/Court.cs` is the one entity: commands `NewGame`, `Launch`, `Steer`, `Push`, `Pause`, `Quit`; fixed-point state; named random streams `serve` and `drops`.
- `Sim/Physics.cs` sweeps the ball against walls, paddle and bricks, so a fast ball never tunnels.
- `levels/*.yaml` are read through OneYaml's generated metadata; unknown keys are errors.
- `Views/` only reads the court and sends commands. Held keys, sticks and the pointer are polled each frame; launch and pause are events.
- `Profile.cs` is the settings and score file, written through the data store.
