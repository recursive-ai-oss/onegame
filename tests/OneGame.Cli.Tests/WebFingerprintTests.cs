using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using OneGame.Cli.Verbs;

namespace OneGame.Cli.Tests;

public class WebFingerprintTests
{
    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)))[..10];

    private static string MakeExport()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onegame-fp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "_framework"));
        Directory.CreateDirectory(Path.Combine(dir, "_content", "RecursiveAI.OneGpu.WebGpu"));
        File.WriteAllText(Path.Combine(dir, "_framework", "dotnet.js"), "// loader");
        File.WriteAllText(Path.Combine(dir, "_content", "RecursiveAI.OneGpu.WebGpu", "onegpu.js"), "// gpu");
        File.WriteAllText(Path.Combine(dir, "onegame-platform.js"), "// platform");
        File.WriteAllText(Path.Combine(dir, "onegame-audio.js"), "// audio");
        File.WriteAllText(Path.Combine(dir, "boot.mjs"), "import { dotnet } from './_framework/dotnet.js';\nimport * as gpu from './_content/RecursiveAI.OneGpu.WebGpu/onegpu.js';\nimport * as platform from './onegame-platform.js';\nimport * as audio from './onegame-audio.js';\n");
        File.WriteAllText(Path.Combine(dir, "index.html"), "<html><body><script type=\"module\" src=\"./boot.mjs\"></script></body></html>");
        // The publish step leaves compressed copies beside the text files.
        foreach (var name in new[] { "boot.mjs", "index.html" })
        {
            var path = Path.Combine(dir, name);
            File.WriteAllBytes(path + ".br", [1, 2, 3]);
            File.WriteAllBytes(path + ".gz", [1, 2, 3]);
        }

        return dir;
    }

    [Fact]
    public void EveryScriptUrlCarriesTheHashOfItsContent()
    {
        var dir = MakeExport();
        try
        {
            WebExport.Fingerprint(dir);
            var boot = File.ReadAllText(Path.Combine(dir, "boot.mjs"));
            Assert.Contains($"'./_framework/dotnet.js?v={Hash(Path.Combine(dir, "_framework", "dotnet.js"))}'", boot);
            Assert.Contains($"'./_content/RecursiveAI.OneGpu.WebGpu/onegpu.js?v={Hash(Path.Combine(dir, "_content", "RecursiveAI.OneGpu.WebGpu", "onegpu.js"))}'", boot);
            Assert.Contains($"'./onegame-platform.js?v={Hash(Path.Combine(dir, "onegame-platform.js"))}'", boot);
            Assert.Contains($"'./onegame-audio.js?v={Hash(Path.Combine(dir, "onegame-audio.js"))}'", boot);
            Assert.Contains($"src=\"./boot.mjs?v={Hash(Path.Combine(dir, "boot.mjs"))}\"", File.ReadAllText(Path.Combine(dir, "index.html")));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ChangingAScriptChangesItsUrlAndOnlyItsUrl()
    {
        var a = MakeExport();
        var b = MakeExport();
        try
        {
            File.WriteAllText(Path.Combine(b, "onegame-platform.js"), "// platform, edited");
            WebExport.Fingerprint(a);
            WebExport.Fingerprint(b);
            var bootA = File.ReadAllLines(Path.Combine(a, "boot.mjs"));
            var bootB = File.ReadAllLines(Path.Combine(b, "boot.mjs"));
            Assert.NotEqual(bootA[2], bootB[2]);
            Assert.Equal(bootA[0], bootB[0]);
            Assert.Equal(bootA[3], bootB[3]);
            // boot.mjs changed, so the page's URL for it changed too: a cached page cannot pair the old loader with the new scripts.
            Assert.NotEqual(File.ReadAllText(Path.Combine(a, "index.html")), File.ReadAllText(Path.Combine(b, "index.html")));
        }
        finally
        {
            Directory.Delete(a, true);
            Directory.Delete(b, true);
        }
    }

    [Fact]
    public void CompressedCopiesAreRewrittenToMatch()
    {
        var dir = MakeExport();
        try
        {
            WebExport.Fingerprint(dir);
            foreach (var name in new[] { "boot.mjs", "index.html" })
            {
                var text = File.ReadAllText(Path.Combine(dir, name));
                using var br = new BrotliStream(File.OpenRead(Path.Combine(dir, name + ".br")), CompressionMode.Decompress);
                using var gz = new GZipStream(File.OpenRead(Path.Combine(dir, name + ".gz")), CompressionMode.Decompress);
                Assert.Equal(text, new StreamReader(br, Encoding.UTF8).ReadToEnd());
                Assert.Equal(text, new StreamReader(gz, Encoding.UTF8).ReadToEnd());
            }
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
