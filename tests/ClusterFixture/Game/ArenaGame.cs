using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace ClusterFixture;

/// <summary>The arena as a client: a label with the score and a button.</summary>
public sealed class ArenaGame : GameDefinition
{
    public override string Name => "Arena";

    /// <summary>A scope to subscribe to once the main one is live, as game code does when a player moves somewhere (0: none).</summary>
    public uint FollowLater { get; init; }

    private bool _followed;

    public override void OnEachFrame(GameInstance game)
    {
        if (FollowLater == 0 || _followed || game.Remote is not { } remote || remote.Replica.State != OneGame.Sync.ReplicaState.Live)
        {
            return;
        }

        _followed = true;
        var registry = new EntityRegistry();
        RegisterEntities(registry);
        var world = new World(game.Seed, registry, WorldRole.Replica);
        RegisterSystems(world);
        _ = remote.Node.Subscribe(remote.Link, new OneGame.Sync.ScopeId(FollowLater), world);
    }

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.ClusterGame.EntityTypes.Register(registry);

    public override void Setup(World world) => ArenaWorld.Seed(world);

    public override void DefineScreens(ScreenSet screens) =>
        screens.Add("main", game => Ui.Column(
            Ui.Label(() => game.World.Entities.Count > 0 ? "Hits: " + ((Arena)game.World.Entities[0]).Hits : "connecting").Named("hits"),
            Ui.Button("Hit", () => ((Arena)game.World.Entities[0]).Send.Hit(1, "button")).Named("hit"))
            .Padding(Length.Px(8)));
}
