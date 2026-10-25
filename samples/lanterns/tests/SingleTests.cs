using OneGame.Testing;

namespace Lanterns.Tests;

public class SingleTests
{
    private static void Drive(TestClient c, int frame)
    {
        switch (frame)
        {
            case 2:
                c.Send("Player", "Steer", "1", "1");
                break;
            case 90:
                c.Send("Player", "Steer", "-1", "0");
                break;
            case 150:
                c.Send("Player", "Steer", "0", "-1");
                break;
        }
    }

    [Fact]
    public void TheSameRunTwiceGivesTheSameHashEveryFrame() =>
        Determinism.RunTwice(() => new LanternsGame(), Drive, 240).ThrowIfDifferent();

    [Fact]
    public void ASnapshotRestoredHalfwayContinuesIdentically() =>
        Determinism.SnapshotRestore(() => new LanternsGame(), Drive, 240, 100).ThrowIfDifferent();

    [Fact]
    public void ARecordingReplaysToTheSameHashes() =>
        Determinism.Replay(() => new LanternsGame(), Drive, 240).ThrowIfDifferent();
}
