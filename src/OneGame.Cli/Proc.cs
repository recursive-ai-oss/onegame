using System.Diagnostics;
using System.Text.RegularExpressions;

namespace OneGame.Cli;

/// <summary>The result of running another program.</summary>
/// <param name="ExitCode">Its exit code.</param>
/// <param name="Output">Everything it wrote to standard output.</param>
/// <param name="Error">Everything it wrote to standard error.</param>
internal sealed record ProcessResult(int ExitCode, string Output, string Error);

/// <summary>Runs <c>dotnet</c> and the game.</summary>
internal static class Proc
{
    /// <summary>The dotnet executable: the one running this tool if known, else the one on the path.</summary>
    public static string Dotnet => Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet";

    /// <summary>Runs a program to completion, capturing its output.</summary>
    public static ProcessResult Run(string file, IEnumerable<string> args, string workingDirectory, Action<string>? onOutputLine = null, IReadOnlyDictionary<string, string>? environment = null)
    {
        var start = Start(file, args, workingDirectory);
        if (environment is not null)
        {
            foreach (var (name, value) in environment)
            {
                start.Environment[name] = value;
            }
        }

        using var p = new Process { StartInfo = start };
        var output = new StringBuilder();
        var error = new StringBuilder();
        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (output)
                {
                    output.AppendLine(e.Data);
                }

                onOutputLine?.Invoke(e.Data);
            }
        };
        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                lock (error)
                {
                    error.AppendLine(e.Data);
                }
            }
        };
        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();
        p.WaitForExit();
        return new ProcessResult(p.ExitCode, output.ToString(), error.ToString());
    }

    /// <summary>Prepares a process start with an argument list (no shell, no quoting problems).</summary>
    public static ProcessStartInfo Start(string file, IEnumerable<string> args, string workingDirectory)
    {
        var info = new ProcessStartInfo(file)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            UseShellExecute = false,
        };
        foreach (var a in args)
        {
            info.ArgumentList.Add(a);
        }

        // Quieter, steadier builds when an agent is the one reading them.
        info.Environment["DOTNET_NOLOGO"] = "1";
        info.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        info.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        return info;
    }
}

/// <summary>One compiler or build message.</summary>
/// <param name="Severity">error or warning.</param>
/// <param name="Code">The diagnostic id, such as CS0103 or OG0008.</param>
/// <param name="Message">The message.</param>
/// <param name="File">The source file, when the message has one.</param>
/// <param name="Line">Line in the file.</param>
/// <param name="Column">Column in the file.</param>
internal sealed record Diagnostic(string Severity, string Code, string Message, string? File, int Line, int Column);

/// <summary>Reads MSBuild's text output.</summary>
internal static partial class BuildOutput
{
    [GeneratedRegex(@"^(?<file>.+?)\((?<line>\d+),(?<col>\d+)\): (?<sev>error|warning) (?<code>[A-Za-z]+\d+): (?<msg>.*?)(?: \[[^\]]+\])?$")]
    private static partial Regex WithFile();

    [GeneratedRegex(@"^(?:\S+ : )?(?<sev>error|warning) (?<code>[A-Za-z]+\d+): (?<msg>.*?)(?: \[[^\]]+\])?$")]
    private static partial Regex WithoutFile();

    /// <summary>
    /// The last lines of a failed build's output that are not MSBuild diagnostics: what the native linker or another tool printed itself.
    /// MSBuild reports such a failure as only the tool's command line, so the reason would otherwise be lost.
    /// </summary>
    public static string ToolTail(string text, int lines = 6) =>
        string.Join(" | ", text.Split('\n').Select(l => l.Trim())
            .Where(l => l.Length is > 0 and < 400 && !l.Contains("MSB3073", StringComparison.Ordinal) && !l.StartsWith("Time Elapsed", StringComparison.Ordinal) && !l.EndsWith("Error(s)", StringComparison.Ordinal) && !l.EndsWith("Warning(s)", StringComparison.Ordinal))
            .TakeLast(lines));

    /// <summary>Extracts the distinct errors and warnings from build output.</summary>
    public static List<Diagnostic> Parse(string text)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<Diagnostic>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            Diagnostic? d = null;
            var m = WithFile().Match(line);
            if (m.Success)
            {
                d = new Diagnostic(m.Groups["sev"].Value, m.Groups["code"].Value, m.Groups["msg"].Value, m.Groups["file"].Value,
                    int.Parse(m.Groups["line"].Value, System.Globalization.CultureInfo.InvariantCulture), int.Parse(m.Groups["col"].Value, System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                m = WithoutFile().Match(line);
                if (m.Success)
                {
                    d = new Diagnostic(m.Groups["sev"].Value, m.Groups["code"].Value, m.Groups["msg"].Value, null, 0, 0);
                }
            }

            if (d is not null && seen.Add($"{d.Severity}|{d.Code}|{d.File}|{d.Line}|{d.Column}|{d.Message}"))
            {
                list.Add(d);
            }
        }

        return list;
    }
}
