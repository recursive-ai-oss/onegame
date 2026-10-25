using System.Diagnostics;

namespace OneGame.Platform.Tests;

/// <summary>
/// A headless Weston compositor for Wayland tests. It uses the weston on PATH, or the tree named by ONEGAME_TEST_WESTON (an extracted
/// package with usr/bin/weston). Tests skip themselves when neither exists.
/// </summary>
public sealed class WestonFixture : IDisposable
{
    private readonly Process? _process;
    private readonly string? _runtimeDir;

    /// <summary>Stops (true) or resumes (false) the compositor process, to see how a client behaves when its window system stops answering.</summary>
    public void Freeze(bool frozen)
    {
        if (_process is null)
        {
            return;
        }

        using var kill = Process.Start(new ProcessStartInfo("/bin/kill", $"-{(frozen ? "STOP" : "CONT")} {_process.Id}") { UseShellExecute = false })!;
        kill.WaitForExit();
    }

    public WestonFixture()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var root = Environment.GetEnvironmentVariable("ONEGAME_TEST_WESTON");
        string exe;
        var start = new ProcessStartInfo { UseShellExecute = false };
        if (!string.IsNullOrEmpty(root) && File.Exists(Path.Combine(root, "usr/bin/weston")))
        {
            exe = Path.Combine(root, "usr/bin/weston");
            var lib = Path.Combine(root, "usr/lib/x86_64-linux-gnu");
            start.Environment["LD_LIBRARY_PATH"] = lib + ":" + Path.Combine(lib, "weston");

            // The packaged binary looks for its modules in /usr/lib; point it at the extracted copies.
            start.Environment["WESTON_MODULE_MAP"] = string.Join(
                ';',
                $"headless-backend.so={lib}/libweston-14/headless-backend.so",
                $"desktop-shell.so={lib}/weston/desktop-shell.so",
                $"kiosk-shell.so={lib}/weston/kiosk-shell.so");
        }
        else if (File.Exists("/usr/bin/weston"))
        {
            exe = "/usr/bin/weston";
        }
        else
        {
            return;
        }

        _runtimeDir = Path.Combine(Path.GetTempPath(), "onegame-weston-" + Environment.ProcessId + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_runtimeDir);
        File.SetUnixFileMode(_runtimeDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        start.Environment["XDG_RUNTIME_DIR"] = _runtimeDir;
        start.FileName = exe;
        var socket = "og-test-" + Environment.ProcessId + "-" + Path.GetFileName(_runtimeDir)[^8..];
        foreach (var a in new[] { "--backend=headless", "--renderer=pixman", "--socket=" + socket, "--width=1024", "--height=768", "--no-config", "--shell=" + (Environment.GetEnvironmentVariable("ONEGAME_TEST_WESTON_SHELL") ?? "kiosk") })
        {
            start.ArgumentList.Add(a);
        }

        _process = Process.Start(start);
        var path = Path.Combine(_runtimeDir, socket);
        for (var i = 0; i < 100 && !File.Exists(path); i++)
        {
            Thread.Sleep(50);
        }

        if (File.Exists(path) && _process is { HasExited: false })
        {
            SocketPath = path;
        }
    }

    /// <summary>The absolute path of the compositor's socket, or null when no compositor could be started.</summary>
    public string? SocketPath { get; }

    public bool Available => SocketPath is not null;

    public void Dispose()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill();
            _process.WaitForExit(3000);
        }

        _process?.Dispose();
        if (_runtimeDir is not null)
        {
            try
            {
                Directory.Delete(_runtimeDir, true);
            }
            catch (IOException)
            {
            }
        }
    }
}
