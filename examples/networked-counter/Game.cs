using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace NetworkedCounterExample;

/// <summary>The client. On its own it is a one-node game; started with --connect udp://host:port its world is a replica of the
/// server's, and the same button sends the command over the network.</summary>
public sealed class NetworkedCounterGame : GameDefinition
{
    public override string Name => "Networked counter";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.NetworkedCounterExample.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Counter());

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game => Ui.Column(
        Ui.Label(() => game.World.Entities.Count > 0 ? "Count: " + CounterOf(game).Count : "connecting").Named("count"),
        Ui.Button("Add one", () => CounterOf(game).Send.Increment(1)).Named("add"))
        .Padding(Length.Px(16)).Gap(Length.Px(8)).TextColor(Color.White));

    private static Counter CounterOf(GameInstance game) => (Counter)game.World.Entities[0];
}
