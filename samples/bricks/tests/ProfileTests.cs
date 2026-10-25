namespace Bricks.Tests;

/// <summary>The settings and high-score file: round trip, ordering, and surviving damage.</summary>
public class ProfileTests
{
    [Fact]
    public void ATableKeepsTheBestEightAndPutsAnEqualScoreAfterTheOlderOne()
    {
        var p = new Profile();
        Assert.False(p.Qualifies(0));
        for (var i = 0; i < 10; i++)
        {
            p.Add("P" + i, 100 * (i + 1), 1, false);
        }

        Assert.Equal(Profile.TableSize, p.Scores.Count);
        Assert.Equal(1000, p.Scores[0].Score);
        Assert.Equal(300, p.Scores[^1].Score);
        Assert.Equal(-1, p.Add("Low", 200, 1, false));
        Assert.Equal(1, p.Add("Tie", 1000, 2, false));
        Assert.Equal("P9", p.Scores[0].Name);
        Assert.Equal("Tie", p.Scores[1].Name);
    }

    [Theory]
    [InlineData("  Ada  ", "Ada")]
    [InlineData("", "Player")]
    [InlineData("   ", "Player")]
    [InlineData("A very long name indeed", "A very lon")]
    [InlineData("Tab\there", "Tabhere")]
    public void NamesAreTrimmedCutAndCleaned(string typed, string expected) => Assert.Equal(expected, Profile.Clean(typed));

    [Fact]
    public void TheFileRoundTrips()
    {
        var p = new Profile { Volume = 3, UiScaleIndex = 2, LastName = "Ada" };
        p.Add("Ada", 900, 4, false);
        p.Add("Grace", 1200, 10, true);
        Assert.True(Profile.TryRead(p.Write(), out var read));
        Assert.Equal(3, read.Volume);
        Assert.Equal(1.25f, read.UiScale);
        Assert.Equal("Grace", read.LastName);
        Assert.Equal(p.Scores, read.Scores);
    }

    [Fact]
    public void DamagedFilesAreRefusedAndNeverThrow()
    {
        var p = new Profile();
        p.Add("Ada", 900, 4, false);
        p.Add("Grace", 800, 2, false);
        var good = p.Write();
        for (var length = 0; length < good.Length; length++)
        {
            Assert.False(Profile.TryRead(good.AsSpan(0, length), out _), $"a file cut at {length} bytes was accepted");
        }

        var random = new Random(7);
        for (var round = 0; round < 3000; round++)
        {
            var bad = (byte[])good.Clone();
            bad[random.Next(bad.Length)] ^= (byte)(1 << random.Next(8));
            if (Profile.TryRead(bad, out var read))
            {
                // A flip that still parses must still be a sane table.
                Assert.InRange(read.Volume, 0, 10);
                Assert.True(read.Scores.Count <= Profile.TableSize);
                Assert.True(read.Scores.Zip(read.Scores.Skip(1), (a, b) => a.Score >= b.Score).All(x => x));
            }
        }

        Assert.False(Profile.TryRead(new byte[100_000], out _));
    }
}
