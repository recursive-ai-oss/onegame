using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>Packages a game as a folder (and an archive) that contains everything it ships and nothing else.</summary>
internal sealed partial class ExportVerb : IVerb
{
    public string Name => "export";

    public string Summary => "Package the game: onegame export --target linux-x64|win-x64|web [--steam] [--out <dir>] [--no-archive] [--no-aot]; or its dedicated server: export --server [--plugins].";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        var a = new ProjectArgs(args, "--target", "--out", "--steam-redist");
        if (a.Get("--target") == "web")
        {
            return RunWebExport(a, a.Game.Remove("--no-archive"), json, output);
        }

        var steam = a.Game.Remove("--steam");
        var noArchive = a.Game.Remove("--no-archive");
        var noAot = a.Game.Remove("--no-aot");
        var server = a.Game.Remove("--server");
        var plugins = a.Game.Remove("--plugins");
        if (plugins && !server)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "--plugins goes with --server: onegame export --server --plugins packages the plugin-capable server host.");
        }

        if (server)
        {
            return RunServerExport(a, steam, plugins, noArchive, noAot, json, output);
        }

        if (a.Game.Count > 0)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", $"Unknown option {a.Game[0]}. Usage: onegame export --target linux-x64|win-x64 [--steam] [--steam-redist <dir>] [--out <dir>] [--no-archive] [--no-aot].");
        }

        var target = a.Get("--target") ?? (OperatingSystem.IsWindows() ? "win-x64" : "linux-x64");
        if (target is not ("linux-x64" or "win-x64"))
        {
            throw new CliException(ExitCodes.Usage, "bad_target", $"Unknown target '{target}'. Targets: linux-x64, win-x64, web.");
        }

        var project = Projects.Find(a.Get("--project"));
        var clock = Stopwatch.StartNew();
        var warnings = new List<ExportWarning>();

        // NativeAOT links with the target's own toolchain, so it needs a host of the same OS. Anywhere else the game ships as a
        // self-contained, trimmed CoreCLR build with the same layout.
        var hostMatches = target.StartsWith("win", StringComparison.Ordinal) ? OperatingSystem.IsWindows() : OperatingSystem.IsLinux();
        var aot = hostMatches && !noAot;
        if (!aot)
        {
            warnings.Add(new ExportWarning("not_native_aot", hostMatches
                ? "Built as a self-contained CoreCLR program because --no-aot was given."
                : $"NativeAOT for {target} needs a {(target.StartsWith("win", StringComparison.Ordinal) ? "Windows" : "Linux")} host; this is a self-contained, trimmed CoreCLR build. Run the same command on a matching host for the NativeAOT build."));
        }

        var props = ReadProperties(project);
        var version = props.Version;
        var exeName = props.AssemblyName + (target.StartsWith("win", StringComparison.Ordinal) ? ".exe" : "");
        var folderName = $"{project.Name}-{version}-{target}";
        var outRoot = Path.GetFullPath(a.Get("--out") ?? Path.Combine(project.Directory, "export"));
        var folder = Path.Combine(outRoot, folderName);

        // Find the redistributable before the slow part, so a missing one fails fast.
        string? redist = steam ? FindSteamLibrary(target, a.Get("--steam-redist")) : null;

        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
        }

        Directory.CreateDirectory(folder);
        Publish(project, target, aot, folder);

        // Debug symbols and the XML documentation of the engine assemblies are not part of what ships.
        foreach (var f in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).ToArray())
        {
            if (f.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".dbg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".dSYM", StringComparison.OrdinalIgnoreCase)
                || (Path.GetDirectoryName(f) == folder && f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)))
            {
                File.Delete(f);
            }
        }

        // A self-contained publish carries crash-dump and debugger helpers and alternative garbage collectors. A game runs without them.
        foreach (var f in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly).ToArray())
        {
            var n = Path.GetFileName(f);
            if (n.StartsWith("createdump", StringComparison.Ordinal) || n.Contains("mscordaccore", StringComparison.Ordinal) || n.Contains("mscordbi", StringComparison.Ordinal)
                || n.Contains("coreclrtraceptprovider", StringComparison.Ordinal) || n.Contains("msquic", StringComparison.Ordinal) || n.Contains("clrgc", StringComparison.Ordinal))
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
        ExportAssets.Copy(project.Directory, folder, skipped, warnings);

        if (redist is not null)
        {
            File.Copy(redist, Path.Combine(folder, Path.GetFileName(redist)), true);
        }

        // With this file present Steam's relaunch check always passes and the game would run outside Steam's control.
        foreach (var f in Directory.EnumerateFiles(folder, "steam_appid.txt", SearchOption.AllDirectories).ToArray())
        {
            File.Delete(f);
        }

        if (File.Exists(Path.Combine(project.Directory, "steam_appid.txt")))
        {
            skipped.Add(new ExportSkipped("steam_appid.txt", "never shipped: it disables the relaunch-through-Steam check"));
        }

        if (target == "linux-x64" && NewestGlibc(exe) is { } glibc && glibc > SteamRuntimeGlibc)
        {
            warnings.Add(new ExportWarning("glibc_too_new",
                $"{exeName} needs glibc {glibc.Major}.{glibc.Minor}, newer than {SteamRuntimeGlibc.Major}.{SteamRuntimeGlibc.Minor} in Steam's Linux runtime (and so on the Steam Deck). Build on an older system, or inside the Steam Linux Runtime SDK: see docs/exporting.md."));
        }

        if (DevToolsPresent(folder))
        {
            throw new CliException(ExitCodes.RuntimeError, "dev_tools_present", "The export contains the live-control listener (OneGame.Live). Exports are Release builds; check the project for OneGameDev=true.");
        }

        File.WriteAllText(Path.Combine(folder, "THIRD-PARTY-NOTICES.txt"), ExportAssets.Notices(project.Name, version, Program.GetVersion(), folder, redist is not null, warnings));

        if (target == "linux-x64")
        {
            var launcher = Path.Combine(folder, "run.sh");
            // Starts the game from its own folder whatever the caller's directory is, so relative content and the Steam library resolve.
            File.WriteAllText(launcher, $"#!/bin/sh\nHERE=\"$(cd \"$(dirname \"$(readlink -f \"$0\")\")\" && pwd)\"\ncd \"$HERE\" || exit 1\nexec \"$HERE/{props.AssemblyName}\" \"$@\"\n");
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(launcher, Executable);
                File.SetUnixFileMode(exe, Executable);
            }
        }

        var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(folder, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .Select(rel => Describe(folder, rel))
            .ToArray();

        ExportArchive? archive = null;
        if (!noArchive)
        {
            archive = Pack(outRoot, folderName, target, folder, props.AssemblyName);
        }

        var result = new ExportResult(true, project.Name, version, Program.GetVersion(), target, "Release", aot ? "nativeaot" : "coreclr-self-contained", steam, false,
            "2d", folder, exeName, archive, files, [.. skipped], [.. warnings], Math.Round(clock.Elapsed.TotalSeconds, 1));
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(result, CliJson.Default.ExportResult));
        }
        else
        {
            output.WriteLine($"exported {project.Name} {version} for {target} ({result.Runtime}): {files.Length} files, {files.Sum(f => f.Size) / 1024} KiB");
            output.WriteLine($"  folder:  {folder}");
            if (archive is not null)
            {
                output.WriteLine($"  archive: {archive.Path} ({archive.Size / 1024} KiB)");
            }

            foreach (var s in skipped)
            {
                output.WriteLine($"  skipped {s.Path}: {s.Reason}");
            }

            foreach (var w in warnings)
            {
                output.WriteLine($"  warning {w.Code}: {w.Message}");
            }
        }

        return ExitCodes.Ok;
    }

    private const UnixFileMode Executable = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

    private const UnixFileMode Plain = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;

    private static void Publish(ProjectRef project, string target, bool aot, string folder)
    {
        List<string> args = ["publish", project.File, "-c", "Release", "-r", target, "--self-contained", "true", "-o", folder, "--nologo", "-v", "q", "-clp:NoSummary",
            "-p:OneGameDev=false", "-p:DebugType=none", "-p:DebugSymbols=false", "-p:GenerateFullPaths=true"];
        if (aot)
        {
            args.Add("-p:OneGameAot=true");
        }
        else
        {
            // Trimming is safe because games are written to the trim rules; ReadyToRun starts faster where the cross-OS pack is available.
            args.Add("-p:OneGameTrim=true");
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
    }

    private static (string AssemblyName, string Version) ReadProperties(ProjectRef project)
    {
        var run = Proc.Run(Proc.Dotnet, ["msbuild", project.File, "-getProperty:AssemblyName", "-getProperty:Version", "-nologo"], project.Directory);
        if (run.ExitCode != 0)
        {
            throw new CliException(ExitCodes.BuildFailed, "bad_project", $"Could not read {project.File}: {run.Output.Trim()} {run.Error.Trim()}");
        }

        using var doc = JsonDocument.Parse(run.Output);
        var props = doc.RootElement.GetProperty("Properties");
        var name = props.GetProperty("AssemblyName").GetString();
        var version = props.GetProperty("Version").GetString();
        return (string.IsNullOrEmpty(name) ? project.Name : name, string.IsNullOrEmpty(version) ? "1.0.0" : version);
    }

    /// <summary>
    /// Finds Valve's library for the target in a developer-supplied location, in this order: <c>--steam-redist</c>, then the
    /// <c>ONEGAME_STEAM_REDIST</c> variable, each either a folder with <c>&lt;target&gt;/&lt;file&gt;</c> or the file's own folder; then the
    /// Steamworks SDK folder in <c>ONEGAME_STEAM_SDK</c> (<c>redistributable_bin/win64</c> or <c>linux64</c>).
    /// </summary>
    internal static string FindSteamLibrary(string target, string? option)
    {
        var file = target == "win-x64" ? "steam_api64.dll" : "libsteam_api.so";
        var sdkSub = target == "win-x64" ? "win64" : "linux64";
        var searched = new List<string>();
        foreach (var root in new[] { option, Environment.GetEnvironmentVariable("ONEGAME_STEAM_REDIST") })
        {
            if (string.IsNullOrEmpty(root))
            {
                continue;
            }

            foreach (var candidate in new[] { Path.Combine(root, target, file), Path.Combine(root, file) })
            {
                searched.Add(candidate);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
        }

        if (Environment.GetEnvironmentVariable("ONEGAME_STEAM_SDK") is { Length: > 0 } sdk)
        {
            var candidate = Path.Combine(sdk, "redistributable_bin", sdkSub, file);
            searched.Add(candidate);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        throw new CliException(ExitCodes.RuntimeError, "steam_redist_missing",
            $"--steam needs {file} from your Steamworks SDK. Pass --steam-redist <folder>, or set ONEGAME_STEAM_REDIST (folder with {target}/{file}) or ONEGAME_STEAM_SDK (the SDK folder)."
            + (searched.Count > 0 ? " Looked in: " + string.Join(", ", searched) : ""));
    }

    /// <summary>The glibc the Steam Linux Runtime 3 (sniper) provides, which is what Steam runs Linux games against.</summary>
    private static readonly Version SteamRuntimeGlibc = new(2, 31);

    /// <summary>The newest glibc symbol version an ELF program asks for, found by reading the version names in its file; null if none.</summary>
    internal static Version? NewestGlibc(string file)
    {
        var bytes = File.ReadAllBytes(file).AsSpan();
        var prefix = "GLIBC_"u8;
        Version? newest = null;
        for (var i = bytes.IndexOf(prefix); i >= 0;)
        {
            var rest = bytes[(i + prefix.Length)..];
            var length = 0;
            while (length < rest.Length && length < 12 && (rest[length] is >= (byte)'0' and <= (byte)'9' || rest[length] == (byte)'.'))
            {
                length++;
            }

            if (Version.TryParse(System.Text.Encoding.ASCII.GetString(rest[..length]), out var v) && (newest is null || v > newest))
            {
                newest = v;
            }

            var next = rest.IndexOf(prefix);
            i = next < 0 ? -1 : i + prefix.Length + next;
        }

        return newest;
    }

    /// <summary>Whether any program file in the export mentions the live-control assembly or the shader hot-reload watcher.</summary>
    private static bool DevToolsPresent(string folder)
    {
        var needle = "OneGame.Live"u8.ToArray();
        var reloader = "ShaderReloader"u8.ToArray();
        foreach (var f in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
        {
            if (f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && Path.GetFileName(f).StartsWith("OneGame.Live", StringComparison.Ordinal))
            {
                return true;
            }

            var ext = Path.GetExtension(f);
            if (ext is ".txt" or ".json" or ".so" or ".md" or ".sh")
            {
                continue;
            }

            var bytes = File.ReadAllBytes(f);
            if (bytes.AsSpan().IndexOf(needle) >= 0 || bytes.AsSpan().IndexOf(reloader) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static ExportFile Describe(string folder, string relative)
    {
        var path = Path.Combine(folder, relative);
        using var stream = File.OpenRead(path);
        return new ExportFile(relative, stream.Length, Convert.ToHexStringLower(SHA256.HashData(stream)));
    }

    /// <summary>Writes a .tar.gz (Linux) or .zip (Windows) with a fixed timestamp and sorted entries, so the same folder gives the same archive.</summary>
    private static ExportArchive Pack(string outRoot, string folderName, string target, string folder, string assemblyName)
    {
        var timestamp = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var files = Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(folder, f).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray();
        string path;
        string format;
        if (target == "linux-x64")
        {
            format = "tar.gz";
            path = Path.Combine(outRoot, folderName + ".tar.gz");
            using var raw = File.Create(path);
            using var gz = new GZipStream(raw, CompressionLevel.SmallestSize);
            using var tar = new TarWriter(gz, TarEntryFormat.Pax);
            foreach (var rel in files)
            {
                var full = Path.Combine(folder, rel);
                using var data = File.OpenRead(full);
                var entry = new PaxTarEntry(TarEntryType.RegularFile, $"{folderName}/{rel}")
                {
                    Mode = rel == assemblyName || rel == "run.sh" ? Executable : Plain,
                    ModificationTime = timestamp,
                    DataStream = data,
                    Uid = 0,
                    Gid = 0,
                    UserName = "",
                    GroupName = "",
                };
                tar.WriteEntry(entry);
            }
        }
        else
        {
            format = "zip";
            path = Path.Combine(outRoot, folderName + ".zip");
            using var raw = File.Create(path);
            using var zip = new ZipArchive(raw, ZipArchiveMode.Create);
            foreach (var rel in files)
            {
                var entry = zip.CreateEntry($"{folderName}/{rel}", CompressionLevel.SmallestSize);
                entry.LastWriteTime = timestamp;
                using var s = entry.Open();
                using var data = File.OpenRead(Path.Combine(folder, rel));
                data.CopyTo(s);
            }
        }

        using var check = File.OpenRead(path);
        return new ExportArchive(path, format, check.Length, Convert.ToHexStringLower(SHA256.HashData(check)));
    }
}

/// <summary>What goes into an export besides the program: content folders, and the licence text.</summary>
internal static class ExportAssets
{
    /// <summary>The project folders that ship, copied under the same name beside the program. Anything else in the project is left out.</summary>
    internal static readonly string[] ContentFolders = ["fonts", "art", "data"];

    /// <summary>Project folders that hold build products, tests or tooling, never content. Not reported.</summary>
    private static readonly string[] Quiet = ["bin", "obj", "artifacts", "captures", "export", "scenarios", "tests", "node_modules"];

    private static readonly Dictionary<string, string[]> AllowedExtensions = new(StringComparer.Ordinal)
    {
        ["fonts"] = [".ttf", ".otf", ".txt", ".md"],
        ["art"] = [".svg", ".png", ".jpg", ".jpeg", ".psd", ".webp"],
    };

    /// <summary>The size above which a content file is reported (it still ships).</summary>
    internal const long LargeFile = 64L * 1024 * 1024;

    public static void Copy(string projectDir, string folder, List<ExportSkipped> skipped, List<ExportWarning> warnings)
    {
        foreach (var name in ContentFolders)
        {
            var source = Path.Combine(projectDir, name);
            if (!Directory.Exists(source))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                var relative = Path.GetRelativePath(projectDir, file).Replace('\\', '/');
                if (AllowedExtensions.TryGetValue(name, out var allowed) && Array.IndexOf(allowed, Path.GetExtension(file).ToLowerInvariant()) < 0)
                {
                    skipped.Add(new ExportSkipped(relative, $"not a {name} file type ({string.Join(" ", allowed)})"));
                    continue;
                }

                var length = new FileInfo(file).Length;
                if (length > LargeFile)
                {
                    warnings.Add(new ExportWarning("large_file", $"{relative} is {length / (1024 * 1024)} MiB."));
                }

                var target = Path.Combine(folder, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, true);
            }
        }

        foreach (var dir in Directory.EnumerateDirectories(projectDir))
        {
            var name = Path.GetFileName(dir);
            if (name.StartsWith('.') || Array.IndexOf(ContentFolders, name) >= 0 || Array.IndexOf(Quiet, name) >= 0)
            {
                continue;
            }

            skipped.Add(new ExportSkipped(name + "/", $"not a content folder (shipped folders: {string.Join(", ", ContentFolders)})"));
        }
    }

    /// <summary>The text of <c>THIRD-PARTY-NOTICES.txt</c>: the engine's list, then the licences of this game's fonts and platform binaries.</summary>
    public static string Notices(string game, string version, string engineVersion, string folder, bool steam, List<ExportWarning> warnings)
    {
        var text = new StringBuilder();
        text.Append("Third-party notices for ").Append(game).Append(' ').Append(version).Append("\n(built with onegame ").Append(engineVersion).Append(")\n\n");
        text.Append(EmbeddedNotices()).Append('\n');

        var fonts = Path.Combine(folder, "fonts");
        if (Directory.Exists(fonts))
        {
            var licenses = Directory.EnumerateFiles(fonts, "*", SearchOption.AllDirectories)
                .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".txt" or ".md")
                .Order(StringComparer.Ordinal)
                .ToArray();
            var faces = Directory.EnumerateFiles(fonts, "*", SearchOption.AllDirectories).Where(f => Path.GetExtension(f).ToLowerInvariant() is ".ttf" or ".otf").Select(Path.GetFileName).Order(StringComparer.Ordinal).ToArray();
            text.Append("\n# Fonts shipped with this game\n\n").Append(string.Join(", ", faces)).Append('\n');
            if (faces.Length > 0 && licenses.Length == 0)
            {
                warnings.Add(new ExportWarning("font_licence_missing", "fonts/ has font files but no licence text (a .txt or .md file next to them). Add the fonts' licences."));
                text.Append("\nWARNING: no licence file was found next to these fonts.\n");
            }

            foreach (var license in licenses)
            {
                text.Append("\n## ").Append(Path.GetRelativePath(folder, license).Replace('\\', '/')).Append("\n\n").Append(File.ReadAllText(license).Trim()).Append('\n');
            }
        }

        if (steam)
        {
            text.Append("\n# Steam\n\nThis build includes Valve's Steam API library (steam_api64.dll or libsteam_api.so), distributed under the Steamworks SDK Access Agreement.\n");
        }

        return text.ToString();
    }

    private static string EmbeddedNotices()
    {
        using var stream = typeof(ExportAssets).Assembly.GetManifestResourceStream("THIRD-PARTY-NOTICES.md")
            ?? throw new CliException(ExitCodes.RuntimeError, "no_notices", "The tool was built without its licence notices.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Writes the font and licence a new game starts with.</summary>
    public static void WriteDefaultFont(string projectDir)
    {
        foreach (var name in new[] { "NotoSans-Regular.ttf", "OFL.txt" })
        {
            using var stream = typeof(ExportAssets).Assembly.GetManifestResourceStream("fonts/" + name)
                ?? throw new CliException(ExitCodes.RuntimeError, "no_font", "The tool was built without its default font.");
            var path = Path.Combine(projectDir, "fonts", name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var file = File.Create(path);
            stream.CopyTo(file);
        }
    }
}
