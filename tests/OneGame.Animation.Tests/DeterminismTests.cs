using CharacterExample;

namespace OneGame.Animation.Tests;

/// <summary>
/// The rule: animation is view code. The sim moves the figure by fixed-point root motion steps baked at load, so the state hashes are the
/// same however the run is made, split, restored or replayed; the animator never touches them.
/// </summary>
public class DeterminismTests
{
    private static readonly TestOptions Options = new() { Gpu = OneGame.Host.GpuMode.Off };

    // Walk, run, jump (in the middle of a run), turn left, walk on, stop.
    private static void Drive(TestClient client, int frame)
    {
        switch (frame)
        {
            case 3: client.Send("Hero", "SetGait", "1"); break;
            case 100: client.Send("Hero", "SetGait", "2"); break;
            case 160: client.Send("Hero", "Jump"); break;
            case 230: client.Send("Hero", "Turn", "90"); break;
            case 232: client.Send("Hero", "SetGait", "1"); break;
            case 300: client.Send("Hero", "SetGait", "0"); break;
        }
    }

    [Fact]
    public void TwoRunsGiveTheSameHashEveryTick()
    {
        var report = Determinism.RunTwice(() => new CharacterGame(), Drive, 360, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Theory]
    [InlineData(50)]  // walking
    [InlineData(175)] // in the air
    [InlineData(240)] // after the turn
    public void SnapshotAndRestoreGivesTheSameHashes(int split)
    {
        var report = Determinism.SnapshotRestore(() => new CharacterGame(), Drive, 360, split, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Fact]
    public void ReplayOfTheRecordingReproducesTheHashes()
    {
        var report = Determinism.Replay(() => new CharacterGame(), Drive, 360, Options);
        Assert.True(report.Identical, report.ToString());
    }
}
