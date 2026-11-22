using System.Text.Json;
using OneGame.Core;
using OneGame.Host;
using OneGame.UI;

namespace OneGame.Testing.Tests;

[SimEvent]
public partial struct Scored
{
    public int Points { get; set; }

    public string Who { get; set; }
}

[Entity]
public partial class Scorer : Entity
{
    [Synced] public partial int Total { get; private set; }

    [Command]
    public void Score(int points, string who)
    {
        Total += points;
        Emit(new Scored { Points = points, Who = who });
    }
}

public class EventScenarioTests
{
    private sealed class ScoreGame : GameDefinition
    {
        public override string Name => "ScoreGame";

        public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.OneGame_Testing_Tests.EntityTypes.Register(registry);

        public override void Setup(World world) => world.Add(new Scorer());

        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => Ui.Column(Ui.Label("x")));
    }

    private static ScenarioResult Run(string yaml)
    {
        using var cluster = TestCluster.Start(new ScoreGame(), new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Off });
        return cluster.Run(ScenarioParser.Parse(yaml));
    }

    [Fact]
    public void AScenarioCountsEventsByNameAndPayload()
    {
        var result = Run("""
            name: scores
            steps:
              - command: {entity: Scorer, name: Score, args: [5, ann]}
              - command: {entity: Scorer, name: Score, args: [7, bo]}
              - command: {entity: Scorer, name: Score, args: [5, bo]}
              - step: 2
            expect:
              - event: { name: Scored }
              - event: { name: Scored, count: 3 }
              - event: { name: Scored, where: { Points: 5 }, count: 2 }
              - event: { name: Scored, where: { Points: 5, Who: bo }, count: 1 }
              - event: { name: Scored, where: { Who: nobody }, count: 0 }
              - event: { name: Other, count: 0 }
            """);
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));
    }

    [Fact]
    public void AWrongCountFailsWithWhatWasSeen()
    {
        var result = Run("""
            name: wrong
            steps:
              - command: {entity: Scorer, name: Score, args: [5, ann]}
              - step: 1
            expect:
              - event: { name: Scored, count: 2 }
              - event: { name: Scored, where: { Points: 9 } }
            """);
        Assert.False(result.Passed);
        var json = JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult);
        Assert.Contains("event Scored was emitted 1 times, expected exactly 2", json, StringComparison.Ordinal);
        Assert.Contains("event Scored where Points = 9 was emitted 0 times, expected at least 1", json, StringComparison.Ordinal);
    }

    [Fact]
    public void EventsBeforeTheScenarioDoNotCount()
    {
        using var cluster = TestCluster.Start(new ScoreGame(), new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Off });
        cluster.Client.Call("command_send", """{"entity":"Scorer","command":"Score","args":[1,"early"]}""");
        cluster.Client.Step(2);
        var result = cluster.Run(ScenarioParser.Parse("name: later\nsteps:\n  - step: 1\nexpect:\n  - event: { name: Scored, count: 0 }\n"));
        Assert.True(result.Passed, JsonSerializer.Serialize(result, ScenarioJson.Default.ScenarioResult));

        // The live op still lists it, with the payload.
        var listed = JsonDocument.Parse(cluster.Client.Call("sim_events", "{}")).RootElement;
        Assert.Equal("early", listed.GetProperty("events")[0].GetProperty("data").GetProperty("Who").GetString());
    }

    [Fact]
    public void BadEventExpectationsAreParseErrors()
    {
        Assert.Throws<ScenarioException>(() => ScenarioParser.Parse("expect:\n  - event: { count: 1 }\n"));
        Assert.Throws<ScenarioException>(() => ScenarioParser.Parse("expect:\n  - event: { name: Scored, frames: 2 }\n"));
    }
}
