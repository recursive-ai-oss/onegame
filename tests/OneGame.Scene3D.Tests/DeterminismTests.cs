using Scene3DGame;

namespace OneGame.Scene3D.Tests;

/// <summary>The 3D sim state follows the same determinism rules as any other: reruns, snapshot and restore, and replay give the same hashes.</summary>
public class DeterminismTests
{
    private static readonly TestOptions Options = new() { Gpu = OneGame.Host.GpuMode.Off };

    private static void Drive(TestClient client, int frame)
    {
        if (frame == 40)
        {
            client.Send("Crate#1", "Nudge", "1");
        }

        if (frame == 90)
        {
            client.Send("Rig", "MoveTo", "6", "2", "6");
        }

        if (frame == 130)
        {
            client.Send("Crate#3", "Nudge", "-0.5");
        }
    }

    [Fact]
    public void TwoRunsGiveTheSameHashEveryTick()
    {
        var report = Determinism.RunTwice(() => new Scene3DGameDefinition(), Drive, 240, Options);
        Assert.True(report.Identical, report.ToString());
        Assert.Equal(240, report.Frames);
    }

    [Fact]
    public void SnapshotAndRestoreHalfwayGivesTheSameHashes()
    {
        var report = Determinism.SnapshotRestore(() => new Scene3DGameDefinition(), Drive, 240, 120, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Fact]
    public void ReplayOfTheRecordingReproducesTheHashes()
    {
        var report = Determinism.Replay(() => new Scene3DGameDefinition(), Drive, 240, Options);
        Assert.True(report.Identical, report.ToString());
        Assert.Equal(240, report.Frames);
    }
}
