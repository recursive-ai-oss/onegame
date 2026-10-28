[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# GameDefinition class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public abstract class GameDefinition
```

What a game is, as far as the host cares: its entity types, how its world starts, its screens and its named fixtures. A game derives from this once and hands an instance to [`GameApp.Run()`](GameApp.md#m-run-gamedefinition-string).

**Example**

The smallest game: one entity type, one screen.

```csharp
public sealed class CounterGame : GameDefinition
{
    public override string Name => "counter";

    public override void RegisterEntities(EntityRegistry registry) =>
        OneGame.Generated.Counter.EntityTypes.Register(registry);   // generated for the game's assembly

    public override void Setup(World world) => world.Add(new Counter());

    public override void DefineScreens(ScreenSet screens) =>
        screens.Add("main", game => Ui.Label(() => ((Counter)game.World.Entities[0]).Label()));
}

internal static class Program
{
    internal static int Main(string[] args) => GameApp.Run(new CounterGame(), args);
}
```

## Constructors

| Member | Description |
| --- | --- |
| [`GameDefinition()`](#m-ctor) |  |

### <a id="m-ctor"></a>GameDefinition()

```csharp
protected GameDefinition();
```

## Properties

| Member | Description |
| --- | --- |
| [`MaxSaveBytes`](#m-maxsavebytes) | The largest save slot file the game reads or writes, in bytes (default 64 MiB). |
| [`Name`](#m-name) | The game's name, shown in the window title and in harness output. |
| [`Seed`](#m-seed) | Seed of the world's random streams. |
| [`Version`](#m-version) | The game's version text, recorded in each save slot's description (for a load screen: "made with 1.2"). |

### <a id="m-maxsavebytes"></a>MaxSaveBytes

```csharp
public virtual int MaxSaveBytes { get; }
```

The largest save slot file the game reads or writes, in bytes (default 64 MiB). Set it to what a save of this game can be, so a damaged file is refused early.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public abstract string Name { get; }
```

The game's name, shown in the window title and in harness output.

**Type:** `string`

### <a id="m-seed"></a>Seed

```csharp
public virtual ulong Seed { get; }
```

Seed of the world's random streams. Runs with the same seed and the same commands are identical.

**Type:** `ulong`

### <a id="m-version"></a>Version

```csharp
public virtual string Version { get; }
```

The game's version text, recorded in each save slot's description (for a load screen: "made with 1.2"). Optional.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`CreateAssetSource()`](#m-createassetsource) | Where [`GameInstance.Assets`](GameInstance.md#m-assets) reads content from. |
| [`CreateRenderStage(HostResources)`](#m-createrenderstage-hostresources) | The stage that draws each frame. |
| [`CreateStore(GameInstance)`](#m-createstore-gameinstance) | Where this game keeps its saves and settings, when it wants something other than the host's default (a store's cloud, a test's in-memory files). |
| [`DefineActions(ActionMap)`](#m-defineactions-actionmap) | Declares the game's input actions (buttons, axes and vectors with default bindings, grouped in contexts a screen switches on). |
| [`DefineArt(ArtAtlas)`](#m-defineart-artatlas) | Registers SVG and image art the UI refers to by id. |
| [`DefineFixtures(FixtureSet)`](#m-definefixtures-fixtureset) | Declares named fixtures: ready-made worlds that tests and live control can load by name. |
| [`DefineFonts(FontLibrary)`](#m-definefonts-fontlibrary) | Loads the game's fonts. |
| [`DefineScreens(ScreenSet)`](#m-definescreens-screenset) | Declares the screens. |
| [`OnEachFrame(GameInstance)`](#m-oneachframe-gameinstance) | Called at the start of every frame of every game, local or connected, right after pending input was handled. |
| [`OnFrame(GameInstance)`](#m-onframe-gameinstance) | Called once per frame of a game connected to a server, after the network was pumped and before the UI updates. |
| [`OnInput(GameInstance, in PlatformEvent)`](#m-oninput-gameinstance-platformevent) | Called for an input event the UI did not consume, such as a click on empty space or a gameplay key. |
| [`RegisterEntities(EntityRegistry)`](#m-registerentities-entityregistry) | Registers every entity type, normally with the generated `EntityTypes.Register(registry)` of the game's assembly. |
| [`RegisterSystems(World)`](#m-registersystems-world) | Adds systems and hot-data tables to a new world. |
| [`Setup(World)`](#m-setup-world) | Creates the starting entities of a new game. |
| [`Style(UiTree)`](#m-style-uitree) | Adds style sheets to the UI tree before the first screen is built. |

### <a id="m-createassetsource"></a>CreateAssetSource()

```csharp
public virtual IAssetSource CreateAssetSource();
```

Where [`GameInstance.Assets`](GameInstance.md#m-assets) reads content from. The default is the program's folder, a macOS bundle's `Resources` and the working directory, in that order, so an export finds its `art/` and `data/` next to the program and a development run finds them in the project.

**Returns:** [`IAssetSource`](IAssetSource.md)

### <a id="m-createrenderstage-hostresources"></a>CreateRenderStage(HostResources)

```csharp
public virtual IRenderStage CreateRenderStage(HostResources resources);
```

The stage that draws each frame. The default is the 2D renderer when the game has fonts and compiled shaders are available, and otherwise a plain preview of the UI. Override to plug in another renderer.

**Parameters**

- `resources` ([`HostResources`](HostResources.md)): The fonts, art and device-independent resources the stage may use.

**Returns:** [`IRenderStage`](IRenderStage.md)

### <a id="m-createstore-gameinstance"></a>CreateStore(GameInstance)

```csharp
public virtual IByteStore? CreateStore(GameInstance game);
```

Where this game keeps its saves and settings, when it wants something other than the host's default (a store's cloud, a test's in-memory files). Return null for the default. Called once per game instance, the first time the game uses [`GameInstance.Storage`](GameInstance.md#m-storage).

**Parameters**

- `game` ([`GameInstance`](GameInstance.md)): The instance asking; its options say whether this run should persist.

**Returns:** [`IByteStore`](../../OneGame.Core/OneGame.Storage/IByteStore.md)? — The store to use, or null for the default (see [`GameStorage.Create()`](GameStorage.md#m-create-string-gameoptions)).

### <a id="m-defineactions-actionmap"></a>DefineActions(ActionMap)

```csharp
public virtual void DefineActions(ActionMap actions);
```

Declares the game's input actions (buttons, axes and vectors with default bindings, grouped in contexts a screen switches on). View code reads them from [`GameInstance.Actions`](GameInstance.md#m-actions) and sends commands. Games that read raw input need not override this.

**Parameters**

- `actions` ([`ActionMap`](../../OneGame.Platform/OneGame.Platform/ActionMap.md)): The map to declare contexts, actions and default bindings on.

### <a id="m-defineart-artatlas"></a>DefineArt(ArtAtlas)

```csharp
public virtual void DefineArt(ArtAtlas art);
```

Registers SVG and image art the UI refers to by id.

**Parameters**

- `art` ([`ArtAtlas`](../../OneGame.Atlas/OneGame.Atlas/ArtAtlas.md)): The atlas to register SVG and image sources on, each under the id the UI refers to it by.

### <a id="m-definefixtures-fixtureset"></a>DefineFixtures(FixtureSet)

```csharp
public virtual void DefineFixtures(FixtureSet fixtures);
```

Declares named fixtures: ready-made worlds that tests and live control can load by name.

**Parameters**

- `fixtures` ([`FixtureSet`](FixtureSet.md)): Add each fixture with [`FixtureSet.Add()`](FixtureSet.md#m-add-string-action-world).

### <a id="m-definefonts-fontlibrary"></a>DefineFonts(FontLibrary)

```csharp
public virtual void DefineFonts(FontLibrary fonts);
```

Loads the game's fonts. The default reads every `.ttf` and `.otf` file in a `fonts` folder next to the program, or or in `Contents/Resources/fonts` of a macOS bundle, or else in the working directory. With no fonts the UI measures text with a fixed-width estimate and the stub stage draws it as bars.

**Parameters**

- `fonts` ([`FontLibrary`](../../OneGame.Text/OneGame.Text/FontLibrary.md)): Add font faces to it; the first family added is the default.

### <a id="m-definescreens-screenset"></a>DefineScreens(ScreenSet)

```csharp
public abstract void DefineScreens(ScreenSet screens);
```

Declares the screens. A screen named `main` is shown at start; others open with [`GameInstance.OpenScreen()`](GameInstance.md#m-openscreen-string).

**Parameters**

- `screens` ([`ScreenSet`](ScreenSet.md)): Add each screen with [`ScreenSet.Add()`](ScreenSet.md#m-add-string-func-gameinstance-element). A screen builder runs every time the screen opens and returns the root [`Element`](../../OneGame.UI/OneGame.UI/Element.md).

### <a id="m-oneachframe-gameinstance"></a>OnEachFrame(GameInstance)

```csharp
public virtual void OnEachFrame(GameInstance game);
```

Called at the start of every frame of every game, local or connected, right after pending input was handled. Read devices and services that need polling here (Steam Input, for example); events they post are handled in the next frame.

**Parameters**

- `game` ([`GameInstance`](GameInstance.md)): The running game. This is view code: read the world and the actions, and send commands; do not write synced state.

### <a id="m-onframe-gameinstance"></a>OnFrame(GameInstance)

```csharp
public virtual void OnFrame(GameInstance game);
```

Called once per frame of a game connected to a server, after the network was pumped and before the UI updates. Advance predicted worlds here.

**Parameters**

- `game` ([`GameInstance`](GameInstance.md)): The running game.

### <a id="m-oninput-gameinstance-platformevent"></a>OnInput(GameInstance, in PlatformEvent)

```csharp
public virtual void OnInput(GameInstance game, in PlatformEvent input);
```

Called for an input event the UI did not consume, such as a click on empty space or a gameplay key.

**Parameters**

- `game` ([`GameInstance`](GameInstance.md)): The running game.
- `input` ([`PlatformEvent`](../../OneGame.Platform/OneGame.Platform/PlatformEvent.md)): The event: a key, pointer, touch or gamepad change in screen pixels.

### <a id="m-registerentities-entityregistry"></a>RegisterEntities(EntityRegistry)

```csharp
public abstract void RegisterEntities(EntityRegistry registry);
```

Registers every entity type, normally with the generated `EntityTypes.Register(registry)` of the game's assembly.

**Parameters**

- `registry` ([`EntityRegistry`](../../OneGame.Core/OneGame.Core/EntityRegistry.md)): Receives the entity types; add every type the game can save or sync.

### <a id="m-registersystems-world"></a>RegisterSystems(World)

```csharp
public virtual void RegisterSystems(World world);
```

Adds systems and hot-data tables to a new world. Runs before [`GameDefinition.Setup()`](GameDefinition.md#m-setup-world) and also before a snapshot is restored.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The new world. Add systems and tables in the same order on every node.

### <a id="m-setup-world"></a>Setup(World)

```csharp
public abstract void Setup(World world);
```

Creates the starting entities of a new game.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): An empty authority world with the systems already registered. Runs for every new game, not when a save is loaded.

### <a id="m-style-uitree"></a>Style(UiTree)

```csharp
public virtual void Style(UiTree ui);
```

Adds style sheets to the UI tree before the first screen is built.

**Parameters**

- `ui` ([`UiTree`](../../OneGame.UI/OneGame.UI/UiTree.md)): The tree; add style sheets with [`UiTree.AddStyleSheet()`](../../OneGame.UI/OneGame.UI/UiTree.md#m-addstylesheet-stylesheet) or theme the kit.
