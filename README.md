# onegame

A C#-native game engine with no editor: a game is plain C# plus text assets. Entities, sync, rendering and UI are generated or enforced at build time by Roslyn source generators and analyzers, everything is NativeAOT- and trim-clean, and a command line, a C# test API and a live-control protocol (MCP) let you build, run, inspect, capture and replay games from a terminal.

It runs on Windows, Linux (X11 and Wayland, the Steam Deck), macOS, Android, iOS and the web (WebGPU). Clients and servers run the same stack, so state syncs between processes, with prediction, authority transfer and drift detection built in.

## Getting started

Games are built against a checkout of this repository. The `RecursiveAI.OneGame.*` packages on nuget.org carry the engine libraries and analyzers, but a game's build (`build/Game.targets`, the `onegame` command, exports) currently comes from the checkout, so start there:

```
git clone https://github.com/recursive-ai-oss/onegame
cd onegame
dotnet build OneGame.slnx
dotnet run --project src/OneGame.Cli -- new mygame --dir ../mygame --engine .
cd ../mygame
dotnet run --project ../onegame/src/OneGame.Cli -- run --headless --frames 60 --json
```

Publishing the command once (`dotnet publish src/OneGame.Cli -c Release -r linux-x64`, or `win-x64`/`osx-arm64`) and putting it on your `PATH` lets you type `onegame new`, `onegame test`, `onegame export` and so on; set `ONEGAME_ROOT` to the checkout when the command lives elsewhere.

Then read, in order:

- [docs/driving-a-game.md](docs/driving-a-game.md): making a game, scenarios, replays, captures and live control.
- [docs/ui-guide.md](docs/ui-guide.md): screens, styling and the component kit.
- [docs/exporting.md](docs/exporting.md): Windows, Linux, macOS, Android, iOS, web and dedicated-server builds.
- [docs/devices.md](docs/devices.md): checking a build on real phones, tablets, the Steam Deck and gamepads.
- `docs/interfaces/`: the written contracts between engine areas.

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
