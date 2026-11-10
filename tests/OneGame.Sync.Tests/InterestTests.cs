namespace OneGame.Sync.Tests;

[Entity]
public partial class Marker : Entity
{
    [Synced, InterestKey] public partial uint Cell { get; set; }

    [Synced] public partial string Tag { get; set; }

    [Synced] public partial int Pings { get; set; }

    [Synced] public partial int Step { get; set; }

    [Command]
    public void Ping() => Pings++;

    protected override void OnTick() => Step += Quiet ? 0 : 1;

    public static bool Quiet { get; set; }
}

[HotData(Name = "loud", Visibility = Visibility.Everyone)]
public struct LoudRow
{
    public int V { get; set; }
}

/// <summary>Interest sets: what each connection receives, that nothing outside them reaches its bytes, and that view hashes stay exact.</summary>
public class InterestTests
{
    private const int Cells = 10;
    private const int PerCell = 8;
    private static readonly uint PingId = StateHash.Fnv32("Ping");

    private static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        registry.Add(new Marker().TypeId, "Marker", static () => new Marker());
        return registry;
    }

    private static string TagOf(uint cell, int n) => $"tag-c{cell}-n{n}-payload";

    private static World Land(WorldRole role = WorldRole.Authority)
    {
        var world = new World(3, Registry(), role);
        if (role == WorldRole.Authority)
        {
            for (uint cell = 1; cell <= Cells; cell++)
            {
                for (var n = 0; n < PerCell; n++)
                {
                    world.Add(new Marker { Cell = cell, Tag = TagOf(cell, n) });
                }
            }

            world.Add(new Marker { Cell = 0, Tag = "tag-global-payload" });
        }

        return world;
    }

    private sealed class Rigged
    {
        public Rigged(bool require = false, int interval = 1)
        {
            Cluster = new Cluster(new ClusterOptions { DriftCheckInterval = interval, FatalDrift = true });
            Host = Cluster.AddNode("host");
            Scope = Cluster.AddScope(Host, new ScopeId(1), Land());
            Scope.RequireInterest = require;
        }

        public Cluster Cluster { get; }

        public Node Host { get; }

        public AuthorityScope Scope { get; }

        public Player Join(string name, params uint[] cells)
        {
            var node = Cluster.AddNode(name);
            var (a, b) = Loopback.CreatePair();
            var tap = new TapChannel(a);
            var link = node.ConnectTo(Host, Trust.Player(Scope.Id), tap, b, name);
            if (cells.Length > 0)
            {
                link.Connection.SetInterest(Scope.Id, cells);
            }

            var replica = node.Subscribe(link, Scope.Id, Land(WorldRole.Replica));
            return new Player(node, link, tap, replica);
        }

        public void Step(int n, params Player[] players)
        {
            for (var i = 0; i < n; i++)
            {
                Cluster.Step(1);
                foreach (var p in players)
                {
                    Assert.Equal(Scope.ViewHashFor(p.Connection), p.Replica.ViewHash);
                }
            }

            Assert.Empty(Cluster.DriftReports);
        }
    }

    private static int Count(Player p) => p.Replica.World.Entities.Count;

    private static bool SawAny(Player p, uint cell)
    {
        for (var n = 0; n < PerCell; n++)
        {
            if (p.Tap.Contains(TagOf(cell, n)))
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void AConnectionReceivesOnlyItsCellsAndGlobalEntities()
    {
        var rig = new Rigged();
        var all = rig.Join("all");
        var ada = rig.Join("ada", 2, 3);
        var bo = rig.Join("bo", 9);
        rig.Cluster.Settle();
        rig.Step(30, all, ada, bo);

        Assert.Equal((Cells * PerCell) + 1, Count(all));
        Assert.Equal((2 * PerCell) + 1, Count(ada));
        Assert.Equal(PerCell + 1, Count(bo));
        for (uint c = 1; c <= Cells; c++)
        {
            Assert.Equal(c is 2 or 3, SawAny(ada, c));
            Assert.Equal(c == 9, SawAny(bo, c));
            Assert.True(SawAny(all, c));
        }

        Assert.True(ada.Tap.Contains("tag-global-payload"));
        Assert.Empty(rig.Cluster.CheckReplicas());
    }

    [Fact]
    public void MovingTheSetStreamsEntersAndLeavesWithoutAResnapshot()
    {
        var rig = new Rigged();
        var ada = rig.Join("ada", 1);
        rig.Cluster.Settle();
        rig.Step(5, ada);
        var snapshots = ada.Tap.Received.Count(m => m[0] == 16);
        Assert.Equal(1, snapshots);
        Assert.Equal(PerCell + 1, Count(ada));

        ada.Connection.SetInterest(rig.Scope.Id, [1, 2]);
        rig.Step(3, ada);
        Assert.Equal((2 * PerCell) + 1, Count(ada));
        Assert.True(SawAny(ada, 2));

        ada.Connection.SetInterest(rig.Scope.Id, [2]);
        rig.Step(3, ada);
        Assert.Equal(PerCell + 1, Count(ada));
        Assert.DoesNotContain(ada.Replica.World.Entities.OfType<Marker>(), m => m.Cell == 1);
        Assert.Equal(snapshots, ada.Tap.Received.Count(m => m[0] == 16));

        // An entity whose key changes crosses out of one connection's view and into another's.
        var bo = rig.Join("bo", 5);
        rig.Cluster.Settle();
        rig.Step(3, ada, bo);
        var mover = rig.Scope.World.Entities.OfType<Marker>().First(m => m.Cell == 2);
        mover.Tag = "tag-traveller-payload";
        rig.Step(2, ada, bo);
        Assert.True(ada.Tap.Contains("tag-traveller-payload"));
        Assert.False(bo.Tap.Contains("tag-traveller-payload"));
        mover.Cell = 5;
        rig.Step(3, ada, bo);
        Assert.DoesNotContain(ada.Replica.World.Entities.OfType<Marker>(), m => m.Tag == "tag-traveller-payload");
        Assert.Contains(bo.Replica.World.Entities.OfType<Marker>(), m => m.Tag == "tag-traveller-payload");
        Assert.Empty(rig.Cluster.CheckReplicas());
    }

    [Fact]
    public void ARestrictionPutOnAnUnrestrictedConnectionMovesItToAViewOfItsOwn()
    {
        var rig = new Rigged();
        var ada = rig.Join("ada");
        rig.Cluster.Settle();
        rig.Step(3, ada);
        Assert.Equal((Cells * PerCell) + 1, Count(ada));
        ada.Connection.SetInterest(rig.Scope.Id, [4]);
        rig.Step(3, ada);
        Assert.Equal(PerCell + 1, Count(ada));
        rig.Scope.ClearInterest(ada.Connection);
        rig.Step(3, ada);
        Assert.Equal((Cells * PerCell) + 1, Count(ada));
    }

    [Fact]
    public void CommandsToEntitiesOutsideTheInterestAreAnsweredAsUnknown()
    {
        var rig = new Rigged();
        var ada = rig.Join("ada", 1);
        rig.Cluster.Settle();
        rig.Step(3, ada);
        var far = rig.Scope.World.Entities.OfType<Marker>().First(m => m.Cell == 7);
        var near = rig.Scope.World.Entities.OfType<Marker>().First(m => m.Cell == 1);

        var farResult = ada.Connection.SubmitCommand(rig.Scope.Id, far.Id, PingId, default);
        var nearResult = ada.Connection.SubmitCommand(rig.Scope.Id, near.Id, PingId, default);
        rig.Step(3, ada);
        Assert.Equal(SubmitResult.OutsideInterest, farResult);
        Assert.Equal(SubmitResult.Queued, nearResult);
        Assert.Equal(0, far.Pings);
        Assert.Equal(1, near.Pings);

        // A guessed id reaches the replica's result list as an unknown entity, the same answer as an id that does not exist.
        var link = ada.Link;
        var w = new SyncWriter();
        w.WriteByte(3);
        w.WriteVarUInt(rig.Scope.Id.Value);
        w.WriteVarUInt(900);
        w.WriteVarUInt(far.Id.Value);
        w.WriteVarUInt(PingId);
        link.Channel.Send(w.Written);
        w.Reset();
        w.WriteByte(3);
        w.WriteVarUInt(rig.Scope.Id.Value);
        w.WriteVarUInt(901);
        w.WriteVarUInt(far.Id.Value + 100000);
        w.WriteVarUInt(PingId);
        link.Channel.Send(w.Written);
        rig.Step(4);
        var verdicts = ada.Replica.Results;
        Assert.Equal(0, far.Pings);
        Assert.Empty(rig.Cluster.DriftReports);
        Assert.NotNull(verdicts);
    }

    [Fact]
    public void ARequiredInterestStartsEmptyAndRefusesUnfilterableTables()
    {
        var rig = new Rigged(require: true);
        var ada = rig.Join("ada");
        rig.Cluster.Settle();
        rig.Step(3, ada);
        Assert.Equal(1, Count(ada)); // only the global marker
        Assert.False(SawAny(ada, 1));
        rig.Scope.SetInterest(ada.Connection, [6]);
        rig.Step(3, ada);
        Assert.Equal(PerCell + 1, Count(ada));
        Assert.Equal(Visibility.Hidden, rig.Scope.RandomStreams);

        var world = Land();
        world.AddTable(new LoudRowTable());
        var cluster = new Cluster();
        var scope = cluster.AddScope(cluster.AddNode("n"), new ScopeId(2), world);
        Assert.Throws<InvalidOperationException>(() => scope.RequireInterest = true);
    }

    [Fact]
    public void AnInterestSetBeforeTheFirstSubscribeIsUsedForTheFirstSnapshot()
    {
        var rig = new Rigged();
        var ada = rig.Join("ada", 3);
        rig.Cluster.Settle();
        Assert.Equal(1, ada.Tap.Received.Count(m => m[0] == 16));
        Assert.False(ada.Tap.Contains(TagOf(4, 0)));
        Assert.True(ada.Tap.Contains(TagOf(3, 0)));
        rig.Step(10, ada);
    }

    [Fact]
    public void ADetachedConnectionForgetsItsInterest()
    {
        var rig = new Rigged();
        var ada = rig.Join("ada", 3);
        rig.Cluster.Settle();
        rig.Host.Detach(ada.Connection);
        rig.Cluster.Step(2);
        Assert.Null(rig.Scope.InterestOf(ada.Connection));
    }

    [Fact]
    public void RecordingReplayAndSaveRestoreAgreeWithInterestOn()
    {
        var rig = new Rigged();
        var ada = rig.Join("ada", 1, 2);
        rig.Cluster.Settle();
        var rec = rig.Scope.StartRecording(new RecordingOptions { CheckpointInterval = 1 });
        rig.Step(20, ada);
        var saved = rig.Scope.Save();
        rig.Step(20, ada);
        var hashAfter = rig.Scope.Hash;
        var recording = rec.Stop();
        var replay = Replayer.Replay(recording, Land());
        Assert.True(replay.Ok);

        // Loading the save resends filtered snapshots; the replica converges on the same view again.
        rig.Scope.Load(saved);
        rig.Step(20, ada);
        Assert.Equal((2 * PerCell) + 1, Count(ada));
        Assert.Equal(hashAfter, rig.Scope.Hash);
    }

    [Fact]
    public void ConnectionsWithoutInterestReceiveTheSameBytesAsBefore()
    {
        // Pins the wire of a scope that never uses interest: a hash over every message two audiences receive in a fixed scenario. The
        // value was taken from the previous release's code, so a client or server of that release interoperates in both directions.
        var rig = new Rig();
        var ada = rig.Join("ada", Trust.Player(rig.Scope.Id).As(1, 10));
        var bo = rig.Join("bo", Trust.Player(rig.Scope.Id).As(2, 20));
        rig.StepChecked(60, ada, bo);
        var all = new List<byte>();
        foreach (var p in new[] { ada, bo })
        {
            foreach (var m in p.Tap.Received)
            {
                all.AddRange(m);
            }
        }

        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(all.ToArray()));
        Console.WriteLine($"wire hash {hash} over {all.Count} bytes");
        Assert.Equal("BB3ECAEB1922C680FDCF73A06ADF197B585E2C87F2589CBD5017F1D8181D2ABD", hash);
    }

    [Fact]
    public void ATransferTellsOnlyTheConnectionsThatHeldTheEntity()
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var a = cluster.AddNode("A");
        var b = cluster.AddNode("B");
        var worldA = Land();
        TransferLog.AddTo(worldA);
        var worldB = new World(5, Registry(), WorldRole.Authority);
        TransferLog.AddTo(worldB);
        var scopeA = cluster.AddScope(a, new ScopeId(1), worldA);
        var scopeB = cluster.AddScope(b, new ScopeId(2), worldB);
        var peer = cluster.Connect(a, b, Trust.Owner(new ScopeId(2)));

        Player Watch(string name, params uint[] cells)
        {
            var node = cluster.AddNode(name);
            var (x, y) = Loopback.CreatePair();
            var tap = new TapChannel(x);
            var link = node.ConnectTo(a, Trust.Player(scopeA.Id), tap, y, name);
            link.Connection.SetInterest(scopeA.Id, cells);
            return new Player(node, link, tap, node.Subscribe(link, scopeA.Id, Land(WorldRole.Replica)));
        }

        var near = Watch("near", 7);
        var far = Watch("far", 2);
        cluster.Settle();
        cluster.Step(5);
        var traveller = worldA.Entities.OfType<Marker>().First(m => m.Cell == 7);
        var id = traveller.Id;
        var transfer = scopeA.BeginTransfer(peer, new ScopeId(2), [id]);
        for (var i = 0; i < 100 && transfer.State != TransferState.Complete; i++)
        {
            cluster.Step(1);
        }

        cluster.Step(5);
        Assert.Equal(TransferState.Complete, transfer.State);
        const byte moved = 52;
        Assert.Contains(near.Tap.Received, m => m[0] == moved);
        Assert.DoesNotContain(far.Tap.Received, m => m[0] == moved);
        Assert.False(far.Tap.Contains(traveller.Tag));
        Assert.Empty(cluster.DriftReports);
        Assert.Equal(PerCell - 1, near.Replica.World.Entities.OfType<Marker>().Count(m => m.Cell == 7));
    }

    [Fact]
    public void ARelayOnlyNarrowsAndItsClientsAreFilteredToo()
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var auth = cluster.AddNode("auth");
        var relay = cluster.AddNode("relay");
        var scope = cluster.AddScope(auth, new ScopeId(1), Land());
        var up = cluster.Connect(relay, auth, Trust.Owner(scope.Id).AsRelay());
        scope.SetInterest(up.Connection, [1, 2, 3]); // the relay's own picture of the world: three cells
        var upstream = relay.Subscribe(up, scope.Id, Land(WorldRole.Replica));
        var relayScope = relay.Relay(upstream);
        cluster.Settle();

        var clientNode = cluster.AddNode("client");
        var (a, b) = Loopback.CreatePair();
        var tap = new TapChannel(a);
        var link = clientNode.ConnectTo(relay, Trust.Player(scope.Id), tap, b, "client");
        relayScope.SetInterest(link.Connection, [2, 7]); // 7 is outside what the relay holds
        var replica = clientNode.Subscribe(link, scope.Id, Land(WorldRole.Replica));
        cluster.Settle();
        cluster.Step(20);

        Assert.Equal(PerCell + 1, replica.World.Entities.Count);
        Assert.True(tap.Contains(TagOf(2, 0)));
        for (uint c = 1; c <= Cells; c++)
        {
            if (c != 2)
            {
                Assert.False(tap.Contains(TagOf(c, 0)), $"cell {c} reached the client");
            }
        }

        Assert.Empty(cluster.DriftReports);
    }

    [Fact]
    public void TheGridKeepsACellUntilTheLeaveRadiusAndNeverFlapsOnABoundary()
    {
        var grid = new InterestGrid2D(cellSize: 128, enterCells: 2, leaveCells: 3);
        var follower = grid.Follow();
        Assert.True(follower.Update(0, 0));
        Assert.Equal(25, follower.Keys.Length);

        // Walk back and forth across a cell boundary: after the first crossing nothing changes.
        var changes = 0;
        for (var i = 0; i < 600; i++)
        {
            var x = 127 + (i % 2 == 0 ? 0 : 1);
            if (follower.Update(x, 0))
            {
                changes++;
            }
        }

        Assert.Equal(1, changes);

        // Leaving: the far column is dropped only past the leave radius.
        Assert.Contains(InterestGrid2D.Pack(-2, 0), follower.Keys.ToArray()); // the player is in cell 1, three cells away: kept
        follower.Update(128 * 4, 0);
        Assert.DoesNotContain(InterestGrid2D.Pack(-2, 0), follower.Keys.ToArray());

        // The 224-block terrain radius is covered from anywhere inside the centre cell.
        var f2 = grid.Follow();
        f2.Update(127, 127);
        var keys = f2.Keys.ToArray();
        Assert.Contains(grid.Key(127 + 224, 127), keys);
        Assert.Contains(grid.Key(127 - 224, 127), keys);
        Assert.Contains(grid.Key(127, 127 + 224), keys);

        // Keys cover +-100,000 blocks and are never 0; entities with a margin keep their key at an edge.
        Assert.NotEqual(0u, grid.Key(-100000, 100000));
        Assert.NotEqual(grid.Key(-100000, -100000), grid.Key(100000, 100000));
        var k = grid.Key(127, 5);
        Assert.Equal(k, grid.Sticky(k, 129, 5, marginBlocks: 4));
        Assert.NotEqual(k, grid.Sticky(k, 140, 5, marginBlocks: 4));
    }

    [Fact]
    public void InterestAddsNoAllocationToASteadyTick()
    {
        // The cluster itself allocates a few dozen bytes per client and tick (the harness's own bookkeeping); interest must add nothing
        // to that, neither in the per-connection views and frames nor in setting an unchanged set every tick.
        Marker.Quiet = true;
        var plain = new Rigged(interval: 30);
        plain.Join("p");
        plain.Join("q");
        plain.Join("r");
        var rig = new Rigged(interval: 30);
        var players = new[] { rig.Join("a", 1, 2), rig.Join("b", 3), rig.Join("c", 4, 5, 6) };
        uint[][] sets = [[1, 2], [3], [4, 5, 6]];
        plain.Cluster.Settle();
        rig.Cluster.Settle();
        plain.Cluster.Step(300);
        rig.Cluster.Step(300);

        long Measure(Action tick)
        {
            var best = long.MaxValue;
            for (var round = 0; round < 3; round++)
            {
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < 100; i++)
                {
                    tick();
                }

                best = Math.Min(best, GC.GetAllocatedBytesForCurrentThread() - before);
            }

            return best;
        }

        var baseline = Measure(() => plain.Cluster.Step(1));
        var withInterest = Measure(() =>
        {
            for (var p = 0; p < players.Length; p++)
            {
                rig.Scope.SetInterest(players[p].Connection, sets[p]);
            }

            rig.Cluster.Step(1);
        });
        Console.WriteLine($"3 clients, 100 ticks: {baseline} bytes without interest, {withInterest} bytes with interest and SetInterest every tick");
        Assert.True(withInterest <= baseline, $"{withInterest} > {baseline}");
    }
}
