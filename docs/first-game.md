# Your first game

From nothing to an exported program, with only the .NET 10 SDK and nuget.org.

```
dotnet tool install -g RecursiveAI.OneGame.Cli
onegame new mygame
cd mygame
```

`onegame new` writes five files you edit (a project, an entry point, the game, a scenario, a readme) and a default font. The project file is the whole build setup: the `RecursiveAI.OneGame.Sdk` package brings the target framework, the engine libraries, the analyzers that make the sim and view rules compile errors, the development tools of Debug builds and the export targets.

**`mygame.csproj`**

```xml
<Project Sdk="RecursiveAI.OneGame.Sdk/<version>">

  <PropertyGroup>
    <AssemblyName>Mygame</AssemblyName>
    <RootNamespace>Mygame</RootNamespace>
  </PropertyGroup>

</Project>
```

**`Program.cs`**

```csharp
using Mygame;
using OneGame.Host;

// A named class rather than top-level statements, so the Android entry point (onegame export --target android) can call Main.
internal static class Program
{
    internal static int Main(string[] args) => GameApp.Run(new MygameGame(), args);
}
```

**`Game.cs`**

```csharp
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace Mygame;

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

/// <summary>The game: its entity types, its starting world and its screens.</summary>
public sealed class MygameGame : GameDefinition
{
    public override string Name => "mygame";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.Mygame.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Counter());

    public override void DefineScreens(ScreenSet screens) =>
        screens.Add("main", game => Ui.Column(
            Ui.Label(() => CounterOf(game).Label()).Named("count"),
            Ui.Button("Add one", () => CounterOf(game).Send.Increment(1)).Named("add").Padding(Length.Px(8)).Background(Color.Rgb(0x3060A0)))
            .Padding(Length.Px(24)).Gap(Length.Px(12)).Background(Color.Rgb(0x203040)).TextColor(Color.White));

    // Read the world through the instance: it is replaced when a snapshot or fixture is loaded.
    private static Counter CounterOf(GameInstance game) => (Counter)game.World.Entities[0];
}
```

What is in it:

- `Count` and `Ticks` are **synced state**: partial properties the generator completes with change tracking, binary codecs, deltas and snapshots. They have private setters, so only sim code writes them.
- `Increment` is a **command**. The UI calls it through `Send`; the authority runs it. Commands are the only way a view changes the world.
- `OnTick` is the **simulation step**, run at a fixed rate. Reading the clock or using a `float` in synced state here is a compile error with an `OG....` id and the fix in the message.
- `Label` is a **view member**. Assigning `Count` inside it is a compile error.
- `DefineScreens` builds the UI with a declarative C# builder; lambdas such as `() => CounterOf(game).Label()` are bindings that are re-read every frame.

**`scenarios/click-button.yaml`**

```yaml
name: click-button
description: Pressing the button adds one to the counter.
steps:
  - expect:
      ui: { id: count, text: "Count: 0" }
  - click: add
  - step: 1
  - click: add
  - step: 1
expect:
  - state: { entity: Counter, property: Count, equals: 2 }
  - ui: { id: count, text: "Count: 2" }
```

A scenario is a test written as text: it clicks elements by id, steps frames and checks state and UI.

## Run it

```
onegame run --headless --frames 60 --json
# {"ok":true,"game":"mygame","headless":true,"frames":60,"tick":60,"hash":"...","width":1280,"height":720}
onegame run                                   # the same game in a window
```

`hash` is the hash of the whole world after the last tick. The same game, seed and inputs give the same hash on every system.

## Test it

```
onegame test --json
# {"ok":true,"scenarios":{"ok":true,"passed":1,"failed":0,...},"unitTests":{...}}
```

This runs every file in `scenarios/` in a headless game, and any C# test projects next to the game. Add a scenario by adding a file; `onegame capture --size 720p` writes a picture of the screen to look at.

## Export it

```
onegame export --target linux      # or win, osx
```

The result is a folder and an archive in `./export`: a NativeAOT program, its fonts and a licence-notices file, with the development tools left out. Run it with `--headless --frames 60 --json` to check it starts; the hash equals the one from `onegame run`. NativeAOT needs the platform's C++ linker (`clang` and `zlib1g-dev` on Linux, the Visual Studio C++ tools on Windows); an export on another OS's host falls back to a self-contained build and says so in `warnings`.

## Next

- [driving-a-game.md](driving-a-game.md): scenarios in depth, replays, captures and live control for an agent.
- [ui-guide.md](ui-guide.md): screens, styling and the component kit.
- [exporting.md](exporting.md): every target, Steam, servers.
