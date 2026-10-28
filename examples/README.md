# Examples

Each folder is a complete game of a few dozen lines that shows one feature on the happy-path API. Read `Game.cs` (the entity, the game class) and the scenarios in `scenarios/`; the scenarios are how CI proves the example still works.

| Example | Shows | `Game.cs` |
|---|---|---|
| [sprite](sprite) | A sprite and shapes drawn from an entity's view with `Draw2D`; a capture checked against a reference ([docs/2d.md](../docs/2d.md)) | 44 lines |
| [input-actions](input-actions) | Actions with default bindings, read in the view and turned into commands; driven by `action:` in a scenario ([docs/input.md](../docs/input.md)) | 62 lines |
| [tween](tween) | A tween started from a button, and a `transition` style on a class change ([docs/tweens.md](../docs/tweens.md)) | 43 lines |
| [particles](particles) | A spark emitter and a burst when the state changes ([docs/particles.md](../docs/particles.md)) | 72 lines |
| [saves](saves) | A save slot and a typed settings file ([docs/saves.md](../docs/saves.md)) | 45 lines |
| [pathing](pathing) | **Preview** (`OneGame.Navigation` is a prerelease on a prerelease of OnePath). Two units path round walls on a grid: searches spread over ticks, state synced and restored with the world, re-search when the map changes, paths drawn by the debug overlay ([docs/navigation.md](../docs/navigation.md)) | 270 lines |
| [networked-counter](networked-counter) | One shared `Sim.cs`, a server and two clients on real sockets, replicas equal to the server ([docs/driving-a-game.md](../docs/driving-a-game.md), section 7) | 23 + 14 + 12 lines |

## Run one

```
cd examples/sprite
onegame run                  # a window
onegame run --headless --frames 60 --json
onegame test --json          # the scenarios
```

These projects build against this checkout (`examples/Directory.Build.props` points at it). Outside a checkout, `onegame new` writes the same project on the `RecursiveAI.OneGame.Sdk` package; copy a `Game.cs` into it.

The networked example needs its server and client published first; `onegame cluster test` then starts three processes:

```
cd examples/networked-counter
dotnet publish server/NetworkedCounterExampleServer.csproj -c Debug -o bin
dotnet publish NetworkedCounterExample.csproj -c Debug -o bin
onegame cluster test cluster/play.yaml --spec cluster.yaml --json
```

## Run them all

```
tools/ci/examples.sh onegame out     # every example's scenarios, plus the cluster; non-zero exit if any fails
```
