using System.Diagnostics;
using System.Reflection;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

internal sealed partial class ExportVerb
{
    /// <summary>
    /// Packages a dedicated server. A game's own server project (plugin-less) is published NativeAOT; with <c>--plugins</c> the engine's plugin-capable
    /// <c>onegame-server</c> is published framework-dependent (it loads plugin assemblies, so it needs CoreCLR). The report is the one of a game export.
    /// </summary>
    private static int RunServerExport(ProjectArgs a, bool steam, bool plugins, bool noArchive, bool noAot, bool json, TextWriter output)
    {
        if (steam || a.Game.Count > 0)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", $"{(steam ? "--steam" : a.Game[0])} does not apply to a server export. Usage: onegame export --server [--plugins] [--project <file>] [--out <dir>] [--no-archive] [--no-aot].");
        }

        var target = a.Get("--target") ?? "linux-x64";
        if (target != "linux-x64")
        {
            throw new CliException(ExitCodes.Usage, "bad_target", $"Servers export for linux-x64 only, not '{target}'.");
        }

        var clock = Stopwatch.StartNew();
        var warnings = new List<ExportWarning>();
        var project = plugins ? FindRunner() : Projects.FindServer(a.Get("--project"));
        var aot = !plugins && OperatingSystem.IsLinux() && !noAot;
        if (!plugins && !aot)
        {
            warnings.Add(new ExportWarning("not_native_aot", OperatingSystem.IsLinux()
                ? "Built as a self-contained CoreCLR program because --no-aot was given."
                : "NativeAOT for linux-x64 needs a Linux host; this is a self-contained, trimmed CoreCLR build."));
        }

        var props = ReadProperties(project);
        var exeName = props.AssemblyName;
        var folderName = $"{project.Name}-{props.Version}-{target}";
        var outRoot = Path.GetFullPath(a.Get("--out") ?? Path.Combine(plugins ? Directory.GetCurrentDirectory() : project.Directory, "export"));
        var folder = Path.Combine(outRoot, folderName);
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
        }

        Directory.CreateDirectory(folder);
        List<string> args = ["publish", project.File, "-c", "Release", "-r", target, "-o", folder, "--nologo", "-v", "q", "-clp:NoSummary", "-p:DebugType=none", "-p:DebugSymbols=false", "-p:GenerateFullPaths=true"];
        if (plugins)
        {
            // Plugins are loaded assemblies: the host runs on the installed .NET runtime.
            args.AddRange(["--self-contained", "false"]);
        }
        else
        {
            args.AddRange(["--self-contained", "true", aot ? "-p:OneGameAot=true" : "-p:OneGameTrim=true"]);
        }

        var run = Proc.Run(Proc.Dotnet, args, project.Directory);
        if (run.ExitCode != 0)
        {
            var errors = BuildOutput.Parse(run.Output + "\n" + run.Error).Where(d => d.Severity == "error").Take(8)
                .Select(d => d.File is null ? $"{d.Code}: {d.Message}" : $"{d.File}({d.Line}): {d.Code}: {d.Message}");
            var text = string.Join(" | ", errors);
            if (text.Length == 0)
            {
                text = string.Join(" | ", (run.Output + "\n" + run.Error).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).TakeLast(6));
            }

            throw new CliException(ExitCodes.BuildFailed, "publish_failed", $"dotnet publish failed (exit {run.ExitCode}): {text}");
        }

        foreach (var f in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToArray())
        {
            if (f.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".dbg", StringComparison.OrdinalIgnoreCase)
                || (Path.GetDirectoryName(f) == folder && f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
            {
                File.Delete(f);
            }
        }

        var exe = Path.Combine(folder, exeName);
        if (!File.Exists(exe))
        {
            throw new CliException(ExitCodes.BuildFailed, "no_executable", $"The publish did not produce {exeName}.");
        }

        var skipped = new List<ExportSkipped>();
        if (!plugins)
        {
            ExportAssets.Copy(project.Directory, folder, skipped, warnings);
            if (NewestGlibc(exe) is { } glibc && glibc > SteamRuntimeGlibc)
            {
                warnings.Add(new ExportWarning("glibc_too_new", $"{exeName} needs glibc {glibc.Major}.{glibc.Minor}; a host with an older glibc cannot run it. Build on the oldest system you deploy to."));
            }
        }

        File.WriteAllText(Path.Combine(folder, "THIRD-PARTY-NOTICES.txt"), ExportAssets.Notices(project.Name, props.Version, Program.GetVersion(), folder, false, warnings));
        var launcher = Path.Combine(folder, "run.sh");
        var start = plugins ? $"exec dotnet \"$HERE/{exeName}.dll\" \"$@\"" : $"exec \"$HERE/{exeName}\" \"$@\"";
        File.WriteAllText(launcher, $"#!/bin/sh\nHERE=\"$(cd \"$(dirname \"$(readlink -f \"$0\")\")\" && pwd)\"\ncd \"$HERE\" || exit 1\n{start}\n");
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(launcher, Executable);
            File.SetUnixFileMode(exe, Executable);
        }

        var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(folder, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .Select(rel => Describe(folder, rel))
            .ToArray();
        var archive = noArchive ? null : Pack(outRoot, folderName, target, folder, exeName);
        var result = new ExportResult(true, project.Name, props.Version, Program.GetVersion(), target, "Release", aot ? "nativeaot" : plugins ? "coreclr-framework-dependent" : "coreclr-self-contained", false, false,
            "none", folder, exeName, archive, files, [.. skipped], [.. warnings], Math.Round(clock.Elapsed.TotalSeconds, 1));
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(result, CliJson.Default.ExportResult));
        }
        else
        {
            output.WriteLine($"exported server {project.Name} {props.Version} for {target} ({result.Runtime}): {files.Length} files, {files.Sum(f => f.Size) / 1024} KiB");
            output.WriteLine($"  folder:  {folder}");
            if (archive is not null)
            {
                output.WriteLine($"  archive: {archive.Path} ({archive.Size / 1024} KiB)");
            }

            foreach (var w in warnings)
            {
                output.WriteLine($"  warning {w.Code}: {w.Message}");
            }
        }

        return ExitCodes.Ok;
    }

    /// <summary>The engine's plugin-capable server host project, found from the tool's own checkout.</summary>
    private static ProjectRef FindRunner()
    {
        var root = Environment.GetEnvironmentVariable("ONEGAME_ROOT")
            ?? typeof(ExportVerb).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(m => m.Key == "OneGameRoot")?.Value;
        var dir = root is null ? null : Path.Combine(root, "src", "OneGame.Server.Runner");
        return dir is not null && Directory.Exists(dir)
            ? new ProjectRef(dir, Path.Combine(dir, "OneGame.Server.Runner.csproj"), "onegame-server")
            : throw new CliException(ExitCodes.Usage, "no_engine", "Cannot find the engine sources that hold the plugin host. Set ONEGAME_ROOT to the onegame checkout.");
    }
}
