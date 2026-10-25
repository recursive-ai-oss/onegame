namespace OneGame.Sync.Tests;

/// <summary>
/// A cluster the way a networked game uses it: a master, two game nodes, and three player nodes, each player following its own game scope
/// and the master. Everything runs for a thousand ticks with drift checks on every tick.
/// </summary>
public class ClusterAcceptanceTests
{
    private const int Ticks = 1000;

    private sealed class Game
    {
        public Cluster Cluster = null!;
        public Node Master = null!;
        public Node Game1 = null!;
        public Node Game2 = null!;
        public AuthorityScope Bank = null!;
        public AuthorityScope Match1 = null!;
        public AuthorityScope Match2 = null!;
        public Seat[] Seats = null!;
        public List<Recorder> Recorders = [];
    }

    private sealed record Seat(uint Player, Node Node, Player InGame, Player InBank);

    private static World NewMaster() => Arena.Master(1, 2, 3);

    private static World NewMatch1() => Arena.Match(Arena.Seed + 2, 1, 2);

    private static World NewMatch2() => Arena.Match(Arena.Seed + 3, 3, 4);

    private static Game Build()
    {
        var g = new Game { Cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true }) };
        g.Master = g.Cluster.AddNode("master");
        g.Game1 = g.Cluster.AddNode("game1");
        g.Game2 = g.Cluster.AddNode("game2");
        g.Bank = g.Cluster.AddScope(g.Master, new ScopeId(1), NewMaster());
        g.Match1 = g.Cluster.AddScope(g.Game1, new ScopeId(2), NewMatch1());
        g.Match2 = g.Cluster.AddScope(g.Game2, new ScopeId(3), NewMatch2());
        g.Seats =
        [
            Seated(g, 1, 10, g.Game1, g.Match1),
            Seated(g, 2, 20, g.Game1, g.Match1),
            Seated(g, 3, 10, g.Game2, g.Match2),
        ];
        g.Cluster.Settle();
        foreach (var scope in new[] { g.Bank, g.Match1, g.Match2 })
        {
            g.Recorders.Add(scope.StartRecording(new RecordingOptions { CheckpointInterval = 1 }));
        }

        return g;
    }

    private static Seat Seated(Game g, uint player, uint team, Node gameNode, AuthorityScope match)
    {
        var node = g.Cluster.AddNode("player" + player);
        Player Follow(Node server, AuthorityScope scope)
        {
            var (a, b) = Loopback.CreatePair();
            var tap = new TapChannel(a);
            var link = node.ConnectTo(server, Trust.Owner(scope.Id).As(player, team), tap, b, node.Name);
            return new Player(node, link, tap, node.Subscribe(link, scope.Id, Arena.Empty(WorldRole.Replica)));
        }

        return new Seat(player, node, Follow(gameNode, match), Follow(g.Master, g.Bank));
    }

    /// <summary>The scripted players: movement, spending, deposits, a withdrawal the bank refuses, and some commands they may not send.</summary>
    private static void Input(Game g, long tick)
    {
        foreach (var seat in g.Seats)
        {
            var offset = (int)seat.Player;
            if (seat.InGame.Replica.State != ReplicaState.Live)
            {
                continue;
            }

            var hero = Rig.HeroOf(seat.InGame.Replica, seat.Player);
            if (hero is null)
            {
                continue;
            }

            if ((tick + offset) % 7 == 0)
            {
                hero.Send.Walk((int)(tick % 3) - 1, (int)(tick % 5) - 2);
            }

            if ((tick + offset) % 13 == 0)
            {
                hero.Send.Spend(3);
            }

            if ((tick + offset) % 97 == 0)
            {
                hero.Send.Rename("Hero " + seat.Player + "/" + tick);
            }

            if ((tick + offset) % 151 == 0)
            {
                hero.Send.Hoard("cache-" + tick);
            }

            if (seat.InBank.Replica.State == ReplicaState.Live && Rig.AccountOf(seat.InBank.Replica, seat.Player) is { } account)
            {
                if ((tick + offset) % 29 == 0)
                {
                    account.Send.Deposit(2);
                }

                if ((tick + offset) % 41 == 0)
                {
                    account.Send.Withdraw(1_000_000); // always rejected: "insufficient funds"
                }

                if ((tick + offset) % 211 == 0)
                {
                    account.Send.Note("note at " + tick);
                }
            }
        }

        // A player reaching for someone else's hero is refused (owner only), and the refusal changes nothing.
        if (tick % 120 == 0 && Rig.HeroOf(g.Seats[1].InGame.Replica, 1) is { } others)
        {
            others.Send.Rename("hijack");
        }
    }

    /// <summary>One thousand ticks; returns every hash of every scope on every node, tick by tick.</summary>
    private static (List<ulong> Hashes, Game Game) Run()
    {
        var g = Build();
        var hashes = new List<ulong>();
        for (var tick = 0; tick < Ticks; tick++)
        {
            Input(g, tick);

            // Mid-run trust changes: a player switches teams, another loses the bank and later gets it back.
            if (tick == 400)
            {
                g.Seats[1].InGame.Connection.Trust = Trust.Owner(g.Match1.Id).As(2, 10);
            }

            if (tick == 600)
            {
                g.Seats[2].InBank.Connection.Trust = Trust.None;
            }

            if (tick == 700)
            {
                g.Seats[2].InBank.Connection.Trust = Trust.Owner(g.Bank.Id).As(3, 10);
                g.Seats[2].Node.Resubscribe(g.Seats[2].InBank.Replica);
            }

            g.Cluster.Step();
            foreach (var scope in new[] { g.Bank, g.Match1, g.Match2 })
            {
                hashes.Add(scope.Hash);
            }

            foreach (var seat in g.Seats)
            {
                foreach (var p in new[] { seat.InGame, seat.InBank })
                {
                    var authority = p.Link.Server.FindAuthority(p.Replica.Id)!;
                    if (p.Replica.State == ReplicaState.Live)
                    {
                        // The replica holds exactly the view the authority computes for this connection, every tick.
                        Assert.Equal(authority.ViewHashFor(p.Connection), p.Replica.ViewHash);
                        Assert.Equal(authority.World.Tick, p.Replica.World.Tick);
                        hashes.Add(p.Replica.ViewHash);
                    }
                    else
                    {
                        hashes.Add(0);
                    }
                }
            }
        }

        return (hashes, g);
    }

    [Fact]
    public void FourHundredAndMoreTicksAreDriftFreeAndReproducible()
    {
        var (first, g) = Run();
        Assert.Empty(g.Cluster.DriftReports);
        Assert.Empty(g.Cluster.CheckReplicas());
        Assert.All(g.Seats, s => Assert.Equal(ReplicaState.Live, s.InGame.Replica.State));
        Assert.All(g.Seats, s => Assert.Equal(ReplicaState.Live, s.InBank.Replica.State));
        Assert.Equal(Ticks, g.Match1.World.Tick);

        // The scripted play really happened: commands ran, were rejected with reasons, and were refused.
        var results = g.Seats.SelectMany(s => s.InBank.Replica.Results).ToList();
        Assert.Contains(results, r => r.Verdict == CommandVerdict.Executed);
        Assert.Contains(results, r => r is { Verdict: CommandVerdict.Rejected, Reason: "insufficient funds" });
        Assert.Contains(g.Seats.SelectMany(s => s.InGame.Replica.Results), r => r.Verdict == CommandVerdict.Executed);
        Assert.Contains(g.Seats.SelectMany(s => s.InGame.Replica.Results), r => r is { Verdict: CommandVerdict.Refused, Refusal: RefusalReason.NotOwner });
        Assert.True(g.Match1.CommandsRun > 100);

        // Privacy held all the way: no player's channel ever carried another's owner-only strings or anything hidden.
        Assert.True(g.Seats[0].InGame.Tap.Contains("ruby-hoard-7731"));
        Assert.False(g.Seats[1].InGame.Tap.Contains("ruby-hoard-7731"));
        Assert.False(g.Seats[1].InBank.Tap.Contains("notes-of-player-b"));
        Assert.True(g.Seats[0].InBank.Tap.Contains("notes-of-player-b"));
        Assert.False(g.Seats[0].InBank.Tap.Contains("notes-of-player-c"));
        Assert.True(g.Seats[1].InBank.Tap.Contains("notes-of-player-c"));
        Assert.False(g.Seats[2].InGame.Tap.Contains("emerald-cache-2208"));

        // Second run: same inputs, the same hash on every tick of every scope and every replica.
        var (second, _) = Run();
        Assert.Equal(first.Count, second.Count);
        Assert.True(first.SequenceEqual(second), "the second run diverged from the first");

        // Replaying each authority's recording rebuilds exactly its final state.
        var worlds = new[] { (g.Bank, NewMaster()), (g.Match1, NewMatch1()), (g.Match2, NewMatch2()) };
        for (var i = 0; i < worlds.Length; i++)
        {
            var (scope, fresh) = worlds[i];
            var recording = g.Recorders[i].Stop();
            var replay = Replayer.Replay(recording, fresh);
            Assert.True(replay.Ok, replay.Divergence?.ToJson());
            Assert.Equal(Ticks, replay.TicksRun);
            Assert.Equal(scope.Hash, fresh.ComputeHash());
        }
    }

    [Fact]
    public void TheCommandLogReplaysTheSameThroughTheFileFormat()
    {
        var (_, g) = Run();
        var recording = g.Recorders[1].Stop();
        Assert.Equal(SyncError.None, Recording.TryRead(recording.ToBytes(), out var read));
        var fresh = NewMatch1();
        Assert.True(Replayer.Replay(read!, fresh).Ok);
        Assert.Equal(g.Match1.Hash, fresh.ComputeHash());
        Assert.Contains(recording.Commands, c => c.CommandId == StateHash.Fnv32("Spend"));
    }
}
