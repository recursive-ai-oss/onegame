# Your first game

From nothing to an exported program, with only the .NET 10 SDK and nuget.org.

```
dotnet tool install -g RecursiveAI.OneGame.Cli
onegame new mygame
cd mygame
```

`onegame new` writes the files you edit (a project, an entry point, the game, two scenarios, a readme), a default font and a ship picture, `art/ship.png`. The result is a ship you fly with the keyboard or a gamepad, drawn from the picture, with its code, tests and a hot-reloaded asset in about 90 lines. The project file is the whole build setup: the `RecursiveAI.OneGame.Sdk` package brings the target framework, the engine libraries, the analyzers that make the sim and view rules compile errors, the development tools of Debug builds and the export targets.

**`mygame.csproj`**

```xml
<Project Sdk="RecursiveAI.OneGame.Sdk/<version>">

  <PropertyGroup>
    <AssemblyName>Mygame</AssemblyName>
    <RootNamespace>Mygame</RootNamespace>
  </PropertyGroup>

</Project>
```

NuGet central package management works as usual: put `Directory.Packages.props` with `ManagePackageVersionsCentrally` beside the game and list your own packages there. The SDK versions its engine packages itself (as `VersionOverride`), so you don't list them. Leave `CentralPackageVersionOverrideEnabled` at its default (true).

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
using System;
using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.UI;

namespace Mygame;

/// <summary>A ship. Synced state is fixed-point and changes only through commands; the view draws it from art/ship.png and never writes state.</summary>
[Entity]
public partial class Ship : Entity, IView2D
{
    [Synced] public partial Fixed X { get; private set; }
    [Synced] public partial Fixed Y { get; private set; }
    [Synced] public partial int Dx { get; private set; }
    [Synced] public partial int Dy { get; private set; }
    [Synced] public partial bool Boost { get; private set; }

    [Command]
    public void Steer(int dx, int dy, bool boost) => (Dx, Dy, Boost) = (dx, dy, boost);

    // Runs once per fixed simulation tick: deterministic code only (no floats, clocks or I/O in synced state).
    protected override void OnTick()
    {
        var speed = Boost ? Fixed.FromInt(3) : Fixed.FromRatio(3, 2);
        X += speed * Dx;
        Y += speed * Dy;
    }

    // View code reads state and never writes it. The picture is an asset: edit art/ship.png while the game runs and it changes.
    [View]
    public void Draw(Draw2D d) =>
        d.Sprite("art/ship.png", new Vector2(X.ToSingle(), Y.ToSingle()), scale: 2, tint: Boost ? Color.Rgb(0xFFD080) : Color.White);

    [View]
    public string Label() => $"x {X.ToInt()}  y {Y.ToInt()}";
}

/// <summary>The game: its entity types, its starting world, its input actions and its screens.</summary>
public sealed class MygameGame : GameDefinition
{
    public override string Name => "mygame";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.Mygame.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Ship());

    // Input is declared as actions with default bindings. One declaration answers the keyboard, a gamepad and the test harness.
    public override void DefineActions(ActionMap actions) =>
        actions.Context("play", c => c
            .Vector("move", Bind.Wasd, Bind.Arrows, Bind.LeftStick)
            .Button("boost", Bind.Key(Key.Space), Bind.Pad(GamepadButton.South)));

    // View code reads the actions and sends the ship a command when they change.
    public override void OnEachFrame(GameInstance game)
    {
        var ship = ShipOf(game);
        var move = game.Actions.Vector("move");
        int dx = (int)MathF.Round(move.X), dy = (int)MathF.Round(move.Y);
        var boost = game.Actions.Held("boost");
        if (dx != ship.Dx || dy != ship.Dy || boost != ship.Boost)
        {
            ship.Send.Steer(dx, dy, boost);
        }
    }

    public override void DefineScreens(ScreenSet screens) =>
        screens.Add("main", game => Ui.Label(() => ShipOf(game).Label()).Named("pos").Padding(Length.Px(8)).TextColor(Color.White));

    // Read the world through the instance: it is replaced when a snapshot or fixture is loaded.
    private static Ship ShipOf(GameInstance game) => (Ship)game.World.Entities[0];
}
```

What is in it:

- **A moving sprite.** `X`, `Y`, `Dx`, `Dy` and `Boost` are **synced state**: partial properties the generator completes with change tracking, binary codecs, deltas and snapshots. Positions are `Fixed` (fixed-point, so every machine computes the same numbers); private setters mean only sim code writes them. `OnTick` is the **simulation step**, run at a fixed rate; a `float`, the clock or I/O in it is a compile error with an `OG....` id and the fix in the message. `Draw` is a **view member** of an `IView2D` entity: the 2D renderer calls it every frame and the sprite is drawn where the ship is. Assigning `X` inside it is a compile error. See [2d.md](2d.md) for sprites, shapes, text, sprite sheets and the camera.
- **An action-mapped input.** `DefineActions` declares a `move` vector and a `boost` button with default bindings (WASD, arrows, the left stick; Space, the pad's south button). `OnEachFrame` reads them and sends the ship a command, `Steer`, when they change. Commands are the only way a view changes the world: the authority runs them, so replays, saves and the network see commands, never keys. The same declaration answers the keyboard, a gamepad, Steam Input and the scenario step `action:`; players can rebind ([input.md](input.md)).
- **A hot-reloaded asset.** `d.Sprite("art/ship.png", ...)` loads the file once through the asset service and caches it. Run `onegame run`, edit `art/ship.png` in any image editor, save: the next frame shows the new picture and the game keeps running (Debug builds only; an export reads the files it ships). A damaged file keeps the old picture and reports the error. See [assets.md](assets.md).
- **Hot-reloaded code.** `onegame run` also applies edits to the game's C# while it runs, with the world kept: change the `Label` text or the ship's speed in `OnTick`, save, and the running game changes. See [hot-reload.md](hot-reload.md) for what applies live, what restarts the game (and keeps its state) and what that means for replays.
- `DefineScreens` builds the UI with a declarative C# builder; the lambda `() => ShipOf(game).Label()` is a binding re-read every frame. The ship is read through the instance because the world is replaced when a snapshot or fixture is loaded.

**`scenarios/steer-ship.yaml`**

```yaml
name: steer-ship
description: The move action steers the ship, and holding boost makes it faster.
steps:
  - action: { name: move, state: down, x: 1, y: 0 }
  - step: 10
  - action: { name: move, state: up }
  - step: 2
  - expect:
      state: { entity: Ship, property: X, min: 14, max: 17 }
  - action: { name: boost, state: down }
  - action: { name: move, state: down, x: 0, y: 1 }
  - step: 10
expect:
  - action: { name: boost, held: true }
  - state: { entity: Ship, property: Boost, equals: true }
  - state: { entity: Ship, property: Y, min: 27, max: 33 }
```

**`scenarios/ship-looks-right.yaml`**

```yaml
name: ship-looks-right
description: The ship is drawn from art/ship.png after it has flown a little, and the picture is loaded.
window: 640x360
steps:
  - action: { name: move, state: down, x: 1, y: -1 }
  - step: 40
expect:
  - capture: { reference: refs/ship.png, tolerance: 48, max-diff: 0.01 }
  - asset: { path: art/ship.png, state: ready }
```

A scenario is a test written as text: it presses actions, steps frames and checks state, assets and a captured picture. `action:` steps go through the same actions the keyboard does. The capture compares the drawn frame with `scenarios/refs/ship.png`; it needs a Vulkan driver (a software one is fine).

## Run it

```
onegame run --headless --frames 60 --json
# {"ok":true,"game":"mygame","headless":true,"frames":60,"tick":60,"hash":"...","width":1280,"height":720}
onegame run                                   # the same game in a window: fly with WASD, edit art/ship.png and save
```

`hash` is the hash of the whole world after the last tick. The same game, seed and inputs give the same hash on every system.

## Test it

```
onegame test --json
# {"ok":true,"scenarios":{"ok":true,"passed":2,"failed":0,...},"unitTests":{...}}
```

This runs every file in `scenarios/` in a headless game, and any C# test projects next to the game. Add a scenario by adding a file; `onegame capture --size 720p` writes a picture of the screen to look at.

## Export it

```
onegame export --target linux      # or win, osx
```

The result is a folder and an archive in `./export`: a NativeAOT program, its fonts and pictures and a licence-notices file, with the development tools left out. Run it with `--headless --frames 60 --json` to check it starts; the hash equals the one from `onegame run`. NativeAOT needs the platform's C++ linker (`clang` and `zlib1g-dev` on Linux, the Visual Studio C++ tools on Windows); an export on another OS's host falls back to a self-contained build and says so in `warnings`.

## Next

- [api/index.md](api/index.md): the API reference: every public type of every package, with signatures and docs.
- [pages.md](pages.md): screens written in HTML and CSS through OneRender. [tweens.md](tweens.md), [particles.md](particles.md) and [saves.md](saves.md): animation and style transitions, particle effects, save slots and settings.
- [driving-a-game.md](driving-a-game.md): scenarios in depth, replays, captures and live control for an agent.
- [ui-guide.md](ui-guide.md): screens, styling and the component kit.
- [exporting.md](exporting.md): every target, Steam, servers.
