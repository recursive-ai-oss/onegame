using System.Globalization;
using System.Security.Cryptography;
using OneGame.Corpus;

namespace OneGame.Corpus.Tests;

public class SampleTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "corpus-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }

        GC.SuppressFinalize(this);
    }

    private string Generate(string name, ulong seed)
    {
        var options = new CorpusOptions { Seed = seed, Out = Path.Combine(_dir, name), Sample = true };
        return CorpusRun.Generate(options);
    }

    [Fact]
    public void SampleModeWritesEveryGameAndAManifestThatMatchesTheFiles()
    {
        Generate("a", 1);
        var root = Path.Combine(_dir, "a");
        var lines = File.ReadAllLines(Path.Combine(root, "manifest.tsv"));
        Assert.Equal("game\tkind\tfile\tsize\tsha256\tschema\ttick", lines[0]);

        var seen = new HashSet<(string, string)>();
        long total = 0;
        foreach (var line in lines.Skip(1))
        {
            var c = line.Split('\t');
            Assert.Equal(7, c.Length);
            var bytes = File.ReadAllBytes(Path.Combine(root, c[2]));
            Assert.Equal(int.Parse(c[3], CultureInfo.InvariantCulture), bytes.Length);
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), c[4]);
            Assert.Matches(@"^(frame|snap2|save1|rec1|xfer)\.e\d+$", c[5]);
            Assert.True(long.Parse(c[6], CultureInfo.InvariantCulture) >= 0);
            Assert.StartsWith(c[0] + "/" + c[1] + "/", c[2], StringComparison.Ordinal);
            seen.Add((c[0], c[1]));
            total += bytes.Length;
        }

        foreach (var game in new[] { "lanterns", "bricks", "solitaire" })
        {
            foreach (var kind in new[] { "frames", "snapshots", "saves", "recordings" })
            {
                Assert.Contains((game, kind), seen);
            }
        }

        Assert.Contains(("lanterns", "transfer"), seen);
        Assert.True(total < 400_000, "the sample is " + total + " bytes");
    }

    [Fact]
    public void TheSameSeedGivesTheSameManifestAndAnotherSeedDoesNot()
    {
        var one = Generate("one", 7);
        var again = Generate("again", 7);
        var other = Generate("other", 8);
        Assert.Equal(one, again);
        Assert.NotEqual(one, other);
        Assert.Equal(File.ReadAllText(Path.Combine(_dir, "one", "manifest.tsv")), File.ReadAllText(Path.Combine(_dir, "again", "manifest.tsv")));
    }
}
