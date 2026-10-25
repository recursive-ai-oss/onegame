using OneGame.Core.Tests.Fuzzing;
using OneGame.Core.Tests.Samples;

namespace OneGame.Sync.Tests.Fuzzing;

/// <summary>
/// The sync stack's decoders for untrusted bytes, as functions of a byte array: save files, recordings, digests and transport
/// messages in both directions. Same contract as the entity-model targets: any input returns normally.
/// </summary>
public static class SyncFuzzTargets
{
    private static readonly ClusterOptions Options = new() { DriftCheckInterval = 1 };

    public static IReadOnlyList<FuzzTargets.Target> All { get; } =
    [
        new FuzzTargets.Target("save", RunSave, () => [ValidSave(), Scenario1SaveFixture.Build()]),
        new FuzzTargets.Target("recording", RunRecording, () => [ValidRecording()]),
        new FuzzTargets.Target("digest", RunDigest, () => [ValidDigest()]),
        new FuzzTargets.Target("transport-client", RunToReplica, () => Traffic().ToClient),
        new FuzzTargets.Target("transport-server", RunToAuthority, () => Traffic().ToServer),
        new FuzzTargets.Target("view-snapshot", RunViewSnapshot, ViewSnapshots),
        new FuzzTargets.Target("view-client", RunToFilteredReplica, () => ViewTraffic().ToClient),
        new FuzzTargets.Target("view-server", RunToFilteredAuthority, () => ViewTraffic().ToServer),
    ];

    private static World Small()
    {
        var world = Scenario.NewWorld(WorldRole.Authority);
        Scenario.Populate(world, 3, 6);
        return world;
    }

    private static byte[] ValidSave()
    {
        var world = Small();
        for (var i = 0; i < 5; i++)
        {
            world.Step();
        }

        return SaveFile.Write(world);
    }

    private static bool RunSave(byte[] input)
    {
        var world = Scenario.NewWorld(WorldRole.Authority);
        var result = SaveFile.Load(input, world);
        _ = SaveFile.ToJson(input, Scenario.NewWorld(WorldRole.Authority));
        return result.Status == SaveStatus.Ok;
    }

    private static byte[] ValidRecording()
    {
        var cluster = new Cluster(Options);
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), Small());
        var view = cluster.AddLocalReplica(scope, Scenario.NewWorld(WorldRole.Replica));
        cluster.Settle();
        var recorder = scope.StartRecording(new RecordingOptions { CheckpointInterval = 7 });
        Scenario.Play(cluster, scope, view, 40);
        return recorder.Stop().ToBytes();
    }

    private static bool RunRecording(byte[] input)
    {
        if (Recording.TryRead(input, out var recording) != SyncError.None || recording is null)
        {
            return false;
        }

        // A recording that decodes is replayed too, unless it asks for a very long run.
        if (recording.EndTick - recording.StartTick <= 300)
        {
            _ = Replayer.Replay(recording, Scenario.NewWorld(WorldRole.Authority));
        }

        return true;
    }

    private static byte[] ValidDigest()
    {
        var digest = new StateDigest();
        StateDigest.Capture(Small(), digest, new SyncWriter());
        var w = new SyncWriter();
        digest.Write(w);
        return w.ToArray();
    }

    private static bool RunDigest(byte[] input)
    {
        var r = new SyncReader(input);
        new StateDigest().Read(ref r);
        r.ExpectEnd();
        return r.Ok;
    }

    private static (List<byte[]> ToClient, List<byte[]> ToServer) Traffic()
    {
        // Play the real protocol against a bare link, so the bytes are exactly what a replica and an authority exchange.
        var cluster = new Cluster(Options);
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), Small());
        var client = cluster.AddNode("client");
        var link = cluster.Connect(client, host, Trust.Full);
        var toServer = new List<byte[]>();
        var toClient = new List<byte[]>();

        var subscribe = new byte[] { 1, 1 };
        toServer.Add(subscribe);
        link.Channel.Send(subscribe);
        host.Pump();
        Drain(link, toClient);
        scope.World.Add(new Bot { Hp = 3 }); // the first frame announces it
        scope.StepAndPublish();
        Drain(link, toClient);

        var w = new SyncWriter();
        w.WriteByte(3); // command
        w.WriteVarUInt(1); // scope
        w.WriteVarUInt(1); // sequence
        w.WriteVarUInt(1); // entity
        w.WriteVarUInt(CommandIds.Buy);
        w.WriteInt32(1);
        w.WriteInt32(2);
        toServer.Add(w.ToArray());
        toServer.Add([4, 1, 2]); // digest request for tick 1
        toServer.Add([2, 1]); // unsubscribe
        return (toClient, toServer);
    }

    private static void Drain(Link link, List<byte[]> into)
    {
        while (link.Channel.TryReceive(out var message))
        {
            into.Add(message.ToArray());
        }
    }

    private static bool RunToReplica(byte[] input)
    {
        var (cluster, _, view) = ReplicaSetup();
        view.Link.Connection.Channel.Send(input);
        cluster.Settle();
        return view.Node.ProtocolErrors.Count == 0;
    }

    private static bool RunToAuthority(byte[] input)
    {
        var (cluster, scope, view) = ReplicaSetup();
        view.Link.Channel.Send(input);
        cluster.Settle();
        cluster.Step(2);
        _ = scope.Hash;
        return scope.Node.ProtocolErrors.Count == 0 && scope.Node.Refusals.Count == 0;
    }

    // ---- filtered replicas: view snapshots, frames with masked entities, command answers, trust and subscription messages ----------------

    private static List<byte[]> ViewSnapshots()
    {
        var world = Arena.Match();
        for (var i = 0; i < 6; i++)
        {
            world.Step();
        }

        var list = new List<byte[]>();
        foreach (var (who, random) in new[] { (new Audience(1, 10), true), (new Audience(2, 20), false), (Audience.Anonymous, true), (new Audience(9, 10), false) })
        {
            var w = new SyncWriter();
            world.WriteViewSnapshot(w, who, random);
            list.Add(w.ToArray());
        }

        var plain = new SyncWriter();
        world.WriteSnapshot(plain);
        list.Add(plain.ToArray());
        return list;
    }

    private static bool RunViewSnapshot(byte[] input)
    {
        var world = Arena.Empty(WorldRole.Replica);
        var error = world.ReadSnapshot(input);
        if (error == SyncError.None)
        {
            _ = world.ComputeViewHash();
            _ = world.DumpJson();
        }

        return error == SyncError.None;
    }

    private static (List<byte[]> ToClient, List<byte[]> ToServer) ViewTraffic()
    {
        // Real traffic of a filtered subscriber: the snapshot and the first frame (they apply to a fresh replica), plus every kind of answer.
        var rig = new Rig(Options);
        var ada = rig.Join("ada", Trust.Player(rig.Scope.Id).As(1, 10));
        rig.Cluster.Settle();
        var hero = Rig.HeroOf(ada.Replica, 1);
        hero.Send.Spend(5);
        hero.Send.Spend(100_000);
        hero.Send.Rename("x"); // refused: this trust does not own the scope
        Rig.HeroOf(rig.Scope, 1).Gold += 1; // an owner-only change in the first frame
        rig.Cluster.Step(1);
        var toClient = ada.Tap.Received.Select(m => m.ToArray()).ToList();

        var w = new SyncWriter();
        w.WriteByte(21); // command result
        w.WriteVarUInt(1);
        w.WriteVarUInt(1);
        w.WriteVarUInt(2);
        w.WriteVarUInt(CommandIdsArena.Spend);
        w.WriteByte((byte)CommandVerdict.Rejected);
        w.WriteByte(0);
        w.WriteString("not enough gold");
        toClient.Add(w.ToArray());
        toClient.Add([22, 1, (byte)RefusalReason.ReadRevoked]);

        var toServer = new List<byte[]> { new byte[] { 1, 1 }, new byte[] { 2, 1 }, new byte[] { 4, 1, 2 } };
        var cmd = new SyncWriter();
        cmd.WriteByte(3);
        cmd.WriteVarUInt(1);
        cmd.WriteVarUInt(7); // sequence
        cmd.WriteVarUInt(Rig.HeroOf(rig.Scope, 1).Id.Value);
        cmd.WriteVarUInt(CommandIdsArena.Walk);
        cmd.WriteInt32(1);
        cmd.WriteInt32(-1);
        toServer.Add(cmd.ToArray());
        return (toClient, toServer);
    }

    private static (Rig Rig, Player Ada) ViewSetup()
    {
        var rig = new Rig(Options);
        var ada = rig.Join("ada", Trust.Player(rig.Scope.Id).As(1, 10));
        rig.Cluster.Settle();
        return (rig, ada);
    }

    private static bool RunToFilteredReplica(byte[] input)
    {
        var (rig, ada) = ViewSetup();
        ada.Connection.Channel.Send(input);
        rig.Cluster.Settle();
        _ = ada.Replica.ViewHash;
        return ada.Node.ProtocolErrors.Count == 0;
    }

    private static bool RunToFilteredAuthority(byte[] input)
    {
        var (rig, ada) = ViewSetup();
        ada.Link.Channel.Send(input);
        rig.Cluster.Settle();
        rig.Cluster.Step(2);
        _ = rig.Scope.ViewHashFor(ada.Connection);
        return rig.Host.ProtocolErrors.Count == 0 && rig.Host.Refusals.Count == 0;
    }

    private static (Cluster, AuthorityScope, ReplicaScope) ReplicaSetup()
    {
        var cluster = new Cluster(Options);
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), Small());
        var view = cluster.AddLocalReplica(scope, Scenario.NewWorld(WorldRole.Replica));
        cluster.Settle();
        return (cluster, scope, view);
    }
}

/// <summary>A save written by the previous schema of the shop, kept as a fixture and a fuzz seed.</summary>
public static class Scenario1SaveFixture
{
    /// <summary>The layout schema 1 wrote: name, whole-number gold, items.</summary>
    public static byte[] Build()
    {
        var state = new SyncWriter();
        state.WriteString("Old Shop");
        state.WriteInt32(7);
        state.WriteVarUInt(2);
        state.WriteInt32(1);
        state.WriteInt32(30);
        state.WriteByte((byte)Mood.Calm);
        state.WriteInt32(2);
        state.WriteInt32(4);
        state.WriteByte((byte)Mood.Eager);

        var snapshot = new SyncWriter();
        snapshot.WriteFixed32(0x4E53474F); // snapshot magic
        snapshot.WriteVarUInt(1);
        snapshot.WriteVarInt(12);
        snapshot.WriteVarUInt(2); // next entity id
        snapshot.WriteVarUInt(0); // no random streams
        snapshot.WriteVarUInt(1); // one entity
        snapshot.WriteFixed32(new Shop().TypeId);
        snapshot.WriteVarUInt(1);
        snapshot.WriteVarUInt(1); // schema 1
        snapshot.WriteBytes(state.Written);
        snapshot.WriteVarUInt(1); // one table, empty
        snapshot.WriteString("bots");
        var table = new SyncWriter();
        new BotRowTable().Write(table);
        snapshot.WriteBytes(table.Written);

        var save = new SyncWriter();
        save.WriteFixed32(0x56534F47); // save magic
        save.WriteVarUInt(1);
        save.WriteFixed64(Scenario.Seed);
        save.WriteVarInt(12);
        save.WriteFixed64(0); // the hash of a schema-1 world is not comparable
        save.WriteFixed64(StateHash.Of(snapshot.Written));
        save.WriteVarUInt(1);
        save.WriteFixed32(new Shop().TypeId);
        save.WriteString("Shop");
        save.WriteVarUInt(1);
        save.WriteBytes(snapshot.Written);
        return save.ToArray();
    }
}

/// <summary>Command ids of the visibility sample game.</summary>
public static class CommandIdsArena
{
    public static uint Spend => StateHash.Fnv32("Spend");

    public static uint Walk => StateHash.Fnv32("Walk");
}
