namespace OneGame.Scene3D.Tests;

public class ReplicaTests
{
    [Fact]
    public void TheReplicaShows3DFromItsOwnStateAndMatchesTheAuthority()
    {
        using var cluster = Fixture.Start();
        var game = cluster.Client.Game;
        var replica = game.Replica;
        var captured = 0;
        for (var tick = 1; tick <= 90; tick++)
        {
            if (tick == 30)
            {
                cluster.Client.Send("Crate#1", "Nudge", "1.5");
            }

            if (tick == 60)
            {
                cluster.Client.Send("Rig", "MoveTo", "-5", "2", "7");
            }

            cluster.Client.Step(1);

            // Two nodes, one hash: the replica's view hash is the authority's at the same tick.
            Assert.Equal(game.World.Tick, replica.World.Tick);
            Assert.Equal(game.World.ComputeViewHash(), replica.ViewHash);
            Assert.NotSame(game.World, replica.World);

            if (tick % 15 == 0)
            {
                // The renderer builds the picture from whichever world it is given; the replica's must be the same picture.
                var fromAuthority = game.Capture(640, 360);
                var fromReplica = game.Capture(640, 360, 1f, replica.World);
                Assert.Equal(0, ImageCompare.Compare(fromAuthority, fromReplica).DifferingPixels);
                captured++;
            }
        }

        Assert.Equal(6, captured);
        Assert.Empty(game.DriftReports);

        // And what it holds is what it draws: give the replica's first box the state of the last one (a corrupted replica), and its picture changes.
        var before = game.Capture(640, 360, 1f, replica.World);
        var crates = replica.World.Entities.OfType<Scene3DGame.Crate>().ToArray();
        var bytes = new OneGame.Core.SyncWriter();
        crates[^1].WriteState(bytes);
        var reader = new OneGame.Core.SyncReader(bytes.Written);
        crates[0].ReadState(ref reader);
        var after = game.Capture(640, 360, 1f, replica.World);
        Assert.NotEqual(0, ImageCompare.Compare(before, after).DifferingPixels);
        Assert.Equal(0, ImageCompare.Compare(before, game.Capture(640, 360)).DifferingPixels);
        Assert.Empty(game.TakeGpuValidationMessages());
    }
}
