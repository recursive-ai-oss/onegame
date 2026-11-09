using System.Diagnostics;
using OneGame.Cli.Verbs;

namespace OneGame.Cli.Tests;

/// <summary>
/// A game built for Windows is a GUI-subsystem program (no console window) unless it asks for the console. The proof is the subsystem
/// field of the program's PE header, read from real builds for <c>win-x64</c>; building the apphost for another OS needs no Windows machine.
/// </summary>
public sealed class WindowsSubsystemTests : IDisposable
{
    private const int GuiSubsystem = 2, ConsoleSubsystem = 3;

    private static readonly string Repo = FindRoot();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "onegame-subsystem-" + Guid.NewGuid().ToString("N")[..10]);

    private static string FindRoot([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));

    public void Dispose()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                Directory.Delete(_root, true);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(250);
            }
        }
    }

    /// <summary>Reads the Subsystem field of the optional header (offset 68 in both PE32 and PE32+), Microsoft PE format, "Optional Header Windows-Specific Fields".</summary>
    internal static int Subsystem(string exe)
    {
        var bytes = File.ReadAllBytes(exe);
        var pe = BitConverter.ToInt32(bytes, 0x3C);
        Assert.Equal("PE\0\0", System.Text.Encoding.ASCII.GetString(bytes, pe, 4));
        return BitConverter.ToUInt16(bytes, pe + 4 + 20 + 68);
    }

    private static (int Exit, string Output) Dotnet(string dir, params string[] args)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = dir, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var a in args)
        {
            start.ArgumentList.Add(a);
        }

        using var p = Process.Start(start)!;
        var output = p.StandardOutput.ReadToEndAsync();
        var error = p.StandardError.ReadToEndAsync();
        Assert.True(p.WaitForExit(600_000), "dotnet did not finish");
        return (p.ExitCode, output.Result + error.Result);
    }

    [Fact]
    public void WindowsBuildsAreGuiByDefaultAndTheOptOutKeepsTheConsole()
    {
        var dir = Path.Combine(_root, "mygame");
        Assert.Equal(0, Program.Run(["new", "mygame", "--dir", dir, "--engine", Repo, "--json"], new StringWriter(), new StringWriter()));
        var restore = Dotnet(dir, "restore", "-r", "win-x64");
        Assert.True(restore.Exit == 0, restore.Output);

        // Evaluation: the property each combination ends up with.
        string Evaluate(params string[] props)
        {
            var args = new List<string> { "msbuild", "-getProperty:OutputType", "-p:RuntimeIdentifier=win-x64" };
            args.AddRange(props);
            var r = Dotnet(dir, [.. args]);
            Assert.True(r.Exit == 0, r.Output);
            return r.Output.Trim();
        }

        Assert.Equal("WinExe", Evaluate("-p:Configuration=Release"));
        Assert.Equal("Exe", Evaluate("-p:Configuration=Release", "-p:OneGameConsole=true"));
        Assert.Equal("Exe", Evaluate("-p:Configuration=Debug"));
        Assert.Equal("WinExe", Evaluate("-p:Configuration=Debug", "-p:OneGameConsole=false"));

        // Real builds, read back from the PE header.
        var gui = Dotnet(dir, "build", "-r", "win-x64", "--no-self-contained", "-p:OneGameConsole=false", "-o", Path.Combine(_root, "gui"));
        Assert.True(gui.Exit == 0, gui.Output);
        Assert.Equal(GuiSubsystem, Subsystem(Path.Combine(_root, "gui", "Mygame.exe")));

        var console = Dotnet(dir, "build", "-r", "win-x64", "--no-self-contained", "-p:OneGameConsole=true", "-o", Path.Combine(_root, "console"));
        Assert.True(console.Exit == 0, console.Output);
        Assert.Equal(ConsoleSubsystem, Subsystem(Path.Combine(_root, "console", "Mygame.exe")));
    }

    [Fact]
    public void NonWindowsTargetsAreNeverTouched()
    {
        var dir = Path.Combine(_root, "linuxgame");
        Assert.Equal(0, Program.Run(["new", "linuxgame", "--dir", dir, "--engine", Repo, "--json"], new StringWriter(), new StringWriter()));
        var restore = Dotnet(dir, "restore", "-r", "linux-x64");
        Assert.True(restore.Exit == 0, restore.Output);
        var r = Dotnet(dir, "msbuild", "-getProperty:OutputType", "-p:RuntimeIdentifier=linux-x64", "-p:Configuration=Release", "-p:OneGameConsole=false");
        Assert.Equal("Exe", r.Output.Trim());
    }
}
