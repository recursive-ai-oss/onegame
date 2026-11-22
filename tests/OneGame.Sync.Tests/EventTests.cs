namespace OneGame.Sync.Tests;

[SimEvent] public partial struct Pop { public int Cell { get; set; } public string Tag { get; set; } }

[SimEvent] public partial struct Beat { public int N { get; set; } }

[SimEvent(Visibility = Visibility.Owner)] public partial struct Loot { public int Gold { get; set; } public string Note { get; set; } }

[SimEvent(Visibility = Visibility.Team)] public partial struct Rally { public int N { get; set; } }

[SimEvent(Visibility = Visibility.Hidden)] public partial struct Plot { public string Text { get; set; } }

/// <summary>Emits something every few ticks, with a mix of event visibilities.</summary>
[Entity]
public partial class Popper : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced, TeamKey] public partial uint Team { get; set; }

    [Synced, InterestKey] public partial uint Cell { get; set; }

    [Synced] public partial int N { get; set; }

    protected override void OnTick()
    {
        N++;
        if (N % 3 == 0)
        {
            Emit(new Pop { Cell = (int)Cell, Tag = "pop-tag" });
        }

        if (N % 5 == 0)
        {
            Emit(new Loot { Gold = N, Note = "loot-secret-" + Player.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        }

        if (N % 7 == 0)
        {
            Emit(new Rally { N = N });
        }

        if (N % 11 == 0)
        {
            Emit(new Plot { Text = "plot-secret-xyz" });
        }
    }
}

/// <summary>Exists only for its owner, so everything it emits is the owner's alone, whatever the event type says.</summary>
[Entity(Visibility = Visibility.Owner)]
public partial class Whisperer : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    protected override void OnTick()
    {
        if (World!.Tick % 4 == 0)
        {
            Emit(new Pop { Cell = 99, Tag = "whisper-tag" });
        }
    }
}

/// <summary>A system that emits world events, with no entity behind them.</summary>
public sealed class Announcer : ISimSystem
{
    public void Tick(World world)
    {
        if (world.Tick % 10 == 0)
        {
            world.Emit(new Pop { Cell = -1, Tag = "world-tag" });
        }
    }
}

/// <summary>One-shot sim events: emit, deliver once, restore, replay, privacy, limits.</summary>
public class EventTests
{
    private static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        registry.Add(new Popper().TypeId, "Popper", static () => new Popper());
        registry.Add(new Whisperer().TypeId, "Whisperer", static () => new Whisperer());
        registry.Add(new BeatMaker().TypeId, "BeatMaker", static () => new BeatMaker());
        registry.Add(new Burst().TypeId, "Burst", static () => new Burst());
        registry.AddEvent<Pop>();
        registry.AddEvent<Beat>();
        registry.AddEvent<Loot>();
        registry.AddEvent<Rally>();
        registry.AddEvent<Plot>();
        return registry;
    }

    private static World NewWorld(WorldRole role = WorldRole.Authority, bool populated = true)
    {
        var world = new World(11, Registry(), role);
        if (role == WorldRole.Replica)
        {
            return world;
        }

        world.AddSystem(new Announcer());
        if (populated)
        {
            world.Add(new Popper { Player = 1, Team = 10, Cell = 1 });
            world.Add(new Popper { Player = 2, Team = 20, Cell = 2 });
            world.Add(new Popper { Player = 3, Team = 10, Cell = 3 });
            world.Add(new Whisperer { Player = 1 });
        }

        return world;
    }

    private sealed class Hub
    {
        public Hub(World? world = null, int drift = 1)
        {
            Cluster = new Cluster(new ClusterOptions { DriftCheckInterval = drift, FatalDrift = true });
            Host = Cluster.AddNode("host");
            Scope = Cluster.AddScope(Host, new ScopeId(1), world ?? NewWorld());
        }

        public Cluster Cluster { get; }

        public Node Host { get; }

        public AuthorityScope Scope { get; }

        public Player Join(string name, Trust trust)
        {
            var node = Cluster.AddNode(name);
            var (a, b) = Loopback.CreatePair();
            var tap = new TapChannel(a);
            var link = node.ConnectTo(Host, trust, tap, b, name);
            var replica = node.Subscribe(link, Scope.Id, NewWorld(WorldRole.Replica));
            return new Player(node, link, tap, replica);
        }
    }

    private static Trust Ada(Hub hub) => Trust.Player(hub.Scope.Id).As(1, 10);

    private static Trust Bo(Hub hub) => Trust.Player(hub.Scope.Id).As(2, 20);

    private static Trust Cara(Hub hub) => Trust.Player(hub.Scope.Id).As(3, 10);

    /// <summary>Every retained event as one comparable line: tick, type, entity and payload bytes.</summary>
    private static List<string> Lines(EventLog log, Func<EventRecord, bool>? keep = null)
    {
        var lines = new List<string>();
        for (var n = log.FirstRetained; n < log.Count; n++)
        {
            log.TryGet(n, out var e);
            if (keep is null || keep(e))
            {
                lines.Add($"{e.Tick}:{e.TypeId:x8}:{e.Entity.Value}:{Convert.ToHexString(e.Payload)}");
            }
        }

        return lines;
    }

    [Fact]
    public void TwoRunsEmitTheSameStreamInTheSameOrderWithTheTicksTheyHappenedOn()
    {
        var a = NewWorld();
        var b = NewWorld();
        for (var i = 0; i < 150; i++)
        {
            a.Step();
            b.Step();
        }

        Assert.True(a.Events.Count > 100);
        Assert.True(a.Events.Count <= EventLog.Capacity);
        Assert.Equal(Lines(a.Events), Lines(b.Events));
        Assert.Equal(a.Events.StreamHash, b.Events.StreamHash);
        Assert.NotEqual(0UL, a.Events.StreamHash);

        // Emit order within a tick is the order of the entities, then the systems; ticks never go backwards.
        long last = -1;
        var reader = a.Events.NewReaderFromStart();
        while (reader.Next(out var e))
        {
            Assert.True(e.Tick >= last);
            last = e.Tick;
        }

        // The tick is the one the sim was on when it emitted. Tick 0 comes first: the whisperer, then the system; a popper first speaks on tick 2 (its counter reaches 3).
        a.Events.TryGet(0, out var e0);
        Assert.Equal(0, e0.Tick);
        Assert.Equal(a.Entities.OfType<Whisperer>().Single().Id, e0.Entity);
        a.Events.TryGet(1, out var e1);
        Assert.Equal(EntityId.None, e1.Entity);
        a.Events.TryGet(2, out var e2);
        Assert.Equal(2, e2.Tick);
        Assert.True(e2.Is<Pop>());
    }

    [Fact]
    public void AReaderGetsEachEventOnceAndOnlyTheOnesAfterItWasMade()
    {
        var world = NewWorld();
        for (var i = 0; i < 30; i++)
        {
            world.Step();
        }

        var old = world.Events.Count;
        Assert.True(old > 0);
        var reader = world.Events.NewReader();
        var all = world.Events.NewReaderFromStart();
        Assert.Equal(0, reader.Pending);
        Assert.False(reader.Next(out _));
        for (var i = 0; i < 30; i++)
        {
            world.Step();
        }

        var seen = new List<long>();
        while (reader.Next(out var e))
        {
            seen.Add(e.Sequence);
        }

        Assert.Equal(world.Events.Count - old, seen.Count);
        Assert.Equal(old, seen[0]);
        Assert.Equal(seen.Order().ToList(), seen);
        Assert.False(reader.Next(out _)); // nothing twice
        Assert.Equal(world.Events.Count, all.Pending);

        // A typed read passes over other types and decodes the payload.
        var pops = 0;
        var cells = new List<int>();
        var typed = world.Events.NewReaderFromStart();
        while (typed.Next<Pop>(out var pop, out var info))
        {
            pops++;
            cells.Add(pop.Cell);
            Assert.EndsWith("-tag", pop.Tag, StringComparison.Ordinal);
            Assert.True(info.Tick >= 0);
        }

        Assert.True(pops > 20);
        Assert.Contains(-1, cells); // the system's world event
        Assert.Contains(99, cells); // the whisperer's
    }

    [Fact]
    public void ARestoreHalfwayDoesNotRefireOldEventsAndTheRestReplaysIdentically()
    {
        var live = NewWorld();
        var reader = live.Events.NewReader();
        for (var i = 0; i < 50; i++)
        {
            live.Step();
        }

        var snapshot = new SyncWriter();
        live.WriteSnapshot(snapshot);
        var atFifty = live.Events.Count;
        for (var i = 0; i < 50; i++)
        {
            live.Step();
        }

        var restored = NewWorld(populated: false);
        var restoredReader = restored.Events.NewReader();
        Assert.Equal(SyncError.None, restored.ReadSnapshot(snapshot.Written));
        Assert.Equal(0, restored.Events.Count - restored.Events.FirstRetained);
        Assert.False(restoredReader.Next(out _)); // restoring is not an event
        for (var i = 0; i < 50; i++)
        {
            restored.Step();
        }

        Assert.Equal(1, restoredReader.Restarts);
        Assert.Equal(Lines(live.Events, e => e.Tick >= 50), Lines(restored.Events));
        Assert.True(restored.Events.Count > 10);
        Assert.Equal(live.ComputeHash(), restored.ComputeHash());

        // The live reader saw all of it once.
        Assert.Equal(live.Events.Count, reader.Pending);
        Assert.True(atFifty < live.Events.Count);
    }

    [Fact]
    public void ReplayingARecordingEmitsTheLiveStream()
    {
        var hub = new Hub();
        var ada = hub.Join("ada", Ada(hub));
        hub.Cluster.Settle();
        var recorder = hub.Scope.StartRecording(new RecordingOptions { CheckpointInterval = 10 });
        hub.Cluster.Step(120);
        var recording = recorder.Stop();
        var liveLines = Lines(hub.Scope.World.Events, e => e.Tick >= 0);
        Assert.NotEmpty(liveLines);

        var replayed = NewWorld(populated: false);
        var result = Replayer.Replay(recording, replayed);
        Assert.True(result.Ok, result.Divergence?.ToString());
        Assert.Equal(liveLines, Lines(replayed.Events));
        Assert.Equal(hub.Scope.World.Events.StreamHash, replayed.Events.StreamHash);
        Assert.Equal(ada.Replica.LastTick, hub.Scope.World.Tick);
    }

    [Fact]
    public void AReplicaHoldsExactlyTheEventsItsAudienceMaySeeAndNoOtherByteCarriesThem()
    {
        var hub = new Hub();
        var ada = hub.Join("ada", Ada(hub));
        var bo = hub.Join("bo", Bo(hub));
        var cara = hub.Join("cara", Cara(hub));
        var stranger = hub.Join("stranger", Trust.Reader(hub.Scope.Id));
        hub.Cluster.Settle();
        var world = hub.Scope.World;
        var readers = new[] { ada, bo, cara, stranger }.ToDictionary(p => p, p => p.Replica.World.Events.NewReader());
        hub.Cluster.Step(160);
        Assert.Empty(hub.Cluster.DriftReports);

        var popper = world.Entities.OfType<Popper>().ToDictionary(p => p.Id.Value);
        var whisperer = world.Entities.OfType<Whisperer>().Single();

        // The rule, written out independently of the sync code.
        bool Sees(uint player, uint team, EventRecord e)
        {
            var isPop = e.Is<Pop>();
            if (e.Entity.Value == 0)
            {
                return true; // a world event
            }

            if (e.Entity == whisperer.Id)
            {
                return player == 1;
            }

            var p = popper[e.Entity.Value];
            var owner = player != 0 && player == p.Player;
            var onTeam = owner || (team != 0 && team == p.Team);
            if (isPop || e.Is<Beat>())
            {
                return true;
            }

            if (e.Is<Loot>())
            {
                return owner;
            }

            if (e.Is<Rally>())
            {
                return onTeam;
            }

            return false; // Plot: hidden
        }

        foreach (var (who, player, team) in new[] { (ada, 1u, 10u), (bo, 2u, 20u), (cara, 3u, 10u), (stranger, 0u, 0u) })
        {
            var expected = Lines(world.Events, e => Sees(player, team, e));
            var actual = Lines(who.Replica.World.Events);
            Assert.NotEmpty(actual);
            Assert.Equal(expected, actual);
        }

        // The audiences really differ, so the equality above says something.
        Assert.True(Lines(ada.Replica.World.Events).Count > Lines(stranger.Replica.World.Events).Count);
        Assert.NotEqual(Lines(bo.Replica.World.Events).Count, Lines(cara.Replica.World.Events).Count);

        // Bytes: each secret appears only on the connection of the player it belongs to, and the hidden one nowhere.
        Assert.True(ada.Tap.Contains("loot-secret-1"));
        Assert.False(bo.Tap.Contains("loot-secret-1"));
        Assert.False(cara.Tap.Contains("loot-secret-1"));
        Assert.False(stranger.Tap.Contains("loot-secret-1"));
        Assert.True(bo.Tap.Contains("loot-secret-2"));
        Assert.False(ada.Tap.Contains("loot-secret-2"));
        Assert.True(cara.Tap.Contains("loot-secret-3"));
        Assert.False(stranger.Tap.Contains("loot-secret-3"));
        foreach (var p in new[] { ada, bo, cara, stranger })
        {
            Assert.False(p.Tap.Contains("plot-secret-xyz"));
            Assert.True(p.Tap.Contains("pop-tag"));
            Assert.True(p.Tap.Contains("world-tag"));
        }

        Assert.True(ada.Tap.Contains("whisper-tag"));
        Assert.False(bo.Tap.Contains("whisper-tag"));
        Assert.False(stranger.Tap.Contains("whisper-tag"));

        // Every reader got its events once, in order.
        foreach (var (p, reader) in readers)
        {
            Assert.Equal(p.Replica.World.Events.Count, reader.Pending);
        }
    }

    [Fact]
    public void ChangingTheTrustChangesWhichEventsAreSentAndLosesNoneItMaySee()
    {
        var hub = new Hub();
        var bo = hub.Join("bo", Bo(hub));
        hub.Cluster.Settle();
        hub.Cluster.Step(60);
        Assert.False(bo.Tap.Contains("loot-secret-1"));

        // Bo becomes player 1: from the next tick he gets player 1's owner events.
        bo.Connection.Trust = Trust.Player(hub.Scope.Id).As(1, 10);
        hub.Cluster.Step(40);
        Assert.True(bo.Tap.Contains("loot-secret-1"));
        Assert.Empty(hub.Cluster.DriftReports);
        Assert.Empty(Lines(bo.Replica.World.Events, e => e.Is<Loot>() && e.Tick < 60));
    }

    [Fact]
    public void ALateJoinerGetsNoOldEventsAndOnlyWhatHappensAfter()
    {
        var hub = new Hub();
        hub.Cluster.Step(70);
        Assert.True(hub.Scope.World.Events.Count > 30);
        var early = hub.Join("early", Ada(hub)); // subscribes now, after 70 ticks of events
        hub.Cluster.Settle();
        Assert.Equal(0, early.Replica.World.Events.Count);
        var reader = early.Replica.World.Events.NewReader();
        hub.Cluster.Step(30);
        Assert.True(early.Replica.World.Events.Count > 0);
        Assert.All(Lines(early.Replica.World.Events), l => Assert.True(long.Parse(l.Split(':')[0], System.Globalization.CultureInfo.InvariantCulture) >= 70));
        Assert.Equal(early.Replica.World.Events.Count, reader.Pending);

        // A resync (a fresh snapshot) restarts the log: nothing is replayed to readers.
        var before = early.Replica.World.Events.Count;
        early.Node.Resubscribe(early.Replica);
        hub.Cluster.Settle();
        Assert.Equal(0, early.Replica.World.Events.Count - early.Replica.World.Events.FirstRetained);
        Assert.Equal(0, reader.Pending);
        Assert.Equal(1, reader.Restarts);
        Assert.True(before > 0);
    }

    [Fact]
    public void EventsOfEntitiesOutsideAnInterestSetAreNotSent()
    {
        var hub = new Hub();
        var near = hub.Join("near", Ada(hub));
        hub.Cluster.Settle();
        hub.Scope.SetInterest(near.Connection, [2]);
        hub.Cluster.Settle();
        hub.Cluster.Step(120);
        Assert.Empty(hub.Cluster.DriftReports);

        // Only the cell-2 entity's Pop reaches it; global ones (the system's, and the whisperer, which has no key) still do.
        var cells = new HashSet<int>();
        var reader = near.Replica.World.Events.NewReaderFromStart();
        while (reader.Next<Pop>(out var pop, out _))
        {
            cells.Add(pop.Cell);
        }

        Assert.Contains(2, cells);
        Assert.DoesNotContain(1, cells);
        Assert.DoesNotContain(3, cells);
        Assert.Contains(-1, cells);
    }

    [Fact]
    public void SteadyTicksWithEventsAllocateNothing()
    {
        var world = new World(5, Registry());
        world.Add(new BeatMaker { Player = 1 });
        var hub = new Hub(world, drift: 30);
        // No tap here: the tap copies every message, which is the test's allocation, not the engine's.
        var node = hub.Cluster.AddNode("ada");
        var (ca, cb) = Loopback.CreatePair();
        var link = node.ConnectTo(hub.Host, Ada(hub), ca, cb, "ada");
        var replica = node.Subscribe(link, hub.Scope.Id, NewWorld(WorldRole.Replica));
        hub.Cluster.Settle();
        var authorityReader = world.Events.NewReader();
        var replicaReader = replica.World.Events.NewReader();
        long consumed = 0;
        void Drain()
        {
            while (authorityReader.Next<Beat>(out var b, out _))
            {
                consumed += b.N;
            }

            while (replicaReader.Next<Beat>(out var b, out _))
            {
                consumed += b.N;
            }
        }

        // The first lap round the ring sizes each slot's buffer; after it, nothing allocates.
        for (var i = 0; i < EventLog.Capacity + 100; i++)
        {
            hub.Cluster.Step();
            Drain();
        }

        var best = long.MaxValue;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
            {
                hub.Cluster.Step();
                Drain();
            }

            best = Math.Min(best, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        Console.WriteLine($"events: {best} bytes over 100 ticks with {world.Events.Count} events emitted, {consumed} read");
        Assert.True(replica.World.Events.Count > EventLog.Capacity + 100);
        Assert.Equal(0, best);
    }

    [Fact]
    public void AnOverfullTickDropsTheRestDeterministicallyAndAReplicaKeepsWhatItWasSent()
    {
        var world = new World(5, Registry());
        world.Add(new Burst { Count = 200 });
        var hub = new Hub(world);
        var ada = hub.Join("ada", Trust.Reader(hub.Scope.Id));
        hub.Cluster.Settle();
        hub.Cluster.Step(3);
        Assert.Equal(128 * 3, world.Events.Count);
        Assert.Equal(72 * 3, world.Events.Dropped);
        Assert.Equal(world.Events.Count, ada.Replica.World.Events.Count);

        // The limit is the game's to raise, up to the size of the log; the receiving side does not limit.
        world.Events.EventsPerTickLimit = 300;
        Assert.Equal(300, world.Events.EventsPerTickLimit);
        hub.Cluster.Step(1);
        Assert.Equal(128 * 3 + 200, world.Events.Count);
        Assert.Equal(world.Events.Count, ada.Replica.World.Events.Count);

        // The dropped ones are the last of the tick, so the kept ones are the same on every run.
        var one = new World(5, Registry());
        one.Add(new Burst { Count = 200 });
        one.Step();
        var two = new World(5, Registry());
        two.Add(new Burst { Count = 200 });
        two.Step();
        Assert.Equal(Lines(one.Events), Lines(two.Events));
        var last = -1;
        var r = one.Events.NewReaderFromStart();
        while (r.Next<Beat>(out var b, out _))
        {
            last = b.N;
        }

        Assert.Equal(127, last);
    }

    [Fact]
    public void ASlowReaderLosesTheOldestAndIsToldHowMany()
    {
        var world = new World(5, Registry());
        world.Add(new Burst { Count = 100 });
        var reader = world.Events.NewReader();
        for (var i = 0; i < 12; i++)
        {
            world.Step(); // 1,200 events against a ring of 512
        }

        Assert.Equal(1200, world.Events.Count);
        Assert.Equal(EventLog.Capacity, reader.Pending); // what the ring still holds; the rest is counted as missed
        var n = 0;
        while (reader.Next(out _))
        {
            n++;
        }

        Assert.Equal(EventLog.Capacity, n);
        Assert.Equal(1200 - EventLog.Capacity, reader.Missed);
    }

    [Fact]
    public void RulesAboutWhoMayEmitAndWhatMayBeEmitted()
    {
        var replica = NewWorld(WorldRole.Replica);
        Assert.Throws<ReplicaWriteException>(() => replica.Emit(new Beat { N = 1 }));

        // A predicted world re-runs ticks, so it records nothing: views read the confirmed world.
        var predicted = new World(11, Registry(), WorldRole.Predicted);
        Assert.False(predicted.Emit(new Beat { N = 1 }));
        Assert.Equal(0, predicted.Events.Count);

        var world = NewWorld();
        Assert.Throws<InvalidOperationException>(() => world.Emit(new Loot { Gold = 1, Note = "x" })); // owner-only needs an entity
        Assert.Throws<InvalidOperationException>(() => world.Emit(new Rally { N = 1 }));
        Assert.True(world.Emit(new Plot { Text = "fine" })); // hidden needs none
        Assert.Throws<NotInWorldException>(() => new Popper().Emit(new Beat { N = 1 }));
        Assert.Throws<ArgumentException>(() => world.Emit(new Pop { Cell = 1, Tag = new string('x', EventLog.MaxPayloadBytes) }));
        Assert.Equal(1, world.Events.Count);
    }

    [Fact]
    public void TheJsonListsTypeTickEntityAndPayload()
    {
        var world = NewWorld();
        for (var i = 0; i < 3; i++)
        {
            world.Step();
        }

        var json = new JsonOut();
        world.Events.WriteJson(json);
        var text = json.ToString();
        Assert.Contains("\"type\":\"Pop\"", text, StringComparison.Ordinal);
        Assert.Contains("\"tag\":\"pop-tag\"", text.Replace("\"Tag\"", "\"tag\"", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Contains("\"tick\":2", text, StringComparison.Ordinal);

        // An event nobody registered is still listed, by id and bytes.
        var bare = new World(1, new EntityRegistry());
        bare.Add(new Popper());
        for (var i = 0; i < 3; i++)
        {
            bare.Step();
        }

        var unnamed = new JsonOut();
        bare.Events.WriteJson(unnamed);
        Assert.Contains("\"type\":\"0x", unnamed.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void HostileEventMessagesAreProtocolErrorsNotCrashesOrAllocations()
    {
        var hub = new Hub();
        var (a, b) = Loopback.CreatePair();
        var node = hub.Cluster.AddNode("victim");
        var link = node.ConnectTo(hub.Host, Trust.Reader(hub.Scope.Id), a, b, "victim");
        var replica = node.Subscribe(link, hub.Scope.Id, NewWorld(WorldRole.Replica));
        hub.Cluster.Settle();
        hub.Cluster.Step(5);

        byte[] Message(Action<SyncWriter> body)
        {
            var w = new SyncWriter();
            w.WriteByte(24);
            w.WriteVarUInt(hub.Scope.Id.Value);
            body(w);
            return w.ToArray();
        }

        // A count far beyond the limit, a payload longer than an event may be, a negative age, truncation, trailing bytes.
        // Each is a protocol error that makes the replica ask again; a few ticks in between let it recover.
        var hostile = new Func<long, byte[]>[]
        {
            t => Message(w => { w.WriteVarInt(t); w.WriteVarUInt(1_000_000); }),
            t => Message(w => { w.WriteVarInt(t); w.WriteVarUInt(1); w.WriteVarInt(0); w.WriteFixed32(1); w.WriteVarUInt(0); w.WriteVarUInt(1 << 20); }),
            t => Message(w => { w.WriteVarInt(t); w.WriteVarUInt(1); w.WriteVarInt(-5); w.WriteFixed32(1); w.WriteVarUInt(0); w.WriteBytes([1, 2, 3]); }),
            t => Message(w => { w.WriteVarInt(t); w.WriteVarUInt(2); w.WriteVarInt(0); w.WriteFixed32(1); }),
            t => Message(w => { w.WriteVarInt(t); w.WriteVarUInt(1); w.WriteVarInt(0); w.WriteFixed32(1); w.WriteVarUInt(0); w.WriteBytes([1]); w.WriteByte(9); }),
        };
        foreach (var make in hostile)
        {
            b.Send(make(replica.LastTick));
            hub.Cluster.Settle();
            hub.Cluster.Step(3);
            Assert.Equal(ReplicaState.Live, replica.State);
        }

        Assert.True(node.ProtocolErrors.Count >= 5, node.ProtocolErrors.Count.ToString(System.Globalization.CultureInfo.InvariantCulture));

        // A well-formed one for another tick is ignored, and a good one is kept as the replica applies it.
        var after = replica.World.Events.Count;
        b.Send(Message(w => { w.WriteVarInt(replica.LastTick + 50); w.WriteVarUInt(1); w.WriteVarInt(0); w.WriteFixed32(7); w.WriteVarUInt(0); w.WriteBytes([1]); }));
        hub.Cluster.Settle();
        hub.Cluster.Step(10);
        Assert.NotEqual(ReplicaState.Failed, replica.State);
        Assert.Equal(replica.LastTick, hub.Scope.World.Tick);
        Assert.DoesNotContain(Lines(replica.World.Events, e => e.TypeId == 7 && e.Payload.Length == 1), l => l.Length == 0);
        Assert.True(replica.World.Events.Count >= after);
    }
}

/// <summary>Emits one integer event every tick.</summary>
[Entity]
public partial class BeatMaker : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced] public partial int N { get; set; }

    protected override void OnTick()
    {
        N++;
        Emit(new Beat { N = N });
    }
}

/// <summary>Emits <see cref="Count"/> events each tick.</summary>
[Entity]
public partial class Burst : Entity
{
    [Synced] public partial int Count { get; set; }

    protected override void OnTick()
    {
        for (var i = 0; i < Count; i++)
        {
            Emit(new Beat { N = i });
        }
    }
}
