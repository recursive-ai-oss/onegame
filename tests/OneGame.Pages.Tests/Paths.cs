using System.Runtime.CompilerServices;

namespace OneGame.Pages.Tests;

internal static class Paths
{
    private static string SourceDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    public static string RepoRoot => Path.GetFullPath(Path.Combine(SourceDir(), "..", ".."));

    public static string TextData(string relative) => Path.Combine(RepoRoot, "testdata", "text", relative);

    public static string Golden(string name) => Path.Combine(RepoRoot, "testdata", "pages", name + ".png");
}
