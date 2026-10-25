using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>One simulator in <c>onegame ios devices</c>.</summary>
internal sealed record IosDevice(string Udid, string Name, string State, string Runtime);

/// <summary>Result of <c>onegame ios run</c> and <c>test</c>.</summary>
/// <param name="Ok">The game ran to its end and exited with 0.</param>
/// <param name="ExitCode">The game's exit code, or -1 when it did not finish in time.</param>
/// <param name="BundleId">The app that ran.</param>
/// <param name="Device">The simulator's identifier.</param>
/// <param name="TimedOut">True when the game had not finished after the time limit.</param>
/// <param name="Crashed">True when the app's process ended without writing an exit code. <c>Output</c> then ends with the crash report's head.</param>
/// <param name="Result">The game's own JSON result (its last line of output when that is JSON), when it printed one.</param>
/// <param name="Output">Everything the game wrote to standard output and error.</param>
/// <param name="Pulled">Files copied back from the app's data folder.</param>
/// <param name="Seconds">How long the run took.</param>
internal sealed record IosRunResult(bool Ok, int ExitCode, string BundleId, string Device, bool TimedOut, bool Crashed, JsonElement? Result, string Output, string[] Pulled, double Seconds);

/// <summary>Result of the smaller <c>onegame ios</c> commands.</summary>
internal sealed record IosInfoResult(bool Ok, string Device, string? BundleId, string? Detail, string? Endpoint, string? Next);

/// <summary>Installs, starts, drives and inspects a game in the iOS Simulator through <c>xcrun simctl</c>. Runs on a Mac with Xcode.</summary>
internal sealed class IosVerb : IVerb
{
    public string Name => "ios";

    public string Summary => "Use the iOS Simulator (on a Mac): ios devices | install <app> | run [-- game options] | test [--scenario dir] | live | stop | logs | background | foreground | screenshot | shutdown. Common: --device, --bundle, --app, --timeout S, --push local[=dest], --pull remote[=local].";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        if (args.Length == 0)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "Usage: onegame ios devices|install|run|test|live|stop|logs|background|foreground|screenshot|shutdown. See 'onegame help'.");
        }

        if (!OperatingSystem.IsMacOS())
        {
            throw new CliException(ExitCodes.RuntimeError, "ios_host", "The iOS Simulator runs on a Mac with Xcode.");
        }

        var rest = new List<string>();
        var pushes = new List<string>();
        var pulls = new List<string>();
        var tail = Array.IndexOf(args, "--");
        for (var i = 1; i < args.Length; i++)
        {
            if ((tail < 0 || i < tail) && args[i] is "--push" or "--pull" && i + 1 < args.Length)
            {
                (args[i] == "--push" ? pushes : pulls).Add(args[++i]);
            }
            else
            {
                rest.Add(args[i]);
            }
        }

        var a = new ProjectArgs([.. rest], "--device", "--bundle", "--app", "--timeout", "--scenario", "--port", "--out");
        return args[0] switch
        {
            "devices" => Devices(json, output),
            "install" => Install(a, json, output),
            "run" => RunGame(a, json, output, test: false, pushes, pulls),
            "test" => RunGame(a, json, output, test: true, pushes, pulls),
            "live" => Live(a, json, output),
            "stop" => Stop(a, json, output),
            "logs" => Logs(a, output),
            "background" => Background(a, json, output),
            "foreground" => Foreground(a, json, output),
            "screenshot" => Screenshot(a, json, output),
            "shutdown" => Shutdown(a, json, output),
            var other => throw new CliException(ExitCodes.Usage, "bad_args", $"Unknown ios command '{other}'. Commands: devices, install, run, test, live, stop, logs, background, foreground, screenshot, shutdown."),
        };
    }

    private static ProcessResult Simctl(params string[] args) => Proc.Run("xcrun", ["simctl", .. args], Directory.GetCurrentDirectory());

    private static List<IosDevice> ListDevices()
    {
        var run = Simctl("list", "devices", "available", "-j");
        if (run.ExitCode != 0)
        {
            throw new CliException(ExitCodes.RuntimeError, "simctl_failed", $"xcrun simctl failed: {(run.Output + run.Error).Trim()}. Is Xcode installed and selected (xcode-select -p)?");
        }

        var list = new List<IosDevice>();
        using var doc = JsonDocument.Parse(run.Output);
        foreach (var runtime in doc.RootElement.GetProperty("devices").EnumerateObject())
        {
            if (!runtime.Name.Contains("iOS", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var d in runtime.Value.EnumerateArray())
            {
                if (d.TryGetProperty("isAvailable", out var ok) && ok.ValueKind == JsonValueKind.False)
                {
                    continue;
                }

                list.Add(new IosDevice(d.GetProperty("udid").GetString() ?? "", d.GetProperty("name").GetString() ?? "", d.GetProperty("state").GetString() ?? "", runtime.Name[(runtime.Name.LastIndexOf('.') + 1)..]));
            }
        }

        return list;
    }

    // The device named by --device (identifier or name) or ONEGAME_IOS_DEVICE; else a booted one; else the first available.
    private static IosDevice Resolve(ProjectArgs a, bool boot)
    {
        var devices = ListDevices();
        var want = a.Get("--device") ?? Environment.GetEnvironmentVariable("ONEGAME_IOS_DEVICE");
        var device = want is not null
            ? devices.FirstOrDefault(d => d.Udid.Equals(want, StringComparison.OrdinalIgnoreCase) || d.Name.Equals(want, StringComparison.OrdinalIgnoreCase))
                ?? throw new CliException(ExitCodes.Usage, "no_device", $"No simulator '{want}'. See: onegame ios devices.")
            : devices.FirstOrDefault(d => d.State == "Booted") ?? devices.FirstOrDefault()
                ?? throw new CliException(ExitCodes.RuntimeError, "no_device", "No iOS simulator is available. Install an iOS runtime: xcodebuild -downloadPlatform iOS (or Xcode, Settings, Components).");
        if (boot && device.State != "Booted")
        {
            var run = Simctl("boot", device.Udid);
            if (run.ExitCode != 0 && !(run.Output + run.Error).Contains("current state: Booted", StringComparison.Ordinal))
            {
                throw new CliException(ExitCodes.RuntimeError, "boot_failed", $"Could not boot {device.Name}: {(run.Output + run.Error).Trim()}");
            }

            // Waits for the system to finish starting (the first boot of a device migrates its data and takes a minute).
            Simctl("bootstatus", device.Udid);
            device = device with { State = "Booted" };
        }

        return device;
    }

    private static int Devices(bool json, TextWriter output)
    {
        var list = ListDevices();
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(list.ToArray(), CliJson.Default.IosDeviceArray));
        }
        else
        {
            foreach (var d in list)
            {
                output.WriteLine($"{d.Udid}\t{d.State}\t{d.Name}\t{d.Runtime}");
            }

            if (list.Count == 0)
            {
                output.WriteLine("no simulators. Install an iOS runtime: xcodebuild -downloadPlatform iOS");
            }
        }

        return list.Count > 0 ? ExitCodes.Ok : ExitCodes.RuntimeError;
    }

    private static string BundleIdOf(string app)
    {
        var run = Proc.Run("plutil", ["-extract", "CFBundleIdentifier", "raw", "-o", "-", Path.Combine(app, "Info.plist")], Directory.GetCurrentDirectory());
        return run.ExitCode == 0 && run.Output.Trim() is { Length: > 0 } id
            ? id
            : throw new CliException(ExitCodes.RuntimeError, "bad_app", $"{app} has no readable Info.plist with a bundle identifier: {(run.Output + run.Error).Trim()}");
    }

    private static string InstallApp(IosDevice device, string app)
    {
        app = Path.GetFullPath(app).TrimEnd('/');
        if (!Directory.Exists(app) || !File.Exists(Path.Combine(app, "Info.plist")))
        {
            throw new CliException(ExitCodes.Usage, "no_app", $"{app} is not an app bundle (a folder ending in .app with an Info.plist).");
        }

        var bundle = BundleIdOf(app);
        var run = Simctl("install", device.Udid, app);
        if (run.ExitCode != 0)
        {
            throw new CliException(ExitCodes.RuntimeError, "install_failed", $"simctl install failed: {(run.Output + run.Error).Trim()}");
        }

        return bundle;
    }

    private static int Install(ProjectArgs a, bool json, TextWriter output)
    {
        var app = a.Game.FirstOrDefault(x => !x.StartsWith("--", StringComparison.Ordinal)) ?? a.Get("--app")
            ?? throw new CliException(ExitCodes.Usage, "bad_args", "Name the app: onegame ios install <file.app>.");
        var device = Resolve(a, boot: true);
        var bundle = InstallApp(device, app);
        Info(json, output, new IosInfoResult(true, device.Udid, bundle, "installed", null, "onegame ios run --bundle " + bundle));
        return ExitCodes.Ok;
    }

    private static void Info(bool json, TextWriter output, IosInfoResult result)
    {
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(result, CliJson.Default.IosInfoResult));
        }
        else
        {
            output.WriteLine(string.Join(' ', new[] { result.Detail, result.BundleId, result.Endpoint is null ? null : "listening: " + result.Endpoint, result.Next is null ? null : "next: " + result.Next }.Where(s => s is not null)));
        }
    }

    // --app installs it first; otherwise --bundle names an installed app.
    private static string ResolveBundle(ProjectArgs a, IosDevice device)
    {
        if (a.Get("--app") is { } app)
        {
            return InstallApp(device, app);
        }

        return a.Get("--bundle") ?? throw new CliException(ExitCodes.Usage, "bad_args", "Name the app: --bundle <id>, or --app <file.app> to install and run it.");
    }

    private static string Container(IosDevice device, string bundle, string kind)
    {
        var run = Simctl("get_app_container", device.Udid, bundle, kind);
        return run.ExitCode == 0 && run.Output.Trim() is { Length: > 0 } path
            ? path
            : throw new CliException(ExitCodes.RuntimeError, "not_installed", $"{bundle} is not installed on {device.Name}: install it with onegame ios install <file.app> ({(run.Output + run.Error).Trim()}).");
    }

    private static bool IsRunning(IosDevice device, string bundle) =>
        Simctl("spawn", device.Udid, "launchctl", "list").Output.Contains("UIKitApplication:" + bundle, StringComparison.Ordinal);

    private static int Stop(ProjectArgs a, bool json, TextWriter output)
    {
        var device = Resolve(a, boot: false);
        var bundle = ResolveBundle(a, device);
        Simctl("terminate", device.Udid, bundle);
        Info(json, output, new IosInfoResult(true, device.Udid, bundle, "stopped", null, null));
        return ExitCodes.Ok;
    }

    private static int Background(ProjectArgs a, bool json, TextWriter output)
    {
        // Opening another app (Settings) puts this one in the background the way the home button does.
        var device = Resolve(a, boot: false);
        var run = Simctl("launch", device.Udid, "com.apple.Preferences");
        if (run.ExitCode != 0)
        {
            throw new CliException(ExitCodes.RuntimeError, "launch_failed", $"Could not open Settings: {(run.Output + run.Error).Trim()}");
        }

        Info(json, output, new IosInfoResult(true, device.Udid, a.Get("--bundle"), "in the background", null, "onegame ios foreground --bundle <id>"));
        return ExitCodes.Ok;
    }

    private static int Foreground(ProjectArgs a, bool json, TextWriter output)
    {
        // Launching a running app without arguments brings it to the front.
        var device = Resolve(a, boot: false);
        var bundle = ResolveBundle(a, device);
        var run = Simctl("launch", device.Udid, bundle);
        if (run.ExitCode != 0)
        {
            throw new CliException(ExitCodes.RuntimeError, "launch_failed", $"Could not bring {bundle} forward: {(run.Output + run.Error).Trim()}");
        }

        Info(json, output, new IosInfoResult(true, device.Udid, bundle, "in the foreground", null, null));
        return ExitCodes.Ok;
    }

    private static int Screenshot(ProjectArgs a, bool json, TextWriter output)
    {
        var device = Resolve(a, boot: false);
        var path = Path.GetFullPath(a.Get("--out") ?? Path.Combine(Directory.GetCurrentDirectory(), "ios-screenshot.png"));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var run = Simctl("io", device.Udid, "screenshot", path);
        if (run.ExitCode != 0)
        {
            throw new CliException(ExitCodes.RuntimeError, "screenshot_failed", $"simctl io screenshot failed: {(run.Output + run.Error).Trim()}");
        }

        Info(json, output, new IosInfoResult(true, device.Udid, null, path, null, null));
        return ExitCodes.Ok;
    }

    private static int Shutdown(ProjectArgs a, bool json, TextWriter output)
    {
        var device = Resolve(a, boot: false);
        Simctl("shutdown", device.Udid);
        Info(json, output, new IosInfoResult(true, device.Udid, null, "shut down", null, null));
        return ExitCodes.Ok;
    }

    private static int Logs(ProjectArgs a, TextWriter output)
    {
        var device = Resolve(a, boot: false);
        var bundle = ResolveBundle(a, device);
        var docs = Path.Combine(Container(device, bundle, "data"), "Documents", "onegame-stdout.txt");
        if (File.Exists(docs))
        {
            output.Write(File.ReadAllText(docs));
        }

        return ExitCodes.Ok;
    }

    private static int RunGame(ProjectArgs a, bool json, TextWriter output, bool test, List<string> pushes, List<string> pulls)
    {
        var clock = Stopwatch.StartNew();
        var device = Resolve(a, boot: true);
        var bundle = ResolveBundle(a, device);
        var data = Container(device, bundle, "data");
        var docs = Path.Combine(data, "Documents");
        var timeout = int.TryParse(a.Get("--timeout"), out var t) && t > 0 ? t : 120;
        var gameArgs = new List<string>(a.Game);
        if (test)
        {
            // Scenarios run in the app with the same runner `onegame test` uses on a computer; references and snapshots are read from the folder they live in.
            var scenario = a.Get("--scenario") ?? Path.Combine(Directory.GetCurrentDirectory(), "scenarios");
            var folder = File.Exists(scenario) ? Path.GetDirectoryName(Path.GetFullPath(scenario))! : Path.GetFullPath(scenario);
            if (!Directory.Exists(folder))
            {
                throw new CliException(ExitCodes.Usage, "no_scenario", $"No scenarios at {scenario}.");
            }

            pushes.Add(folder + "=scenarios");
            if (pulls.Count == 0)
            {
                pulls.Add("artifacts");
            }

            var target = File.Exists(scenario) ? "@files/scenarios/" + Path.GetFileName(scenario) : "@files/scenarios";
            var lead = new List<string> { "--scenario", target, "--artifacts", "@files/artifacts", "--json" };
            if (!gameArgs.Contains("--windowed"))
            {
                lead.Insert(0, "--headless");
            }

            gameArgs.Remove("--windowed");
            gameArgs.InsertRange(0, lead);
        }

        Simctl("terminate", device.Udid, bundle);
        Directory.CreateDirectory(docs);
        TryDelete(Path.Combine(docs, "onegame-exit.txt"));
        TryDelete(Path.Combine(docs, "onegame-stdout.txt"));
        if (Directory.Exists(Path.Combine(docs, "artifacts")))
        {
            Directory.Delete(Path.Combine(docs, "artifacts"), true);
        }

        foreach (var push in pushes)
        {
            Push(docs, push);
        }

        var started = DateTime.UtcNow;
        var launch = Simctl(["launch", device.Udid, bundle, .. gameArgs.Select(g => g.Replace("@files", docs, StringComparison.Ordinal))]);
        if (launch.ExitCode != 0)
        {
            throw new CliException(ExitCodes.RuntimeError, "start_failed", $"The app did not start: {(launch.Output + launch.Error).Trim()}");
        }

        var exit = -1;
        var timedOut = true;
        var crashed = false;
        var deadline = Stopwatch.StartNew();
        var gone = 0;
        while (deadline.Elapsed.TotalSeconds < timeout)
        {
            if (int.TryParse(ReadOrEmpty(Path.Combine(docs, "onegame-exit.txt")).Trim(), out exit))
            {
                timedOut = false;
                break;
            }

            // A process that is gone without an exit code crashed (or was killed): do not wait out the time limit for it.
            gone = IsRunning(device, bundle) ? 0 : gone + 1;
            if (gone >= 3)
            {
                crashed = true;
                timedOut = false;
                break;
            }

            Thread.Sleep(300);
        }

        var stdout = ReadOrEmpty(Path.Combine(docs, "onegame-stdout.txt"));
        if (crashed)
        {
            stdout += "\n--- crash report ---\n" + CrashReport(Container(device, bundle, "app"), started);
        }

        var pulled = new List<string>();
        var outDir = Path.GetFullPath(a.Get("--out") ?? Path.Combine(Directory.GetCurrentDirectory(), "artifacts"));
        foreach (var pull in pulls)
        {
            pulled.AddRange(Pull(docs, pull, outDir));
        }

        JsonElement? result = null;
        var last = stdout.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.StartsWith('{') && l.EndsWith('}'));
        if (last is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(last);
                result = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                // Not a result object: the raw output has it.
            }
        }

        var ok = !timedOut && !crashed && exit == ExitCodes.Ok;
        var run = new IosRunResult(ok, timedOut || crashed ? -1 : exit, bundle, device.Udid, timedOut, crashed, result, stdout, [.. pulled], Math.Round(clock.Elapsed.TotalSeconds, 1));
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(run, CliJson.Default.IosRunResult));
        }
        else
        {
            output.Write(stdout);
            if (timedOut)
            {
                output.WriteLine($"error: the game had not finished after {timeout} s");
            }

            if (crashed)
            {
                output.WriteLine("error: the app ended without an exit code (crashed or was killed); the crash report is above");
            }

            foreach (var p in pulled)
            {
                output.WriteLine($"pulled {p}");
            }
        }

        return ok ? ExitCodes.Ok : timedOut || crashed ? ExitCodes.RuntimeError : ExitCodes.Failed;
    }

    private static int Live(ProjectArgs a, bool json, TextWriter output)
    {
        var device = Resolve(a, boot: true);
        var bundle = ResolveBundle(a, device);
        var port = int.TryParse(a.Get("--port"), out var p) ? p : 7878;
        var docs = Path.Combine(Container(device, bundle, "data"), "Documents");
        Simctl("terminate", device.Udid, bundle);
        Directory.CreateDirectory(docs);
        TryDelete(Path.Combine(docs, "onegame-exit.txt"));
        TryDelete(Path.Combine(docs, "onegame-stdout.txt"));
        var launch = Simctl(["launch", device.Udid, bundle, "--live", $"tcp:{port}", "--realtime", "--json", .. a.Game]);
        if (launch.ExitCode != 0)
        {
            throw new CliException(ExitCodes.RuntimeError, "start_failed", $"The app did not start: {(launch.Output + launch.Error).Trim()}");
        }

        // The simulator shares this Mac's network, so the game's listener is reachable directly. Wait until it accepts: a port some other
        // process holds makes the game exit at start-up, and a client must not end up talking to the wrong program.
        var endpoint = $"tcp:127.0.0.1:{port}";
        var wait = Stopwatch.StartNew();
        var ready = false;
        while (wait.Elapsed.TotalSeconds < 45 && !ready)
        {
            Thread.Sleep(400);
            if (!IsRunning(device, bundle))
            {
                var log = ReadOrEmpty(Path.Combine(docs, "onegame-stdout.txt")).Trim();
                throw new CliException(ExitCodes.RuntimeError, "app_exited", $"{bundle} stopped right after it started. Is port {port} in use (--port)? {(log.Length > 0 ? "Its output: " + log : "")}");
            }

            try
            {
                using var probe = new System.Net.Sockets.TcpClient();
                probe.Connect("127.0.0.1", port);
                ready = true;
            }
            catch (System.Net.Sockets.SocketException)
            {
                // Not listening yet.
            }
        }

        if (!ready)
        {
            throw new CliException(ExitCodes.RuntimeError, "not_listening", $"{bundle} did not open port {port} within 45 s.");
        }

        Info(json, output, new IosInfoResult(true, device.Udid, bundle, "started", endpoint, $"onegame mcp --socket {endpoint}"));
        return ExitCodes.Ok;
    }

    private static string ReadOrEmpty(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return "";
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // A stale file is replaced by the run.
        }
    }

    // The newest crash report written since the run began for this app's program, without the long tail of runtime frames.
    private static string CrashReport(string appFolder, DateTime since)
    {
        var name = Proc.Run("plutil", ["-extract", "CFBundleExecutable", "raw", "-o", "-", Path.Combine(appFolder, "Info.plist")], Directory.GetCurrentDirectory()).Output.Trim();
        var reports = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Logs", "DiagnosticReports");
        if (name.Length == 0 || !Directory.Exists(reports))
        {
            return "(no crash report found)";
        }

        var file = Directory.GetFiles(reports, name + "-*.ips").Select(f => new FileInfo(f)).Where(f => f.LastWriteTimeUtc >= since.AddSeconds(-5)).OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
        if (file is null)
        {
            // Reports can take a few seconds to be written.
            Thread.Sleep(3000);
            file = Directory.GetFiles(reports, name + "-*.ips").Select(f => new FileInfo(f)).Where(f => f.LastWriteTimeUtc >= since.AddSeconds(-5)).OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
        }

        return file is null ? "(no crash report found)" : file.FullName + "\n" + string.Join('\n', File.ReadLines(file.FullName).Take(60).Select(l => l.Length > 400 ? l[..400] : l));
    }

    // "src" or "src=dest": src is a file or folder on this Mac, dest is relative to the app's Documents folder (default: the name of src).
    private static void Push(string docs, string spec)
    {
        var eq = spec.LastIndexOf('=');
        var src = Path.GetFullPath(eq > 0 ? spec[..eq] : spec);
        var dest = eq > 0 ? spec[(eq + 1)..] : Path.GetFileName(src);
        if (!File.Exists(src) && !Directory.Exists(src))
        {
            throw new CliException(ExitCodes.Usage, "no_such_path", $"Cannot push {src}: it does not exist.");
        }

        if (Path.IsPathRooted(dest) || dest.Split('/', '\\').Contains(".."))
        {
            throw new CliException(ExitCodes.Usage, "bad_args", $"'{dest}' must be a plain path inside the app's Documents folder.");
        }

        var target = Path.Combine(docs, dest);
        if (Directory.Exists(target))
        {
            Directory.Delete(target, true);
        }
        else if (File.Exists(target))
        {
            File.Delete(target);
        }

        if (Directory.Exists(src))
        {
            CopyTree(src, target);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(src, target, true);
        }
    }

    private static void CopyTree(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyTree(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
    }

    // "remote" or "remote=local": remote is a folder in the app's Documents folder; its files are written under local (default: --out/remote).
    private static List<string> Pull(string docs, string spec, string outDir)
    {
        var eq = spec.LastIndexOf('=');
        var remote = eq > 0 ? spec[..eq] : spec;
        var local = eq > 0 ? Path.GetFullPath(spec[(eq + 1)..]) : Path.Combine(outDir, remote);
        if (Path.IsPathRooted(remote) || remote.Split('/', '\\').Contains(".."))
        {
            throw new CliException(ExitCodes.Usage, "bad_args", $"'{remote}' must be a plain path inside the app's Documents folder.");
        }

        var written = new List<string>();
        var source = Path.Combine(docs, remote);
        if (!Directory.Exists(source))
        {
            return written;
        }

        var root = Path.GetFullPath(local).TrimEnd('/') + "/";
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            // The app's files are the app's own, but a name that would climb out of the folder asked for is skipped anyway.
            var target = Path.GetFullPath(Path.Combine(local, Path.GetRelativePath(source, file)));
            if (!target.StartsWith(root, StringComparison.Ordinal))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
            written.Add(target);
        }

        return written;
    }
}
