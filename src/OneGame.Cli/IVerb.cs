namespace OneGame.Cli;

/// <summary>One command-line verb. Add a verb by implementing this and listing it in <see cref="Program.AllVerbs"/>.</summary>
internal interface IVerb
{
    /// <summary>The word typed after <c>onegame</c>.</summary>
    string Name { get; }

    /// <summary>One line shown by <c>help</c>.</summary>
    string Summary { get; }

    /// <summary>Runs the verb; <paramref name="args"/> excludes the verb itself and <c>--json</c>. Returns the exit code.</summary>
    int Run(string[] args, bool json, TextWriter output);
}
