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
        // A script file avoids cmd.exe and sh quoting differences for the JSON line. ping -n 4 waits about 3 s on Windows.
        var script = Path.Combine(Path.GetTempPath(), "onegame-slow-start-" + Guid.NewGuid().ToString("N") + (OperatingSystem.IsWindows() ? ".cmd" : ".sh"));
        File.WriteAllText(script, OperatingSystem.IsWindows()
            ? "@echo off\r\necho building\r\nping -n 4 127.0.0.1 >nul\r\necho {\"live\":\"x.sock\"}\r\necho more\r\nping -n 2 127.0.0.1 >nul\r\n"
            : "echo building\nsleep 2.5\necho '{\"live\":\"/x.sock\"}'\necho more\nsleep 0.2\n");
        var info = new ProcessStartInfo(OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        if (OperatingSystem.IsWindows())
        {
            info.ArgumentList.Add("/c");
        }

        info.ArgumentList.Add(script);
        using var child = Process.Start(info)!;
        try
        {
            Assert.True(McpVerb.WaitForListening(child, 20_000));
        }
        finally
        {
            child.Kill(true);
            child.WaitForExit();
            File.Delete(script);
        }
    }
}
