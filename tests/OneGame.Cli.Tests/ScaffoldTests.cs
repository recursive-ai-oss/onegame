using System.Diagnostics;
using System.Text.Json;
using OneGame.Cli.Verbs;
using OneGame.Control;
using OneGame.Host;
using OneGame.Testing;

namespace OneGame.Cli.Tests;

/// <summary>The game <c>onegame new</c> writes, built and run as a user would: edit its picture while it runs and the next frame shows it.</summary>
public sealed class ScaffoldTests : IDisposable
{
    private static readonly string Repo = FindRoot();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "onegame-scaffold-" + Guid.NewGuid().ToString("N")[..10]);

    private static string FindRoot([System.Runtime.CompilerServices.CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));

    public void Dispose()
    {
        // Windows refuses to delete a program that is still loaded, and a build server may hold files a moment longer. Retry briefly,
        // then leave the folder in the temp directory: cleanup never fails a test.
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

    [Fact]
    public void EditingTheShipPictureWhileTheGameRunsChangesTheNextCapture()
    {
        Assert.SkipUnless(OneGame.Gpu.GpuBackends.IsAvailable, "needs a Vulkan device (lavapipe is enough)");
        var dir = Path.Combine(_root, "mygame");
        Assert.Equal(0, Program.Run(["new", "mygame", "--dir", dir, "--engine", Repo, "--json"], new StringWriter(), new StringWriter()));
        var build = Program.Run(["build", "--project", dir, "--json"], new StringWriter(), new StringWriter());
        Assert.Equal(0, build);

        var dll = Directory.EnumerateFiles(dir, "Mygame.dll", SearchOption.AllDirectories).First(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        // A Unix socket path is limited to about 100 characters; the temp folder may be longer than that.
        var socket = Path.Combine(OperatingSystem.IsWindows() ? Path.GetTempPath() : "/tmp", "og-" + Guid.NewGuid().ToString("N")[..8] + ".sock");
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = dir, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var a in new[] { dll, "--headless", "--live", socket, "--size", "640x360" })
        {
            start.ArgumentList.Add(a);
        }

        using var game = Process.Start(start)!;
        var log = new System.Text.StringBuilder();
        game.OutputDataReceived += (_, e) => log.AppendLine(e.Data);
        game.ErrorDataReceived += (_, e) => log.AppendLine(e.Data);
        game.BeginOutputReadLine();
        game.BeginErrorReadLine();
        try
        {
            for (var i = 0; i < 300 && !File.Exists(socket); i++)
            {
                Thread.Sleep(100);
            }

            using var live = new LiveClient(socket);
            live.Call("step", """{"frames":5}""");
            live.Call("capture_frame", """{"size":"640x360","path":"before.png"}""");
            var before = Png.Load(Path.Combine(dir, "artifacts", "before.png"));

            // Edit the picture as an artist would: write a new PNG over art/ship.png, with its colours inverted.
            var file = Path.Combine(dir, "art", "ship.png");
            var ship = Png.Load(file);
            var inverted = (byte[])ship.Rgba.Clone();
            for (var i = 0; i < inverted.Length; i += 4)
            {
                inverted[i] = (byte)(255 - inverted[i]);
                inverted[i + 1] = (byte)(255 - inverted[i + 1]);
                inverted[i + 2] = (byte)(255 - inverted[i + 2]);
            }

            File.WriteAllBytes(file, Png.Encode(new CapturedFrame(ship.Width, ship.Height, inverted, "test")));
            File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddSeconds(5));

            // The next capture checks the files first; no step, no restart.
            live.Call("capture_frame", """{"size":"640x360","path":"after.png"}""");
            var after = Png.Load(Path.Combine(dir, "artifacts", "after.png"));
            var changed = 0;
            for (var i = 0; i < before.Rgba.Length; i += 4)
            {
                changed += before.Rgba[i] != after.Rgba[i] || before.Rgba[i + 1] != after.Rgba[i + 1] || before.Rgba[i + 2] != after.Rgba[i + 2] ? 1 : 0;
            }

            Assert.InRange(changed, 300, 4000);

            using var assets = JsonDocument.Parse(live.Call("assets", "{}"));
            var entry = assets.RootElement.GetProperty("assets").EnumerateArray().Single(a => a.GetProperty("path").GetString() == "art/ship.png");
            Assert.Equal(2, entry.GetProperty("version").GetInt32());
            Assert.Equal("ready", entry.GetProperty("state").GetString());
        }
        catch (ControlException ex)
        {
            throw new InvalidOperationException(ex.Message + "\n" + log, ex);
        }
        finally
        {
            File.Delete(socket);
            if (!game.HasExited)
            {
                game.Kill(true);
            }

            game.WaitForExit(10_000);
        }
    }
}
