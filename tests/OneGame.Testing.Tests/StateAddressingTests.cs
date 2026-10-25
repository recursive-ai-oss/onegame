using System.Text.Json;
using OneGame.Control;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.Testing.Tests;

/// <summary>A ship with a synced list of installed parts.</summary>
[Entity]
public partial class Freighter : Entity
{
    [Synced] public partial int Credits { get; private set; }

    [Synced] public SyncList<int> Fitted { get; } = new();

    [Synced] public SyncList<PartSlot> Slots { get; } = new();

    [Command]
    public void Fit(int part) => Fitted.Add(part);
}

/// <summary>A synced struct held in a list.</summary>
[SyncedStruct]
public partial struct PartSlot
{
    public int Kind { get; set; }

    public bool Damaged { get; set; }
}

/// <summary>Row of the hot table of lanes.</summary>
[HotData(Name = "lanes")]
public struct LaneRow
{
    public int Heat { get; set; }

    public Fixed Load { get; set; }

    public string Label { get; set; }

    public bool Open { get; set; }
}

/// <summary>Heats every lane by one each tick, so the table's contents change the state hash as the game runs.</summary>
public sealed class HeatSystem(LaneRowTable table) : ISimSystem
{
    public void Tick(World world)
    {
        var heat = table.Heat;
        for (var i = 0; i < heat.Length; i++)
        {
            heat[i] += 1;
        }
    }
}

/// <summary>A game with a synced list and a hot table, for addressing them from scenarios.</summary>
public sealed class LedgerGame : GameDefinition
{
    public override string Name => "LedgerGame";

    public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.OneGame_Testing_Tests.EntityTypes.Register(registry);

    public override void RegisterSystems(World world) => world.AddSystem(new HeatSystem(world.AddTable(new LaneRowTable())));

    public override void Setup(World world)
    {
        var ship = world.Add(new Freighter());
        ship.Fit(7);
        ship.Fit(9);
        ship.Slots.Add(new PartSlot { Kind = 3, Damaged = true });
        var lanes = (LaneRowTable)world.Tables[0];
        lanes.Add(new LaneRow { Heat = 10, Load = Fixed.FromInt(1), Label = "north", Open = true });
        lanes.Add(new LaneRow { Heat = 20, Load = Fixed.FromInt(2), Label = "south", Open = false });
    }

    public override void DefineScreens(ScreenSet screens) => screens.Add("main", game => Ui.Column(Ui.Label("ledger")));
}

public class StateAddressingTests
{
    private static TestOptions Options() => new() { Width = 320, Height = 200, Gpu = GpuMode.Off };

    [Fact]
    public void ScenariosReadListsAndTablesAndWriteTableCells()
    {
        using var cluster = TestCluster.Start(new LedgerGame(), Options());
        var scenario = ScenarioParser.Parse(
            """
            steps:
              - set: { table: lanes, row: 1, property: Heat, value: 500 }
              - set: { table: lanes, row: 0, property: Load, value: 2.5 }
              - set: { table: lanes, row: 0, property: Label, value: harbour }
              - set: { table: lanes, row: 1, property: Open, value: true }
              - step: 3
            expect:
              - state: { entity: Freighter, property: Fitted.count, equals: 2 }
              - state: { entity: Freighter, property: "Fitted[1]", equals: 9 }
              - state: { entity: Freighter, property: Fitted, equals: "[7,9]" }
              - state: { entity: Freighter, property: "Slots[0].Kind", equals: 3 }
              - state: { entity: Freighter, property: "Slots[0].Damaged", equals: true }
              - state: { table: lanes, row: 1, property: Heat, equals: 503 }
              - state: { table: lanes, row: 0, property: Heat, equals: 13 }
              - state: { table: lanes, row: 0, property: Load, equals: 2.5 }
              - state: { table: lanes, row: 0, property: Label, equals: harbour }
              - state: { table: lanes, row: 1, property: Open, equals: true }
              - state: { table: lanes, property: count, equals: 2 }
              - state: { table: lanes, property: "[1].Heat", equals: 503 }
            """, "inline.yaml", "tables");
        var result = cluster.Run(scenario);
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
        Assert.Equal(12, result.Expectations.Length);
        Assert.Equal("lanes[1].Heat", result.Expectations[5].Target);
    }

    [Fact]
    public void BadAddressesAreReportedWithWhatExists()
    {
        using var cluster = TestCluster.Start(new LedgerGame(), Options());
        var c = cluster.Client;
        Assert.Contains("lanes", Assert.Throws<ControlException>(() => c.Call("state_query", """{"table":"nope"}""")).Message, StringComparison.Ordinal);
        Assert.Contains("2 items", Assert.Throws<ControlException>(() => c.Call("state_query", """{"entity":"Freighter","property":"Fitted[5]"}""")).Message, StringComparison.Ordinal);
        Assert.Contains("Credits", Assert.Throws<ControlException>(() => c.Call("state_query", """{"entity":"Freighter","property":"Nope"}""")).Message, StringComparison.Ordinal);
        Assert.Equal("not_found", Assert.Throws<ControlException>(() => c.Call("state_patch", """{"table":"lanes","row":9,"property":"Heat","value":"1"}""")).Code);
        Assert.Equal("rejected", Assert.Throws<ControlException>(() => c.Call("state_patch", """{"table":"lanes","row":0,"property":"Nope","value":"1"}""")).Code);
        Assert.Equal("rejected", Assert.Throws<ControlException>(() => c.Call("state_patch", """{"table":"lanes","row":0,"property":"Heat","value":"abc"}""")).Code);
        Assert.Equal("bad_args", Assert.Throws<ControlException>(() => c.Call("state_query", """{"entity":"Freighter","table":"lanes"}""")).Code);
        Assert.Throws<ScenarioException>(() => ScenarioParser.Parse("steps:\n  - set: { table: lanes, property: Heat, value: 1 }\n"));
        Assert.Throws<ScenarioException>(() => ScenarioParser.Parse("steps:\n  - set: { entity: Freighter, table: lanes, row: 0, property: Heat, value: 1 }\n"));
    }

    [Fact]
    public void SettingHotDataInARecordedRunReplaysToTheSameHashes()
    {
        using var cluster = TestCluster.Start(new LedgerGame(), Options());
        var c = cluster.Client;
        var recorder = c.Game.StartSessionRecording();
        c.Step(4);
        c.Call("state_patch", """{"table":"lanes","row":0,"property":"Heat","value":"1000"}""");
        c.Step(5);
        c.Send("Freighter", "Fit", "4");
        c.Step(2);
        var finalHash = c.Game.World.ComputeHash();
        Assert.Equal("1007", JsonDocument.Parse(c.Call("state_query", """{"table":"lanes","row":0,"property":"Heat"}""")).RootElement.GetRawText());
        var session = recorder.Stop();

        using var fresh = TestCluster.Start(new LedgerGame(), Options());
        var replay = fresh.Client.Game.Replay(session);
        Assert.True(replay.Ok);
        Assert.Equal(finalHash, replay.FinalHash);
        Assert.Equal(7, replay.TicksRun - 4);
    }
}
