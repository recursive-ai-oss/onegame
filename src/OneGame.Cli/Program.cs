using System.Reflection;
using OneGame.Cli.Verbs;
using OneGame.Host;

namespace OneGame.Cli;

/// <summary>Entry point of the <c>onegame</c> command.</summary>
internal static class Program
{
    /// <summary>Every verb the command knows. New verbs are listed here.</summary>
    internal static IVerb[] AllVerbs { get; } =
    [
        new HelpVerb(), new NewVerb(), new BuildVerb(), new RunVerb(), new TestVerb(), new CaptureVerb(), new ReplayVerb(), new McpVerb(), new ExportVerb(),
    ];

    private static int Main(string[] args) => Run(args, Console.Out, Console.Error);

    /// <summary>Runs the command with the given arguments and returns the process exit code.</summary>
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var json = false;
        var version = false;
        var rest = new List<string>();
        var passthrough = false;
        foreach (var arg in args)
        {
            // After "--" everything belongs to the game being run.
            if (arg == "--")
            {
                passthrough = true;
                rest.Add(arg);
                continue;
            }

            if (!passthrough && arg == "--json")
            {
                json = true;
            }
            else if (!passthrough && arg == "--version")
            {
                version = true;
            }
            else
            {
                rest.Add(arg);
            }
        }

        if (version)
        {
            if (json)
            {
                output.WriteLine(JsonSerializer.Serialize(new VersionResult("onegame", GetVersion()), CliJson.Default.VersionResult));
            }
            else
            {
                output.WriteLine($"onegame {GetVersion()}");
            }

            return 0;
        }

        // With no verb, show the help.
        var name = rest.Count == 0 ? "help" : rest[0];
        foreach (var verb in AllVerbs)
        {
            if (verb.Name == name)
            {
                try
                {
                    return verb.Run(rest.Count == 0 ? [] : [.. rest.Skip(1)], json, output, error);
                }
                catch (CliException ex)
                {
                    return Fail(json, output, error, ex.ExitCode, ex.Code, ex.Message);
                }
            }
        }

        return Fail(json, output, error, ExitCodes.Usage, "unknown_verb", $"unknown verb '{name}'; run 'onegame help'");
    }

    /// <summary>Prints a failure as one JSON object or as a line on standard error, and returns the exit code.</summary>
    internal static int Fail(bool json, TextWriter output, TextWriter error, int exitCode, string code, string message)
    {
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(new ErrorResult(false, new ErrorInfo(code, message)), CliJson.Default.ErrorResult));
        }
        else
        {
            error.WriteLine($"error: {message}");
        }

        return exitCode;
    }

    internal static string GetVersion()
    {
        var info = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        // Drop the source-revision suffix the SDK appends.
        var plus = info.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? info : info[..plus];
    }
}
