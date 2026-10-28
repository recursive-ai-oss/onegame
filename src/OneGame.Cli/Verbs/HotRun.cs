using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using OneGame.Control;
using OneGame.Host;
using OneGame.Testing;

namespace OneGame.Cli.Verbs;

/// <summary>
/// <c>onegame run</c> in a development build: the game runs under <c>dotnet watch</c>, which compiles an edit and hands the runtime an
/// edit-and-continue delta, so a changed method body, a new method or a new synced member is live in the running game with the world
/// untouched. An edit the runtime cannot apply (a changed member type, a renamed member, a new base class) makes <c>dotnet watch</c> restart the
/// game; this wrapper copies the world through the game's live control when it sees the file change, writes it out when the restart is
/// announced, and the restarted game loads it.
/// </summary>
internal static partial class HotRun
{
    private static readonly string[] BatchOptions =
        ["--frames", "--capture", "--sequence", "--scenario", "--replay", "--lint-theme", "--record", "--after", "--control", "--count"];

    /// <summary>Whether a run should be hot: a Debug build, not a batch run (frames, capture, scenario, replay), not asked off.</summary>
    public static bool Wanted(ProjectArgs a, bool json)
    {
        if (a.Own.ContainsKey("--no-hot") || a.Release)
        {
            return false;
        }

        if (a.Own.ContainsKey("--hot"))
        {
            return true;
        }

        return !json && !a.Game.Any(g => Array.IndexOf(BatchOptions, g) >= 0 || BatchOptions.Any(o => g.StartsWith(o + "=", StringComparison.Ordinal)));
    }

    public static int Run(ProjectArgs a, ProjectRef project, TextWriter output, TextWriter error)
    {
        var state = Path.Combine(project.Directory, "obj", "onegame-hot");
        Directory.CreateDirectory(state);
        var statusFile = Path.Combine(state, "status.json");
        var resumeFile = Path.Combine(state, "resume.ogsv");
        File.Delete(resumeFile);
        var status = new Status(statusFile);
        status.Write("idle", null);

        var gameArgs = new List<string>(a.Game);
        if (!gameArgs.Contains("--live") && !gameArgs.Any(g => g.StartsWith("--live=", StringComparison.Ordinal)))
        {
            // The wrapper needs the live socket to save the world before a restart, and a hot game runs on its own.
            gameArgs.AddRange(["--live", SocketPath()]);
            if (!gameArgs.Contains("--realtime"))
            {
                gameArgs.Add("--realtime");
            }
        }

        if (!gameArgs.Contains("--parent"))
        {
            gameArgs.AddRange(["--parent", Environment.ProcessId.ToString(CultureInfo.InvariantCulture)]);
        }

        List<string> args = ["watch", "--verbose", "--project", project.File, "run", "-c", a.Configuration, "--no-launch-profile", "--"];
        args.AddRange(gameArgs);
        var info = Proc.Start(Proc.Dotnet, args, project.Directory);
        info.Environment["ONEGAME_HOT_STATUS"] = statusFile;
        info.Environment["ONEGAME_HOT_RESUME"] = resumeFile;
        info.Environment["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "1";
        // An edit the runtime cannot apply restarts the game by itself; the world is copied before that (see Watched).
        info.Environment["DOTNET_WATCH_RESTART_ON_RUDE_EDIT"] = "1";
        using var process = new Process { StartInfo = info };
        var gate = new object();
        using var done = new ManualResetEventSlim();
        var problems = new List<string>();
        string? livePath = null;
        string? lastReason = null;
        var restarts = 0;
        var exitCode = ExitCodes.Ok;
        var expectRestart = false;

        byte[]? saved = null;

        void Line(string? raw)
        {
            if (raw is null)
            {
                return;
            }

            var line = Ansi().Replace(raw, "").TrimEnd();
            lock (gate)
            {
                if (Noise(line))
                {
                    return;
                }

                var listening = Listening().Match(line);
                if (listening.Success)
                {
                    livePath = listening.Groups[1].Value;
                    if (expectRestart)
                    {
                        expectRestart = false;
                        restarts++;
                        status.Write("restarted", $"Restarted after an edit that could not be applied live ({lastReason}).{(File.Exists(resumeFile) ? " The world is loaded from the saved copy." : string.Empty)}", restarts);
                    }
                }
                else if (Watch().Match(line) is { Success: true } w)
                {
                    Watched(w.Groups[1].Value);
                    return;
                }

                output.WriteLine(line);
                output.Flush();
            }
        }

        // What dotnet watch says about an edit. Verbose, because it prints the compiler's messages for an edit that does not compile only
        // there; everything it says that is not one of these is dropped.
        void Watched(string text)
        {
            if (text.StartsWith("File updated", StringComparison.Ordinal))
            {
                problems.Clear();
                // The edit's fate is not known yet. If it needs a restart the game is stopped within a second, so the world is copied now.
                saved = livePath is null ? null : SaveWorld(livePath, error);
            }
            else if (text.Contains("changes applied", StringComparison.Ordinal))
            {
                status.Write("applied", null, restarts);
                output.WriteLine("hot reload: edit applied; the world is unchanged.");
                output.Flush();
            }
            else if (text.Contains("No managed code changes to apply", StringComparison.Ordinal))
            {
                // The file is back to code that is already running (a compile error was fixed by undoing the edit).
                if (status.State == "error")
                {
                    status.Write("applied", null, restarts);
                    output.WriteLine("hot reload: the code compiles again and matches what is running.");
                    output.Flush();
                }
            }
            else if (text.Contains("[auto-restart]", StringComparison.Ordinal))
            {
                var reason = Describe(text);
                lastReason = reason;
                if (saved is not null)
                {
                    File.WriteAllBytes(resumeFile, saved);
                }

                var kept = saved is not null;
                status.Write("rude", $"This edit cannot be applied to the running game: {reason}. {(kept ? "The world was saved; restarting with it." : "The world could not be saved; restarting from a fresh start.")}", restarts);
                output.WriteLine($"hot reload: this edit cannot be applied to the running game: {reason}.");
                output.WriteLine(kept ? "hot reload: world saved; restarting and loading it." : "hot reload: world not saved; restarting.");
                output.Flush();
                expectRestart = true;
            }
            else if (text.Contains("Unable to apply changes due to compilation errors", StringComparison.Ordinal))
            {
                status.Write("error", "The edit does not compile, so the running game is unchanged.", restarts);
                error.WriteLine("hot reload: the edit does not compile; the game keeps running the last good code.");
            }
            else if (Diagnostic().IsMatch(text) && !expectRestart)
            {
                var problem = Describe(text);
                problems.Add(problem);
                status.Write("error", "The edit does not compile, so the running game is unchanged: " + string.Join("; ", problems.Distinct()), restarts);
                error.WriteLine("hot reload: " + problem);
            }
            else if (text.Contains("Waiting for a file to change before restarting", StringComparison.Ordinal))
            {
                // The game ended by itself (window closed, quit): nothing to watch for any more.
                done.Set();
            }
            else if (ExitCode().Match(text) is { Success: true } m && !expectRestart)
            {
                exitCode = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            }
        }

        process.OutputDataReceived += (_, e) => Line(e.Data);
        process.ErrorDataReceived += (_, e) => Line(e.Data);
        output.WriteLine("hot reload on: edit code and save to see it in the running game (--no-hot turns it off; see docs/hot-reload.md).");
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var term = PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ => done.Set());
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            done.Set();
        };
        var exited = Task.Run(() =>
        {
            process.WaitForExit();
            done.Set();
        });
        done.Wait();
        var stopped = !process.HasExited;
        if (stopped)
        {
            Stop(process);
        }

        exited.Wait(5000);
        if (exitCode == ExitCodes.Ok && process.HasExited && process.ExitCode != 0 && !stopped)
        {
            // dotnet watch itself failed (for example, no room for another file watcher); its message is above.
            exitCode = process.ExitCode;
        }

        return exitCode;
    }

    // Unix socket paths are limited to about 100 characters, which a project folder can use up, so the socket lives in the per-user
    // folder the live-control server uses by default.
    private static string SocketPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onegame-live");
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(dir);
        }
        else
        {
            Directory.CreateDirectory(dir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        return Path.Combine(dir, "hot-" + Environment.ProcessId.ToString(CultureInfo.InvariantCulture) + ".sock");
    }

    private static void Stop(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // Already gone.
        }
    }

    // The first lines of a watch run are the build's own progress ('X -> path') and a warning about the engine's own projects when the
    // game builds from an engine checkout; they bury what matters.
    private static bool Noise(string line) =>
        line.Length == 0 || line.StartsWith("Build succeeded", StringComparison.Ordinal) || line.StartsWith("    0 ", StringComparison.Ordinal) || line.StartsWith("Time Elapsed", StringComparison.Ordinal)
        || ProjectArrow().IsMatch(line) || line.Contains("Duplicate source file", StringComparison.Ordinal)
        || line.StartsWith("  Determining projects", StringComparison.Ordinal) || line.StartsWith("  All projects are up-to-date", StringComparison.Ordinal)
        || line.Contains("Loading projects", StringComparison.Ordinal) || (line.Contains("Loaded ", StringComparison.Ordinal) && line.Contains("project(s)", StringComparison.Ordinal));

    // 'dotnet watch ❌ [demo] /path/obj/.../Demo_Ship.Entity.g.cs(50,9): error ENC0009: Updating the type of field requires restarting the application.'
    private static string Describe(string line)
    {
        var m = Diagnostic().Match(line);
        if (!m.Success)
        {
            return line;
        }

        var file = m.Groups["file"].Value;
        var generated = file.EndsWith(".g.cs", StringComparison.Ordinal);
        var where = generated ? $"generated code of {file.Split('.')[0]}, from a change to its synced members, commands or views" : $"{file}({m.Groups["line"].Value})";
        return $"{m.Groups["text"].Value.TrimEnd('.')} [{m.Groups["code"].Value}, {where}]";
    }

    private static byte[]? SaveWorld(string livePath, TextWriter error)
    {
        try
        {
            using var client = new LiveClient(livePath, 10_000);
            using var doc = JsonDocument.Parse(client.Call("snapshot_save"));
            return Convert.FromBase64String(doc.RootElement.GetProperty("base64").GetString() ?? string.Empty);
        }
        catch (Exception ex) when (ex is ControlException or IOException or JsonException or FormatException or KeyNotFoundException)
        {
            error.WriteLine("hot reload: could not copy the world: " + ex.Message);
            return null;
        }
    }

    // Written atomically, so the game never reads half a file.
    private sealed class Status(string path)
    {
        public string State { get; private set; } = "idle";

        public void Write(string state, string? message, int restarts = 0)
        {
            State = state;
            var temp = path + ".tmp";
            using (var stream = File.Create(temp))
            using (var w = new Utf8JsonWriter(stream))
            {
                w.WriteStartObject();
                w.WriteString("state", state);
                if (message is not null)
                {
                    w.WriteString("message", message);
                }

                w.WriteNumber("restarts", restarts);
                w.WriteEndObject();
            }

            File.Move(temp, path, overwrite: true);
        }
    }

    [GeneratedRegex(@"^dotnet watch \S+ (.*)$")]
    private static partial Regex Watch();

    [GeneratedRegex(@"\x1B\[[0-9;?]*[A-Za-z]")]
    private static partial Regex Ansi();

    [GeneratedRegex(@"^listening on (.+?) \(MCP")]
    private static partial Regex Listening();

    [GeneratedRegex(@"^\s+\S+ -> \S")]
    private static partial Regex ProjectArrow();

    [GeneratedRegex(@"Exited with error code (\d+)")]
    private static partial Regex ExitCode();

    [GeneratedRegex(@"(?<file>[^/\\ ]+)\((?<line>\d+),\d+\): (?:error|warning) (?<code>\w+): (?<text>.+)$")]
    private static partial Regex Diagnostic();
}
