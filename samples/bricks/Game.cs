using System.Runtime.CompilerServices;
using OneGame.Atlas;
using OneGame.Audio;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;
using OneGame.UI.Kit;
using Bricks.Views;

namespace Bricks;

/// <summary>Where the game gets its files and sounds: the parts that differ between a player's machine and a test.</summary>
public sealed class GameServices
{
    /// <summary>Makes the data store for a running game.</summary>
    public Func<GameInstance, IDataStore> CreateStore { get; init; } = DefaultStore;

    /// <summary>Where sounds go.</summary>
    public ISoundSink Sounds { get; init; } = new SilentSounds();

    /// <summary>
    /// The default: files in memory for a headless run (a test never touches the player's own scores), otherwise a folder
    /// (<c>BRICKS_DATA</c> if set, else the user's local application data).
    /// </summary>
    private static IDataStore DefaultStore(GameInstance game) =>
        (game.Options.Headless || !game.Options.PresentFrames) && Environment.GetEnvironmentVariable("BRICKS_DATA") is not { Length: > 0 }
            ? new MemoryStore()
            : new LocalFileStore(Environment.GetEnvironmentVariable("BRICKS_DATA") is { Length: > 0 } dir ? dir : LocalFileStore.DefaultDirectory());
}

/// <summary>One kit host per UI tree, so screens built later find the one that installed the theme.</summary>
internal static class Kits
{
    private static readonly ConditionalWeakTable<UiTree, KitHost> Hosts = new();

    public static KitHost Install(UiTree ui)
    {
        var host = new KitHost(ui);
        Hosts.AddOrUpdate(ui, host);
        return host;
    }

    public static KitHost For(UiTree ui) => Hosts.TryGetValue(ui, out var host) ? host : Install(ui);
}

/// <summary>The game: its entity, its art, its screens and the few things that are not the entity's business.</summary>
public sealed class BricksGame : GameDefinition
{
    private static readonly string[] Art = ["brick", "brick-solid", "ball", "capsule-wide", "capsule-multi", "heart"];

    /// <summary>Creates the game with the default services.</summary>
    public BricksGame()
        : this(new GameServices())
    {
    }

    /// <summary>Creates the game with its own services (tests and alternative builds).</summary>
    public BricksGame(GameServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>The files and sounds the game uses.</summary>
    public GameServices Services { get; }

    /// <summary>
    /// How many times the starting world has been built. The host builds it for the first game and again whenever the harness resets, so a
    /// session can tell a harness reset (which acts like starting the program again) from a fixture or a loaded save.
    /// </summary>
    internal int Resets { get; private set; }

    /// <summary>
    /// How many times the "restart" fixture was loaded. It stands for quitting the program and starting it again: the session drops what it
    /// kept in memory and reads the settings and scores back from the data store. A scenario can't start a second process, so this is how it
    /// checks that the table was really saved.
    /// </summary>
    internal int Restarts { get; private set; }

    /// <inheritdoc />
    public override string Name => "Bricks";

    /// <inheritdoc />
    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.Bricks.EntityTypes.Register(registry);

    /// <inheritdoc />
    public override void Setup(World world)
    {
        Resets++;
        world.Add(new Court());
    }

    /// <inheritdoc />
    public override void DefineFixtures(FixtureSet fixtures) =>
        fixtures
            .Add("restart", world =>
            {
                Restarts++;
                world.Add(new Court());
            })
            .Add("serve", world => world.Add(new Court()).Started(1))
            .Add("last-brick", world => world.Add(new Court()).SetupLastBrick())
            .Add("types", world => world.Add(new Court()).SetupTypes())
            .Add("falling", world => world.Add(new Court()).SetupFalling())
            .Add("capsules", world => world.Add(new Court()).SetupCapsules())
            .Add("last-life", world => world.Add(new Court()).SetupLastLife());

    /// <inheritdoc />
    public override void DefineFonts(OneGame.Text.FontLibrary fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        fonts.Add(Assets.Read("fonts/NotoSans-Subset-Regular.ttf"));
        fonts.Add(Assets.Read("fonts/NotoSans-Subset-Bold-kern.ttf"));
    }

    /// <inheritdoc />
    public override void DefineArt(ArtAtlas art)
    {
        ArgumentNullException.ThrowIfNull(art);
        foreach (var a in KitArt.Assets)
        {
            art.AddSvg(a.Id, a.Svg, new NineSlice(a.Slice, a.Slice, a.Slice, a.Slice));
        }

        foreach (var name in Art)
        {
            art.AddSvg(name, Assets.Read("art/" + name + ".svg"));
        }

        // The paddle keeps its round ends and stretches in the middle, so one drawing serves both widths.
        art.AddSvg("paddle", Assets.Read("art/paddle.svg"), new NineSlice(14, 0, 14, 0));
    }

    /// <inheritdoc />
    public override void Style(UiTree ui)
    {
        ArgumentNullException.ThrowIfNull(ui);
        Kits.Install(ui);
        ui.AddStyleSheet(Theme.Sheet());
    }

    /// <inheritdoc />
    public override void DefineScreens(ScreenSet screens) =>
        screens
            .Add("main", Screens.Title)
            .Add("play", CourtView.Build)
            .Add("scores", Screens.HighScores)
            .Add("settings", Screens.Settings);

    /// <inheritdoc />
    public override void OnEachFrame(GameInstance game)
    {
        ArgumentNullException.ThrowIfNull(game);
        Session.For(game);
    }

    /// <inheritdoc />
    public override void OnInput(GameInstance game, in OneGame.Platform.PlatformEvent input)
    {
        ArgumentNullException.ThrowIfNull(game);
        Session.For(game).HandleInput(input);
    }
}

/// <summary>Reads the files embedded in the program.</summary>
internal static class Assets
{
    public static byte[] Read(string name)
    {
        using var stream = typeof(Assets).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"The program has no embedded file '{name}'.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}

/// <summary>Command-line helpers for the program entry point.</summary>
public static class Launch
{
    /// <summary>
    /// The services for a run. A windowed run plays sound through the audio service. A headless run (a scenario, a replay) gets a capture
    /// service with no device, so the game still asks for its sounds and a scenario can check which ones with <c>sound:</c>.
    /// </summary>
    public static GameServices CreateServices(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        var headless = Array.IndexOf(args, "--headless") >= 0;
        var sounds = AudioSounds.TryCreate(headless ? new AudioServiceOptions { Capture = true } : null);
        return new GameServices { Sounds = (ISoundSink?)sounds ?? new SilentSounds() };
    }
}
