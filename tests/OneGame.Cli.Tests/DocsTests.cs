using System.Runtime.CompilerServices;
using OneGame.Cli.Verbs;

namespace OneGame.Cli.Tests;

/// <summary>The code in the written guides is the code <c>onegame new</c> writes, so the guides cannot drift from the scaffold.</summary>
public sealed class DocsTests
{
    private static readonly string Repo = FindRoot();

    private static string FindRoot([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", ".."));

    private static string[] CodeBlocks(string file)
    {
        var blocks = new List<string>();
        System.Text.StringBuilder? current = null;
        foreach (var line in File.ReadAllText(Path.Combine(Repo, file)).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                if (current is null)
                {
                    current = new System.Text.StringBuilder();
                }
                else
                {
                    blocks.Add(current.ToString().TrimEnd());
                    current = null;
                }
            }
            else
            {
                current?.Append(line).Append('\n');
            }
        }

        return [.. blocks];
    }

    private static string Normal(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd();

    [Fact]
    public void FirstGameGuideShowsTheScaffoldExactly()
    {
        var blocks = CodeBlocks("docs/first-game.md");
        var generated = new Dictionary<string, string>
        {
            ["mygame.csproj"] = Templates.Csproj(null, "<version>", "Mygame"),
            ["Program.cs"] = Templates.Program("Mygame", "Mygame"),
            ["Game.cs"] = Templates.Game("Mygame", "Mygame", "mygame"),
            ["scenarios/click-button.yaml"] = Templates.Scenario,
        };
        foreach (var (file, text) in generated)
        {
            Assert.True(blocks.Contains(Normal(text)), $"docs/first-game.md has no code block equal to the {file} that 'onegame new' writes.");
        }
    }

    [Fact]
    public void ReadmeShowsTheScaffoldsEntity()
    {
        Assert.Contains(Normal(Templates.CounterEntity), CodeBlocks("README.md"));
    }
}
