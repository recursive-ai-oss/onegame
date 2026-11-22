namespace OneGame.Sync.Tests;

/// <summary>An entity whose interest key is computed from its state (and moves with the tick), counting how often the sync layer asks.</summary>
[Entity]
public partial class Roamer : Entity
{
    public static long Asked { get; set; }

    [Synced] public partial uint Cell { get; set; }

    [Synced] public partial int Steps { get; set; }

    public override uint InterestKey
    {
        get
        {
            Asked++;
            return Cell;
        }
    }

    // The key moves every few ticks, so publish must read it after the sim.
    protected override void OnTick()
    {
        Steps++;
        if (Steps % 3 == 0)
        {
            Cell = (Cell % 6) + 1;
        }
    }
}

/// <summary>A computed interest key is read once per entity per tick, however many views there are.</summary>
public class ComputedKeyTests
{
    private const int Bodies = 40;

    private static (Cluster Cluster, AuthorityScope Scope, List<Player> Players) Build(int views)
    {
        var registry = new EntityRegistry();
        registry.Add(new Roamer().TypeId, "Roamer", static () => new Roamer());
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var world = new World(5, registry);
        for (var i = 0; i < Bodies; i++)
        {
            world.Add(new Roamer { Cell = (uint)(1 + (i % 6)) });
        }

        var scope = cluster.AddScope(host, new ScopeId(1), world);
        scope.RequireInterest = true;
        var players = new List<Player>();
        for (var v = 0; v < views; v++)
        {
            var node = cluster.AddNode("c" + v);
            var (a, b) = Loopback.CreatePair();
            var link = node.ConnectTo(host, Trust.Player(scope.Id), new TapChannel(a), b, "c" + v);
            link.Connection.SetInterest(scope.Id, [(uint)(1 + (v % 6)), (uint)(1 + ((v + 1) % 6))]);
            players.Add(new Player(node, link, new TapChannel(a), node.Subscribe(link, scope.Id, new World(5, registry, WorldRole.Replica))));
        }

        cluster.Settle();
        return (cluster, scope, players);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(60)]
    public void TheKeyIsReadOncePerEntityPerTickAndEveryViewStaysExact(int views)
    {
        var (cluster, scope, players) = Build(views);
        cluster.Step(10);
        Roamer.Asked = 0;
        for (var t = 0; t < 20; t++)
        {
            cluster.Step(1);
            foreach (var p in players)
            {
                Assert.Equal(scope.ViewHashFor(p.Link.Connection), p.Replica.ViewHash);
            }
        }

        // ViewHashFor above reads keys outside the publish, so count a bare run separately.
        Roamer.Asked = 0;
        cluster.Step(20);
        Assert.True(Roamer.Asked <= 20L * Bodies, $"{Roamer.Asked} reads for 20 ticks of {Bodies} entities and {views} views");
        Assert.Empty(cluster.DriftReports);
    }
}
