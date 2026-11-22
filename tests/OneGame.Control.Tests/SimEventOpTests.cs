using System.Text.Json;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.Control.Tests;

[SimEvent]
public partial struct Ping
{
    public int N { get; set; }

    public string Who { get; set; }
}

[Entity]
public partial class Pinger : Entity
{
    [Synced] public partial int N { get; set; }

    protected override void OnTick()
    {
        N++;
        if (N % 2 == 0)
        {
            Emit(new Ping { N = N, Who = "pinger" });
        }
    }
}

[Collection("AudioService.Active")]
public class SimEventOpTests
{
    private sealed class PingGame : GameDefinition
    {
        public override string Name => "PingGame";

        public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.OneGame_Control_Tests.EntityTypes.Register(registry);

        public override void Setup(World world) => world.Add(new Pinger());

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("x")));
    }

    [Fact]
    public void TheOpListsEventsByNameWithTheirPayloadAndTheNextSequence()
    {
        using var game = GameInstance.Create(new PingGame(), new GameOptions { Headless = true, Gpu = GpuMode.Off });
        var surface = new ControlSurface(game);
        game.Step(10);

        var all = JsonDocument.Parse(surface.Call("sim_events", "{}")).RootElement;
        Assert.Equal("authority", all.GetProperty("world").GetString());
        Assert.Equal(5, all.GetProperty("next").GetInt64());
        var events = all.GetProperty("events").EnumerateArray().ToArray();
        Assert.Equal(5, events.Length);
        Assert.Equal("Ping", events[0].GetProperty("type").GetString());
        Assert.Equal(1, events[0].GetProperty("tick").GetInt64());
        Assert.Equal(2, events[0].GetProperty("data").GetProperty("N").GetInt32());
        Assert.Equal("pinger", events[4].GetProperty("data").GetProperty("Who").GetString());
        Assert.Equal(16, all.GetProperty("hash").GetString()!.Length);

        // Asking again with the last next returns only what is new.
        game.Step(4);
        var fresh = JsonDocument.Parse(surface.Call("sim_events", """{"since":5}""")).RootElement;
        Assert.Equal(7, fresh.GetProperty("next").GetInt64());
        Assert.Equal([5L, 6L], fresh.GetProperty("events").EnumerateArray().Select(e => e.GetProperty("sequence").GetInt64()).ToArray());
        Assert.Equal(1, JsonDocument.Parse(surface.Call("sim_events", """{"max":1}""")).RootElement.GetProperty("events").GetArrayLength());
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => surface.Call("sim_events", """{"max":0}""")).Code);
        Assert.Contains(ControlOps.All, o => o.Name == "sim_events" && !o.ChangesState);
    }
}
