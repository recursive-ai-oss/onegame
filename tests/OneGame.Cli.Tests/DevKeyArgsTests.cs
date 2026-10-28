using OneGame.Cli.Verbs;
using OneGame.Core;

namespace OneGame.Cli.Tests;

/// <summary>What <c>onegame run --connect</c> hands a game so local play needs no key ceremony, and what it leaves alone.</summary>
[Collection("environment")]
public sealed class DevKeyArgsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "onegame-devkeys-" + Guid.NewGuid().ToString("N"));
    private readonly string? _before = Environment.GetEnvironmentVariable("ONEGAME_KEYS");

    public DevKeyArgsTests() => Environment.SetEnvironmentVariable("ONEGAME_KEYS", _folder);

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ONEGAME_KEYS", _before);
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    [Fact]
    public void AConnectingGameGetsTheServersPublicKeyAndAPlayerKeyInAFolderOutsideTheProject()
    {
        var args = new List<string> { "--connect", "udp://127.0.0.1:7777" };
        DevKeys.AddClientArgs(args);
        var serverKey = args[args.IndexOf("--server-key") + 1];
        var identity = args[args.IndexOf("--identity") + 1];
        Assert.StartsWith(_folder, serverKey, StringComparison.Ordinal);
        Assert.StartsWith(_folder, identity, StringComparison.Ordinal);
        Assert.EndsWith(".pub", serverKey, StringComparison.Ordinal);
        Assert.True(PeerKey.TryParse(File.ReadAllText(serverKey).Trim(), out _), "the pinned file holds a public key only");
        Assert.True(NetKeyPair.Load(identity).IsDevelopment);

        // The same keys again on the next run: the player stays the same player.
        var again = new List<string> { "--connect=udp://127.0.0.1:7777" };
        DevKeys.AddClientArgs(again);
        Assert.Equal(File.ReadAllText(serverKey), File.ReadAllText(again[again.IndexOf("--server-key") + 1]));
        Assert.Equal(NetKeyPair.Load(identity).PublicKey, NetKeyPair.Load(again[again.IndexOf("--identity") + 1]).PublicKey);
    }

    [Fact]
    public void NothingIsAddedWithoutAConnectionOrWithInsecureOrWhenTheCallerGaveItsOwnKeys()
    {
        var local = new List<string> { "--headless" };
        DevKeys.AddClientArgs(local);
        Assert.Equal(["--headless"], local);

        var insecure = new List<string> { "--connect", "udp://x:1", "--insecure" };
        DevKeys.AddClientArgs(insecure);
        Assert.Equal(3, insecure.Count);

        var own = new List<string> { "--connect", "udp://x:1", "--server-key", "k.pub", "--identity", "me.key" };
        DevKeys.AddClientArgs(own);
        Assert.Equal(6, own.Count);
        Assert.False(Directory.Exists(_folder), "no key files were made for a caller that brought its own");
    }
}

[CollectionDefinition("environment", DisableParallelization = true)]
public sealed class EnvironmentSerial;
