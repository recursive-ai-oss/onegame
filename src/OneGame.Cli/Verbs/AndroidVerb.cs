using System.Diagnostics;
using System.Formats.Tar;
using System.Text;
using System.Text.Json;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>Result of <c>onegame android devices</c>.</summary>
internal sealed record AndroidDevice(string Serial, string State, string? Model, string? Abi);

/// <summary>Result of <c>onegame android run</c> and <c>test</c>.</summary>
/// <param name="Ok">The game ran to its end and exited with 0.</param>
/// <param name="ExitCode">The game's exit code, or -1 when it did not finish in time.</param>
/// <param name="Package">The app that ran.</param>
/// <param name="Serial">The device.</param>
/// <param name="TimedOut">True when the game had not finished after the time limit.</param>
/// <param name="Result">The game's own JSON result (its last line of output when that is JSON), when it printed one.</param>
/// <param name="Output">Everything the game wrote to standard output and error.</param>
/// <param name="Pulled">Files copied back from the device.</param>
/// <param name="Seconds">How long the run took.</param>
internal sealed record AndroidRunResult(bool Ok, int ExitCode, string Package, string Serial, bool TimedOut, JsonElement? Result, string Output, string[] Pulled, double Seconds);

/// <summary>Result of <c>onegame android live</c>.</summary>
internal sealed record AndroidLiveResult(bool Ok, string Package, string Serial, string Endpoint, int DevicePort, string Next);

/// <summary>Installs, starts, drives and inspects a game on an Android device or emulator through <c>adb</c>.</summary>
internal sealed class AndroidVerb : IVerb
{
    private const string Activity = "android.app.NativeActivity";

    public string Name => "android";

    public string Summary => "Use a device or emulator: android devices | install <apk> | run [-- game options] | test [--scenario dir] | live | stop | logs. Common: --serial, --package, --apk, --timeout S, --push local[=dest], --pull remote[=local].";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        if (args.Length == 0)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "Usage: onegame android devices|install|run|test|live|stop|logs. See 'onegame help'.");
        }

        // --push and --pull can be given more than once, which ProjectArgs does not keep.
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

        var a = new ProjectArgs([.. rest], "--serial", "--package", "--apk", "--timeout", "--scenario", "--port", "--sdk", "--lines", "--out");
        var sdk = AndroidSdk.Find(a.Get("--sdk"), needNdk: false);
        var adb = new Adb(sdk.Adb, a.Get("--serial"));
        return args[0] switch
        {
            "devices" => Devices(adb, json, output),
            "install" => Install(a, sdk, adb, json, output),
            "run" => RunGame(a, sdk, adb, json, output, test: false, pushes, pulls),
            "test" => RunGame(a, sdk, adb, json, output, test: true, pushes, pulls),
            "live" => Live(a, sdk, adb, json, output),
            "stop" => Stop(a, sdk, adb, json, output),
            "logs" => Logs(a, adb, output),
            var other => throw new CliException(ExitCodes.Usage, "bad_args", $"Unknown android command '{other}'. Commands: devices, install, run, test, live, stop, logs."),
        };
    }

    private static int Devices(Adb adb, bool json, TextWriter output)
    {
        var run = adb.Global(["devices", "-l"]);
        var list = new List<AndroidDevice>();
        foreach (var line in run.Output.Split('\n').Skip(1))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var model = parts.FirstOrDefault(p => p.StartsWith("model:", StringComparison.Ordinal))?[6..];
            string? abi = null;
            if (parts[1] == "device")
            {
                var props = new Adb(adb.Path, parts[0]).Shell("getprop ro.product.cpu.abi");
                abi = props.Output.Trim();
            }

            list.Add(new AndroidDevice(parts[0], parts[1], model, abi));
        }

        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(list.ToArray(), CliJson.Default.AndroidDeviceArray));
        }
        else
        {
            foreach (var d in list)
            {
                output.WriteLine($"{d.Serial}\t{d.State}\t{d.Model}\t{d.Abi}");
            }

            if (list.Count == 0)
            {
                output.WriteLine("no devices. Start an emulator or connect a phone with USB debugging on.");
            }
        }

        return list.Count > 0 ? ExitCodes.Ok : ExitCodes.RuntimeError;
    }

    private static int Install(ProjectArgs a, AndroidSdk sdk, Adb adb, bool json, TextWriter output)
    {
        var apk = a.Game.FirstOrDefault(x => !x.StartsWith("--", StringComparison.Ordinal)) ?? a.Get("--apk")
            ?? throw new CliException(ExitCodes.Usage, "bad_args", "Name the APK: onegame android install <file.apk>.");
        var package = InstallApk(sdk, adb, apk);
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(new AndroidLiveResult(true, package, adb.SerialOrDefault, "", 0, "onegame android run --package " + package), CliJson.Default.AndroidLiveResult));
        }
        else
        {
            output.WriteLine($"installed {package}");
        }

        return ExitCodes.Ok;
    }

    private static string InstallApk(AndroidSdk sdk, Adb adb, string apk)
    {
        apk = Path.GetFullPath(apk);
        if (!File.Exists(apk))
        {
            throw new CliException(ExitCodes.Usage, "no_apk", $"{apk} does not exist.");
        }

        var package = PackageOf(sdk, apk);
        // -t allows the test-only and debuggable builds exports make; -r replaces an earlier install and keeps the app's data.
        var run = adb.Run(["install", "-r", "-t", apk]);
        if (run.ExitCode != 0 || !run.Output.Contains("Success", StringComparison.Ordinal))
        {
            throw new CliException(ExitCodes.RuntimeError, "install_failed", $"adb install failed: {(run.Output + run.Error).Trim()}");
        }

        return package;
    }

    private static string PackageOf(AndroidSdk sdk, string apk)
    {
        var run = Proc.Run(sdk.BuildTool("aapt2"), ["dump", "packagename", apk], Directory.GetCurrentDirectory());
        return run.ExitCode == 0 && run.Output.Trim() is { Length: > 0 } name
            ? name
            : throw new CliException(ExitCodes.RuntimeError, "bad_apk", $"aapt2 could not read the package name of {apk}: {(run.Output + run.Error).Trim()}");
    }

    private static string ResolvePackage(ProjectArgs a, AndroidSdk sdk, Adb adb)
    {
        if (a.Get("--apk") is { } apk)
        {
            return InstallApk(sdk, adb, apk);
        }

        return a.Get("--package") ?? throw new CliException(ExitCodes.Usage, "bad_args", "Name the app: --package <id>, or --apk <file> to install and run it.");
    }

    private static int Stop(ProjectArgs a, AndroidSdk sdk, Adb adb, bool json, TextWriter output)
    {
        var package = ResolvePackage(a, sdk, adb);
        adb.Shell("am force-stop " + package);
        adb.Run(["forward", "--remove-all"]);
        output.WriteLine(json ? "{\"ok\":true}" : $"stopped {package}");
        return ExitCodes.Ok;
    }

    private static int Logs(ProjectArgs a, Adb adb, TextWriter output)
    {
        var lines = a.Get("--lines") ?? "200";
        var run = adb.Run(["logcat", "-d", "-t", lines, "-s", "onegame:*", "DEBUG:*", "AndroidRuntime:*", "libc:*", "vulkan:*"]);
        output.Write(run.Output);
        return ExitCodes.Ok;
    }

    private static int RunGame(ProjectArgs a, AndroidSdk sdk, Adb adb, bool json, TextWriter output, bool test, List<string> pushes, List<string> pulls)
    {
        var clock = Stopwatch.StartNew();
        var package = ResolvePackage(a, sdk, adb);
        var files = $"/data/user/0/{package}/files";
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

        adb.Shell("am force-stop " + package);
        adb.RunAs(package, "mkdir -p files && rm -f files/onegame-exit.txt files/onegame-stdout.txt && rm -rf files/artifacts");
        foreach (var push in pushes)
        {
            Push(adb, package, push);
        }

        var line = string.Join(' ', gameArgs.Select(g => Quote(g.Replace("@files", files, StringComparison.Ordinal))));
        var start = adb.Shell($"am start -W -n {package}/{Activity} -e onegame.args {ShellQuote(line)}");
        if (start.ExitCode != 0 || start.Output.Contains("Error", StringComparison.Ordinal))
        {
            throw new CliException(ExitCodes.RuntimeError, "start_failed", $"The app did not start: {(start.Output + start.Error).Trim()}");
        }

        var exit = -1;
        var timedOut = true;
        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed.TotalSeconds < timeout)
        {
            var marker = adb.RunAs(package, "cat files/onegame-exit.txt 2>/dev/null").Output.Trim();
            if (int.TryParse(marker, out exit))
            {
                timedOut = false;
                break;
            }

            Thread.Sleep(300);
        }

        var text = adb.ExecOut(["run-as", package, "cat", "files/onegame-stdout.txt"]);
        var stdout = Encoding.UTF8.GetString(text);
        var pulled = new List<string>();
        var outDir = Path.GetFullPath(a.Get("--out") ?? Path.Combine(Directory.GetCurrentDirectory(), "artifacts"));
        foreach (var pull in pulls)
        {
            pulled.AddRange(Pull(adb, package, pull, outDir));
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

        var ok = !timedOut && exit == ExitCodes.Ok;
        var run = new AndroidRunResult(ok, timedOut ? -1 : exit, package, adb.SerialOrDefault, timedOut, result, stdout, [.. pulled], Math.Round(clock.Elapsed.TotalSeconds, 1));
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(run, CliJson.Default.AndroidRunResult));
        }
        else
        {
            output.Write(stdout);
            if (timedOut)
            {
                output.WriteLine($"error: the game had not finished after {timeout} s");
            }

            foreach (var p in pulled)
            {
                output.WriteLine($"pulled {p}");
            }
        }

        return ok ? ExitCodes.Ok : timedOut ? ExitCodes.RuntimeError : ExitCodes.Failed;
    }

    private static int Live(ProjectArgs a, AndroidSdk sdk, Adb adb, bool json, TextWriter output)
    {
        var package = ResolvePackage(a, sdk, adb);
        var devicePort = int.TryParse(a.Get("--port"), out var p) ? p : 7878;
        adb.Shell("am force-stop " + package);
        adb.RunAs(package, "mkdir -p files && rm -f files/onegame-exit.txt files/onegame-stdout.txt");
        var args = $"--live tcp:{devicePort} --realtime --json";
        var start = adb.Shell($"am start -W -n {package}/{Activity} -e onegame.args {ShellQuote(args)}");
        if (start.ExitCode != 0 || start.Output.Contains("Error", StringComparison.Ordinal))
        {
            throw new CliException(ExitCodes.RuntimeError, "start_failed", $"The app did not start: {(start.Output + start.Error).Trim()}");
        }

        // adb forward tcp:0 picks a free port on the computer and prints it.
        var fwd = adb.Run(["forward", "tcp:0", $"tcp:{devicePort}"]);
        if (fwd.ExitCode != 0 || !int.TryParse(fwd.Output.Trim(), out var local))
        {
            throw new CliException(ExitCodes.RuntimeError, "forward_failed", $"adb forward failed: {(fwd.Output + fwd.Error).Trim()}");
        }

        var endpoint = $"tcp:127.0.0.1:{local}";
        var result = new AndroidLiveResult(true, package, adb.SerialOrDefault, endpoint, devicePort, $"onegame mcp --socket {endpoint}");
        output.WriteLine(json ? JsonSerializer.Serialize(result, CliJson.Default.AndroidLiveResult) : $"listening: {endpoint}\nnext: {result.Next}");
        return ExitCodes.Ok;
    }

    // "src" or "src=dest": src is a file or folder on this computer, dest is relative to the app's files folder (default: the name of src).
    private static void Push(Adb adb, string package, string spec)
    {
        var eq = spec.LastIndexOf('=');
        var src = Path.GetFullPath(eq > 0 ? spec[..eq] : spec);
        var dest = eq > 0 ? spec[(eq + 1)..] : Path.GetFileName(src);
        if (!File.Exists(src) && !Directory.Exists(src))
        {
            throw new CliException(ExitCodes.Usage, "no_such_path", $"Cannot push {src}: it does not exist.");
        }

        if (Path.IsPathRooted(dest) || dest.Split('/', '\\').Contains("..") || dest.Contains('\'', StringComparison.Ordinal))
        {
            throw new CliException(ExitCodes.Usage, "bad_args", $"'{dest}' must be a plain path inside the app's files folder.");
        }

        var staging = $"/data/local/tmp/onegame-{package}-push";
        adb.Shell($"rm -rf {staging} && mkdir -p {staging}");
        var up = adb.Run(["push", src, staging + "/item"]);
        if (up.ExitCode != 0)
        {
            throw new CliException(ExitCodes.RuntimeError, "push_failed", $"adb push failed: {(up.Output + up.Error).Trim()}");
        }

        var parent = dest.Contains('/', StringComparison.Ordinal) ? dest[..dest.LastIndexOf('/')] : ".";
        var copy = adb.RunAs(package, $"mkdir -p 'files/{parent}' && rm -rf 'files/{dest}' && cp -r {staging}/item 'files/{dest}'");
        adb.Shell($"rm -rf {staging}");
        if (copy.ExitCode != 0 || copy.Output.Contains("cp:", StringComparison.Ordinal))
        {
            throw new CliException(ExitCodes.RuntimeError, "push_failed", $"Could not place {dest} in the app's files folder: {(copy.Output + copy.Error).Trim()}");
        }
    }

    // "remote" or "remote=local": remote is a folder in the app's files folder; its files are written under local (default: --out/remote).
    private static List<string> Pull(Adb adb, string package, string spec, string outDir)
    {
        var eq = spec.LastIndexOf('=');
        var remote = eq > 0 ? spec[..eq] : spec;
        var local = eq > 0 ? Path.GetFullPath(spec[(eq + 1)..]) : Path.Combine(outDir, remote);
        if (Path.IsPathRooted(remote) || remote.Split('/', '\\').Contains("..") || remote.Contains('\'', StringComparison.Ordinal))
        {
            throw new CliException(ExitCodes.Usage, "bad_args", $"'{remote}' must be a plain path inside the app's files folder.");
        }

        var tar = adb.ExecOut(["run-as", package, "tar", "-cf", "-", "-C", $"files/{remote}", "."]);
        var written = new List<string>();
        if (tar.Length == 0)
        {
            return written;
        }

        Directory.CreateDirectory(local);
        var root = Path.GetFullPath(local).TrimEnd('/') + "/";
        using var reader = new TarReader(new MemoryStream(tar));
        while (reader.GetNextEntry() is { } entry)
        {
            if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile) || entry.DataStream is null)
            {
                continue;
            }

            // The device is not trusted to name files outside the folder we asked to fill.
            var target = Path.GetFullPath(Path.Combine(local, entry.Name));
            if (!target.StartsWith(root, StringComparison.Ordinal))
            {
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using (var fs = File.Create(target))
            {
                entry.DataStream.CopyTo(fs);
            }

            written.Add(target);
        }

        return written;
    }

    private static string Quote(string s) => s.Length == 0 || s.Any(char.IsWhiteSpace) ? "\"" + s + "\"" : s;

    private static string ShellQuote(string s) => "'" + s.Replace("'", "'\\''", StringComparison.Ordinal) + "'";

    /// <summary>A thin wrapper over the <c>adb</c> executable for one device.</summary>
    private sealed class Adb
    {
        private readonly string? _serial;

        public Adb(string path, string? serial)
        {
            Path = path;
            _serial = serial ?? Environment.GetEnvironmentVariable("ANDROID_SERIAL");
        }

        public string Path { get; }

        public string SerialOrDefault => _serial ?? Shell("getprop ro.serialno").Output.Trim();

        private string[] Prefix => _serial is null ? [] : ["-s", _serial];

        public ProcessResult Global(IEnumerable<string> args) => Proc.Run(Path, args, Directory.GetCurrentDirectory());

        public ProcessResult Run(IEnumerable<string> args) => Proc.Run(Path, [.. Prefix, .. args], Directory.GetCurrentDirectory());

        // adb joins the arguments with spaces and hands the line to the device's shell.
        public ProcessResult Shell(string command) => Run(["shell", command]);

        public ProcessResult RunAs(string package, string command) => Shell($"run-as {package} sh -c {ShellQuote(command)}");

        public byte[] ExecOut(IEnumerable<string> args)
        {
            var info = Proc.Start(Path, [.. Prefix, "exec-out", .. args], Directory.GetCurrentDirectory());
            using var p = Process.Start(info)!;
            using var ms = new MemoryStream();
            p.StandardOutput.BaseStream.CopyTo(ms);
            p.WaitForExit();
            return ms.ToArray();
        }
    }
}
