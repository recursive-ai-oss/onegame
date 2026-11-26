using System.Diagnostics;
using OneGame.Cli.Verbs;

namespace OneGame.Cli.Tests;

/// <summary>Starting a game under <c>onegame mcp</c> when the game is slow to say it is listening.</summary>
public sealed class McpStartTests
{
    [Fact]
    public void AGameThatTakesSeveralSecondsToListenIsWaitedFor()
    {
        // The wait polls once a second, so a 2.5 s start crosses the timeout twice with one read outstanding.
        var info = new ProcessStartInfo("/bin/sh")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        info.ArgumentList.Add("-c");
        info.ArgumentList.Add("echo building; sleep 2.5; echo '{\"live\":\"/x.sock\"}'; echo more; sleep 0.2");
        using var child = Process.Start(info)!;
        try
        {
            Assert.True(McpVerb.WaitForListening(child, 20_000));
        }
        finally
        {
            child.Kill(true);
        }
    }
}
