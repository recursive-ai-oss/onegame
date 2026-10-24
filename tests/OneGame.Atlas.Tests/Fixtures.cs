using System.Runtime.CompilerServices;

namespace OneGame.Atlas.Tests;

internal static class Fixtures
{
    public static string Dir([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "testdata", "text"));

    public static byte[] Svg(string name) => File.ReadAllBytes(Path.Combine(Dir(), "svg", name));
}
