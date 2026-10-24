using OneGame.Core;
using OneGame.UI;

namespace OneGame.Host;

/// <summary>
/// What a game is, as far as the host cares: its entity types, how its world starts, its screens and its named fixtures.
/// A game derives from this once and hands an instance to <see cref="GameApp.Run(GameDefinition, string[])"/>.
/// </summary>
public abstract class GameDefinition
{
    /// <summary>The game's name, shown in the window title and in harness output.</summary>
    public abstract string Name { get; }

    /// <summary>Seed of the world's random streams. Runs with the same seed and the same commands are identical.</summary>
    public virtual ulong Seed => 1;

    /// <summary>Registers every entity type, normally with the generated <c>EntityTypes.Register(registry)</c> of the game's assembly.</summary>
    public abstract void RegisterEntities(EntityRegistry registry);

    /// <summary>Adds systems and hot-data tables to a new world. Runs before <see cref="Setup"/> and also before a snapshot is restored.</summary>
    public virtual void RegisterSystems(World world)
    {
    }

    /// <summary>Creates the starting entities of a new game.</summary>
    public abstract void Setup(World world);

    /// <summary>Declares the screens. A screen named <c>main</c> is shown at start; others open with <see cref="GameInstance.OpenScreen"/>.</summary>
    public abstract void DefineScreens(ScreenSet screens);

    /// <summary>Adds style sheets to the UI tree before the first screen is built.</summary>
    public virtual void Style(UiTree ui)
    {
    }

    /// <summary>Declares named fixtures: ready-made worlds that tests and live control can load by name.</summary>
    public virtual void DefineFixtures(FixtureSet fixtures)
    {
    }

    /// <summary>Called for an input event the UI did not consume, such as a click on empty space or a gameplay key.</summary>
    public virtual void OnInput(GameInstance game, in OneGame.Platform.PlatformEvent input)
    {
    }

    /// <summary>The stage that draws each frame. Override to plug in a renderer; the default draws a plain preview of the UI.</summary>
    public virtual IRenderStage CreateRenderStage() => new StubStage();
}

/// <summary>The named screens of a game. Each is built fresh when opened, and may read the game's world through the instance it is given.</summary>
public sealed class ScreenSet
{
    private readonly Dictionary<string, Func<GameInstance, Element>> _screens = new(StringComparer.Ordinal);
    private readonly List<string> _order = [];

    /// <summary>Declares a screen.</summary>
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

    /// <summary>Declares a fixture.</summary>
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
