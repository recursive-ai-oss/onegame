# Bricks: a brick-breaker

The third onegame sample and its first real-time action game: a paddle, a ball, ten levels of bricks (plain, two-hit, three-hit and unbreakable), two power-ups (wide paddle, multi-ball), lives, score, a title screen, settings (sound volume, UI size), a pause dialog and a saved high-score table. Input from keyboard, mouse, touch and gamepad.

```
onegame test --json                 # 16 scenarios (with captures) plus the C# tests
onegame run                         # a window
onegame export --target linux-x64   # NativeAOT
onegame export --target web --dev && onegame web test --tolerance 48 --max-diff 0.02
```

## How to run it

**Desktop.** `onegame run` opens a window; `onegame export --target linux-x64` (or `win-x64`, `osx`) writes a NativeAOT folder with `Bricks` and `run.sh`. Keys: arrows or A/D move, Space or Enter launches, P pauses. The mouse steers by position, a gamepad by stick or d-pad.

**Browser.** `onegame export --target web` writes a folder for any static server (Chrome or Edge 113+ with WebGPU). `onegame export --target web --dev && onegame web test --tolerance 48 --max-diff 0.02` runs the scenarios in headless Chrome.

**Android.** `onegame export --target android --abi arm64-v8a --dev` writes a debug-signed APK. With an emulator or a phone attached (see `docs/devices.md`): `onegame android install <apk>`, then `onegame android test --scenario scenarios --json -- --capture-tolerance 48 --capture-max-diff 0.02`. The game is portrait and a finger steers the paddle.

**iPhone and iPad.** On a Mac, `onegame export --target ios-simulator --dev`, then `onegame ios install <Bricks.app>` and `onegame ios test --scenario scenarios --app <Bricks.app> --json -- --capture-tolerance 8 --capture-max-diff 0.02`. A real device needs a signed `--target ios` build (see `docs/devices.md`).

Android and iOS reach the desktop scenarios' state hashes. In the browser all but `capture-high-scores` do: a page's click really moves its pointer, and the court steers by the pointer, so a click before a new game changes that game. Phones and pages have no Quit button, so their title capture needs the looser tolerance above.

## What is in it

- `Sim/Court.cs` is the one entity: commands `NewGame`, `Launch`, `Steer`, `Push`, `Pause`, `Quit`; fixed-point state; named random streams `serve` and `drops`.
- `Sim/Physics.cs` sweeps the ball against walls, paddle and bricks, so a fast ball never tunnels.
- `levels/*.yaml` are read through OneYaml's generated metadata; unknown keys are errors.
- `Views/` only reads the court and sends commands. Held keys, sticks and the pointer are polled each frame; launch and pause are events.
- `Profile.cs` is the settings and score file, written through the data store.
