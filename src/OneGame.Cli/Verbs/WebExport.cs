using OneGame.Host;
using System.Security;
using System.Text;
using System.Text.Json;

namespace OneGame.Cli.Verbs;

/// <summary>What a web export produced.</summary>
/// <param name="Folder">The folder to serve: <c>index.html</c>, the script modules and <c>_framework</c>.</param>
/// <param name="Files">Number of files, not counting the precompressed copies.</param>
/// <param name="Bytes">Their total size.</param>
/// <param name="WireBytes">What a browser downloads from a server that sends the Brotli copies.</param>
/// <param name="Configuration">Debug (development tools included) or Release.</param>
/// <param name="Seconds">How long the export took.</param>
internal sealed record WebExportResult(string Folder, int Files, long Bytes, long WireBytes, string Configuration, double Seconds);

/// <summary>
/// Builds a game for the browser. A game's own project is a desktop program; the browser needs the WebAssembly SDK, so the export writes a small
/// wrapper project that compiles the game's own files, embedded resources and references against it and publishes that.
/// </summary>
internal static class WebExport
{
    private static readonly string[] EngineReferences = ["OneGame.Host", "OneGame.Core", "OneGame.UI", "OneGame.Generators", "OneGame.Live"];

    public static WebExportResult Export(ProjectRef project, bool dev, string outFolder)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var configuration = dev ? "Debug" : "Release";
        var query = Proc.Run(Proc.Dotnet, ["msbuild", project.File, "-getItem:Compile,EmbeddedResource,ProjectReference", "-getProperty:OneGameRoot,AssemblyName,RootNamespace", "-nologo"], project.Directory);
        if (query.ExitCode != 0)
        {
            throw new CliException(ExitCodes.BuildFailed, "bad_project", $"Could not read {project.File}: {query.Output.Trim()} {query.Error.Trim()}");
        }

        using var doc = JsonDocument.Parse(query.Output);
        var props = doc.RootElement.GetProperty("Properties");
        var root = props.GetProperty("OneGameRoot").GetString();
        if (string.IsNullOrEmpty(root))
        {
            throw new CliException(ExitCodes.BuildFailed, "no_engine_root", "The project does not set OneGameRoot, so the web export cannot find the engine. Import build/Game.targets as 'onegame new' does.");
        }

        root = Path.GetFullPath(root, project.Directory);
        var assembly = props.GetProperty("AssemblyName").GetString() ?? project.Name;
        var ns = props.GetProperty("RootNamespace").GetString() ?? assembly;
        var items = doc.RootElement.GetProperty("Items");
        var projectDir = Path.GetFullPath(project.Directory) + Path.DirectorySeparatorChar;
        var build = Path.Combine(root, "build") + Path.DirectorySeparatorChar;

        var xml = new StringBuilder();
        xml.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk.WebAssembly\">");
        xml.AppendLine("  <PropertyGroup>");
        xml.AppendLine($"    <OneGameRoot>{Esc(root)}{Path.DirectorySeparatorChar}</OneGameRoot>");
        xml.AppendLine($"    <AssemblyName>{Esc(assembly)}</AssemblyName>");
        xml.AppendLine($"    <RootNamespace>{Esc(ns)}</RootNamespace>");
        xml.AppendLine("    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>");
        xml.AppendLine("    <EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>");
        xml.AppendLine("    <WasmEnableWebcil>false</WasmEnableWebcil>");
        xml.AppendLine($"    <OneGameDev>{(dev ? "true" : "false")}</OneGameDev>");
        xml.AppendLine("  </PropertyGroup>");
        xml.AppendLine("  <Import Project=\"$(OneGameRoot)build/Game.targets\" />");
        xml.AppendLine("  <ItemGroup>");
        foreach (var item in items.GetProperty("Compile").EnumerateArray())
        {
            var full = Full(item, project.Directory);
            if (full.StartsWith(build, StringComparison.Ordinal) || IsGenerated(full, projectDir))
            {
                continue;
            }

            xml.AppendLine($"    <Compile Include=\"{Esc(full)}\" />");
        }

        foreach (var item in items.GetProperty("EmbeddedResource").EnumerateArray())
        {
            var full = Full(item, project.Directory);
            if (IsGenerated(full, projectDir))
            {
                continue;
            }

            var logical = item.TryGetProperty("LogicalName", out var l) && l.GetString() is { Length: > 0 } name ? $" LogicalName=\"{Esc(name)}\"" : "";
            xml.AppendLine($"    <EmbeddedResource Include=\"{Esc(full)}\"{logical} />");
        }

        foreach (var item in items.GetProperty("ProjectReference").EnumerateArray())
        {
            var full = Full(item, project.Directory);
            if (Array.IndexOf(EngineReferences, Path.GetFileNameWithoutExtension(full)) >= 0)
            {
                continue;
            }

            xml.AppendLine($"    <ProjectReference Include=\"{Esc(full)}\" />");
        }

        // The page: the host's script modules and a start page, served beside the runtime files.
        xml.AppendLine("    <Content Include=\"$(OneGameRoot)tools/web/page/index.html\" Link=\"wwwroot/index.html\" />");
        xml.AppendLine("    <Content Include=\"$(OneGameRoot)tools/web/page/boot.mjs\" Link=\"wwwroot/boot.mjs\" />");
        xml.AppendLine("    <Content Include=\"$(OneGameRoot)src/OneGame.Gpu.WebGpu/js/onegame-gpu.js\" Link=\"wwwroot/onegame-gpu.js\" />");
        xml.AppendLine("    <Content Include=\"$(OneGameRoot)src/OneGame.Platform/js/onegame-platform.js\" Link=\"wwwroot/onegame-platform.js\" />");
        xml.AppendLine("    <Content Include=\"$(OneGameRoot)src/OneGame.Audio/js/onegame-audio.js\" Link=\"wwwroot/onegame-audio.js\" />");
        xml.AppendLine("  </ItemGroup>");
        xml.AppendLine("</Project>");

        var work = Path.Combine(projectDir, "obj", "onegame-web");
        Directory.CreateDirectory(work);
        var wrapper = Path.Combine(work, assembly + ".Web.csproj");
        File.WriteAllText(wrapper, xml.ToString());

        var temp = Path.Combine(work, "publish");
        if (Directory.Exists(temp))
        {
            Directory.Delete(temp, true);
        }

        var run = Proc.Run(Proc.Dotnet, ["publish", wrapper, "-c", configuration, "-o", temp, "--nologo", "-v", "q", "-clp:NoSummary", "-p:GenerateFullPaths=true"], work);
        if (run.ExitCode != 0)
        {
            var errors = BuildOutput.Parse(run.Output + "\n" + run.Error).Where(d => d.Severity == "error").Take(8)
                .Select(d => d.File is null ? $"{d.Code}: {d.Message}" : $"{d.File}({d.Line}): {d.Code}: {d.Message}");
            var text = string.Join(" | ", errors);
            if (text.Length == 0)
            {
                text = string.Join(" | ", (run.Output + "\n" + run.Error).Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).TakeLast(6));
            }

            throw new CliException(ExitCodes.BuildFailed, "publish_failed", $"dotnet publish for the browser failed (exit {run.ExitCode}): {text}");
        }

        var www = Path.Combine(temp, "wwwroot");
        if (Directory.Exists(outFolder))
        {
            Directory.Delete(outFolder, true);
        }

        Directory.CreateDirectory(outFolder);
        long bytes = 0, wire = 0;
        var files = 0;
        foreach (var file in Directory.GetFiles(www, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(www, file);
            var target = Path.Combine(outFolder, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
            var length = new FileInfo(file).Length;
            if (file.EndsWith(".br", StringComparison.Ordinal) || file.EndsWith(".gz", StringComparison.Ordinal))
            {
                continue;
            }

            files++;
            bytes += length;
            var br = file + ".br";
            wire += File.Exists(br) ? new FileInfo(br).Length : length;
        }

        return new WebExportResult(outFolder, files, bytes, wire, configuration, clock.Elapsed.TotalSeconds);
    }

    private static string Full(JsonElement item, string directory) => Path.GetFullPath(item.GetProperty("FullPath").GetString() ?? item.GetProperty("Identity").GetString()!, directory);

    private static bool IsGenerated(string path, string projectDir) =>
        path.StartsWith(projectDir + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal) || path.StartsWith(projectDir + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    private static string Esc(string text) => SecurityElement.Escape(text) ?? "";
}
