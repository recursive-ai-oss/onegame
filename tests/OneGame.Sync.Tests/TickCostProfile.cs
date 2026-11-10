using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace OneGame.Sync.Tests;

/// <summary>A terrain chunk: mostly idle, about 4.8 KB of synced state.</summary>
[Entity]
public partial class ProfileChunk : Entity
{
    [Synced] public partial int X { get; set; }

    [Synced] public partial int Z { get; set; }

    [Synced] public partial int Revision { get; set; }

    /// <summary>Cells, one varint byte each when small; sized so the whole entity is near 4.8 KB.</summary>
    [Synced] public SyncList<int> Cells { get; } = new();
}

/// <summary>A body that moves every tick, one per subscriber.</summary>
[Entity]
public partial class ProfileMover : Entity
{
    [Synced, OwnerKey] public partial uint Player { get; set; }

    [Synced] public partial FVec2 Position { get; set; }

    protected override void OnTick() => Position += new FVec2(1, 1);
}

/// <summary>
/// Opt-in measurement of the authority's and a replica's per-tick cost against entity count (set <c>ONEGAME_SYNC_PROFILE=1</c>).
/// One authority scope, V in-process subscribers over loopback channels, mostly idle chunks, a few chunks edited per tick.
/// Knobs: <c>ONEGAME_SYNC_PROFILE_N</c> (comma list of chunk counts), <c>_V</c> (subscribers), <c>_SHARED</c> (1 = all subscribers share one audience),
/// <c>_TICKS</c>, <c>_EDITS</c> (chunks edited per tick), <c>_HASH</c> (drift check interval, 0 = off), <c>_CELLS</c> (cells per chunk).
/// Prints one line per run to stdout and to <c>ONEGAME_SYNC_PROFILE_OUT</c> if set.
/// </summary>
public class TickCostProfile
{
    private static int Env(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable("ONEGAME_SYNC_PROFILE_" + name), CultureInfo.InvariantCulture, out var v) ? v : fallback;

    [Fact]
    public void MeasureTickCost()
    {
        if (Environment.GetEnvironmentVariable("ONEGAME_SYNC_PROFILE") != "1")
        {
            return;
        }

        var counts = (Environment.GetEnvironmentVariable("ONEGAME_SYNC_PROFILE_N") ?? "15,1500,5000")
            .Split(',').Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        var views = Env("V", 16);
        var shared = Env("SHARED", 0) == 1;
        var ticks = Env("TICKS", 200);
        var edits = Env("EDITS", 2);
        var hash = Env("HASH", 30);
        var cells = Env("CELLS", 1200);
        var outPath = Environment.GetEnvironmentVariable("ONEGAME_SYNC_PROFILE_OUT");
        foreach (var n in counts)
        {
            var line = Run(n, views, shared, ticks, edits, hash, cells);
            Console.WriteLine(line);
            if (outPath is not null)
            {
                File.AppendAllText(outPath, line + "\n");
            }
        }
    }

    private static EntityRegistry Registry()
    {
        var registry = new EntityRegistry();
        registry.Add(new ProfileChunk().TypeId, "ProfileChunk", static () => new ProfileChunk());
        registry.Add(new ProfileMover().TypeId, "ProfileMover", static () => new ProfileMover());
        return registry;
    }

    private static string Run(int chunks, int views, bool shared, int ticks, int edits, int hash, int cells)
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = hash, FatalDrift = false });
        var host = cluster.AddNode("host");
        var world = new World(7, Registry(), WorldRole.Authority);
        var chunkList = new List<ProfileChunk>(chunks);
        for (var i = 0; i < chunks; i++)
        {
            var c = new ProfileChunk { X = i % 100, Z = i / 100 };
            for (var k = 0; k < cells; k++)
            {
                c.Cells.Add((1 << 22) + ((i + k) & 63));
            }

            world.Add(c);
            chunkList.Add(c);
        }

        for (var p = 0; p < views; p++)
        {
            world.Add(new ProfileMover { Player = (uint)(p + 1) });
        }

        var scope = cluster.AddScope(host, new ScopeId(1), world);
        var replicas = new List<(Node Node, ReplicaScope Replica)>();
        for (var p = 0; p < views; p++)
        {
            var node = cluster.AddNode("c" + p);
            var trust = Trust.Reader(scope.Id).As(shared ? 1u : (uint)(p + 1));
            var link = node.ConnectTo(host, trust, "c" + p);
            var replica = node.Subscribe(link, scope.Id, new World(7, Registry(), WorldRole.Replica));
            replicas.Add((node, replica));
        }

        cluster.Step(3); // snapshots out and applied
        var snapshotBytes = 0;
        var entityBytes = new SyncWriter(8192);
        chunkList[0].WriteState(entityBytes);
        snapshotBytes = entityBytes.Written.Length;

        // Per-phase accumulators (ticks), plus a split between hash ticks and plain ticks for publish.
        long pump1 = 0, step = 0, publishPlain = 0, publishHash = 0, pump2Clients = 0, edit = 0, plainTicks = 0, hashTicks = 0;
        var clientHashTime = 0L;
        long publishAlloc = 0, clientAlloc = 0;
        var rng = new Random(1);
        var alloc0 = GC.GetAllocatedBytesForCurrentThread();
        var gc0 = GC.CollectionCount(0);
        var warm = Math.Min(20, ticks / 4);
        for (var t = 0; t < ticks + warm; t++)
        {
            var measure = t >= warm;
            var s0 = Stopwatch.GetTimestamp();
            for (var e = 0; e < edits && chunks > 0; e++)
            {
                var c = chunkList[rng.Next(chunks)];
                c.Revision++;
                c.Cells[rng.Next(cells)] = (1 << 22) + rng.Next(64);
            }

            var s1 = Stopwatch.GetTimestamp();
            host.Pump();
            var s2 = Stopwatch.GetTimestamp();
            world.Step();
            var s3 = Stopwatch.GetTimestamp();
            var a3 = GC.GetAllocatedBytesForCurrentThread();
            scope.Publish();
            var a4 = GC.GetAllocatedBytesForCurrentThread();
            var s4 = Stopwatch.GetTimestamp();
            foreach (var (node, _) in replicas)
            {
                node.Pump();
            }

            var s5 = Stopwatch.GetTimestamp();
            var a5 = GC.GetAllocatedBytesForCurrentThread();
            if (!measure)
            {
                continue;
            }

            publishAlloc += a4 - a3;
            clientAlloc += a5 - a4;
            var isHash = hash > 0 && world.Tick % hash == 0;
            edit += s1 - s0;
            pump1 += s2 - s1;
            step += s3 - s2;
            if (isHash)
            {
                publishHash += s4 - s3;
                hashTicks++;
            }
            else
            {
                publishPlain += s4 - s3;
                plainTicks++;
            }

            pump2Clients += s5 - s4;
            if (isHash)
            {
                clientHashTime += s5 - s4;
            }
        }

        var allocPerTick = (GC.GetAllocatedBytesForCurrentThread() - alloc0) / (ticks + warm);
        var gcs = GC.CollectionCount(0) - gc0;
        var drift = cluster.DriftReports.Count;
        double Ms(long ticksDone, long count) => count == 0 ? 0 : ticksDone * 1000.0 / Stopwatch.Frequency / count;
        var perTick = ticks;
        // Client time is the sum over V clients; report per client.
        var clientHashMs = Ms(clientHashTime, hashTicks) / views;
        var clientPlainMs = Ms(pump2Clients - clientHashTime, plainTicks) / views;
        return string.Create(CultureInfo.InvariantCulture,
            $"{{\"chunks\":{chunks},\"views\":{views},\"shared\":{shared.ToString().ToLowerInvariant()},\"entityBytes\":{snapshotBytes},\"edits\":{edits},\"hashInterval\":{hash},\"ticks\":{ticks}," +
            $"\"ms\":{{\"sim_step\":{Ms(step, perTick):F3},\"publish_plain\":{Ms(publishPlain, plainTicks):F3},\"publish_hash\":{Ms(publishHash, hashTicks):F3},\"host_pump\":{Ms(pump1, perTick):F3},\"client_plain_each\":{clientPlainMs:F3},\"client_hash_each\":{clientHashMs:F3}}}," +
            $"\"allocBytesPerTick\":{allocPerTick},\"publishAllocPerTick\":{publishAlloc / ticks},\"clientsAllocPerTick\":{clientAlloc / ticks},\"gen0\":{gcs},\"drift\":{drift}}}");
    }
}
