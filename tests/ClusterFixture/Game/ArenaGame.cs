using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace ClusterFixture;

/// <summary>The arena as a client: a label with the score and a button.</summary>
public sealed class ArenaGame : GameDefinition
{
    public override string Name => "Arena";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.ClusterGame.EntityTypes.Register(registry);

    public override void Setup(World world) => ArenaWorld.Seed(world);

    public override void DefineScreens(ScreenSet screens) =>
        screens.Add("main", game => Ui.Column(
            Ui.Label(() => game.World.Entities.Count > 0 ? "Hits: " + ((Arena)game.World.Entities[0]).Hits : "connecting").Named("hits"),
            Ui.Button("Hit", () => ((Arena)game.World.Entities[0]).Send.Hit(1, "button")).Named("hit"))
            .Padding(Length.Px(8)));
}
