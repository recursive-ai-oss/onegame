using OneGame.Core;
using OneGame.Host;
using OneGame.Net;

namespace OneGame.Testing.Tests;

/// <summary>Switching an instance between local play and a server, and back, starts each session fresh while the instance itself carries on.</summary>
public sealed class SwitchConnectionTests
{
    private static GameInstance Start() =>
        GameInstance.Create(new ShopGame(), new GameOptions { Headless = true, Width = 320, Height = 200, Gpu = GpuMode.Off });

    [Fact]
    public void ALocalGameBecomesAClientAndThenALocalGameAgainWithFreshWorlds()
    {
        using var game = Start();
        game.Step(7);
        var started = game.World;
        Assert.Equal(7, game.World.Tick);
        var replaced = 0;
        game.WorldReplaced += _ => replaced++;

        // Nothing listens on this port: the client is a replica waiting for its server, which is all a switch needs to show.
        game.SwitchConnection(new ConnectOptions { Address = "udp://127.0.0.1:9", Name = "second", Security = NetSecurity.Insecure });
        Assert.NotNull(game.Remote);
        Assert.NotSame(started, game.World);
        Assert.Equal(0, game.World.Tick);
        Assert.Equal("udp://127.0.0.1:9", game.Options.Connect!.Address);
        Assert.Throws<ReplicaWriteException>(() => game.Scope);
        Assert.Equal(1, replaced);

        game.SwitchConnection(null);
        Assert.Null(game.Remote);
        Assert.Null(game.Options.Connect);
        Assert.NotSame(started, game.World);
        Assert.Equal(0, game.World.Tick);
        Assert.Equal(2, replaced);

        // The fresh local game plays exactly as the first one did: same seed, same ticks, same hash.
        game.Step(7);
        using var other = Start();
        other.Step(7);
        Assert.Equal(other.World.ComputeHash(), game.World.ComputeHash());
        Assert.Empty(game.DriftReports);
    }

    [Fact]
    public void ASwitchOnADisposedInstanceIsRefused()
    {
        var game = Start();
        game.Dispose();
        Assert.Throws<ObjectDisposedException>(() => game.SwitchConnection(null));
    }
}
