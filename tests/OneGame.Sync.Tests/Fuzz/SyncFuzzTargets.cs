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
        new FuzzTargets.Target("transfer-server", RunTransferServer, () => TransferTraffic().ToDestination),
        new FuzzTargets.Target("transfer-client", RunTransferSource, () => TransferTraffic().ToSource),
        new FuzzTargets.Target("transfer-follow", RunTransferFollower, () => TransferTraffic().ToFollower),
        new FuzzTargets.Target("transfer-log", RunTransferLog, () => [ValidTransferLog()]),
        new FuzzTargets.Target("recording-transfer", RunTransferRecording, () => TransferRecordings()),
        new FuzzTargets.Target("relay-upstream", RunToRelay, () => RelayTraffic()),
        new FuzzTargets.Target("escrow-book", RunEscrowBook, () => [ValidEscrowBook()]),
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

    // ---- authority transfer, relaying and escrow ------------------------------------------------------------------------------------

    private static readonly ScopeId HandoffA = new(1);
    private static readonly ScopeId HandoffB = new(2);

    /// <summary>The bytes of a real hand-off: what the destination receives, what the source receives, and what a client following the source receives.</summary>
    private static (List<byte[]> ToDestination, List<byte[]> ToSource, List<byte[]> ToFollower) TransferTraffic()
    {
        var cluster = new Cluster(Options);
        var a = cluster.AddNode("A");
        var b = cluster.AddNode("B");
        var c = cluster.AddNode("client");
        var scopeA = cluster.AddScope(a, HandoffA, Realm.NewWorld(WorldRole.Authority));
        cluster.AddScope(b, HandoffB, Realm.NewWorld(WorldRole.Authority, 12));
        var traveler = scopeA.World.Add(new Traveler { Player = 5, Name = "Wanda", Diary = "private", Secret = 3 });
        var (x1, y1) = Loopback.CreatePair();
        var atA = new TapChannel(x1);
        var atB = new TapChannel(y1);
        var linkAB = a.ConnectTo(b, Trust.Owner(HandoffB), atA, atB, "A");
        var (x2, y2) = Loopback.CreatePair();
        var atClient = new TapChannel(x2);
        var linkCA = c.ConnectTo(a, Trust.Owner(HandoffA).As(5), atClient, y2, "client");
        c.Subscribe(linkCA, HandoffA, Realm.NewWorld(WorldRole.Replica));
        cluster.Settle();
        cluster.Step(3);
        scopeA.BeginTransfer(linkAB, HandoffB, [traveler.Id]);
        cluster.Step(3);
        var hit = new SyncWriter();
        linkCA.Connection.SubmitCommand(HandoffA, traveler.Id, StateHash.Fnv32("Hit"), hit.Written); // a command for the old id is forwarded
        cluster.Step(3);

        // Frames only apply to a replica at the right tick, so the follower's seeds are the snapshot and the messages that do not depend on it.
        var follower = new List<byte[]>();
        foreach (var m in atClient.Received)
        {
            if ((m[0] == 16 && follower.Count == 0) || m[0] is 52 or 21 or 19)
            {
                follower.Add(m);
            }
        }

        return (atB.Received, atA.Received, follower);
    }

    /// <summary>A node with a scope and a transfer log that peers hand entities to.</summary>
    private static (Cluster, Node, AuthorityScope, Link) DestinationSetup()
    {
        var cluster = new Cluster(Options);
        var a = cluster.AddNode("A");
        var b = cluster.AddNode("B");
        var scopeB = cluster.AddScope(b, HandoffB, Realm.NewWorld(WorldRole.Authority, 12));
        scopeB.World.Add(new Beacon());
        var link = cluster.Connect(a, b, Trust.Owner(HandoffB));
        cluster.Settle();
        return (cluster, b, scopeB, link);
    }

    private static bool RunTransferServer(byte[] input)
    {
        var (cluster, b, scopeB, link) = DestinationSetup();
        link.Channel.Send(input);
        cluster.Settle();
        cluster.Step(2);
        _ = scopeB.World.ComputeHash();
        _ = scopeB.World.DumpJson();
        return b.ProtocolErrors.Count == 0 && b.Refusals.Count == 0;
    }

    /// <summary>A source scope with a transfer waiting for its destination, which never answers on its own: the input plays the destination.</summary>
    private static (Cluster, Node, AuthorityScope, Link) SourceSetup()
    {
        var cluster = new Cluster(Options);
        var a = cluster.AddNode("A");
        var silent = new Node("B", Options); // never pumped
        var scopeA = cluster.AddScope(a, HandoffA, Realm.NewWorld(WorldRole.Authority));
        var traveler = scopeA.World.Add(new Traveler { Player = 5, Name = "Wanda", Diary = "private", Secret = 3 });
        var link = a.ConnectTo(silent, Trust.Owner(HandoffB));
        cluster.Step(1);
        scopeA.BeginTransfer(link, HandoffB, [traveler.Id]);
        cluster.Step(2);
        return (cluster, a, scopeA, link);
    }

    private static bool RunTransferSource(byte[] input)
    {
        var (cluster, a, scopeA, link) = SourceSetup();
        link.Connection.Channel.Send(input);
        cluster.Step(3);
        _ = scopeA.World.ComputeHash();
        _ = TransferLog.Of(scopeA.World)!.OpenCount;
        return a.ProtocolErrors.Count == 0;
    }

    private static bool RunTransferFollower(byte[] input)
    {
        var cluster = new Cluster(Options);
        var a = cluster.AddNode("A");
        var c = cluster.AddNode("client");
        var silent = new Node("B", Options);
        cluster.AddScope(a, HandoffA, Realm.NewWorld(WorldRole.Authority)).World.Add(new Traveler { Player = 5, Name = "Wanda" });
        var toSilent = c.ConnectTo(silent, Trust.Owner(HandoffB).As(5));
        c.FollowResolver = scope => new FollowTarget(toSilent, Realm.NewWorld(WorldRole.Replica, 12));
        var link = c.ConnectTo(a, Trust.Owner(HandoffA).As(5));
        c.Subscribe(link, HandoffA, Realm.NewWorld(WorldRole.Replica));
        cluster.Settle();
        link.Connection.Channel.Send(input);
        cluster.Settle();
        cluster.Step(2);
        return c.ProtocolErrors.Count == 0;
    }

    private static byte[] ValidTransferLog()
    {
        var (cluster, _, scopeA, link) = SourceSetup();
        _ = cluster;
        _ = link;
        var w = new SyncWriter();
        TransferLog.Of(scopeA.World)!.Write(w);
        return w.ToArray();
    }

    private static bool RunTransferLog(byte[] input)
    {
        var log = new TransferLog();
        var r = new SyncReader(input);
        log.Read(ref r);
        r.ExpectEnd();
        var w = new SyncWriter();
        log.Write(w);
        _ = log.IsDone(1);
        return r.Ok;
    }

    /// <summary>Recordings of both sides of a hand-off: the operations that moved the entities are in the command log.</summary>
    private static List<byte[]> TransferRecordings()
    {
        var rig = new HandoffRig(options: Options);
        rig.ScopeOnA.TransferResendTicks = 5;
        var a = rig.ScopeOnA.StartRecording(new RecordingOptions { CheckpointInterval = 5 });
        var b = rig.ScopeOnB.StartRecording(new RecordingOptions { CheckpointInterval = 5 });
        rig.Run(5);
        rig.ScopeOnA.BeginTransfer(rig.LinkAB, HandoffRig.ScopeB, [rig.Original]);
        rig.Run(20);
        return [a.Stop().ToBytes(), b.Stop().ToBytes()];
    }

    private static bool RunTransferRecording(byte[] input)
    {
        if (Recording.TryRead(input, out var recording) != SyncError.None || recording is null)
        {
            return false;
        }

        if (recording.EndTick - recording.StartTick <= 300)
        {
            _ = Replayer.Replay(recording, Realm.NewWorld(WorldRole.Authority));
        }

        return true;
    }

    /// <summary>What an authority sends a relay: a snapshot, frames, and answers to commands the relay passed up.</summary>
    private static List<byte[]> RelayTraffic()
    {
        TapChannel? tap = null;
        var rig = new RelayRig(Options, inner => tap = new TapChannel(inner));
        var ada = rig.JoinRelay("ada", Trust.Owner(rig.Scope.Id).As(1, 10));
        rig.Cluster.Settle();
        var hero = Rig.HeroOf(ada.Replica, 1);
        hero.Send.Walk(1, 2);
        hero.Send.Rename("Ada II");
        hero.Send.Spend(1);
        rig.Cluster.Step(4);

        // Single messages run against a fresh relay: keep the ones that apply to it (the snapshot, the first frame, answers).
        var seeds = new List<byte[]>();
        var frames = 0;
        foreach (var m in tap!.Received)
        {
            if (m[0] == 16 || (m[0] == 17 && frames++ == 0) || m[0] is 21 or 19)
            {
                seeds.Add(m);
            }
        }

        return seeds;
    }

    private static bool RunToRelay(byte[] input)
    {
        var rig = new RelayRig(Options);
        var ada = rig.JoinRelay("ada", Trust.Owner(rig.Scope.Id).As(1, 10));
        rig.Cluster.Settle();
        rig.Upstream.Link.Connection.Channel.Send(input);
        rig.Cluster.Settle();
        _ = ada.Replica.ViewHash;
        _ = rig.RelayScope.ViewHashFor(ada.Connection);
        return rig.Relay.ProtocolErrors.Count == 0 && ada.Node.ProtocolErrors.Count == 0;
    }

    private static EscrowBook NewBook()
    {
        var world = new World(1, new EntityRegistry(), WorldRole.Authority);
        return Escrow.Install(world, new NullHandler());
    }

    private static byte[] ValidEscrowBook()
    {
        var book = NewBook();
        book.Open(1, 5, new EscrowLeg { Scope = new ScopeId(1), Giver = 1, Receiver = 2, Kind = 1, Amount = 5 }, new EscrowLeg { Scope = new ScopeId(2), Giver = 2, Receiver = 1, Kind = 2, Amount = 1 });
        book.ReportHold(1, 0, true, 6);
        book.Credit(1, 1, 2, 1, 5);
        book.Abort(2, 0);
        var w = new SyncWriter();
        book.Write(w);
        return w.ToArray();
    }

    private static bool RunEscrowBook(byte[] input)
    {
        var book = NewBook();
        var r = new SyncReader(input);
        book.Read(ref r);
        r.ExpectEnd();
        book.Advance(1000);
        _ = EscrowBook.InFlight([book]);
        var w = new SyncWriter();
        book.Write(w);
        return r.Ok;
    }

    private sealed class NullHandler : IEscrowHandler
    {
        public bool TryDebit(World world, uint owner, uint kind, long amount) => true;

        public bool Credit(World world, uint owner, uint kind, long amount) => true;
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
