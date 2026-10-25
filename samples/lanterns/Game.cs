using System.Runtime.CompilerServices;
using OneGame.Atlas;
using OneGame.Core;
using OneGame.Sync;
using OneGame.Host;
using OneGame.UI;
using OneGame.UI.Kit;
using Lanterns.Views;

namespace Lanterns;

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

/// <summary>
/// Lanterns. Started on its own it is a one-island game with a walker to swap with. Started with <c>--connect</c> it is a client of the
/// island servers: the same entities, now replicas of the servers' scopes, with walking predicted.
/// </summary>
public sealed class LanternsGame : GameDefinition
{
    /// <inheritdoc />
    public override string Name => "Lanterns";

    /// <inheritdoc />
    public override void RegisterEntities(EntityRegistry registry) => Worlds.Register(registry);

    /// <inheritdoc />
    public override void RegisterSystems(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        Worlds.Build(world, new ScopeId(Rules.IslandA));
    }

    /// <inheritdoc />
    public override void Setup(World world) => Worlds.SeedSinglePlayer(world);

    /// <inheritdoc />
    public override void DefineFixtures(FixtureSet fixtures)
    {
        ArgumentNullException.ThrowIfNull(fixtures);
        fixtures.Add("crowded", world =>
        {
            Worlds.SeedSinglePlayer(world);
            var island = Island.Of(world)!;
            island.Join(3, "Bo");
            island.Join(4, "Cy");
            foreach (var p in world.Entities.OfType<Player>())
            {
                p.Auto = p.Owner != 1;
                p.X = 60 + (int)(p.Owner * 70);
                p.Y = 40 + (int)(p.Owner * 30);
            }

            var me = world.Entities.OfType<Player>().First(p => p.Owner == 1);
            me.Change(0, 2);
            me.Change(1, 1);
        });
    }

    /// <inheritdoc />
    public override void DefineFonts(OneGame.Text.FontLibrary fonts)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        foreach (var name in new[] { "NotoSans-Subset-Regular.ttf", "NotoSans-Subset-Bold-kern.ttf" })
        {
            using var stream = typeof(LanternsGame).Assembly.GetManifestResourceStream("fonts/" + name) ?? throw new InvalidOperationException("missing font " + name);
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            fonts.Add(copy.ToArray());
        }
    }

    /// <inheritdoc />
    public override void DefineArt(ArtAtlas art)
    {
        ArgumentNullException.ThrowIfNull(art);
        foreach (var a in KitArt.Assets)
        {
            art.AddSvg(a.Id, a.Svg, new NineSlice(a.Slice, a.Slice, a.Slice, a.Slice));
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
            .Add("main", game => IslandView.Build(game, swap: false))
            .Add("swap", game => IslandView.Build(game, swap: true));

    /// <inheritdoc />
    public override void OnFrame(GameInstance game) => Session.For(game).Frame();

    /// <inheritdoc />
    public override void OnInput(GameInstance game, in OneGame.Platform.PlatformEvent input) => Session.For(game).HandleInput(input);
}
