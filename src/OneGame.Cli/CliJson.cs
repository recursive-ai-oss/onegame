using OneGame.Testing;

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

/// <summary>One file of an export.</summary>
/// <param name="Path">Path relative to the export folder, with forward slashes.</param>
/// <param name="Size">Size in bytes.</param>
/// <param name="Sha256">Lower-case hex SHA-256 of the contents.</param>
internal sealed record ExportFile(string Path, long Size, string Sha256);

/// <summary>A project file the export left out, and why.</summary>
internal sealed record ExportSkipped(string Path, string Reason);

/// <summary>Something about an export worth a look that did not stop it.</summary>
internal sealed record ExportWarning(string Code, string Message);

/// <summary>The archive made from an export folder.</summary>
internal sealed record ExportArchive(string Path, string Format, long Size, string Sha256);

/// <summary>Result of <c>onegame export</c>.</summary>
internal sealed record ExportResult(
    bool Ok, string Game, string GameVersion, string EngineVersion, string Target, string Configuration, string Runtime, bool Steam, bool DevTools,
    string Renderer, string Directory, string Executable, ExportArchive? Archive, ExportFile[] Files, ExportSkipped[] Skipped, ExportWarning[] Warnings, double Seconds);

/// <summary>What the <c>cluster</c> verbs print with <c>--json</c>: whether it went well, and the result object.</summary>
internal sealed record ClusterResult<T>(bool Ok, T Result);

/// <summary>Result of <c>onegame cluster test</c>.</summary>
internal sealed record ClusterTestResult(int Passed, int Failed, ScenarioResult[] Scenarios, DownReport Down);

/// <summary>Source-generated JSON metadata; the command line never uses reflection-based serialization.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(VersionResult))]
[JsonSerializable(typeof(HelpResult))]
[JsonSerializable(typeof(ErrorResult))]
[JsonSerializable(typeof(NewResult))]
[JsonSerializable(typeof(BuildResult))]
[JsonSerializable(typeof(TestResult))]
[JsonSerializable(typeof(ExportResult))]
[JsonSerializable(typeof(AssetInspectResult))]
[JsonSerializable(typeof(Verbs.ViewResult))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(ClusterResult<ClusterState>))]
[JsonSerializable(typeof(ClusterResult<ClusterStatus>))]
[JsonSerializable(typeof(ClusterResult<ClusterHashReport>))]
[JsonSerializable(typeof(ClusterResult<DownReport>))]
[JsonSerializable(typeof(ClusterResult<ClusterTestResult>))]
[JsonSerializable(typeof(string))]
internal sealed partial class CliJson : JsonSerializerContext
{
}
