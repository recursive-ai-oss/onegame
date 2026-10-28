using OneGame.Atlas;
using OneGame.Core;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Host;

/// <summary>
/// What a game is, as far as the host cares: its entity types, how its world starts, its screens and its named fixtures.
/// A game derives from this once and hands an instance to <see cref="GameApp.Run(GameDefinition, string[])"/>.
/// </summary>
/// <example>
/// The smallest game: one entity type, one screen.
/// <code>
/// public sealed class CounterGame : GameDefinition
/// {
///     public override string Name =&gt; "counter";
///
///     public override void RegisterEntities(EntityRegistry registry) =&gt;
///         OneGame.Generated.Counter.EntityTypes.Register(registry);   // generated for the game's assembly
///
///     public override void Setup(World world) =&gt; world.Add(new Counter());
///
///     public override void DefineScreens(ScreenSet screens) =&gt;
///         screens.Add("main", game =&gt; Ui.Label(() =&gt; ((Counter)game.World.Entities[0]).Label()));
/// }
///
/// internal static class Program
/// {
///     internal static int Main(string[] args) =&gt; GameApp.Run(new CounterGame(), args);
/// }
/// </code>
/// </example>
public abstract class GameDefinition
{
    /// <summary>The game's name, shown in the window title and in harness output.</summary>
    public abstract string Name { get; }

    /// <summary>The game's version text, recorded in each save slot's description (for a load screen: "made with 1.2"). Optional.</summary>
    public virtual string Version => string.Empty;

    /// <summary>
    /// Where this game keeps its saves and settings, when it wants something other than the host's default (a store's cloud, a test's
    /// in-memory files). Return null for the default. Called once per game instance, the first time the game uses <see cref="GameInstance.Storage"/>.
    /// </summary>
    /// <param name="game">The instance asking; its options say whether this run should persist.</param>
    /// <returns>The store to use, or null for the default (see <see cref="GameStorage.Create"/>).</returns>
    public virtual OneGame.Storage.IByteStore? CreateStore(GameInstance game) => null;

    /// <summary>The largest save slot file the game reads or writes, in bytes (default 64 MiB). Set it to what a save of this game can be, so a damaged file is refused early.</summary>
    public virtual int MaxSaveBytes => 64 * 1024 * 1024;

    /// <summary>Seed of the world's random streams. Runs with the same seed and the same commands are identical.</summary>
    public virtual ulong Seed => 1;

    /// <summary>Registers every entity type, normally with the generated <c>EntityTypes.Register(registry)</c> of the game's assembly.</summary>
    /// <param name="registry">Receives the entity types; add every type the game can save or sync.</param>
    public abstract void RegisterEntities(EntityRegistry registry);

    /// <summary>Adds systems and hot-data tables to a new world. Runs before <see cref="Setup"/> and also before a snapshot is restored.</summary>
    /// <param name="world">The new world. Add systems and tables in the same order on every node.</param>
    public virtual void RegisterSystems(World world)
    {
    }

    /// <summary>Creates the starting entities of a new game.</summary>
    /// <param name="world">An empty authority world with the systems already registered. Runs for every new game, not when a save is loaded.</param>
    public abstract void Setup(World world);

    /// <summary>Declares the screens. A screen named <c>main</c> is shown at start; others open with <see cref="GameInstance.OpenScreen"/>.</summary>
    /// <param name="screens">Add each screen with <see cref="ScreenSet.Add"/>. A screen builder runs every time the screen opens and returns the root <see cref="Element"/>.</param>
    public abstract void DefineScreens(ScreenSet screens);

    /// <summary>
    /// Declares the game's input actions (buttons, axes and vectors with default bindings, grouped in contexts a screen switches on). View code reads them from <see cref="GameInstance.Actions"/> and sends commands.
    /// Games that read raw input need not override this.
    /// </summary>
    /// <param name="actions">The map to declare contexts, actions and default bindings on.</param>
    public virtual void DefineActions(OneGame.Platform.ActionMap actions)
    {
    }

    /// <summary>Adds style sheets to the UI tree before the first screen is built.</summary>
    /// <param name="ui">The tree; add style sheets with <see cref="UiTree.AddStyleSheet"/> or theme the kit.</param>
    public virtual void Style(UiTree ui)
    {
    }

    /// <summary>Declares named fixtures: ready-made worlds that tests and live control can load by name.</summary>
    /// <param name="fixtures">Add each fixture with <see cref="FixtureSet.Add"/>.</param>
    public virtual void DefineFixtures(FixtureSet fixtures)
    {
    }

    /// <summary>Called once per frame of a game connected to a server, after the network was pumped and before the UI updates. Advance predicted worlds here.</summary>
    /// <param name="game">The running game.</param>
    public virtual void OnFrame(GameInstance game)
    {
    }

    /// <summary>Called at the start of every frame of every game, local or connected, right after pending input was handled. Read devices and services that need polling here (Steam Input, for example); events they post are handled in the next frame.</summary>
    /// <param name="game">The running game. This is view code: read the world and the actions, and send commands; do not write synced state.</param>
    public virtual void OnEachFrame(GameInstance game)
    {
    }

    /// <summary>Called for an input event the UI did not consume, such as a click on empty space or a gameplay key.</summary>
    /// <param name="game">The running game.</param>
    /// <param name="input">The event: a key, pointer, touch or gamepad change in screen pixels.</param>
    public virtual void OnInput(GameInstance game, in OneGame.Platform.PlatformEvent input)
    {
    }

    /// <summary>
    /// Loads the game's fonts. The default reads every <c>.ttf</c> and <c>.otf</c> file in a <c>fonts</c> folder next to the program, or
    /// or in <c>Contents/Resources/fonts</c> of a macOS bundle, or else in the working directory. With no fonts the UI measures text with a fixed-width estimate and the stub stage draws it as bars.
    /// </summary>
    /// <param name="fonts">Add font faces to it; the first family added is the default.</param>
    public virtual void DefineFonts(FontLibrary fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        // Inside a macOS application bundle the program sits in Contents/MacOS and its content in Contents/Resources.
        foreach (var root in new[] { Path.Combine(AppContext.BaseDirectory, "fonts"), Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "fonts")), Path.Combine(Directory.GetCurrentDirectory(), "fonts") })
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(root).Order(StringComparer.Ordinal))
            {
                if (file.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                {
                    fonts.Add(File.ReadAllBytes(file));
                }
            }

            return;
        }
    }

    /// <summary>
    /// Where <see cref="GameInstance.Assets"/> reads content from. The default is the program's folder, a macOS bundle's <c>Resources</c> and the working
    /// directory, in that order, so an export finds its <c>art/</c> and <c>data/</c> next to the program and a development run finds them in the project.
    /// </summary>
    public virtual IAssetSource CreateAssetSource() => new FileAssetSource();

    /// <summary>Registers SVG and image art the UI refers to by id.</summary>
    /// <param name="art">The atlas to register SVG and image sources on, each under the id the UI refers to it by.</param>
    public virtual void DefineArt(ArtAtlas art)
    {
    }

    /// <summary>
    /// The stage that draws each frame. The default is the 2D renderer when the game has fonts and compiled shaders are available, and
    /// otherwise a plain preview of the UI. Override to plug in another renderer.
    /// </summary>
    /// <param name="resources">The fonts, art and device-independent resources the stage may use.</param>
    public virtual IRenderStage CreateRenderStage(HostResources resources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        return resources.Fonts is not null && resources.Shaders is not null ? new Render2DStage(resources) : new StubStage();
    }
}

/// <summary>The named screens of a game. Each is built fresh when opened, and may read the game's world through the instance it is given.</summary>
public sealed class ScreenSet
{
    private readonly Dictionary<string, Func<GameInstance, Element>> _screens = new(StringComparer.Ordinal);
    private readonly List<string> _order = [];

    /// <summary>
    /// Declares a screen. The builder runs each time the screen opens and returns its root element; read the world through the
    /// <see cref="GameInstance"/> it is given, because the world object is replaced when a save or fixture is loaded.
    /// </summary>
    /// <param name="name">The screen's name; <c>main</c> opens first.</param>
    /// <param name="build">Builds the screen's element tree.</param>
    /// <returns>This set, for chaining.</returns>
    /// <exception cref="ArgumentException">The name is already taken.</exception>
    public ScreenSet Add(string name, Func<GameInstance, Element> build)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (!_screens.TryAdd(name, build))
        {
            throw new ArgumentException($"A screen named '{name}' already exists.", nameof(name));
        }

        _order.Add(name);
        return this;
    }

    /// <summary>The screen names in declaration order.</summary>
    public IReadOnlyList<string> Names => _order;

    internal bool TryGet(string name, out Func<GameInstance, Element> build) => _screens.TryGetValue(name, out build!);
}

/// <summary>The named fixtures of a game. A fixture fills a fresh world (systems and tables are already registered).</summary>
public sealed class FixtureSet
{
    private readonly Dictionary<string, Action<World>> _fixtures = new(StringComparer.Ordinal);
    private readonly List<string> _order = [];

    /// <summary>Declares a fixture: a named starting state that tests, scenarios and live control can load without playing up to it.</summary>
    /// <param name="name">The fixture's name.</param>
    /// <param name="populate">Adds entities and sets state on the fresh world (sim code, run on the authority).</param>
    /// <returns>This set, for chaining.</returns>
    /// <exception cref="ArgumentException">The name is already taken.</exception>
    public FixtureSet Add(string name, Action<World> populate)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (!_fixtures.TryAdd(name, populate))
        {
            throw new ArgumentException($"A fixture named '{name}' already exists.", nameof(name));
        }

        _order.Add(name);
        return this;
    }

    /// <summary>The fixture names in declaration order.</summary>
    public IReadOnlyList<string> Names => _order;

    internal bool TryGet(string name, out Action<World> populate) => _fixtures.TryGetValue(name, out populate!);
}
