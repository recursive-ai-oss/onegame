namespace OneGame.Cli;

/// <summary>Result of <c>onegame --version --json</c>.</summary>
internal sealed record VersionResult(string Name, string Version);

/// <summary>One verb in the <c>help</c> listing.</summary>
internal sealed record CommandInfo(string Name, string Summary);

/// <summary>Result of <c>onegame help --json</c>.</summary>
internal sealed record HelpResult(string Name, string Usage, CommandInfo[] Commands, string[] Options);

/// <summary>Result printed for a usage error when <c>--json</c> is given.</summary>
internal sealed record ErrorResult(string Error);

/// <summary>Source-generated JSON metadata; the command line never uses reflection-based serialization.</summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = false)]
[JsonSerializable(typeof(VersionResult))]
[JsonSerializable(typeof(HelpResult))]
[JsonSerializable(typeof(ErrorResult))]
internal sealed partial class CliJson : JsonSerializerContext
{
}
