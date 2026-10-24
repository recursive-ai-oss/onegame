using System.Runtime.CompilerServices;
using OneGame.Text;

namespace OneGame.Text.Tests;

internal static class Fonts
{
    public static string Dir([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "testdata", "text"));

    public static byte[] Bytes(string file) => File.ReadAllBytes(Path.Combine(Dir(), "fonts", file));

    /// <summary>Noto Sans (family "Noto Sans"), then Arabic, Hebrew and CJK as fallbacks.</summary>
    public static FontLibrary Library()
    {
        var lib = new FontLibrary();
        lib.Add(Bytes("NotoSans-Subset-Regular.ttf"));
        lib.Add(Bytes("NotoSans-Subset-Bold-kern.ttf"));
        lib.AddFallback(lib.Add(Bytes("NotoSansArabic-Subset-Regular.ttf")));
        lib.AddFallback(lib.Add(Bytes("NotoSansHebrew-Subset-Regular.ttf")));
        lib.AddFallback(lib.Add(Bytes("NotoSansCJK-Subset.otf")));
        return lib;
    }
}
