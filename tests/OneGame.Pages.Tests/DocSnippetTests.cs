using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace OneGame.Pages.Tests;

/// <summary>
/// Every <c>csharp</c> block of docs/pages.md is a complete type. Generated/DocSnippets.g.cs holds them and is compiled with the tests, so a snippet that stops
/// compiling breaks the build; this test fails when the guide and the file differ. After editing the guide run the tests once with ONEGAME_UPDATE_SNIPPETS=1, then build again.
/// </summary>
public partial class DocSnippetTests
{
    [GeneratedRegex("""```csharp\n(.*?)\n```""", RegexOptions.Singleline)]
    private static partial Regex Block();

    private static string Root([CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    [Fact]
    public void TheCodeBlocksOfTheGuideAreTheCodeThatCompiles()
    {
        var guide = File.ReadAllText(Path.Combine(Root(), "docs", "pages.md")).Replace("\r\n", "\n", StringComparison.Ordinal);
        var file = new StringBuilder("// Generated from docs/pages.md by DocSnippetTests. Do not edit.\n#pragma warning disable\n");
        var n = 0;
        foreach (Match m in Block().Matches(guide))
        {
            n++;
            // Each block opens with its own usings, so each gets a namespace of its own.
            file.Append("namespace PageDocs.Block").Append(n).Append("\n{\n");
            foreach (var line in m.Groups[1].Value.Split('\n'))
            {
                file.Append(line.Length == 0 ? "" : "    " + line).Append('\n');
            }

            file.Append("}\n\n");
        }

        Assert.True(n >= 3, "the guide should have its code blocks");
        var path = Path.Combine(Root(), "tests", "OneGame.Pages.Tests", "Generated", "DocSnippets.g.cs");
        if (Environment.GetEnvironmentVariable("ONEGAME_UPDATE_SNIPPETS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, file.ToString());
        }

        Assert.Equal(file.ToString(), File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal));
    }
}
