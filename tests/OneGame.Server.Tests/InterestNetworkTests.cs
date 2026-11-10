using System.Globalization;
using System.Net;
using System.Net.Sockets;
using OneGame.Core;
using OneGame.Host;
using OneGame.Net;

namespace OneGame.Server.Tests;

/// <summary>A creature or player body: its position is the interest key's source, and it carries enough changing state to be expensive to send.</summary>
[Entity]
public partial class Body : Entity
{
    [Synced, InterestKey] public partial uint Cell { get; set; }

    [Synced] public partial int X { get; set; }

    [Synced] public partial int Y { get; set; }

    [Synced] public partial int Vx { get; set; }

    [Synced] public partial int Vy { get; set; }

    [Synced] public partial int Seq { get; set; }

    [Synced] public partial string Detail { get; set; }

    [Synced] public partial string Tag { get; set; }

    [Command]
    public void Nudge() => Seq += 1000;

    protected override void OnTick()
    {
        X += Vx;
        Y += Vy;
        Seq++;
        Cell = InterestWorld.Grid.Sticky(Cell, X >> 4, Y >> 4, 8);
        Detail = Seq.ToString("x8", CultureInfo.InvariantCulture) + InterestWorld.Filler;
    }
}

/// <summary>An edited terrain chunk: about 4.8 KB that rarely changes.</summary>
[Entity]
public partial class Chunk : Entity
{
    [Synced, InterestKey] public partial uint Cell { get; set; }

    [Synced] public partial string Data { get; set; }

    [Synced] public partial string Tag { get; set; }
}

internal static class InterestWorld
{
    public static readonly InterestGrid2D Grid = new(cellSize: 128, enterCells: 2, leaveCells: 3);

    public static readonly string Filler = new('x', 400);

    public static void Register(EntityRegistry r)
    {
        r.Add(new Body().TypeId, "Body", static () => new Body());
        r.Add(new Chunk().TypeId, "Chunk", static () => new Chunk());
    }

    public static string ChunkData(int n)
    {
        var rng = new Random(n);
        var chars = new char[4800];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)('a' + rng.Next(26));
        }

        return new string(chars);
    }

    /// <summary>Player home points, 50 km apart on a flat plane inside +-100,000 blocks.</summary>
    public static (int X, int Y) Home(int player) => (-75000 + (player % 4 * 50000), -75000 + (player / 4 * 50000));
}

/// <summary>
/// 16 clients over real UDP in one scope: bytes per client with and without interest sets, how that scales with far-away entities, join
/// size, and that nothing outside a client's interest reaches its bytes.
/// </summary>
public class InterestNetworkTests
{
    private const int Players = 16;
    private const int TickRate = 20;
    private static readonly (string File, PeerKey Public) ServerKey = MakeKey();

    private static (string, PeerKey) MakeKey()
    {
        var keys = KeyStore.Create(Path.Combine(Path.GetTempPath(), "onegame-interest-tests-" + Guid.NewGuid().ToString("N")), "server");
        return (keys.PrivateFile, keys.PublicKey);
    }

    private static int Env(string name, int fallback)
        => int.TryParse(Environment.GetEnvironmentVariable(name), CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private static ServerHost StartHost(bool interest, int creaturesPerPlayer, int far, int chunks, string security, int sendQueue = 64 << 20)
    {
        var setup = new ServerSetup
        {
            RegisterEntities = InterestWorld.Register,
            SeedWorld = (world, _) => Seed(world, creaturesPerPlayer, far, chunks),
        };
        var config = ServerConfig.Parse($"name: net\ntickRate: {TickRate}\nhashHistory: 512\nscopes: [{{ id: 1, seed: 7 }}]\nlisten:\n  - {{ transport: udp, port: 0, address: 127.0.0.1 }}\n" + security);
        var next = 0u;
        var host = new ServerHost(config, setup) { Authenticate = _ => Trust.Player(new ScopeId(1)).As(++next), MaxSendQueueBytes = sendQueue };
        host.Start();
        var scope = host.Node.FindAuthority(new ScopeId(1))!;
        scope.RequireInterest = interest;
        if (interest)
        {
            // The interest is set the moment the client is admitted, so its very first snapshot is already filtered.
            host.ClientJoined += connection =>
            {
                var index = (int)connection.Trust.Who.Player - 1;
                if (index >= Players)
                {
                    return;
                }

                var body = (Body)scope.World.Entities.First(e => e is Body b && b.Tag == $"<player-{index}>");
                var follower = InterestWorld.Grid.Follow();
                follower.Update(body.X >> 4, body.Y >> 4);
                scope.SetInterest(connection, follower.Keys);
            };
        }

        return host;
    }

    private static void Seed(World world, int creaturesPerPlayer, int far, int chunks)
    {
        // Players' own bodies come first, so body i is player i + 1.
        var rng = new Random(11);
        for (var p = 0; p < Players; p++)
        {
            var (hx, hy) = InterestWorld.Home(p);
            world.Add(new Body { X = hx << 4, Y = hy << 4, Vx = 5, Vy = 0, Tag = $"<player-{p}>" });
        }

        for (var p = 0; p < Players; p++)
        {
            var (hx, hy) = InterestWorld.Home(p);
            for (var c = 0; c < creaturesPerPlayer; c++)
            {
                world.Add(new Body { X = (hx + rng.Next(-60, 60)) << 4, Y = (hy + rng.Next(-60, 60)) << 4, Vx = rng.Next(-3, 4), Vy = rng.Next(-3, 4), Tag = $"<near-{p}-{c}>" });
            }
        }

        // Far creatures live in the gaps between the players' regions (25 km offsets), never within a few cells of anyone.
        for (var f = 0; f < far; f++)
        {
            var gx = -50000 + ((f % 4) * 50000) + rng.Next(-2000, 2000);
            var gy = -50000 + (((f / 4) % 4) * 50000) + rng.Next(-2000, 2000);
            world.Add(new Body { X = gx << 4, Y = gy << 4, Vx = rng.Next(-3, 4), Vy = rng.Next(-3, 4), Tag = $"<far-{f}>" });
        }

        for (var n = 0; n < chunks; n++)
        {
            var p = n % Players;
            var (hx, hy) = InterestWorld.Home(p);
            var x = hx + rng.Next(-300, 300);
            var y = hy + rng.Next(-300, 300);
            world.Add(new Chunk { Cell = InterestWorld.Grid.Key(x, y), Data = InterestWorld.ChunkData(n), Tag = $"<chunk-{n}>" });
        }
    }

    private sealed class Client
    {
        public required RemoteSession Session { get; init; }

        public required Connection Server { get; init; }

        public InterestGrid2D.Follower Follower { get; } = InterestWorld.Grid.Follow();

        public long JoinBytes { get; set; }

        public long Bytes => Session.Connection.Stats.BytesReceived;
    }

    private static void Pump(ServerHost host, IReadOnlyList<Client> clients, int times = 3)
    {
        for (var i = 0; i < times; i++)
        {
            host.PumpNetwork();
            foreach (var c in clients)
            {
                c.Session.Pump();
            }
        }
    }

    private static List<Client> Join(ServerHost host, bool interest, int count)
    {
        var port = host.Endpoints.First(e => e.Transport == "udp").Port;
        var clients = new List<Client>();
        var scope = host.Node.FindAuthority(new ScopeId(1))!;
        for (var i = 0; i < count; i++)
        {
            var before = host.Node.Connections.Count;
            var world = host.CreateReplicaWorld(new ScopeId(1));
            var session = new RemoteSession(new ConnectOptions { Address = $"udp://127.0.0.1:{port}", Name = "c" + i, Security = NetSecurity.ForClient([ServerKey.Public], "net/1"), MaxSendQueueBytes = 64 << 20 }, world);
            var deadline = Environment.TickCount64 + 300_000;
            Client? client = null;
            while (Environment.TickCount64 < deadline)
            {
                host.PumpNetwork();
                host.StepOnce();
                session.Pump();
                if (client is null && host.Node.Connections.Count > before)
                {
                    client = new Client { Session = session, Server = host.Node.Connections[^1] };
                    clients.Add(client);
                    client.Follower.Update(((Body)scope.World.Entities[i]).X >> 4, ((Body)scope.World.Entities[i]).Y >> 4);
                }

                if (client is not null && session.Replica.State == ReplicaState.Live)
                {
                    break;
                }

                if (client is not null)
                {
                    Pump(host, clients, 1);
                }
            }

            Assert.NotNull(client);
            Assert.Equal(ReplicaState.Live, session.Replica.State);
            client.JoinBytes = client.Bytes;
        }

        return clients;
    }

    private static void Tick(ServerHost host, AuthorityScope scope, IReadOnlyList<Client> clients, bool interest)
    {
        if (interest)
        {
            for (var i = 0; i < clients.Count; i++)
            {
                var body = (Body)scope.World.Entities[i];
                if (clients[i].Follower.Update(body.X >> 4, body.Y >> 4))
                {
                    scope.SetInterest(clients[i].Server, clients[i].Follower.Keys);
                }
            }
        }

        host.StepOnce();
        Pump(host, clients);
    }

    private sealed record Run(double[] KBps, double[] JoinKB, int Entities, int Seen, bool ViewsMatch);

    private static Run Measure(bool interest, int creaturesPerPlayer, int far, int chunks, int ticks = 200)
    {
        var host = StartHost(interest, creaturesPerPlayer, far, chunks, $"security:\n  key: {ServerKey.File}\n  game: net/1\n");
        try
        {
            var scope = host.Node.FindAuthority(new ScopeId(1))!;
            var clients = Join(host, interest, Players);
            // Let the joins drain: measure only once every replica keeps up with the server, so a backlog of snapshot bytes is not counted as steady traffic.
            for (var i = 0; i < 40 || (i < 20_000 && clients.Any(c => host.Tick - c.Session.Replica.World.Tick > 2)); i++)
            {
                Tick(host, scope, clients, interest);
            }

            var start = clients.Select(c => c.Bytes).ToArray();
            for (var i = 0; i < ticks; i++)
            {
                Tick(host, scope, clients, interest);
            }

            var seconds = ticks / (double)TickRate;
            var rates = clients.Select((c, i) => (c.Bytes - start[i]) / 1024.0 / seconds).Order().ToArray();
            var joins = clients.Select(c => c.JoinBytes / 1024.0).Order().ToArray();

            // Settle and compare every replica with the authority's view for its connection.
            for (var i = 0; i < 20; i++)
            {
                Pump(host, clients);
            }

            var match = true;
            foreach (var c in clients)
            {
                var at = host.HashesAt(c.Session.Replica.World.Tick, host.Clients().First(k => k.Id == c.Server.Id).Name)[0];
                match &= at.View == c.Session.Replica.ViewHash.ToString("x16", CultureInfo.InvariantCulture);
            }

            return new Run(rates, joins, scope.World.Entities.Count, clients[0].Session.Replica.World.Entities.Count, match);
        }
        finally
        {
            host.Stop();
        }
    }

    private static double P(double[] sorted, double q) => sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(q * sorted.Length) - 1)];

    [Fact]
    public void SixteenPlayersOverUdpStayUnderTheBudgetWhetherOrNotFarEntitiesDouble()
    {
        var chunks = Env("INTEREST_CHUNKS", 160);
        var creatures = Env("INTEREST_NEAR", 2);
        var far = Env("INTEREST_FAR", 16);

        var without = Measure(interest: false, creatures, far, chunks);
        var with = Measure(interest: true, creatures, far, chunks);
        var withMore = Measure(interest: true, creatures, far * 2, chunks);

        void Report(string label, Run r)
            => Console.WriteLine($"{label}: entities {r.Entities}, client 0 holds {r.Seen}, down KB/s p50 {P(r.KBps, 0.5):F1} p95 {P(r.KBps, 0.95):F1} max {r.KBps[^1]:F1}, join KB p50 {P(r.JoinKB, 0.5):F0} p95 {P(r.JoinKB, 0.95):F0}, views match {r.ViewsMatch}");

        Report($"no interest      (far {far}, chunks {chunks})", without);
        Report($"interest         (far {far}, chunks {chunks})", with);
        Report($"interest, far x2 (far {far * 2}, chunks {chunks})", withMore);

        Assert.True(without.ViewsMatch && with.ViewsMatch && withMore.ViewsMatch);
        Assert.True(P(with.KBps, 0.95) < 64, "per-client downstream is over the budget");
        Assert.True(P(without.KBps, 0.5) > 2 * P(with.KBps, 0.5), "interest should cut downstream by more than half in this scenario");

        // Per-client downstream tracks the nearby entities: doubling the far ones changes nothing worth speaking of.
        Assert.InRange(P(withMore.KBps, 0.5) / P(with.KBps, 0.5), 0.9, 1.1);
        Assert.True(P(without.KBps, 0.5) > P(with.KBps, 0.5) * 1.5);

        // A join carries only what is in interest: a small fraction of the chunks, and no growth with the world.
        Assert.True(P(with.JoinKB, 0.95) * 4 < P(without.JoinKB, 0.5), "the join was not bounded by the interest radius");
        Assert.InRange(P(withMore.JoinKB, 0.5) / P(with.JoinKB, 0.5), 0.9, 1.1);
        Assert.True(with.Seen < with.Entities / 4);
    }

    // ---- send queue: pacing and typed overflow ------------------------------------------------------------------------

    private static RemoteSession JoinOnce(ServerHost host, bool reconnect)
    {
        var port = host.Endpoints.First(e => e.Transport == "udp").Port;
        var world = host.CreateReplicaWorld(new ScopeId(1));
        return new RemoteSession(new ConnectOptions { Address = $"udp://127.0.0.1:{port}", Name = "big", Security = NetSecurity.ForClient([ServerKey.Public], "net/1"), Reconnect = reconnect }, world);
    }

    private static bool RunUntil(ServerHost host, RemoteSession session, Func<bool> done, int ms = 120_000)
    {
        var deadline = Environment.TickCount64 + ms;
        while (Environment.TickCount64 < deadline)
        {
            host.PumpNetwork();
            host.StepOnce();
            session.Pump();
            if (done())
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public void AJoinHookThatThrowsRefusesThatClientAndNotTheServer()
    {
        var host = StartHost(false, 0, 0, chunks: 0, $"security:\n  key: {ServerKey.File}\n  game: net/1\n");
        try
        {
            var calls = 0;
            host.ClientJoined += _ =>
            {
                if (calls++ == 0)
                {
                    throw new InvalidOperationException("the game's hook failed for this one client");
                }
            };
            var first = JoinOnce(host, reconnect: false);
            RunUntil(host, first, () => first.Replica.State == ReplicaState.Live, ms: 5_000);
            Assert.NotEqual(ReplicaState.Live, first.Replica.State);

            var second = JoinOnce(host, reconnect: false);
            Assert.True(RunUntil(host, second, () => second.Replica.State == ReplicaState.Live && second.Replica.World.Entities.Count == Players), "the next client could not join");
            Assert.Contains(host.Log.Tail(200), l => l.Message.Contains("join hook failed", StringComparison.Ordinal));
        }
        finally
        {
            host.Stop();
        }
    }

    [Fact]
    public void ASnapshotLargerThanTheSendQueueIsPacedThroughItInsteadOfOverflowing()
    {
        // About 4.8 MB of chunks against a 4 MiB queue: queued all at once this would close the link; paced, it arrives.
        var host = StartHost(false, 0, 0, chunks: 1000, $"security:\n  key: {ServerKey.File}\n  game: net/1\n", sendQueue: 4 << 20);
        try
        {
            var session = JoinOnce(host, reconnect: false);
            Assert.True(RunUntil(host, session, () => session.Replica.State == ReplicaState.Live && session.Replica.World.Entities.Count == 1016), "the snapshot never completed");
            Assert.Null(session.Status().CloseReason);
            Assert.DoesNotContain(host.Log.Tail(200), l => l.Message.Contains("SendQueueFull", StringComparison.Ordinal));
        }
        finally
        {
            host.Stop();
        }
    }

    [Fact]
    public void AClientThatCannotTakeWhatIsQueuedIsClosedWithATypedReasonBothEndsShow()
    {
        // 14 MB against a 4 MiB queue is more than pacing may hold: the link ends with SendQueueFull, said to the client and logged.
        var host = StartHost(false, 0, 0, chunks: 3000, $"security:\n  key: {ServerKey.File}\n  game: net/1\n", sendQueue: 4 << 20);
        try
        {
            var session = JoinOnce(host, reconnect: false);
            Assert.True(RunUntil(host, session, () => session.Status().CloseReason is not null), "the connection was never closed");
            Assert.Equal("SendQueueFull", session.Status().CloseReason);
            for (var i = 0; i < 10; i++)
            {
                host.PumpNetwork();
                host.StepOnce();
            }

            Assert.Contains(host.Log.Tail(200), l => l.Message.Contains("SendQueueFull", StringComparison.Ordinal) && l.Message.Contains("maxSendQueueBytes", StringComparison.Ordinal));
        }
        finally
        {
            host.Stop();
        }
    }

    [Fact]
    public void AListenerTakesItsOwnSendQueueSizeFromTheConfiguration()
    {
        var yaml = "listen:\n  - { transport: udp, port: 0, address: 127.0.0.1, maxSendQueueBytes: 33554432 }\nsecurity: { insecure: true }\n";
        Assert.Equal(33554432, ServerConfig.Parse(yaml).Listen[0].MaxSendQueueBytes);
        Assert.Throws<ServerConfigException>(() => ServerConfig.Parse(yaml.Replace("33554432", "1000", StringComparison.Ordinal)));
    }

    // ---- nothing outside the interest reaches a connection's bytes ---------------------------------------------------

    /// <summary>Keeps every datagram and message a client receives.</summary>
    private sealed class Sniffer(IDatagramSocket inner) : IDatagramSocket
    {
        public List<byte[]> Datagrams { get; } = [];

        public SocketAddress LocalAddress => inner.LocalAddress;

        public void Send(ReadOnlySpan<byte> datagram, SocketAddress destination) => inner.Send(datagram, destination);

        public int Receive(Span<byte> buffer, SocketAddress from)
        {
            var n = inner.Receive(buffer, from);
            if (n > 0)
            {
                Datagrams.Add(buffer[..n].ToArray());
            }

            return n;
        }

        public void Dispose() => inner.Dispose();

        public bool Contains(string text)
        {
            var needle = System.Text.Encoding.UTF8.GetBytes(text);
            return Datagrams.Any(d => d.AsSpan().IndexOf(needle) >= 0);
        }
    }

    private sealed class TapMessages(IMessageChannel inner) : IMessageChannel
    {
        public List<byte[]> Received { get; } = [];

        public void Send(ReadOnlySpan<byte> message) => inner.Send(message);

        public bool TryReceive(out ReadOnlySpan<byte> message)
        {
            if (!inner.TryReceive(out message))
            {
                return false;
            }

            Received.Add(message.ToArray());
            return true;
        }

        public bool Contains(string text)
        {
            var needle = System.Text.Encoding.UTF8.GetBytes(text);
            return Received.Any(m => m.AsSpan().IndexOf(needle) >= 0);
        }
    }

    [Fact]
    public void NothingOutsideTheInterestReachesTheDatagramsOrTheDecryptedStream()
    {
        // Insecure so the datagrams themselves are readable; a second, encrypted client checks the decrypted message stream.
        foreach (var secure in new[] { false, true })
        {
            var security = secure ? $"security:\n  key: {ServerKey.File}\n  game: net/1\n" : "security: { insecure: true }\n";
            Environment.SetEnvironmentVariable("ONEGAME_DEV", secure ? null : "1");
            var host = StartHost(true, creaturesPerPlayer: 2, far: 8, chunks: 160, security);
            try
            {
                var scope = host.Node.FindAuthority(new ScopeId(1))!;
                var port = host.Endpoints.First(e => e.Transport == "udp").Port;
                var remote = new IPEndPoint(IPAddress.Loopback, port);
                var options = new UdpOptions { Security = secure ? NetSecurity.ForClient([ServerKey.Public], "net/1") : NetSecurity.Insecure, MaxSendQueueBytes = 64 << 20 };
                var sniffer = new Sniffer(new SystemDatagramSocket(new IPEndPoint(IPAddress.Loopback, 0)));
                var connection = UdpTransport.Connect(sniffer, remote.Serialize(), options);
                var tap = new TapMessages(connection);
                var node = new Node("raw");
                var link = node.ConnectVia(tap);
                var world = host.CreateReplicaWorld(new ScopeId(1));
                var replica = node.Subscribe(link, new ScopeId(1), world);

                Connection? server = null;
                var follower = InterestWorld.Grid.Follow();
                var everIn = new HashSet<uint>();
                void Step(int n)
                {
                    for (var i = 0; i < n; i++)
                    {
                        if (server is not null)
                        {
                            var body = (Body)scope.World.Entities[0];
                            // Player 0 walks east for good: it crosses cell boundaries, so cells enter and leave.
                            if (follower.Update(body.X >> 4, body.Y >> 4))
                            {
                                scope.SetInterest(server, follower.Keys);
                                foreach (var k in follower.Keys)
                                {
                                    everIn.Add(k);
                                }
                            }
                        }

                        host.StepOnce();
                        for (var k = 0; k < 4; k++)
                        {
                            host.PumpNetwork();
                            connection.Pump();
                            node.Pump();
                        }

                        server ??= host.Node.Connections.Count > 0 ? host.Node.Connections[^1] : null;
                    }
                }

                Step(60);
                Assert.NotNull(server);
                Assert.Equal(ReplicaState.Live, replica.State);

                // The interest is player 0's: its own tag family is visible, and entities far away never are.
                var homeBodies = scope.World.Entities.OfType<Body>().Where(b => b.Tag.StartsWith("<player-", StringComparison.Ordinal) && b.Tag != "<player-0>").ToList();
                var farTags = scope.World.Entities.OfType<Body>().Where(b => b.Tag.StartsWith("<far-", StringComparison.Ordinal)).Select(b => b.Tag).ToList();

                // Walk across several boundaries, ask for out-of-interest targets, and keep stepping.
                var target = homeBodies[0];
                var nudged = target.Seq;
                node.ReplicaScopes[0].World.Entities.OfType<Body>().First().Send.Nudge();
                Step(1);
                for (var i = 0; i < 30; i++)
                {
                    server!.SubmitCommand(new ScopeId(1), target.Id, StateHash.Fnv32("Nudge"), default);
                }

                Step(400);
                var farChunks = scope.World.Entities.OfType<Chunk>().Where(c => !everIn.Contains(c.Cell)).ToList();
                Assert.NotEmpty(farChunks);
                Assert.True(everIn.Count > 25, "the walk should have crossed cell boundaries");

                Assert.Equal(ReplicaState.Live, replica.State);
                var leaks = new List<string>();
                foreach (var tag in farTags.Concat(homeBodies.Select(b => b.Tag)).Concat(farChunks.Select(c => c.Tag)))
                {
                    if (tap.Contains(tag) || (!secure && sniffer.Contains(tag)))
                    {
                        leaks.Add(tag);
                    }
                }

                foreach (var c in farChunks)
                {
                    if (tap.Contains(c.Data[..64]) || (!secure && sniffer.Contains(c.Data[..64])))
                    {
                        leaks.Add("data of " + c.Tag);
                    }
                }

                Assert.Empty(leaks);

                // Positive control: the same search does find what was in interest.
                var seen = scope.World.Entities.OfType<Chunk>().Where(c => everIn.Contains(c.Cell)).ToList();
                Assert.NotEmpty(seen);
                Assert.All(seen, c => Assert.True(tap.Contains(c.Tag) && tap.Contains(c.Data[..64]), c.Tag + " was in interest but never arrived"));
                Assert.True(target.Seq >= nudged, "the far body kept running");
                Assert.True(tap.Contains("<player-0>"), "the client's own body must arrive");
                Assert.True(secure || sniffer.Contains("<player-0>"));
                Assert.True(scope.World.Entities.OfType<Body>().Single(b => b.Tag == "<player-0>").Seq > 0);
                Assert.Equal(scope.ViewHashFor(server!), replica.ViewHash);
                Assert.Empty(node.DriftReports);
                connection.Disconnect();
            }
            finally
            {
                host.Stop();
                Environment.SetEnvironmentVariable("ONEGAME_DEV", null);
            }
        }
    }
}
