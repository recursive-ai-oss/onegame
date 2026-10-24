namespace OneGame.Cli.Verbs;

/// <summary>Lists the verbs and global options.</summary>
internal sealed class HelpVerb : IVerb
{
    public string Name => "help";

    public string Summary => "List the commands and options.";

    public int Run(string[] args, bool json, TextWriter output)
    {
        var verbs = Program.AllVerbs;
        if (json)
        {
            var commands = new CommandInfo[verbs.Length];
            for (var i = 0; i < verbs.Length; i++)
            {
                commands[i] = new CommandInfo(verbs[i].Name, verbs[i].Summary);
            }

            output.WriteLine(JsonSerializer.Serialize(
                new HelpResult("onegame", "onegame <verb> [options]", commands, ["--json", "--version"]),
                CliJson.Default.HelpResult));
            return 0;
        }

        output.WriteLine("usage: onegame <verb> [options]");
        output.WriteLine();
        foreach (var verb in verbs)
        {
            output.WriteLine($"  {verb.Name,-10} {verb.Summary}");
        }

        output.WriteLine();
        output.WriteLine("options:");
        output.WriteLine("  --version  Print the version.");
        output.WriteLine("  --json     Print one JSON object instead of text.");
        return 0;
    }
}
