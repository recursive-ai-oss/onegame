using Draw2DGame;

namespace OneGame.Draw2D.Tests;

/// <summary>Drawing is view-only: the sim hashes are the same however a game is run.</summary>
public class DeterminismTests
{
    private static readonly TestOptions Options = new() { Gpu = OneGame.Host.GpuMode.Off };

    private static void Drive(TestClient client, int frame)
    {
        if (frame == 20)
        {
            client.Send("Hero", "Go", "1.5");
        }

        if (frame == 120)
        {
            client.Send("Hero", "Go", "-2");
        }

        if (frame == 200)
        {
            client.Send("Hero", "Go", "0");
        }
    }

    [Fact]
    public void TwoRunsGiveTheSameHashEveryTick()
    {
        var report = Determinism.RunTwice(() => new Draw2DGameDefinition(), Drive, 240, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Fact]
    public void SnapshotAndRestoreHalfwayGivesTheSameHashes()
    {
        var report = Determinism.SnapshotRestore(() => new Draw2DGameDefinition(), Drive, 240, 120, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Fact]
    public void ReplayOfTheRecordingReproducesTheHashes()
    {
        var report = Determinism.Replay(() => new Draw2DGameDefinition(), Drive, 240, Options);
        Assert.True(report.Identical, report.ToString());
    }
}
