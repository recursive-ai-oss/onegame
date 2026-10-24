namespace OneGame.Cli;

/// <summary>A command-line verb. Add a verb by implementing this and listing it in <see cref="Program.AllVerbs"/>.</summary>
internal interface IVerb
{
    /// <summary>The word typed after <c>onegame</c>.</summary>
    string Name { get; }

    /// <summary>One line shown by <c>help</c>.</summary>
    string Summary { get; }

    /// <summary>Runs the verb; <paramref name="args"/> excludes the verb itself and <c>--json</c>. Returns the exit code.</summary>
    int Run(string[] args, bool json, TextWriter output, TextWriter error);
}

/// <summary>A verb failed in a way worth a stable code. <see cref="Program"/> prints it as JSON or text.</summary>
internal sealed class CliException : Exception
{
    public CliException(int exitCode, string code, string message)
        : base(message)
    {
        ExitCode = exitCode;
        Code = code;
    }

    public int ExitCode { get; }

    public string Code { get; }
}
