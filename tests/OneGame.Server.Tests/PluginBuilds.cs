using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using OneGame.Generators;

namespace OneGame.Server.Tests;

/// <summary>
/// Builds the sample plugin the way a rebuild would: the real generator over the plugin's source, emitted to a folder. Edits are made to
/// the source text (a different variant symbol, a removed migration) so a test can "edit, rebuild and reload" without running msbuild.
/// </summary>
internal static class PluginBuilds
{
    private static readonly MetadataReference[] References = BuildReferences();
    private static readonly Lazy<string> Source = new(() =>
    {
        using var stream = typeof(PluginBuilds).Assembly.GetManifestResourceStream("CounterPlugin.cs")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    private static MetadataReference[] BuildReferences()
    {
        var trusted = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        return [.. trusted.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))];
    }

    /// <summary>Runs the generator and the simulation-rule analyzer over the sample source (after an edit) and returns the ids of the errors.</summary>
    /// <param name="edit">An edit of the source text.</param>
    public static async Task<string[]> AnalyzerErrors(Func<string, string> edit)
    {
        var options = new CSharpParseOptions(LanguageVersion.Latest).WithPreprocessorSymbols("VARIANT1");
        var compilation = CSharpCompilation.Create(
            "counter",
            [CSharpSyntaxTree.ParseText(edit(Source.Value), options)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new EntityGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        var all = await output.WithAnalyzers(ImmutableArray.Create<Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer>(new SimRulesAnalyzer()))
            .GetAllDiagnosticsAsync(CancellationToken.None);
        return [.. all.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Id).Distinct()];
    }

    /// <summary>A fresh empty folder.</summary>
    public static string NewFolder()
    {
        var dir = Path.Combine(Path.GetTempPath(), "og-plugin-" + Guid.NewGuid().ToString("N")[..10]);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Compiles the sample plugin as variant 1, 2 or 3 into <paramref name="dir"/>/counter.dll (replacing it) and returns the folder.</summary>
    /// <param name="dir">The folder.</param>
    /// <param name="variant">1: the original; 2: the system adds twice the step; 3: schema 2 with a migration.</param>
    /// <param name="edit">An optional edit of the source text.</param>
    public static string Build(string dir, int variant, Func<string, string>? edit = null)
    {
        var text = Source.Value;
        if (edit is not null)
        {
            text = edit(text);
        }

        var options = new CSharpParseOptions(LanguageVersion.Latest).WithPreprocessorSymbols("VARIANT" + variant);
        var compilation = CSharpCompilation.Create(
            "counter",
            [CSharpSyntaxTree.ParseText(text, options)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new EntityGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        var errors = generatorDiagnostics.Concat(output.GetDiagnostics()).Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException("The sample plugin did not compile: " + string.Join("; ", errors.Take(5)));
        }

        var file = Path.Combine(dir, "counter.dll");
        using var stream = new MemoryStream();
        var emitted = output.Emit(stream);
        if (!emitted.Success)
        {
            throw new InvalidOperationException("Emit failed: " + string.Join("; ", emitted.Diagnostics.Take(5)));
        }

        // Replace the file the way a build does; a loaded copy is unaffected because the loader reads bytes.
        File.WriteAllBytes(file, stream.ToArray());
        return dir;
    }
}
