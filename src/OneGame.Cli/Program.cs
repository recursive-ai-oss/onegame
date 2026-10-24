using System.Reflection;
using OneGame.Cli.Verbs;

namespace OneGame.Cli;

/// <summary>Entry point of the <c>onegame</c> command.</summary>
internal static class Program
{
    /// <summary>Every verb the command knows. New verbs are listed here.</summary>
    internal static IVerb[] AllVerbs { get; } = [new HelpVerb()];

    private static int Main(string[] args) => Run(args, Console.Out, Console.Error);

    /// <summary>Runs the command with the given arguments and returns the process exit code.</summary>
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var json = false;
        var version = false;
        var rest = new List<string>();
        foreach (var arg in args)
        {
            switch (arg)
            {
                case "--json":
                    json = true;
                    break;
                case "--version":
                    version = true;
                    break;
                default:
                    rest.Add(arg);
                    break;
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
                return verb.Run(rest.Count == 0 ? [] : [.. rest.Skip(1)], json, output);
            }
        }

        var message = $"unknown verb '{name}'; run 'onegame help'";
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(new ErrorResult(message), CliJson.Default.ErrorResult));
        }
        else
        {
            error.WriteLine(message);
        }

        return 2;
    }

    private static string GetVersion()
    {
        var info = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        // Drop the source-revision suffix the SDK appends.
        var plus = info.IndexOf('+', StringComparison.Ordinal);
        return plus < 0 ? info : info[..plus];
    }
}
