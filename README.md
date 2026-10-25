# onegame

A C#-native game engine with no editor: a game is plain C# plus text assets. Entities, sync, rendering and UI are generated or enforced at build time by Roslyn source generators and analyzers, everything is NativeAOT- and trim-clean, and a command line, a C# test API and a live-control protocol let you build, run, inspect, capture and replay games from a terminal.

The engine is at its first steps: the solution structure and build rules are in place, and the features land project by project.

## Build and test

Requires the .NET SDK pinned in `global.json` (10.0.400 or a later feature band).

```
dotnet build OneGame.slnx
dotnet test --solution OneGame.slnx
dotnet run --project src/OneGame.Cli -- help --json
```

Build output goes to `artifacts/` at the repository root. Set `ONEGAME_ARTIFACTS` to put it elsewhere.

The renderer's HLSL shaders are compiled during the build by the `RecursiveAI.OneShader.Build` package (from nuget.org), and audio mixing and decoding come from `RecursiveAI.OneAudio`. While OneAudio is a prerelease not yet on nuget.org, point `RestoreAdditionalProjectSources` (an MSBuild property, read from the environment) at a folder holding its `.nupkg` file before restoring: `export RestoreAdditionalProjectSources=/path/to/feed`. Games made with `onegame new` restore the same way.

Native AOT publish (Linux needs `clang` and `zlib1g-dev`):

```
dotnet publish tests/OneGame.AotSmoke -c Release -r linux-x64
dotnet publish src/OneGame.Cli -c Release -r linux-x64
```

## Layout

- `src/`: the engine, one project per area (`OneGame.Core`, `.Sync`, `.Gpu`, `.Gpu.Vulkan`, `.Platform`, `.Render2D`, `.UI`, `.Audio`, `.Steam`, `.Control`, `.Testing`), the `OneGame.Generators` analyzers and source generators, and the `onegame` command in `OneGame.Cli`.
- `tests/`: one test project per area, `OneGame.Build.Tests` for the build rules, and `OneGame.AotSmoke`, a native-AOT app that touches every runtime assembly.
- `build/`: shared MSBuild targets.
- `docs/`: documentation, and `docs/interfaces/` for the written boundaries between areas.

## Build rules

- Only our code ships: projects under `src/` and `samples/` may reference the one* family of libraries and nothing else. The build fails otherwise (`build/PackageGuard.targets`). Test and tool projects may use any package.
- Warnings are errors, nullable is on, builds are deterministic, and every shipping project is checked by the AOT and trim analyzers.

## License

Licensed under either of MIT ([LICENSE-MIT](LICENSE-MIT)) or Apache-2.0 ([LICENSE-APACHE](LICENSE-APACHE)), at your option. Copyright (c) Recursive AI, LLC.
