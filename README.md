# onegame

A C#-native game engine with no editor: a game is plain C# plus text assets. Entities, sync, rendering and UI are generated or enforced at build time by Roslyn source generators and analyzers, everything is NativeAOT- and trim-clean, and a command line, a C# test API and a live-control protocol (MCP) let you build, run, inspect, capture and replay games from a terminal.

It runs on Windows, Linux (X11 and Wayland, the Steam Deck), macOS, Android, iOS and the web (WebGPU). Clients and servers run the same stack, so state syncs between processes, with prediction, authority transfer and drift detection built in.

One class holds an entity's synced state, its commands, its deterministic simulation and its view. The generator writes the change tracking, codecs, deltas and snapshots; the analyzers turn the sim and view rules into compile errors:

```csharp
/// <summary>One synced counter. Sim state is partial properties; views read it and send commands.</summary>
[Entity]
public partial class Counter : Entity
{
    [Synced] public partial int Count { get; private set; }
    [Synced] public partial int Ticks { get; private set; }

    [Command]
    public void Increment(int by) => Count += by;

    // Runs once per fixed simulation tick: deterministic code only (no floats, clocks or I/O in synced state).
    protected override void OnTick() => Ticks++;

    // View code reads state and never writes it.
    [View]
    public string Label() => $"Count: {Count}";
}
```

## Getting started

You need the .NET 10 SDK and nothing else (a NativeAOT export also needs the platform's C++ linker: `clang` and `zlib1g-dev` on Linux, the Visual Studio C++ tools on Windows).

```
dotnet tool install -g RecursiveAI.OneGame.Cli     # the onegame command
onegame new mygame
cd mygame
onegame run --headless --frames 60 --json         # build and run; drop --headless for a window
onegame test --json                                # the scenarios in scenarios/
onegame export --target linux                      # or win, osx: a NativeAOT build in ./export
```

`onegame new` writes a project on the `RecursiveAI.OneGame.Sdk` MSBuild SDK, which brings the engine libraries, the analyzers, the development tools of Debug builds and the export targets from nuget.org, at the same version as the tool. Add `--server` for a dedicated-server project beside the game. Web, Android and iOS exports and plugin servers still need an engine checkout (see below).

**Working on the engine:** clone this repository, `dotnet build OneGame.slnx`, and create games against it with `dotnet run --project src/OneGame.Cli -- new mygame --dir ../mygame --engine .` (or set `ONEGAME_ROOT` to the checkout). Such a game references the engine's projects instead of packages.

Then read, in order:

- [docs/first-game.md](docs/first-game.md): the game `onegame new` writes, line by line, then run, test and export.
- [docs/driving-a-game.md](docs/driving-a-game.md): making a game, scenarios, replays, captures and live control.
- [docs/ui-guide.md](docs/ui-guide.md): screens, styling and the component kit.
- [docs/exporting.md](docs/exporting.md): Windows, Linux, macOS, Android, iOS, web and dedicated-server builds.
- [docs/devices.md](docs/devices.md): checking a build on real phones, tablets, the Steam Deck and gamepads.
- `docs/interfaces/`: engine-internal contracts between engine areas, for people working on the engine itself; not the user guide.

## Samples

- `samples/solitaire`: a UI-heavy card game.
- `samples/bricks`: a real-time brick-breaker with levels in YAML, power-ups and a high-score table.
- `samples/lanterns`: a networked 3D game on two island servers and a master, with players moving between servers and trades settled through the master.

Each sample has scenarios (`onegame test --json` in its folder) and a README.

## Build and test the engine

Requires the .NET SDK pinned in `global.json`.

```
dotnet build OneGame.slnx
dotnet test --solution OneGame.slnx
```

Build output goes to `artifacts/` at the repository root; set `ONEGAME_ARTIFACTS` to put it elsewhere. Shaders are compiled during the build by `RecursiveAI.OneShader.Build`, and audio decoding and mixing come from `RecursiveAI.OneAudio`; both restore from nuget.org. Native AOT publishing on Linux needs `clang` and `zlib1g-dev`.

## Layout

- `src/`: the engine, one project per area, the `OneGame.Generators` analyzers and source generators, and the `onegame` command in `OneGame.Cli`.
- `tests/`: one test project per area, plus `OneGame.AotSmoke`, a native-AOT app that touches every runtime assembly.
- `samples/`: the sample games.
- `build/`: the MSBuild targets games import.
- `tools/`: CI and device scripts.
- `docs/`: documentation.

## Build rules

- Only our code ships: projects under `src/` and `samples/` may reference the one* family of libraries and nothing else, and the build fails otherwise (`build/PackageGuard.targets`). Test and tool projects may use any package.
- Warnings are errors, nullable is on, builds are deterministic, and every shipping project is checked by the AOT and trim analyzers.

## License

Licensed under either of MIT ([LICENSE-MIT](LICENSE-MIT)) or Apache-2.0 ([LICENSE-APACHE](LICENSE-APACHE)), at your option. Copyright (c) Recursive AI, LLC.
