using System.Runtime.CompilerServices;
using OneGame.Atlas;
using OneGame.Core;
using OneGame.Host;
using OneGame.Steam;
using OneGame.Stores;
using OneGame.UI;
using OneGame.UI.Kit;
using Solitaire.Views;

namespace Solitaire;

/// <summary>Where the game gets its files, sounds and store: the parts that differ between a player's machine and a test.</summary>
public sealed class GameServices
{
    /// <summary>Makes the data store for a running game.</summary>
    public Func<GameInstance, IDataStore> CreateStore { get; init; } = DefaultStore;

    /// <summary>Where sounds go.</summary>
    public ISoundSink Sounds { get; init; } = new SilentSounds();

    /// <summary>
    /// The store the game runs under, or null. Statistics and the saved game go to its cloud when it is running, and to a local file otherwise.
    /// </summary>
    public IStorePlatform? Store { get; init; }

    /// <summary>
    /// The default: files in memory for a headless run (a test never touches the player's save), otherwise the store's cloud or a folder
    /// (<c>SOLITAIRE_DATA</c> if set, else the user's local application data).
    /// </summary>
    private static IDataStore DefaultStore(GameInstance game) => (game.Options.Headless || !game.Options.PresentFrames) && Environment.GetEnvironmentVariable("SOLITAIRE_DATA") is not { Length: > 0 }
        ? new MemoryStore()
        : DataStores.Choose(((SolitaireGame)game.Definition).Services.Store, Environment.GetEnvironmentVariable("SOLITAIRE_DATA") is { Length: > 0 } dir ? dir : LocalFileStore.DefaultDirectory());
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

/// <summary>The game: its entity, its starting deal, its art and its screens.</summary>
public sealed class SolitaireGame : GameDefinition
{
    /// <summary>Creates the game with the default services.</summary>
    public SolitaireGame()
        : this(new GameServices())
    {
    }

    /// <summary>Creates the game with its own services (tests and alternative builds).</summary>
    public SolitaireGame(GameServices services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>The files, sounds and store the game uses.</summary>
    public GameServices Services { get; }

    /// <inheritdoc />
    public override string Name => "Solitaire";

    /// <inheritdoc />
    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.Solitaire.EntityTypes.Register(registry);

    /// <inheritdoc />
    public override void Setup(World world)
    {
        Resets++;
        world.Add(new Table()).NewGame(1);
    }

    /// <summary>
    /// How many times the starting world has been built. The host builds it for the first game and again whenever the harness resets, so a
    /// session can tell a harness reset from a new game or a loaded save.
    /// </summary>
    internal int Resets { get; private set; }

    /// <inheritdoc />
    public override void DefineFixtures(FixtureSet fixtures) =>
        fixtures
            .Add("nearly-won", world => world.Add(new Table()).SetupNearlyWon())
            .Add("fresh", world => world.Add(new Table()).NewGame(1));

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

        foreach (var name in new[] { "card-face", "card-back", "slot", "recycle", "suit-spade", "suit-heart", "suit-club", "suit-diamond" })
        {
            art.AddSvg(name, Assets.Read("art/" + name + ".svg"));
        }
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
            .Add("main", Screens.Menu)
            .Add("table", TableView.Build)
            .Add("stats", Screens.Statistics)
            .Add("settings", Screens.Settings);

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
    /// The services for a run. A headless run gets none (silent, files in memory). A windowed run gets sound through the audio service, and the
    /// store named by <c>SOLITAIRE_STEAM_APPID</c> when that is set; with no store the data goes to a local folder.
    /// </summary>
    public static GameServices CreateServices(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (Array.IndexOf(args, "--headless") >= 0)
        {
            return new GameServices();
        }

        IStorePlatform? store = null;
        if (uint.TryParse(Environment.GetEnvironmentVariable("SOLITAIRE_STEAM_APPID"), NumberStyles.None, CultureInfo.InvariantCulture, out var appId) && appId != 0)
        {
            store = SteamPlatform.Start(new SteamOptions { AppId = appId });
        }

        return new GameServices { Sounds = (ISoundSink?)AudioSounds.TryCreate() ?? new SilentSounds(), Store = store };
    }

    /// <summary>Adds a seed from the clock to a windowed run that was not given one, so every game deals differently. Headless runs keep the fixed default so they repeat.</summary>
    public static string[] WithRandomSeed(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (Array.IndexOf(args, "--seed") >= 0 || Array.IndexOf(args, "--headless") >= 0)
        {
            return args;
        }

        return [.. args, "--seed", ((ulong)Environment.TickCount64 ^ (ulong)Environment.ProcessId).ToString(CultureInfo.InvariantCulture)];
    }
}
