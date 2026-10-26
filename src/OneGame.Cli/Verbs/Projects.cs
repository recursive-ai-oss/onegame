using System.Diagnostics;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>A game project on disk.</summary>
/// <param name="Directory">Its folder.</param>
/// <param name="File">Its project file.</param>
/// <param name="Name">The project name.</param>
internal sealed record ProjectRef(string Directory, string File, string Name);

/// <summary>The options every project verb shares, and the rest of the command line, which goes to the game.</summary>
internal sealed class ProjectArgs
{
    private static readonly string[] WithValue = ["--project"];
    private static readonly string[] Flags = ["--release", "--no-build"];

    public ProjectArgs(string[] args, params string[] extraWithValue)
    {
        var game = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a == "--")
            {
                game.AddRange(args.Skip(i + 1));
                break;
            }

            if (Array.IndexOf(WithValue, a) >= 0 || Array.IndexOf(extraWithValue, a) >= 0)
            {
                if (i + 1 >= args.Length)
                {
                    throw new CliException(ExitCodes.Usage, "bad_args", $"{a} needs a value.");
                }

                Own[a] = args[++i];
            }
            else if (Array.IndexOf(Flags, a) >= 0)
            {
                Own[a] = "";
            }
            else
            {
                game.Add(a);
            }
        }

        Game = game;
    }

    /// <summary>The options this tool consumed.</summary>
    public Dictionary<string, string> Own { get; } = new(StringComparer.Ordinal);

    /// <summary>Everything else, in order, for the game's command line.</summary>
    public List<string> Game { get; }

    public bool Release => Own.ContainsKey("--release");

    public bool NoBuild => Own.ContainsKey("--no-build");

    public string Configuration => Release ? "Release" : "Debug";

    public string? Get(string name) => Own.GetValueOrDefault(name);
}

/// <summary>Finding, building and running a game project.</summary>
internal static class Projects
{
    public static ProjectRef Find(string? arg)
    {
        var path = Path.GetFullPath(arg ?? Directory.GetCurrentDirectory());
        if (File.Exists(path) && path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return new ProjectRef(Path.GetDirectoryName(path)!, path, Path.GetFileNameWithoutExtension(path));
        }

        if (!Directory.Exists(path))
        {
            throw new CliException(ExitCodes.RuntimeError, "no_project", $"'{path}' is not a folder or a project file.");
        }

        var found = Directory.GetFiles(path, "*.csproj", SearchOption.TopDirectoryOnly);
        return found.Length switch
        {
            1 => new ProjectRef(path, found[0], Path.GetFileNameWithoutExtension(found[0])),
            0 => throw new CliException(ExitCodes.RuntimeError, "no_project", $"No .csproj in {path}. Run from a game folder or pass --project."),
            _ => throw new CliException(ExitCodes.RuntimeError, "ambiguous_project", $"{path} has {found.Length} project files; pass --project <file>."),
        };
    }

    /// <summary>Finds a game's dedicated-server project: a project file that imports <c>build/Server.targets</c>, in the folder, its <c>server</c> folder or a folder one level down.</summary>
    public static ProjectRef FindServer(string? arg)
    {
        var path = Path.GetFullPath(arg ?? Directory.GetCurrentDirectory());
        if (File.Exists(path) && path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return new ProjectRef(Path.GetDirectoryName(path)!, path, Path.GetFileNameWithoutExtension(path));
        }

        if (!Directory.Exists(path))
        {
            throw new CliException(ExitCodes.RuntimeError, "no_project", $"'{path}' is not a folder or a project file.");
        }

        var folders = new List<string> { path, Path.Combine(path, "server") };
        folders.AddRange(Directory.EnumerateDirectories(path).Where(d => !Path.GetFileName(d).StartsWith('.')).Order(StringComparer.Ordinal));
        var found = folders.Where(Directory.Exists).Distinct()
            .SelectMany(d => Directory.GetFiles(d, "*.csproj", SearchOption.TopDirectoryOnly))
            .Where(f => File.ReadAllText(f) is var text && (text.Contains("Server.targets", StringComparison.Ordinal) || text.Contains("<OneGameKind>Server<", StringComparison.Ordinal)))
            .ToArray();
        return found.Length switch
        {
            1 => new ProjectRef(Path.GetDirectoryName(found[0])!, found[0], Path.GetFileNameWithoutExtension(found[0])),
            0 => throw new CliException(ExitCodes.RuntimeError, "no_server_project", $"No server project in {path}: a project file that imports build/Server.targets or sets OneGameKind to Server. Create one with 'onegame new <name> --server', or pass --project <file>."),
            _ => throw new CliException(ExitCodes.RuntimeError, "ambiguous_project", $"{path} has {found.Length} server projects; pass --project <file>."),
        };
    }

    public static BuildResult Build(ProjectRef project, string configuration)
    {
        var clock = Stopwatch.StartNew();
        var result = Proc.Run(Proc.Dotnet, ["build", project.File, "-c", configuration, "--nologo", "-v", "q", "-clp:NoSummary", "-p:GenerateFullPaths=true"], project.Directory);
        var diagnostics = BuildOutput.Parse(result.Output + "\n" + result.Error);
        var errors = diagnostics.Count(d => d.Severity == "error");
        var warnings = diagnostics.Count(d => d.Severity == "warning");
        var ok = result.ExitCode == 0;
        if (!ok && errors == 0)
        {
            // The build failed without a compiler message (restore, SDK or setup problems): show the tail of the output.
            var tail = string.Join(" ", (result.Output + result.Error).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).TakeLast(6));
            diagnostics.Add(new Diagnostic("error", "BUILD", tail, null, 0, 0));
            errors = 1;
        }

        return new BuildResult(ok, project.File, configuration, errors, warnings, [.. diagnostics], Math.Round(clock.Elapsed.TotalSeconds, 2));
    }

    /// <summary>Builds unless told not to. Returns the exit code of a failed build (already printed) or null when the project is ready.</summary>
    public static int? EnsureBuilt(ProjectArgs a, ProjectRef project, bool json, TextWriter output, TextWriter error)
    {
        if (a.NoBuild)
        {
            return null;
        }

        var build = Build(project, a.Configuration);
        if (build.Ok)
        {
            return null;
        }

        PrintBuild(build, json, output, error);
        return ExitCodes.BuildFailed;
    }

    public static void PrintBuild(BuildResult build, bool json, TextWriter output, TextWriter error)
    {
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(build, CliJson.Default.BuildResult));
            return;
        }

        foreach (var d in build.Diagnostics)
        {
            var where = d.File is null ? "" : $"{d.File}({d.Line},{d.Column}): ";
            (d.Severity == "error" ? error : output).WriteLine($"{where}{d.Severity} {d.Code}: {d.Message}");
        }

        output.WriteLine(build.Ok
            ? $"build succeeded ({build.Configuration}, {build.Warnings} warnings, {build.Seconds}s)"
            : $"build FAILED ({build.Errors} errors, {build.Warnings} warnings)");
    }

    /// <summary>Runs the built game with its own command line and returns what it printed and its exit code. Output lines are also sent to <paramref name="stream"/> as they arrive.</summary>
    public static (int Exit, string Output, string Error) RunGame(ProjectRef project, string configuration, IEnumerable<string> gameArgs, TextWriter? stream = null)
    {
        List<string> args = ["run", "--no-build", "--project", project.File, "-c", configuration, "--"];
        args.AddRange(gameArgs);
        var result = Proc.Run(Proc.Dotnet, args, project.Directory, line => stream?.WriteLine(line));
        return (result.ExitCode, result.Output, result.Error);
    }

    /// <summary>Whether game output contains a JSON object line (a game that crashed prints none).</summary>
    public static bool HasJsonLine(string output) => output.Split('\n').Any(l => l.TrimStart().StartsWith('{'));

    public static string LastJsonLine(string output) => output.Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.StartsWith('{')) ?? "";
}
