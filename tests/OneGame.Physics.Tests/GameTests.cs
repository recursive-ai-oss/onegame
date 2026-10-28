using OneGame.Core;
using OneGame.Sync;
using PhysicsExample;

namespace OneGame.Physics.Tests;

/// <summary>The physics example as a whole game: the same state hashes however it is run, split, restored or replayed, and a replica agrees.</summary>
public class GameTests
{
    private static readonly TestOptions Options = new() { Gpu = OneGame.Host.GpuMode.Off };

    // Walk right (pushing the crates), jump onto the first platform, then the second, and back down.
    private static void Drive(TestClient client, int frame)
    {
        if (frame is >= 20 and < 49)
        {
            client.Send("Hero", "Walk", "1");
        }
        else if (frame == 49)
        {
            client.Send("Hero", "Walk", "0");
        }
        else if (frame == 54)
        {
            client.Send("Hero", "Jump");
        }
        else if (frame == 76)
        {
            client.Send("Hero", "Walk", "1");
        }
        else if (frame == 146)
        {
            client.Send("Hero", "Jump");
        }
        else if (frame == 260)
        {
            client.Send("Hero", "Walk", "-1");
        }
    }

    [Fact]
    public void TwoRunsGiveTheSameHashEveryTick()
    {
        var report = Determinism.RunTwice(() => new PhysicsGame(), Drive, 500, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Theory]
    [InlineData(10)]  // balls falling
    [InlineData(60)]  // mid-jump
    [InlineData(150)] // mid-air between the platforms
    [InlineData(300)] // walking back, bodies asleep
    public void SnapshotAndRestoreGivesTheSameHashes(int split)
    {
        var report = Determinism.SnapshotRestore(() => new PhysicsGame(), Drive, 500, split, Options);
        Assert.True(report.Identical, report.ToString());
    }

    [Fact]
    public void ReplayOfTheRecordingReproducesTheHashes()
    {
        var report = Determinism.Replay(() => new PhysicsGame(), Drive, 500, Options);
        Assert.True(report.Identical, report.ToString());
    }

    private static World NewWorld(WorldRole role, bool setup)
    {
        var game = new PhysicsGame();
        var registry = new EntityRegistry();
        game.RegisterEntities(registry);
        var world = new World(3, registry, role);
        if (setup)
        {
            game.RegisterSystems(world);
        }

        if (setup)
        {
            game.Setup(world);
        }

        return world;
    }

    [Fact]
    public void AReplicaThatNeverHeardOfThePhysicsSpaceStaysInStepWithZeroDrift()
    {
        var cluster = new Cluster(new ClusterOptions { DriftCheckInterval = 1, FatalDrift = true });
        var host = cluster.AddNode("host");
        var scope = cluster.AddScope(host, new ScopeId(1), NewWorld(WorldRole.Authority, true));
        var view = cluster.AddLocalReplica(scope, NewWorld(WorldRole.Replica, false), Trust.Player(scope.Id));
        cluster.Settle();

        var hero = scope.World.Entities.OfType<Hero>().Single();
        for (var t = 0; t < 400; t++)
        {
            if (t == 20)
            {
                hero.Send.Walk(1);
            }

            if (t == 120)
            {
                hero.Send.Jump();
            }

            cluster.Step();
        }

        Assert.Empty(cluster.DriftReports);
        Assert.Empty(cluster.CheckReplicas());
        var crates = view.World.Entities.OfType<Crate>().ToArray();
        Assert.Equal(5, crates.Length);
        Assert.Equal(scope.World.Entities.OfType<Crate>().Select(c => c.Position).ToArray(), crates.Select(c => c.Position).ToArray());
        Assert.Equal(hero.Position, view.World.Entities.OfType<Hero>().Single().Position);
        Assert.Null(PhysicsSpace2D.Of(view.World));
    }

    [Fact]
    public void TheScenariosOfTheExampleExist()
    {
        Assert.True(Directory.Exists(Path.Combine(AppContext.BaseDirectory, "scenarios")));
    }
}
