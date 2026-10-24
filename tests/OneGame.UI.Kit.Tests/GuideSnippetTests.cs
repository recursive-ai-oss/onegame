using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using OneGame.UI.Kit.Tests.Guide;

namespace OneGame.UI.Kit.Tests;

/// <summary>
/// Every code block in docs/ui-guide.md is compiled (Generated/GuideSnippets.g.cs is made from the guide) and the plain ones are also run against a fresh screen.
/// A block fenced as <c>csharp type</c> holds types, <c>csharp setup</c> compiles but is not run (it reads files), and <c>csharp</c> is a method body
/// with <c>ui</c>, <c>host</c> and <c>game</c> in scope. After editing the guide, run the tests once with ONEGAME_UPDATE_SNIPPETS=1, then build again.
/// </summary>
public partial class GuideSnippetTests
{
    [GeneratedRegex("""```csharp([^\n]*)\n(.*?)\n```""", RegexOptions.Singleline)]
    private static partial Regex Block();

    private static string Root([CallerFilePath] string here = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", ".."));

    private static string Generate(string guide)
    {
        var types = new StringBuilder();
        var methods = new StringBuilder();
        var runnable = new List<string>();
        var n = 0;
        foreach (Match m in Block().Matches(guide))
        {
            var kind = m.Groups[1].Value.Trim();
            var code = m.Groups[2].Value;
            var line = guide[..m.Index].Count(c => c == '\n') + 2;
            n++;
            if (kind == "type")
            {
                types.Append("// docs/ui-guide.md line ").Append(line).Append('\n').Append(code).Append("\n\n");
                continue;
            }

            var name = (kind == "setup" ? "Setup" : "Run") + n;
            methods.Append("    // docs/ui-guide.md line ").Append(line).Append('\n');
            methods.Append("    public static void ").Append(name).Append(kind == "setup" ? "()" : "(UiTree ui, KitHost host, GuideGame game)").Append('\n').Append("    {\n");
            foreach (var l in code.Split('\n'))
            {
                methods.Append(l.Length == 0 ? "" : "        " + l).Append('\n');
            }

            methods.Append("    }\n\n");
            if (kind != "setup")
            {
                runnable.Add(name);
            }
        }

        var sb = new StringBuilder();
        sb.Append("// Generated from docs/ui-guide.md by GuideSnippetTests (ONEGAME_UPDATE_SNIPPETS=1 regenerates it). Do not edit.\n");
        sb.Append("#nullable enable\nusing OneGame.Atlas;\nusing OneGame.Text;\n\nnamespace OneGame.UI.Kit.Tests.Guide;\n\n");
        sb.Append(types);
        sb.Append("internal static class Snippets\n{\n").Append(methods);
        sb.Append("    public static readonly (string Name, Action<UiTree, KitHost, GuideGame> Run)[] Runnable =\n    [\n");
        foreach (var r in runnable)
        {
            sb.Append("        (\"").Append(r).Append("\", ").Append(r).Append("),\n");
        }

        sb.Append("    ];\n}\n");
        return sb.ToString();
    }

    [Fact]
    public void TheCompiledSnippetsAreTheGuidesCodeBlocks()
    {
        var guide = File.ReadAllText(Path.Combine(Root(), "docs", "ui-guide.md"));
        var expected = Generate(guide);
        var path = Path.Combine(Root(), "tests", "OneGame.UI.Kit.Tests", "Generated", "GuideSnippets.g.cs");
        if (Environment.GetEnvironmentVariable("ONEGAME_UPDATE_SNIPPETS") == "1")
        {
            File.WriteAllText(path, expected);
        }

        Assert.True(File.ReadAllText(path) == expected, "docs/ui-guide.md changed: run the tests with ONEGAME_UPDATE_SNIPPETS=1, then build and run again.");
    }

    [Fact]
    public void EveryRunnableSnippetRunsOnAFreshScreen()
    {
        Assert.True(Snippets.Runnable.Length >= 20, "expected the guide's examples");
        foreach (var (name, run) in Snippets.Runnable)
        {
            var s = new Screen();
            try
            {
                run(s.Ui, s.Host, new GuideGame());
                s.Settle();
                // Everything the snippet built must also survive input: Tab through the screen, activate things, send keys.
                for (var i = 0; i < 12; i++)
                {
                    s.Key("Tab");
                    s.Key("ArrowRight");
                    s.Activate();
                }

                s.Cancel();
                Assert.True(s.Ui.DisplayList.Commands.Length > 0 || s.Root.ChildCount <= 1, name + " drew nothing");
            }
            catch (Exception e) when (e is not Xunit.Sdk.XunitException)
            {
                throw new InvalidOperationException($"{name} failed: {e.Message}", e);
            }
        }
    }
}
