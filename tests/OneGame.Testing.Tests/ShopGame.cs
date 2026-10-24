using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.Testing.Tests;

/// <summary>A small shop used by the harness tests: gold, stock, a luck roll each few ticks, and two commands.</summary>
[Entity]
public partial class Shop : Entity
{
    [Synced] public partial Fixed Gold { get; private set; }

    [Synced] public partial int Stock { get; private set; }

    [Synced] public partial int Luck { get; private set; }

    [Synced] public partial string Status { get; private set; }

    [Command]
    public bool Buy(int count)
    {
        if (count <= 0 || count > Stock)
        {
            Status = "refused";
            return false;
        }

        Stock -= count;
        Gold += Fixed.FromInt(5 * count);
        Status = "sold " + count;
        return true;
    }

    [Command]
    public void Restock(int count) => Stock += count;

    [Command]
    public void Rename(string status, bool shout) => Status = shout ? status.ToUpperInvariant() : status;

    protected override void OnTick()
    {
        if (World!.Tick % 5 == 0)
        {
            Luck += World.Rng["luck"].NextInt(1, 10);
        }
    }
}

/// <summary>The game under test: one shop, a main screen with a label and two buttons, a second screen, and a fixture.</summary>
public class ShopGame : GameDefinition
{
    public override string Name => "ShopGame";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.OneGame_Testing_Tests.EntityTypes.Register(registry);

    public override void Setup(World world)
    {
        var shop = world.Add(new Shop());
        shop.Restock(3);
    }

    public override void DefineFixtures(FixtureSet fixtures) =>
        fixtures.Add("rich", world =>
        {
            var shop = world.Add(new Shop());
            shop.Restock(50);
        });

    public override void DefineScreens(ScreenSet screens)
    {
        screens.Add("main", game => Ui.Column(
            Ui.Label(() => "Gold: " + Shop(game).Gold).Named("gold"),
            Ui.Label(() => "Stock: " + Shop(game).Stock).Named("stock"),
            Ui.Button("Buy one", () => Shop(game).Send.Buy(1)).Named("buy").Size(Length.Ui(60), Length.Ui(20)).Background(Color.Rgb(0x3060A0)),
            Ui.Button("Restock", () => Shop(game).Send.Restock(2)).Named("restock").Size(Length.Px(120), Length.Px(40)).Background(Color.Rgb(0x3060A0)))
            .Padding(Length.Px(16)).Gap(Length.Px(8)).Background(Color.Rgb(0x203040)).TextColor(Color.White));
        screens.Add("help", game => Ui.Column(Ui.Label("Help").Named("title")).Padding(Length.Px(16)));
    }

    private static Shop Shop(GameInstance game) => (Shop)game.World.Entities[0];
}
