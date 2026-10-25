# Solitaire: the first onegame sample

Klondike with a main menu, statistics, settings, a pause dialog and a win dialog. It is small enough to read in an hour and uses the parts a real game needs: a synced entity with commands, views that only read it, the UI kit, SVG art, text, sounds, saves, a store, scenarios and replay.

```
onegame build --json                       # from this folder
onegame run                                # a window
onegame run --headless --frames 60 --json  # no window; prints the state hash
onegame test --json                        # 14 scenarios plus the C# tests
onegame capture --out table.png --size 1080p --scale 1.5 --after 30
onegame export --target osx               # on an Apple-silicon Mac: Solitaire.app, ad-hoc signed; `open` runs it
```

Driving a game the way a person or an agent would is covered in [driving a game](../../docs/driving-a-game.md); the UI vocabulary is in the [UI guide](../../docs/ui-guide.md) and the [component kit](../../docs/interfaces/ui-kit.md).

## How it is built

```
Game.cs        the GameDefinition: entities, screens, fixtures, fonts, art, style
Program.cs     the entry point: services (sound, store), then GameApp.Run
Sim/           the rules. Cards.cs packs a card into an int; Table.cs is the one entity
Views/         what the player sees and touches
Profile.cs     settings and statistics, saved as a small binary file
Services.cs    where files go (disk, memory, store cloud) and the sound interface
Audio.cs       sounds made in code and played through the audio service
scenarios/     YAML tests with captured reference images in scenarios/refs/
tests/         C# tests (xUnit): rules, play, replay, profile, stores, sounds, live session
```

### One entity holds the game

`Table` is a single entity. Its piles are `SyncList<int>` properties (stock, waste, four foundations, seven columns); a card is an int holding its id and whether it is face up. Score, moves, elapsed ticks, phase and pause are synced properties. Everything a player can do is a command: `NewGame`, `Draw`, `Move(from, to, count)`, `AutoMove`, `AutoFinish`, `Undo`, `Pause`. A command that is not legal returns false and bumps `Refused`; it never throws.

The sim rules are enforced by the engine's analyzers, so `Table` has no floats in synced state, no clock, no unordered iteration and no suppressions. The deal shuffles with a named random stream, so the same seed gives the same game. The timer counts ticks (it starts at the first move), not wall time.

Undo is exact: every move appends one packed record to `History`, and undoing restores cards, face-up flags, score, move count and recycles. A test undoes after every move of a whole game and compares state hashes.

Scoring follows the usual Windows Klondike rules: waste to tableau 5, to foundation 10, a turned-up column card 5, a card back from the foundation -15, recycling the waste -100 (draw 1) or -20 (draw 3), and a time bonus of 700000 divided by the seconds for wins slower than 30 seconds.

### Views only read and ask

`Session`, `TableView` and `Screens` are marked `[View]`, so a write to synced state in them is a compile error (OG0008). They read the table, and anything that changes it goes through a command. `Session` keeps the things that are not game state: the selected card, the keyboard cursor, statistics, autosave and sounds.

Each of the 52 cards is a UI element with an SVG background from the art atlas, placed by `TableGeometry` from the board's pixel size. Moves are tweened: when the table changes, a card animates from its old position to its new one. Piles are the focus and click targets; cards ignore the pointer, and hit-testing of cards is geometric, which is what makes dragging a run of cards work. The menu, statistics, settings, pause and win dialogs are built from the kit's `Button`, `Dialog`, `Table`, `Slider`, `Toggle`, `RadioGroup` and `InputGlyph`, and styled through tokens in `Theme.cs`.

Input comes from all three devices through the same code: pointer (click, drag a card or a run of cards, right-click to send a card home), keyboard (arrows move the cursor, Space or Enter picks up and places, `D` draws, `U` undoes, `F` sends the focused card to a foundation, `A` finishes the game, `P` pauses, Esc opens the menu) and gamepad (D-pad moves the cursor, the south button picks up and places, West undoes, North sends a card home or finishes the game, Start pauses). The glyphs in the hint bar follow the last device used.

### Saves, statistics and where files go

`Profile` is the settings and the record book. It is read defensively: every number is range-checked and the history length is capped before anything is allocated, so a damaged file gives a fresh profile and never an exception. The game saves itself after every move; `Continue` loads that file and checks it is a sane game before using it.

`IDataStore` has three implementations: `LocalFileStore` (a folder), `MemoryStore` (headless runs and tests) and `CloudStore` (Steam Cloud). `DataStores.Choose` picks the cloud only when the store is running and the player has cloud saves on, and the local folder otherwise. Steam starts only when `SOLITAIRE_STEAM_APPID` is set.

### Sounds

`AudioSounds` generates five short clips in code (no audio files) and plays them through `AudioService`. With no audio device the service runs silently and the game does the same. Tests use `RecordingSounds` to check which sounds a move makes.

## On Android

The game runs as a NativeAOT APK (see [android](../../docs/interfaces/android.md)). The screens are laid out in dp, a finger drags cards like the mouse, and the back button opens the menu.

```
onegame export --target android --dev --abi x86_64 --json                  # debug-signed APK
onegame android install export/Solitaire-*.apk
onegame android test --package dev.onegame.solitaire --scenario scenarios --capture-tolerance 48 --capture-max-diff 0.001 --json   # scenarios on the device; the references come from another GPU driver
onegame android run --package dev.onegame.solitaire -- --headless --frames 5 --json
```

## On iOS

The game runs as a NativeAOT app bundle (see [ios](../../docs/interfaces/ios.md)); it needs a Mac with Xcode and an iOS Simulator runtime. A finger drags cards like the mouse, the screens are laid out in points, and the game keeps its state across the home button and rotation. `IosOrientation` in the project file limits it to landscape.

```
onegame export --target ios-simulator --dev --json                      # ad-hoc signed .app
onegame ios install export/Solitaire-*-ios-simulator-dev/Solitaire.app
onegame ios test --bundle dev.onegame.solitaire --scenario scenarios --json   # 14 of 14, same state hashes as on Linux
onegame ios live --bundle dev.onegame.solitaire -- --seed 1               # then: onegame mcp --socket tcp:127.0.0.1:7878
onegame export --target ios --json                                        # unsigned device build: the owner signs it
```

## Scenarios and tests

`scenarios/*.yaml` run in a headless game and are the main tests. Numbers `01` to `05` play the game: a scripted win of a known seed (the solver in `tests/Solver.cs` found the line), an illegal move, undo restoring the hash, save and continue ending in the same hash as a straight run, and a gamepad-only walk through menu, moves, pause and statistics. Numbers `10` to `16` capture the screens at 1280x720 and at 1920x1080 with a UI scale of 1.5, and compare against `scenarios/refs/`.

The references were made on the software Vulkan driver (lavapipe), which is deterministic, and match it exactly. Hardware GPUs differ at glyph and curve edges by a few levels (up to 9 of 255 on an RTX 4070, up to 56 on one AMD iGPU screen). When you change how something looks, run `onegame test --update-references` and open the new images before committing them.

`tests/` holds the C# tests: rules (deal, refusals, scoring, timer, undo, snapshots), a full-game run twice, snapshot-and-restore and record-and-replay with hashes compared every tick, replica drift, mouse drag and drop through injected pointer events, keyboard play, profile round trip and damaged input, stores with a fake cloud, sounds, and a complete session over the live-control socket.

## Replay and live control

```
onegame run --headless --json -- --scenario scenarios/01-solve-known-seed.yaml --record game.ogrp --artifacts out
onegame replay game.ogrp --json        # {"ok":true,"identical":true,"ticks":143,...}
onegame run -- --live                  # then attach an MCP client with: onegame mcp
```

A Release build or `onegame export` leaves the live-control listener out.

## In a browser

```
onegame export --target web --json                 # a folder to host: index.html, _framework/, script modules
onegame web test --json --tolerance 2 --max-diff 0.02   # the 13 scenarios, in headless Chrome
```

The game runs the same simulation in the page (the scripted win ends on the same hash as on the desktop). Captures differ from the lavapipe references only along card and glyph edges (up to about 1.5 % of pixels), so the web run allows 2 % of pixels to differ. There is no sound in a page yet and the profile is kept in memory.
