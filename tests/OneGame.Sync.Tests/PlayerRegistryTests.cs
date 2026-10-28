using OneGame.Core;

namespace OneGame.Sync.Tests;

public class PlayerRegistryTests
{
    private static PeerKey Key(int n)
    {
        var bytes = new byte[32];
        BitConverter.GetBytes(n).CopyTo(bytes, 0);
        return new PeerKey(bytes);
    }

    [Fact]
    public void TheSameKeyIsTheSamePlayerAndIdsStartAtOne()
    {
        var r = new PlayerRegistry();
        Assert.Equal(1u, r.PlayerFor(Key(1), out var isNew));
        Assert.True(isNew);
        Assert.Equal(2u, r.PlayerFor(Key(2), out _));
        Assert.Equal(1u, r.PlayerFor(Key(1), out isNew));
        Assert.False(isNew);
        Assert.Throws<ArgumentException>(() => r.PlayerFor(default, out _));
    }

    // Key spam must not lock real players out: when the registry is full, a new key takes the place of the oldest key nobody activated.
    [Fact]
    public void KeySpamForgetsProvisionalKeysAndNeverTheActiveOnes()
    {
        var r = new PlayerRegistry(maxPlayers: 5);
        var real = new List<uint>();
        for (var i = 0; i < 3; i++)
        {
            var p = r.PlayerFor(Key(i), out _);
            Assert.True(r.Activate(p));
            real.Add(p);
        }

        for (var i = 100; i < 10_000; i++)
        {
            Assert.True(r.TryPlayerFor(Key(i), out _, out var isNew));
            Assert.True(isNew);
            Assert.True(r.Count <= 5);
        }

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(real[i], r.PlayerFor(Key(i), out var isNew));
            Assert.False(isNew);
        }

        // A new, real player still gets in after ten thousand fake ones, and an old fake key is simply a new player when it returns.
        Assert.True(r.TryPlayerFor(Key(50_000), out var fresh, out _));
        Assert.True(r.Activate(fresh));
        Assert.True(r.TryGetPlayer(Key(9_999), out _));
        Assert.False(r.TryGetPlayer(Key(100), out _));
        Assert.Equal(4, r.ActiveCount);
    }

    [Fact]
    public void AFullRegistryOfActivePlayersRefusesNewKeysAndPlayerForSaysSo()
    {
        var r = new PlayerRegistry(maxPlayers: 2);
        _ = r.Activate(r.PlayerFor(Key(1), out _));
        _ = r.Activate(r.PlayerFor(Key(2), out _));
        Assert.False(r.TryPlayerFor(Key(3), out var player, out var isNew));
        Assert.Equal(0u, player);
        Assert.False(isNew);
        Assert.Throws<PlayerRegistryFullException>(() => r.PlayerFor(Key(3), out _));
        Assert.Equal(1u, r.PlayerFor(Key(1), out _)); // a known key is still fine
        Assert.Equal(2, r.Count);
    }

    [Fact]
    public void PlayerForIsBoundedToo()
    {
        var r = new PlayerRegistry(maxPlayers: 10);
        for (var i = 0; i < 1000; i++)
        {
            _ = r.PlayerFor(Key(i), out _);
        }

        Assert.Equal(10, r.Count);
    }

    [Fact]
    public void SaveAndLoadKeepWhoIsActiveAndWhoIsNext()
    {
        var r = new PlayerRegistry(maxPlayers: 4);
        var a = r.PlayerFor(Key(1), out _);
        _ = r.PlayerFor(Key(2), out _);
        _ = r.Activate(a);
        var version = r.Version;
        Assert.True(version > 0);
        Assert.True(PlayerRegistry.TryLoad(r.Save(), out var loaded, 4));
        Assert.True(loaded.IsActive(a));
        Assert.Equal(1, loaded.ActiveCount);
        Assert.Equal(3u, loaded.PlayerFor(Key(3), out _));
        _ = loaded.PlayerFor(Key(4), out _);

        // Full: the provisional key goes first, in the order they were first seen, and the active one stays.
        _ = loaded.PlayerFor(Key(5), out _);
        Assert.True(loaded.TryGetPlayer(Key(1), out _));
        Assert.False(loaded.TryGetPlayer(Key(2), out _));
        Assert.True(loaded.TryGetPlayer(Key(5), out _));
    }

    [Fact]
    public void ADamagedSaveIsRefusedWithoutChangingAnything()
    {
        var r = new PlayerRegistry();
        _ = r.PlayerFor(Key(1), out _);
        var good = r.Save();
        Assert.False(PlayerRegistry.TryLoad(good.AsSpan(0, good.Length - 1), out _));
        var badFlag = (byte[])good.Clone();
        badFlag[^1] = 7;
        Assert.False(PlayerRegistry.TryLoad(badFlag, out _));
        var bigCount = (byte[])good.Clone();
        bigCount[4] = 2;
        Assert.False(PlayerRegistry.TryLoad(bigCount, out _));
        Assert.True(PlayerRegistry.TryLoad(good, out _));
    }

    [Fact]
    public void VersionMovesOnlyWhenWhatIsSavedChanges()
    {
        var r = new PlayerRegistry();
        var p = r.PlayerFor(Key(1), out _);
        var v = r.Version;
        _ = r.PlayerFor(Key(1), out _);
        Assert.Equal(v, r.Version);
        _ = r.Activate(p);
        Assert.NotEqual(v, r.Version);
        v = r.Version;
        _ = r.Activate(p);
        Assert.Equal(v, r.Version);
        Assert.False(r.Activate(999));
    }
}
