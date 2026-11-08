using OneGame.Core;
using OneGame.Host;
using OneGame.Sync;
using Physics3DExample;

namespace OneGame.Physics.Tests;

public class Game3DTests
{
    private static readonly TestOptions Options = new() { Gpu = GpuMode.Off };
    private static void Drive(TestClient client, int frame)
    {
        if (frame == 40) client.Send("Playground", "Drive", "1", "1");
        if (frame == 140) client.Send("Playground", "Drive", "0", "0");
    }
    [Fact]
    public void HarnessRunTwiceRestoreAndCommandReplayMatch()
    {
        var twice = Determinism.RunTwice(() => new Physics3DGame(), Drive, 240, Options);
        Assert.True(twice.Identical, twice.ToString());
        var restore = Determinism.SnapshotRestore(() => new Physics3DGame(), Drive, 240, 80, Options);
        Assert.True(restore.Identical, restore.ToString());
        var replay = Determinism.Replay(() => new Physics3DGame(), Drive, 240, Options);
        Assert.True(replay.Identical, replay.ToString());
    }

    [Fact]
    public void DetailedRecordingRoundTripsAndNamesTheDriftingBody()
    {
        var world = new World(5, new EntityRegistry());
        var space = PhysicsSpace3D.AddTo(world);
        var body = space.AddSphere(Fixed.One, new FVec3(0, 3, 0));
        var cluster = new Cluster(); var node = cluster.AddNode("authority");
        var scope = cluster.AddScope(node, new ScopeId(1), world);
        var recorder = scope.StartRecording(new RecordingOptions { CheckpointInterval = 1, TableDiagnostics = true });
        cluster.Step(8);
        var recording = recorder.Stop();
        Assert.Equal(SyncError.None, Recording.TryRead(recording.ToBytes(), out var decoded));
        var replayWorld = new World(5, new EntityRegistry());
        var replaySpace = PhysicsSpace3D.AddTo(replayWorld);
        var ok = Replayer.Replay(decoded!, replayWorld);
        Assert.True(ok.Ok, ok.ToJson());
        var fail = Replayer.Replay(decoded!, replayWorld, new ReplayOptions
        {
            OnTick = _ => replaySpace.SetVelocity(body, new FVec3(8, 0, 0)),
        });
        Assert.False(fail.Ok);
        var difference = Assert.Single(fail.Divergence!.Differences, d => d.Kind == DigestKind.Table);
        Assert.Contains($"body {body.Index}", difference.Detail);
        Assert.Contains("detail", fail.ToJson());

        var digest = new StateDigest(); var other = new StateDigest();
        StateDigest.Capture(world, digest, new SyncWriter());
        replaySpace.CopyFrom(space); replaySpace.SetVelocity(body, new FVec3(9, 0, 0));
        StateDigest.Capture(replayWorld, other, new SyncWriter());
        var differences = new List<StateDifference>();
        StateDigest.Diff(digest, other, world, replayWorld, differences);
        Assert.Contains($"body {body.Index}", Assert.Single(differences, d => d.Kind == DigestKind.Table).Detail);
    }

    [Fact]
    public void HiddenPhysicsAndMirroredStateReplicateWithZeroDrift()
    {
        var game = new Physics3DGame();
        var registry = new EntityRegistry(); game.RegisterEntities(registry);
        var world = new World(1, registry); game.RegisterSystems(world); game.Setup(world);
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var scope = cluster.AddScope(cluster.AddNode("host"), new ScopeId(1), world);
        var replica = cluster.AddLocalReplica(scope, new World(1, registry, WorldRole.Replica), Trust.Player(scope.Id));
        cluster.Settle();
        var entity = Assert.Single(world.Entities.OfType<Playground>());
        entity.Send.Drive(Fixed.One, Fixed.One);
        var prediction = PhysicsSpace3D.Of(world)!.Fork();
        for (var i = 0; i < 120; i++)
        {
            prediction.Move(entity.Character, new FVec3(prediction.StepSeconds, Fixed.FromRatio(-1, 10), 0));
            prediction.Drive(entity.Car, Fixed.One); prediction.Step(resimulating: true);
            cluster.Step();
            Assert.Equal(PhysicsSpace3D.Of(world)!.Hash, prediction.Hash);
        }
        Assert.Empty(cluster.DriftReports); Assert.Empty(cluster.CheckReplicas());
        Assert.Equal(entity.CarPosition, Assert.Single(replica.World.Entities.OfType<Playground>()).CarPosition);
        Assert.Null(PhysicsSpace3D.Of(replica.World));
    }

    [Fact]
    public void DetailedDigestRejectsOversizedPayloadAndInvalidPhysicsDetailIsSafe()
    {
        var writer = new SyncWriter();
        writer.WriteVarInt(0); writer.WriteFixed64(0); writer.WriteBool(false); writer.WriteVarUInt(1);
        writer.WriteByte((byte)DigestKind.Table); writer.WriteString("physics3d"); writer.WriteFixed64(1);
        writer.WriteVarUInt(64 * 1024 * 1024 + 1);
        var reader = new SyncReader(writer.Written); var digest = new StateDigest();
        digest.Read(ref reader, includeTableDiagnostics: true);
        Assert.False(reader.Ok); Assert.Empty(digest.Entries);
        var space = new PhysicsSpace3D();
        Assert.Contains("invalid", space.DescribeDifferences(new byte[] { 1, 2, 3 }));
    }
}
