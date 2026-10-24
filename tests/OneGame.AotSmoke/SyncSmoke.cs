using OneGame.Core;
using OneGame.Sync;

namespace OneGame.AotSmoke;

/// <summary>A tiny one-node cluster under NativeAOT: step, commands over loopback, drift checks, save, load, record and replay.</summary>
internal static class SyncSmoke
{
    private static World NewWorld(WorldRole role)
    {
        var registry = new EntityRegistry();
        OneGame.Generated.OneGame_AotSmoke.EntityTypes.Register(registry);
        var world = new World(11, registry, role);
        world.AddTable(new SparkTable());
        return world;
    }

    public static string? Run()
    {
        var authority = NewWorld(WorldRole.Authority);
        ((SparkTable)authority.Tables[0]).Add(new Spark { X = Fixed.FromRatio(1, 5), Life = 3 });
        authority.Add(new Purse { Owner = "sync" });

        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), authority);
        var view = cluster.AddLocalReplica(scope, NewWorld(WorldRole.Replica), Trust.Player(scope.Id));
        cluster.Settle();
        var recorder = scope.StartRecording(new RecordingOptions { CheckpointInterval = 1 });
        var purse = (Purse)view.World.Entities[0];
        for (var i = 0; i < 40; i++)
        {
            if (i % 5 == 0)
            {
                purse.Send.Add(i, Fixed.FromRatio(i, 4));
            }

            cluster.Step();
        }

        var recording = recorder.Stop();
        if (view.Hash != scope.Hash || cluster.DriftReports.Count != 0)
        {
            return "replica drifted from its authority";
        }

        var replay = Replayer.Replay(Recording.TryRead(recording.ToBytes(), out var decoded) == SyncError.None ? decoded! : recording, NewWorld(WorldRole.Authority), new ReplayOptions { MaxTicks = 10_000 });
        if (!replay.Ok || replay.FinalHash != scope.Hash)
        {
            return "replay diverged: " + replay.Divergence;
        }

        var loaded = NewWorld(WorldRole.Authority);
        var result = SaveFile.Load(scope.Save(), loaded);
        if (result.Status != SaveStatus.Ok || loaded.ComputeHash() != scope.Hash || !SaveFile.ToJson(scope.Save(), NewWorld(WorldRole.Authority)).Contains("Purse", StringComparison.Ordinal))
        {
            return "save did not round-trip: " + result.Status;
        }

        var driver = new FixedStepDriver(60);
        if (driver.Advance(TimeSpan.FromMilliseconds(50)) != 3)
        {
            return "fixed-step driver miscounted";
        }

        return null;
    }
}
