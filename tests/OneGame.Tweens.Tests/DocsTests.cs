using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using OneGame.UI;

namespace OneGame.Tweens.Tests;

/// <summary>Every <c>csharp</c> block of docs/tweens.md is compiled as a method body and run against a fresh UI for two seconds of view time.</summary>
public partial class DocsTests
{
    [GeneratedRegex("""```(csharp|yaml)\n(.*?)\n```""", RegexOptions.Singleline)]
    private static partial Regex Block();

    private static string Doc([CallerFilePath] string here = "") =>
        File.ReadAllText(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "docs", "tweens.md")));

    [Fact]
    public void EveryCodeBlockCompilesAndRuns()
    {
        var code = new StringBuilder("using System; using System.Numerics; using OneGame.Tweens; using OneGame.UI;\npublic static class Snippets {\n");
        var n = 0;
        foreach (Match m in Block().Matches(Doc()))
        {
            if (m.Groups[1].Value == "csharp")
            {
                code.Append("public static void S").Append(n++).Append("(TweenSet tweens, UiTree ui, Element panel, Element card, Element score) {\n").Append(m.Groups[2].Value).Append("\n}\n");
            }
        }

        code.Append('}');
        Assert.True(n >= 8, "the guide should have its examples");
        var trusted = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();
        trusted.Add(MetadataReference.CreateFromFile(typeof(UiTree).Assembly.Location));
        var compilation = CSharpCompilation.Create("DocSnippets", [CSharpSyntaxTree.ParseText(code.ToString(), cancellationToken: TestContext.Current.CancellationToken)], trusted,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var stream = new MemoryStream();
        var emitted = compilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));

        var assembly = Assembly.Load(stream.ToArray());
        var type = assembly.GetType("Snippets")!;
        for (var i = 0; i < n; i++)
        {
            var ui = new UiTree();
            var panel = new Element("Box");
            var card = new Element("Box");
            var score = new Element("Label");
            ui.Root.With(panel, card, score);
            ui.Frame();
            type.GetMethod("S" + i)!.Invoke(null, [ui.Tweens, ui, panel, card, score]);
            for (var f = 0; f < 120; f++)
            {
                ui.Tweens.Advance(1 / 60.0);
                ui.Frame();
            }
        }
    }

    [Fact]
    public void TheScenarioBlockParses()
    {
        var yaml = Block().Matches(Doc()).Where(m => m.Groups[1].Value == "yaml").Select(m => m.Groups[2].Value).Single();
        var scenario = OneGame.Testing.ScenarioParser.Parse("name: guide\n" + yaml);
        Assert.Contains(scenario.Steps, s => s.Kind == OneGame.Testing.StepKind.Tweens);
    }
}
