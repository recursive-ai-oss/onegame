using OneGame.Host;
using System.Buffers;
using System.Text;
using System.Text.Json;

namespace OneGame.Cli.Verbs;

internal sealed partial class ExportVerb
{
    /// <summary>
    /// <c>export --target web</c>: a folder any static file server can host (serve <c>.wasm</c> as <c>application/wasm</c>), with Brotli and gzip
    /// copies beside the files. <c>--dev</c> keeps the development tools (a Debug build) for <c>onegame web test</c>.
    /// </summary>
    private static int RunWebExport(ProjectArgs a, bool noArchive, bool json, TextWriter output)
    {
        var dev = a.Game.Remove("--dev");
        _ = noArchive;
        if (a.Game.Count > 0)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", $"Unknown option {a.Game[0]}. Usage: onegame export --target web [--dev] [--out <dir>].");
        }

        var project = Projects.Find(a.Get("--project"));
        var props = ReadProperties(project);
        var outRoot = Path.GetFullPath(a.Get("--out") ?? Path.Combine(project.Directory, "export"));
        var folder = Path.Combine(outRoot, $"{project.Name}-{props.Version}-web");
        var result = WebExport.Export(project, dev, folder);
        var buffer = new ArrayBufferWriter<byte>();
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteBoolean("ok", true);
            w.WriteString("project", project.Name);
            w.WriteString("version", props.Version);
            w.WriteString("target", "web");
            w.WriteString("configuration", result.Configuration);
            w.WriteString("folder", result.Folder);
            w.WriteNumber("files", result.Files);
            w.WriteNumber("bytes", result.Bytes);
            w.WriteNumber("wireBytes", result.WireBytes);
            w.WriteNumber("seconds", Math.Round(result.Seconds, 1));
            w.WriteEndObject();
        }

        output.WriteLine(json
            ? Encoding.UTF8.GetString(buffer.WrittenSpan)
            : $"exported {project.Name} {props.Version} for the web ({result.Configuration}): {result.Files} files, {result.Bytes / 1024} KiB ({result.WireBytes / 1024} KiB with Brotli) in {result.Folder}");
        return ExitCodes.Ok;
    }
}
