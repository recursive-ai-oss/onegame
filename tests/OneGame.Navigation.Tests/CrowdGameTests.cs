using CrowdExample;

namespace OneGame.Navigation.Tests;

/// <summary>The crowd example as a whole game: the same state hashes however it is run, split, restored or replayed.</summary>
public class CrowdGameTests
{
    private static readonly TestOptions Options = new() { Gpu = OneGame.Host.GpuMode.Off };

    // Twelve people, sent across the gap; later some leave, one comes back and a few turn round mid-crossing.
    private static void Drive(TestClient client, int frame)
    {
        switch (frame)
        {
            case 2:
                for (var i = 0; i < 12; i++)
                {
                    client.Send("Plaza", "Spawn", (1 + (i / 6)).ToString(), (3 + (i % 6)).ToString());
                }

                break;
            case 4:
                for (var i = 0; i < 12; i++)
                {
                    client.Send("Plaza", "GoTo", i.ToString(), (16 + (i / 6)).ToString(), (1 + ((i % 6) * 2)).ToString());
                }

                break;
            case 150:
                client.Send("Plaza", "Leave", "3");
                client.Send("Plaza", "Leave", "4");
                client.Send("Plaza", "Spawn", "18", "5");
                client.Send("Plaza", "GoTo", "7", "2", "5");
                client.Send("Plaza", "GoTo", "8", "2", "6");
                break;
            case 152:
                client.Send("Plaza", "GoTo", "12", "1", "1");
                break;
        }
    }

    [Fact]
    public void TwoRunsGiveTheSameHashEveryTick()
    {
        var report = Determinism.RunTwice(() => new CrowdGame(), Drive, 500, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Theory]
    [InlineData(3)]   // just spawned
    [InlineData(100)] // walking to the gap
    [InlineData(155)] // some left, some turned round
    [InlineData(300)] // settling at the far side
    public void SnapshotAndRestoreGivesTheSameHashes(int split)
    {
        var report = Determinism.SnapshotRestore(() => new CrowdGame(), Drive, 500, split, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Fact]
    public void ReplayOfTheRecordingReproducesTheHashes()
    {
        var report = Determinism.Replay(() => new CrowdGame(), Drive, 500, Options);
        Assert.True(report.Identical, report.ToString());
    }
}
