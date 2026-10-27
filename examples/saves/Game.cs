using OneGame.Core;
using OneGame.Host;
using OneGame.Storage;
using OneGame.UI;

namespace SavesExample;

/// <summary>What the game keeps: a whole world in a save slot, and the player's choices as settings.</summary>
[Entity]
public partial class Jar : Entity
{
    [Synced] public partial int Coins { get; private set; }

    [Command] public void Add(int by) => Coins += by;
}

/// <summary>Settings are a plain class plus a schema. There is no reflection, so it works in NativeAOT and on iOS.</summary>
public sealed class Options
{
    public int Volume { get; set; } = 7;

    public static SettingsSchema<Options> Schema { get; } = new SettingsSchema<Options>("options.bin")
        .Whole("volume", o => o.Volume, (o, v) => o.Volume = v, min: 0, max: 10);
}

public sealed class SavesGame : GameDefinition
{
    public override string Name => "Saves";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.SavesExample.EntityTypes.Register(registry);

    public override void Setup(World world) => world.Add(new Jar());

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game =>
    {
        var options = game.Storage.OpenSettings(Options.Schema);       // loads now; defaults if missing or damaged
        return Ui.Column(
            Ui.Label(() => $"Coins: {((Jar)game.World.Entities[0]).Coins}   Volume: {options.Value.Volume}").Named("status"),
            Ui.Button("Add a coin", () => ((Jar)game.World.Entities[0]).Send.Add(1)).Named("add"),
            Ui.Button("Save", () => game.Storage.Save("slot1", "Coins in the jar")).Named("save"),   // call these between ticks, from the UI
            Ui.Button("Load", () => game.Storage.Load("slot1")).Named("load"),                      // swaps the running world only if the save is good
            Ui.Button("Louder", () => { options.Value.Volume++; options.Save(); }).Named("louder"))
            .Padding(Length.Px(16)).Gap(Length.Px(8)).TextColor(Color.White);
    });
}
