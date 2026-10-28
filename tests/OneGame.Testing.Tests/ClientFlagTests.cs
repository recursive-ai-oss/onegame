using System.Text.Json;
using OneGame.Core;
using OneGame.Host;

namespace OneGame.Testing.Tests;

/// <summary>
/// A game's command line can come from outside (a launch link, a shortcut), so the flags that weaken the link are for development
/// runs only, while pinning a server key, which shipped clients do, always works.
/// </summary>
[Collection("client flags")]
public sealed class ClientFlagTests : IDisposable
{
    private readonly string? _before = Environment.GetEnvironmentVariable("ONEGAME_DEV");

    public void Dispose() => Environment.SetEnvironmentVariable("ONEGAME_DEV", _before);

    private static (int Code, string Out) Run(params string[] args)
    {
        var output = new StringWriter();
        try
        {
            var code = GameApp.Run(new ShopGame(), ["--headless", "--gpu", "off", "--json", "--frames", "1", .. args], output, TextWriter.Null);
            return (code, output.ToString());
        }
        catch (ArgumentOutOfRangeException)
        {
            // The shop game draws a shop that a server with nothing in it does not have. Getting as far as drawing it means the flags were accepted.
            return (-1, output.ToString());
        }
    }

    private static string ErrorOf(string text)
    {
        using var doc = JsonDocument.Parse(text.Trim().Split('\n')[^1]);
        return doc.RootElement.GetProperty("error").GetProperty("code").GetString()!;
    }

    [Fact]
    public void InsecureIsRefusedOnAClientUnlessItIsADevelopmentRun()
    {
        Environment.SetEnvironmentVariable("ONEGAME_DEV", null);
        var (code, text) = Run("--connect", "udp://127.0.0.1:9", "--insecure");
        Assert.Equal(ExitCodes.Usage, code);
        Assert.Equal("bad_args", ErrorOf(text));
        Assert.Contains("--server-key", text, StringComparison.Ordinal);

        Environment.SetEnvironmentVariable("ONEGAME_DEV", "1");
        var (devCode, devText) = Run("--connect", "udp://127.0.0.1:9", "--insecure");
        Assert.NotEqual(ExitCodes.Usage, devCode);
        Assert.DoesNotContain("bad_args", devText, StringComparison.Ordinal);
    }

    [Fact]
    public void PinningAServerKeyNeedsNoDevelopmentRunAndABadKeyIsAUsageError()
    {
        Environment.SetEnvironmentVariable("ONEGAME_DEV", null);
        var key = NetKeyPair.Generate().PublicKey.ToString();
        var (code, text) = Run("--connect", "udp://127.0.0.1:9", "--server-key", key, "--server-id", "eu-1.shop", "--game-id", "shop/1");
        Assert.NotEqual(ExitCodes.Usage, code);
        Assert.DoesNotContain("bad_args", text, StringComparison.Ordinal);

        var (badCode, badText) = Run("--connect", "udp://127.0.0.1:9", "--server-key", "not-a-key");
        Assert.Equal(ExitCodes.Usage, badCode);
        Assert.Equal("bad_args", ErrorOf(badText));
    }

    [Fact]
    public void TheKeyFlagsMeanNothingWithoutAConnectionAndSayWhy()
    {
        foreach (var flag in new[] { "--server-key", "--identity", "--server-id", "--game-id", "--insecure" })
        {
            var (code, text) = flag == "--insecure" ? Run(flag) : Run(flag, "x");
            Assert.Equal(ExitCodes.Usage, code);
            Assert.Contains("--connect", text, StringComparison.Ordinal);
        }
    }
}

[CollectionDefinition("client flags", DisableParallelization = true)]
public sealed class ClientFlagsSerial;
