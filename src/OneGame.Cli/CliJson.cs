namespace OneGame.Cli;

/// <summary>Result of <c>onegame --version --json</c>.</summary>
internal sealed record VersionResult(string Name, string Version);

/// <summary>One verb in the <c>help</c> listing.</summary>
internal sealed record CommandInfo(string Name, string Summary);

/// <summary>Result of <c>onegame help --json</c>.</summary>
internal sealed record HelpResult(string Name, string Usage, CommandInfo[] Commands, string[] Options, ExitCodeInfo[] ExitCodes);

/// <summary>One exit code the tool uses.</summary>
internal sealed record ExitCodeInfo(int Code, string Meaning);

/// <summary>What went wrong, for a program to act on.</summary>
internal sealed record ErrorInfo(string Code, string Message);

/// <summary>Printed with <c>--json</c> when a verb fails before it has a result.</summary>
internal sealed record ErrorResult(bool Ok, ErrorInfo Error);

/// <summary>Result of <c>onegame new</c>.</summary>
internal sealed record NewResult(bool Ok, string Name, string Directory, string[] Files, string Engine, string[] Next);

/// <summary>Result of <c>onegame build</c>.</summary>
internal sealed record BuildResult(bool Ok, string Project, string Configuration, int Errors, int Warnings, Diagnostic[] Diagnostics, double Seconds);

/// <summary>The outcome of the C# unit tests of a game.</summary>
internal sealed record UnitTestSummary(bool Ran, bool Ok, int Total, int Passed, int Failed, int Skipped, string[] Projects, string? Output);

/// <summary>Result of <c>onegame test</c>.</summary>
internal sealed record TestResult(bool Ok, JsonElement? Scenarios, UnitTestSummary? UnitTests, BuildResult? Build);

/// <summary>Result of <c>onegame export</c> until export exists.</summary>
internal sealed record ExportResult(bool Ok);

/// <summary>Source-generated JSON metadata; the command line never uses reflection-based serialization.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(VersionResult))]
[JsonSerializable(typeof(HelpResult))]
[JsonSerializable(typeof(ErrorResult))]
[JsonSerializable(typeof(NewResult))]
[JsonSerializable(typeof(BuildResult))]
[JsonSerializable(typeof(TestResult))]
[JsonSerializable(typeof(JsonElement))]
internal sealed partial class CliJson : JsonSerializerContext
{
}
