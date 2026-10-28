using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OneGame.Generators.Tests;

internal static class Harness
{
    private static readonly MetadataReference[] References = BuildReferences();

    private static MetadataReference[] BuildReferences()
    {
        var trusted = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var list = trusted.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();
        list.Add(MetadataReference.CreateFromFile(typeof(OneGame.Core.Fixed).Assembly.Location));
        list.Add(MetadataReference.CreateFromFile(typeof(OneGame.Tweens.TweenSet).Assembly.Location));
        list.Add(MetadataReference.CreateFromFile(typeof(OneGame.Gltf.Animator).Assembly.Location));
        return [.. list];
    }

    public const string Usings =
        "using System; using System.Collections.Generic; using System.Linq; using System.IO; using System.Threading; "
        + "using System.Threading.Tasks; using System.Diagnostics; using System.Net.Http; using System.Security.Cryptography; "
        + "using OneGame.Core;\n";

    /// <summary>Wraps members into an entity with a couple of synced properties and a command, and appends extra top-level types.</summary>
    public static string Entity(string members, string extra = "")
        => Usings + "namespace T;\n"
        + "[Entity] public partial class E : Entity {\n"
        + "  [Synced] public partial int N { get; set; }\n"
        + "  [Synced] public SyncList<int> L { get; } = new();\n"
        + "  [Command] public void Bump() { N++; }\n"
        + members + "\n}\n" + extra;

    public static CSharpCompilation Compile(string source, params string[] more)
    {
        var trees = new[] { source }.Concat(more).Select(s => CSharpSyntaxTree.ParseText(s, new CSharpParseOptions(LanguageVersion.Latest))).ToArray();
        return CSharpCompilation.Create("T", trees, References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    /// <summary>Runs the generator and the analyzer; returns every error-level diagnostic, including compile errors in generated code.</summary>
    public static async Task<List<Diagnostic>> Diagnose(string source, params string[] more)
    {
        var compilation = Compile(source, more);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new EntityGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        var analyzed = await output.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new SimRulesAnalyzer(), new PredictionAnalyzer()))
            .GetAllDiagnosticsAsync(CancellationToken.None);
        return generatorDiagnostics.Concat(analyzed).Where(d => d.Severity == DiagnosticSeverity.Error).Distinct().ToList();
    }

    /// <summary>Like <see cref="Diagnose"/> but returns warnings too, and can set <c>onegame_sim_trusted</c> as an editorconfig option.</summary>
    public static async Task<List<Diagnostic>> DiagnoseAll(string source, string? trusted = null)
    {
        var compilation = Compile(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new EntityGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, new Options(trusted));
        var analyzed = await output.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new SimRulesAnalyzer(), new PredictionAnalyzer()), options)
            .GetAllDiagnosticsAsync(CancellationToken.None);
        return generatorDiagnostics.Concat(analyzed).Where(d => d.Id.StartsWith("OG", StringComparison.Ordinal) || d.Severity == DiagnosticSeverity.Error)
            .Distinct().ToList();
    }

    private sealed class Options(string? trusted) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions => new Values(trusted);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new Values(null);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new Values(null);
    }

    private sealed class Values(string? trusted) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            value = trusted ?? string.Empty;
            return trusted is not null && key == "onegame_sim_trusted";
        }
    }

    public static string Describe(IEnumerable<Diagnostic> diagnostics)
        => string.Join("\n", diagnostics.Select(d => $"{d.Id} {d.GetMessage()} @ {d.Location.GetLineSpan().StartLinePosition}"));
}
