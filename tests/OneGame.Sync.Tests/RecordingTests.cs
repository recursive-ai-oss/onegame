using OneGame.Core.Tests.Samples;

namespace OneGame.Sync.Tests;

public class RecordingTests
{
    private static Recording Record(int ticks, int interval)
    {
        var (cluster, scope, view) = Scenario.NewCluster(options: new ClusterOptions { DriftCheckInterval = 30, FatalDrift = true });
        var recorder = scope.StartRecording(new RecordingOptions { CheckpointInterval = interval });
        Scenario.Play(cluster, scope, view, ticks);
        var recording = recorder.Stop();
        Assert.Equal(ticks, recording.EndTick);
        return recording;
    }

    [Fact]
    public void ThousandTickRecordingReplaysWithIdenticalHashesAtEveryCheckpoint()
    {
        var recording = Record(1000, 1);
        Assert.True(recording.Commands.Count > 200);
        Assert.Equal(1000, recording.Checkpoints.Count);

        var result = Replayer.Replay(recording, Scenario.NewWorld(WorldRole.Authority));
        Assert.True(result.Ok, result.Divergence?.ToString());
        Assert.Equal(1000, result.TicksRun);
        Assert.Equal(1000, result.CheckpointsVerified);
        Assert.Equal(recording.Checkpoints[^1].Hash, result.FinalHash);

        // The same through the file format.
        var bytes = recording.ToBytes();
        Assert.Equal(SyncError.None, Recording.TryRead(bytes, out var loaded));
        Assert.Equal(recording.Commands.Count, loaded!.Commands.Count);
        var again = Replayer.Replay(loaded, Scenario.NewWorld(WorldRole.Authority));
        Assert.True(again.Ok);
        Assert.Equal(result.FinalHash, again.FinalHash);
        Assert.Equal(bytes, loaded.ToBytes());
    }

    [Fact]
    public void ReplayRefusesARecordingLongerThanTheTickLimit()
    {
        var recording = Record(50, 10);
        var ticks = 0;
        var tooShort = Replayer.Replay(recording, Scenario.NewWorld(WorldRole.Authority), new ReplayOptions { MaxTicks = 49, OnTick = _ => ticks++ });
        Assert.False(tooShort.Ok);
        Assert.Equal(DivergenceReason.TooLong, tooShort.Divergence!.Reason);
        Assert.Equal(0, ticks);
        Assert.Equal(0, tooShort.TicksRun);

        Assert.True(Replayer.Replay(recording, Scenario.NewWorld(WorldRole.Authority), new ReplayOptions { MaxTicks = 50 }).Ok);

        // A hostile file that claims an enormous range is refused at once, with the default limit.
        recording.EndTick = long.MaxValue;
        var hostile = Replayer.Replay(recording, Scenario.NewWorld(WorldRole.Authority));
        Assert.Equal(DivergenceReason.TooLong, hostile.Divergence!.Reason);
    }

    [Fact]
    public void ReplayCallsBackAfterEveryTick()
    {
        var recording = Record(50, 10);
        var ticks = new List<long>();
        var result = Replayer.Replay(recording, Scenario.NewWorld(WorldRole.Authority), new ReplayOptions { OnTick = w => ticks.Add(w.Tick) });
        Assert.True(result.Ok);
        Assert.Equal(Enumerable.Range(1, 50).Select(i => (long)i), ticks);
        Assert.Equal(5, result.CheckpointsVerified);
    }

    [Fact]
    public void CorruptedCommandNamesTheTickAndEntity()
    {
        var recording = Record(400, 1);
        var index = recording.Commands.FindIndex(c => c.Tick > 200 && c.Entity == Scenario.ShopId.Value && c.CommandId == CommandIds.Buy);
        Assert.True(index >= 0);
        var original = recording.Commands[index];
        var payload = new SyncWriter();
        payload.WriteInt32(1);
        payload.WriteInt32(37);
        recording.Commands[index] = original with { Payload = payload.ToArray() };

        var result = Replayer.Replay(recording, Scenario.NewWorld(WorldRole.Authority));
        Assert.False(result.Ok);
        var d = result.Divergence!;
        Assert.Equal(DivergenceReason.Checkpoint, d.Reason);
        Assert.Equal(original.Tick + 1, d.Tick);
        Assert.Equal(original.Tick, d.LastGoodTick);
        var diff = Assert.Single(d.Differences, x => x.Kind == DigestKind.Entity);
        Assert.Equal(Scenario.ShopId.Value, diff.EntityId);
        Assert.Equal("Shop", diff.TypeName);
        Assert.Contains($"\"tick\":{original.Tick + 1}", result.ToJson());
    }

    [Fact]
    public void DroppedCommandIsFoundAtTheNextCheckpoint()
    {
        var recording = Record(300, 25);
        var index = recording.Commands.FindIndex(c => c.Tick > 100 && c.CommandId == CommandIds.Buy);
        var tick = recording.Commands[index].Tick;
        recording.Commands.RemoveAt(index);
        var d = Replayer.Replay(recording, Scenario.NewWorld(WorldRole.Authority)).Divergence!;
        Assert.True(d.Tick > tick && d.Tick % 25 == 0, d.ToString());
        Assert.True(d.LastGoodTick <= tick);
        Assert.Contains(d.Differences, x => x.EntityId == Scenario.ShopId.Value);
    }

    [Fact]
    public void ReplayInTheWrongWorldIsReportedNotThrown()
    {
        var recording = Record(20, 5);
        var wrong = new World(Scenario.Seed, Scenario.Registry()); // no hot table: the snapshot does not fit
        var result = Replayer.Replay(recording, wrong);
        Assert.Equal(DivergenceReason.StartSnapshot, result.Divergence!.Reason);
        Assert.Equal(DivergenceReason.Unusable, Replayer.Replay(recording, Scenario.NewWorld(WorldRole.Replica)).Divergence!.Reason);
    }

    [Fact]
    public void RecordingNeedsAQuietBoundaryAndOneRecorderAtATime()
    {
        var (cluster, scope, _) = Scenario.NewCluster();
        ((Shop)scope.World.Find(Scenario.ShopId)!).Send.Buy(1, 1);
        Assert.Throws<InvalidOperationException>(() => scope.StartRecording());
        cluster.Step();
        var recorder = scope.StartRecording();
        Assert.Throws<InvalidOperationException>(() => scope.StartRecording());
        Assert.Throws<InvalidOperationException>(() => scope.Load(scope.Save()));
        recorder.Stop();
        _ = scope.StartRecording();
    }

    [Fact]
    public void LocalInputOutsideTheTickIsLoggedButSimQueuedCommandsAreNot()
    {
        var (cluster, scope, _) = Scenario.NewCluster();
        var recorder = scope.StartRecording();
        ((Shop)scope.World.Find(Scenario.ShopId)!).Send.Buy(1, 2);
        cluster.Step(3);
        var recording = recorder.Stop();
        var logged = Assert.Single(recording.Commands);
        Assert.Equal(0, logged.Tick);
        Assert.Equal(CommandIds.Buy, logged.CommandId);
    }

    [Fact]
    public void DecodingBadRecordingsReturnsErrors()
    {
        var bytes = Record(30, 10).ToBytes();
        Assert.NotEqual(SyncError.None, Recording.TryRead(bytes.AsSpan(0, bytes.Length - 3), out _));
        Assert.NotEqual(SyncError.None, Recording.TryRead([], out _));
        var bad = (byte[])bytes.Clone();
        bad[0] ^= 0xFF;
        Assert.Equal(SyncError.BadHeader, Recording.TryRead(bad, out _));
        Assert.NotEqual(SyncError.None, Recording.TryRead([.. bytes, 0], out _));
    }
}

public class SaveTests
{
    private static byte[] SaveOf(Func<World, byte[]> write)
    {
        var world = Scenario.NewAuthority(2, 4);
        world.Step();
        return write(world);
    }

    [Fact]
    public void SaveAtTick500LoadedIntoAFreshClusterMatchesTheStraightRun()
    {
        var (straight, straightScope, straightView) = Scenario.NewCluster();
        Scenario.Play(straight, straightScope, straightView, 1000);

        var (first, firstScope, firstView) = Scenario.NewCluster();
        Scenario.Play(first, firstScope, firstView, 500);
        var save = firstScope.Save();
        Assert.Equal(500, firstScope.World.Tick);

        // A fresh process-equivalent: new worlds, nothing shared with the first run.
        var world = Scenario.NewWorld(WorldRole.Authority);
        var loaded = SaveFile.Load(save, world);
        Assert.Equal(SaveStatus.Ok, loaded.Status);
        Assert.True(loaded.HashVerified);
        Assert.False(loaded.Migrated);
        Assert.Equal(500, loaded.Header!.Tick);
        var (second, secondScope, secondView) = Scenario.NewCluster(world);
        Assert.Equal(firstScope.Hash, secondScope.Hash);
        Assert.Equal(secondScope.Hash, secondView.Hash);
        Scenario.Play(second, secondScope, secondView, 500);

        Assert.Equal(1000, secondScope.World.Tick);
        Assert.Equal(straightScope.Hash, secondScope.Hash);
        Assert.Equal(straightScope.Hash, secondView.Hash);
    }

    [Fact]
    public void LoadingASaveIntoARunningScopeResendsASnapshotToSubscribers()
    {
        var (cluster, scope, view) = Scenario.NewCluster();
        Scenario.Play(cluster, scope, view, 60);
        var save = scope.Save();
        var hashAt60 = scope.Hash;
        Scenario.Play(cluster, scope, view, 40);
        Assert.Equal(SaveStatus.Ok, scope.Load(save).Status);
        cluster.Settle();
        Assert.Equal(hashAt60, scope.Hash);
        Assert.Equal(hashAt60, view.Hash);
        Scenario.Play(cluster, scope, view, 10);
        Assert.Equal(scope.Hash, view.Hash);
    }

    [Fact]
    public void SaveWrittenAtAnOlderSchemaLoadsThroughItsMigration()
    {
        var world = new World(Scenario.Seed, Scenario.Registry());
        world.AddTable(new BotRowTable());
        var result = SaveFile.Load(Fuzzing.Scenario1SaveFixture.Build(), world);
        Assert.Equal(SaveStatus.Ok, result.Status);
        Assert.True(result.Migrated);
        Assert.False(result.HashVerified);
        var schema = Assert.Single(result.Header!.Schemas);
        Assert.Equal((1u, "Shop"), (schema.Version, schema.TypeName));
        var shop = (Shop)world.Find(Scenario.ShopId)!;
        Assert.Equal("Old Shop", shop.Name);
        Assert.Equal(Fixed.FromInt(7), shop.Gold);
        Assert.Equal(2, shop.Items.Count);
        Assert.Equal(30, shop.Items[0].Count);
        Assert.Equal(12, world.Tick);

        // And it runs on afterwards, then saves at the current schema.
        world.Step();
        var again = SaveFile.Load(SaveFile.Write(world), Scenario.NewWorld(WorldRole.Authority));
        Assert.True(again.HashVerified);
        Assert.False(again.Migrated);
        Assert.Equal(2u, Assert.Single(again.Header!.Schemas).Version);
    }

    [Fact]
    public void DamagedSavesAreRefusedWithAReason()
    {
        var save = SaveOf(SaveFile.Write);
        Assert.Equal(SaveStatus.Ok, SaveFile.Load(save, Scenario.NewWorld(WorldRole.Authority)).Status);
        Assert.Equal(SaveStatus.BadFile, SaveFile.Load(save.AsSpan(0, save.Length / 2), Scenario.NewWorld(WorldRole.Authority)).Status);
        Assert.Equal(SaveStatus.BadFile, SaveFile.Load([], Scenario.NewWorld(WorldRole.Authority)).Status);
        var flipped = (byte[])save.Clone();
        flipped[^5] ^= 0x10;
        Assert.Equal(SaveStatus.Corrupt, SaveFile.Load(flipped, Scenario.NewWorld(WorldRole.Authority)).Status);
        var otherSeed = new World(Scenario.Seed + 1, Scenario.Registry());
        otherSeed.AddTable(new BotRowTable());
        Assert.Equal(SaveStatus.SeedMismatch, SaveFile.Load(save, otherSeed).Status);
    }

    [Fact]
    public void JsonDumpShowsHeaderAndWorld()
    {
        var save = SaveOf(SaveFile.Write);
        var json = SaveFile.ToJson(save, Scenario.NewWorld(WorldRole.Authority));
        Assert.Contains("\"status\":\"Ok\"", json);
        Assert.Contains("\"type\":\"Shop\"", json);
        Assert.Contains("Corner Shop", json);
        Assert.Contains("\"schemas\"", json);
        Assert.Contains("BadFile", SaveFile.ToJson([1, 2, 3], Scenario.NewWorld(WorldRole.Authority)));
    }
}

public class DriverTests
{
    [Fact]
    public void TickCountIsExactUnderVariableFrameTimes()
    {
        var driver = new FixedStepDriver(60, maxStepsPerFrame: 1000);
        var rng = new Random(11);
        long elapsed = 0;
        long steps = 0;
        for (var i = 0; i < 20_000; i++)
        {
            var ticks = rng.Next(1, 600_000); // 0.1 to 60 ms
            elapsed += ticks;
            steps += driver.Advance(TimeSpan.FromTicks(ticks));
            Assert.Equal(elapsed * 60 / TimeSpan.TicksPerSecond, steps);
            Assert.InRange(driver.Alpha, 0.0, 0.999999999);
        }
    }

    [Fact]
    public void CatchUpIsCapped()
    {
        var driver = new FixedStepDriver(60, maxStepsPerFrame: 5);
        Assert.Equal(5, driver.Advance(TimeSpan.FromSeconds(10)));
        Assert.Equal(595, driver.DroppedTicks);
        // The stall does not snowball: the next normal frame is normal.
        Assert.Equal(1, driver.Advance(TimeSpan.FromMilliseconds(17)));
        Assert.Equal(0, driver.Advance(TimeSpan.FromMilliseconds(1)));
    }

    [Fact]
    public void PauseStepAndTimeScale()
    {
        var driver = new FixedStepDriver(10) { Paused = true };
        Assert.Equal(0, driver.Advance(TimeSpan.FromSeconds(1)));
        driver.RequestStep();
        Assert.Equal(1, driver.Advance(TimeSpan.Zero));
        Assert.Equal(0, driver.Advance(TimeSpan.Zero));
        driver.Paused = false;
        Assert.Equal(0, driver.Advance(TimeSpan.FromMilliseconds(50))); // paused time was discarded
        Assert.Equal(1, driver.Advance(TimeSpan.FromMilliseconds(50)));
        driver.TimeScale = 2;
        Assert.Equal(2, driver.Advance(TimeSpan.FromMilliseconds(100)));
        driver.TimeScale = 0.5;
        Assert.Equal(1, driver.Advance(TimeSpan.FromMilliseconds(200)));
        driver.TimeScale = 0;
        Assert.Equal(0, driver.Advance(TimeSpan.FromSeconds(5)));
        Assert.Equal(0, driver.Advance(TimeSpan.FromTicks(-5)));
        Assert.Throws<ArgumentOutOfRangeException>(() => driver.TimeScale = -1);
    }

    [Fact]
    public void ClusterFrameRunsTheDueTicks()
    {
        var (cluster, scope, view) = Scenario.NewCluster();
        cluster.Driver = new FixedStepDriver(60);
        long elapsed = 0;
        for (var i = 0; i < 100; i++)
        {
            var dt = TimeSpan.FromTicks(100_000 + ((i * 37_001L) % 400_000));
            elapsed += dt.Ticks;
            cluster.Frame(dt);
        }

        Assert.Equal(elapsed * 60 / TimeSpan.TicksPerSecond, scope.World.Tick);
        Assert.Equal(scope.Hash, view.Hash);
    }
}

public class AllocationTests
{
    [Fact]
    public void SteadyStateTickOfAuthorityReplicaAndDeltasAllocatesNothing()
    {
        // Drift checks and digests run every 30 ticks in this loop too.
        var (cluster, scope, view) = Scenario.NewCluster(Scenario.NewAuthority(8, 1000), new ClusterOptions { DriftCheckInterval = 30, FatalDrift = true });
        cluster.Step(400); // warm up: buffers grow, the shop's timed price settles
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 300; i++)
        {
            cluster.Step();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.Equal(scope.Hash, view.Hash);
        Assert.True(view.FramesApplied > 600);
    }
}
