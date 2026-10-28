using PathingExample;

namespace OneGame.Navigation.Tests;

/// <summary>The pathing example as a whole game: the same state hashes however it is run, split, restored or replayed.</summary>
public class GameTests
{
    private static readonly TestOptions Options = new() { Gpu = OneGame.Host.GpuMode.Off };

    // Search while the first path is being made, walk, change the map under the walkers, search again.
    private static void Drive(TestClient client, int frame)
    {
        switch (frame)
        {
            case 3:
                client.Send("Unit#1", "Spawn", "0", "5", "1");
                client.Send("Unit#2", "Spawn", "0", "9", "2");
                break;
            case 5:
                client.Send("Unit#1", "GoTo", "19", "5");
                client.Send("Unit#2", "GoTo", "18", "9");
                break;
            case 70:
                client.Send("Terrain", "Toggle", "10", "9");
                client.Send("Terrain", "Toggle", "10", "10");
                break;
            case 400:
                client.Send("Unit#1", "GoTo", "0", "5");
                break;
        }
    }

    [Fact]
    public void TwoRunsGiveTheSameHashEveryTick()
    {
        var report = Determinism.RunTwice(() => new PathingGame(), Drive, 700, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Theory]
    [InlineData(6)]   // inside the first search
    [InlineData(40)]  // walking
    [InlineData(72)]  // just after the map changed, searching again
    [InlineData(401)] // a second trip is searching
    public void SnapshotAndRestoreGivesTheSameHashes(int split)
    {
        var report = Determinism.SnapshotRestore(() => new PathingGame(), Drive, 700, split, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Fact]
    public void ReplayOfTheRecordingReproducesTheHashes()
    {
        var report = Determinism.Replay(() => new PathingGame(), Drive, 700, Options);
        Assert.True(report.Identical, report.ToString());
    }
}
