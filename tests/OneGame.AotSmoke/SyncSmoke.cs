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

        return RunFiltered() ?? RunPredicted();
    }

    /// <summary>A replica predicts a command, rolls back and resimulates as frames arrive, and ends on the authority's state.</summary>
    private static string? RunPredicted()
    {
        var authority = NewWorld(WorldRole.Authority);
        authority.Add(new Purse { Owner = "predicted" });
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), authority);
        var view = cluster.AddLocalReplica(scope, NewWorld(WorldRole.Replica), Trust.Player(scope.Id));
        var prediction = view.EnablePrediction(NewWorld(WorldRole.Predicted), new PredictionOptions { LeadTicks = 2, AutoLead = false });
        cluster.Settle();
        var purse = (Purse)prediction.World.Entities[0];
        purse.Send.Add(3, Fixed.FromInt(2));
        if (purse.Coins.Count != 1 || ((Purse)view.World.Entities[0]).Coins.Count != 0)
        {
            return "a predicted command did not show at once on the predicted world only";
        }

        for (var i = 0; i < 30; i++)
        {
            cluster.Step();
            prediction.Advance();
            if (i == 10)
            {
                ((Purse)prediction.World.Entities[0]).Send.Add(4, Fixed.FromInt(1));
            }
        }

        cluster.Step(10);
        var confirmed = (Purse)view.World.Entities[0];
        var predicted = (Purse)prediction.World.Entities[0];
        if (confirmed.Coins.Count != 2 || predicted.Coins.Count != 2 || prediction.Unconfirmed != 0 || cluster.DriftReports.Count != 0)
        {
            return "the prediction did not settle on the authority";
        }

        if (predicted.Total != confirmed.Total + (Fixed.FromRatio(1, 4) * Fixed.FromInt(prediction.Tick - view.World.Tick)) || prediction.Reconciliations == 0)
        {
            return "the predicted world is not the confirmed state moved on by its lead";
        }

        var glide = new Timed<Fixed>(Fixed.FromInt(0), Fixed.FromInt(10), 0, 10);
        var between = glide.ValueAt(5, Fixed.FromRatio(1, 2));
        return between > Fixed.FromRatio(54, 10) && between < Fixed.FromRatio(56, 10) && between > glide.ValueAt(5) ? null : "a timed value did not interpolate between ticks";
    }

    /// <summary>Two players follow one scope: each gets its own view, private data stays private, answers come back, trust changes apply.</summary>
    private static string? RunFiltered()
    {
        var authority = NewWorld(WorldRole.Authority);
        authority.Add(new Locker { Player = 1, Combination = "left-right-left" });
        authority.Add(new Locker { Player = 2, Combination = "up-down-up" });
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), authority);
        var first = cluster.AddLocalReplica(scope, NewWorld(WorldRole.Replica), Trust.Owner(scope.Id).As(1), "first");
        var second = cluster.AddLocalReplica(scope, NewWorld(WorldRole.Replica), Trust.Owner(scope.Id).As(2), "second");
        cluster.Settle();
        var mine = (Locker)first.World.Entities[0];
        var theirs = (Locker)first.World.Entities[1];
        mine.Send.Open(1);
        var wrongSequence = first.LastSequence;
        mine.Send.Reset("new-combination");
        theirs.Send.Reset("stolen");
        var stolenSequence = first.LastSequence;
        cluster.Step(10);

        if (mine.Combination != "new-combination" || theirs.Combination.Length != 0 || theirs.Alarm != 0)
        {
            return "a filtered replica held the wrong data";
        }

        if (first.ViewHash != scope.ViewHashFor(first.Link.Connection) || second.ViewHash != scope.ViewHashFor(second.Link.Connection) || first.ViewHash == second.ViewHash)
        {
            return "filtered view hashes did not match the authority";
        }

        // A refusal is answered at once; the others once their tick has run.
        CommandResult? wrong = null, refused = null;
        foreach (var result in first.Results)
        {
            if (result.Sequence == wrongSequence)
            {
                wrong = result;
            }
            else if (result.Sequence == stolenSequence)
            {
                refused = result;
            }
        }

        if (first.Results.Count != 3 || wrong?.Reason != "wrong code" || wrong?.Verdict != CommandVerdict.Rejected || refused?.Refusal != RefusalReason.NotOwner)
        {
            return "command answers did not reach the sender";
        }

        first.Link.Connection.Trust = Trust.Owner(scope.Id).As(2);
        cluster.Step(3);
        if (((Locker)first.World.Entities[1]).Combination != "up-down-up" || ((Locker)first.World.Entities[0]).Combination.Length != 0 || cluster.DriftReports.Count != 0)
        {
            return "a trust change did not move the private data";
        }

        return null;
    }
}
