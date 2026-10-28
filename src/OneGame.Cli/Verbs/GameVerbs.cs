using System.Text.Json;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>Builds a game project and summarizes the compiler's messages.</summary>
internal sealed class BuildVerb : IVerb
{
    public string Name => "build";

    public string Summary => "Build the game in this folder (Debug; --release for Release).";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        var a = new ProjectArgs(args);
        var project = Projects.Find(a.Get("--project"));
        var build = Projects.Build(project, a.Configuration);
        Projects.PrintBuild(build, json, output, error);
        return build.Ok ? ExitCodes.Ok : ExitCodes.BuildFailed;
    }
}

/// <summary>Runs the game, windowed or headless. Options after <c>--</c> go to the game untouched.</summary>
internal sealed class RunVerb : IVerb
{
    public string Name => "run";

    public string Summary => "Build and run the game; a Debug run is hot (C# edits apply while it runs; --no-hot turns that off). --headless --frames N, --size, --seed, --live [socket], -- <game options>.";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        var a = new ProjectArgs(args);
        if (!HotRun.Wanted(a, json))
        {
            return GameRunner.Run(a, json, output, error, (_, _) => []);
        }

        var project = Projects.Find(a.Get("--project"));
        return Projects.EnsureBuilt(a, project, json, output, error) ?? HotRun.Run(a, project, output, error);
    }
}

/// <summary>Renders frames offscreen to PNG files.</summary>
internal sealed class CaptureVerb : IVerb
{
    public string Name => "capture";

    public string Summary => "Capture a frame (--out file.png --size 720p --scale 1 --after N) or a sequence (--sequence dir --count N --every T).";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        var a = new ProjectArgs(args, "--out");
        return GameRunner.Run(a, json, output, error, (_, project) =>
        {
            var gameArgs = new List<string> { "--headless" };
            if (a.Game.Contains("--sequence"))
            {
                gameArgs.AddRange(["--artifacts", Path.Combine(project.Directory, "captures")]);
                return gameArgs;
            }

            var target = Path.GetFullPath(a.Get("--out") ?? Path.Combine(project.Directory, "captures", "frame.png"));
            gameArgs.AddRange(["--artifacts", Path.GetDirectoryName(target)!, "--capture", Path.GetFileName(target)]);
            return gameArgs;
        });
    }
}

/// <summary>Checks the game's UI theme for legibility (WCAG AA contrast) and prints the failing token pairs.</summary>
internal sealed class LintThemeVerb : IVerb
{
    public string Name => "lint-theme";

    public string Summary => "Check the UI theme's contrast (WCAG 2.x AA) in a headless game: exits 1 and lists the failing token pairs, with --json a report object.";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error) =>
        GameRunner.Run(new ProjectArgs(args), json, output, error, (_, _) => ["--headless", "--lint-theme"]);
}

/// <summary>Replays a recorded run and compares every tick's state hash.</summary>
internal sealed class ReplayVerb : IVerb
{
    public string Name => "replay";

    public string Summary => "Replay a recording (onegame replay run.ogrp) and compare the state hash of every tick.";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        var a = new ProjectArgs(args);
        var file = a.Game.FirstOrDefault(g => !g.StartsWith("--", StringComparison.Ordinal))
            ?? throw new CliException(ExitCodes.Usage, "bad_args", "Name the recording: onegame replay <file>.");
        a.Game.Remove(file);
        return GameRunner.Run(a, json, output, error, (_, _) => ["--headless", "--replay", Path.GetFullPath(file)]);
    }
}

/// <summary>Shared by the verbs that run the game with some options.</summary>
internal static class GameRunner
{
    public static int Run(ProjectArgs a, bool json, TextWriter output, TextWriter error, Func<ProjectArgs, ProjectRef, List<string>> lead)
    {
        var project = Projects.Find(a.Get("--project"));
        if (Projects.EnsureBuilt(a, project, json, output, error) is { } failed)
        {
            return failed;
        }

        var gameArgs = lead(a, project);
        gameArgs.AddRange(a.Game);
        if (json)
        {
            gameArgs.Add("--json");
        }

        var (exit, text, err) = Projects.RunGame(project, a.Configuration, gameArgs, output);
        if (json && !Projects.HasJsonLine(text))
        {
            var tail = string.Join(" ", (text + "\n" + err).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).TakeLast(8));
            return Program.Fail(true, output, error, exit == 0 ? ExitCodes.RuntimeError : exit, "runtime_error", $"The game exited with code {exit} and printed no result. {tail}");
        }

        if (!json && err.Length > 0)
        {
            error.Write(err);
        }

        return exit;
    }
}

/// <summary>Runs the game's scenarios and its C# tests.</summary>
internal sealed class TestVerb : IVerb
{
    public string Name => "test";

    public string Summary => "Run scenarios (scenarios/*.yaml) in a headless game, and the game's C# test projects. --scenario <file>, --update-references, --no-unit.";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        var a = new ProjectArgs(args, "--scenario");
        var project = Projects.Find(a.Get("--project"));
        var noUnit = a.Game.Remove("--no-unit");
        var update = a.Game.Remove("--update-references");
        if (Projects.EnsureBuilt(a, project, json, output, error) is { } failed)
        {
            return failed;
        }

        var scenarioTarget = a.Get("--scenario") ?? Path.Combine(project.Directory, "scenarios");
        var ok = true;
        JsonElement? scenarioJson = null;
        if (File.Exists(scenarioTarget) || Directory.Exists(scenarioTarget))
        {
            var gameArgs = new List<string>
            {
                "--headless", "--scenario", Path.GetFullPath(scenarioTarget), "--artifacts", Path.Combine(project.Directory, "artifacts"),
            };
            if (update)
            {
                gameArgs.Add("--update-references");
            }

            gameArgs.AddRange(a.Game);
            if (json)
            {
                gameArgs.Add("--json");
            }

            var (exit, text, err) = Projects.RunGame(project, a.Configuration, gameArgs, json ? null : output);
            if (json)
            {
                if (!Projects.HasJsonLine(text))
                {
                    return Program.Fail(true, output, error, exit == 0 ? ExitCodes.RuntimeError : exit, "runtime_error", $"The game exited with code {exit} and printed no result. {err.Trim()}");
                }

                using var doc = JsonDocument.Parse(Projects.LastJsonLine(text));
                scenarioJson = doc.RootElement.Clone();
            }
            else if (err.Length > 0)
            {
                error.Write(err);
            }

            if (exit == ExitCodes.NoDevTools)
            {
                return exit;
            }

            ok &= exit == ExitCodes.Ok;
        }
        else if (a.Get("--scenario") is not null)
        {
            throw new CliException(ExitCodes.RuntimeError, "no_scenario", $"No scenario at {scenarioTarget}.");
        }

        UnitTestSummary? unit = null;
        if (!noUnit)
        {
            unit = RunUnitTests(project, a.Configuration, json ? null : output);
            ok &= !unit.Ran || unit.Ok;
        }

        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(new TestResult(ok, scenarioJson, unit, null), CliJson.Default.TestResult));
        }
        else if (unit is { Ran: true })
        {
            output.WriteLine($"unit tests: {unit.Passed} passed, {unit.Failed} failed, {unit.Skipped} skipped");
        }

        return ok ? ExitCodes.Ok : ExitCodes.Failed;
    }

    private static UnitTestSummary RunUnitTests(ProjectRef project, string configuration, TextWriter? stream)
    {
        var projects = Directory.GetFiles(project.Directory, "*.csproj", SearchOption.AllDirectories)
            .Where(f => f != project.File && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => Path.GetFileNameWithoutExtension(f).Contains("Test", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (projects.Length == 0)
        {
            return new UnitTestSummary(false, true, 0, 0, 0, 0, [], null);
        }

        int total = 0, passed = 0, failed = 0, skipped = 0;
        var ok = true;
        var tail = new List<string>();
        foreach (var p in projects)
        {
            var result = Proc.Run(Proc.Dotnet, ["test", "--project", p, "-c", configuration, "-v", "q"], project.Directory, line => stream?.WriteLine(line));
            ok &= result.ExitCode == 0;
            foreach (var raw in (result.Output + "\n" + result.Error).Split('\n'))
            {
                var line = raw.Trim();
                total += Count(line, "total:");
                passed += Count(line, "succeeded:");
                failed += Count(line, "failed:");
                skipped += Count(line, "skipped:");
            }

            tail.AddRange((result.Output + result.Error).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).TakeLast(5));
        }

        return new UnitTestSummary(true, ok, total, passed, failed, skipped, projects, ok ? null : string.Join("\n", tail));
    }

    private static int Count(string line, string label) =>
        line.StartsWith(label, StringComparison.Ordinal) && int.TryParse(line[label.Length..].Trim(), out var n) ? n : 0;
}
