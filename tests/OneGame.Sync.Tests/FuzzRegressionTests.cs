using OneGame.Sync.Tests.Fuzzing;

namespace OneGame.Sync.Tests;

public class SyncFuzzRegressionTests
{
    private static string Root => Path.Combine(AppContext.BaseDirectory, "testdata", "fuzz-sync");

    public static TheoryData<string, string> Inputs()
    {
        var data = new TheoryData<string, string>();
        if (Directory.Exists(Root))
        {
            foreach (var file in Directory.EnumerateFiles(Root, "*.bin", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
            {
                data.Add(Path.GetFileName(Path.GetDirectoryName(file))!, Path.GetRelativePath(Root, file));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Inputs))]
    public void CommittedInputsAreHandled(string target, string relativePath)
    {
        var t = SyncFuzzTargets.All.Single(x => x.Name == target);
        _ = t.Run(File.ReadAllBytes(Path.Combine(Root, relativePath)));
    }

    [Fact]
    public void EveryTargetAcceptsItsOwnSeeds()
    {
        foreach (var t in SyncFuzzTargets.All)
        {
            foreach (var seed in t.Seeds())
            {
                Assert.True(t.Run(seed), t.Name);
            }
        }
    }

    [Fact]
    public void SeedsAreOnDisk() => Assert.NotEmpty(Directory.EnumerateFiles(Root, "*.bin", SearchOption.AllDirectories));
}

public class HostileMessageTests
{
    [Fact]
    public void TruncatedAndFlippedMessagesNeverThrow()
    {
        var rng = new Random(3);
        foreach (var target in SyncFuzzTargets.All.Where(t => t.Name.StartsWith("transport", StringComparison.Ordinal)))
        {
            foreach (var seed in target.Seeds())
            {
                for (var cut = 0; cut < seed.Length; cut++)
                {
                    _ = target.Run(seed[..cut]);
                }

                for (var i = 0; i < 100; i++)
                {
                    var copy = (byte[])seed.Clone();
                    copy[rng.Next(copy.Length)] = (byte)rng.Next(256);
                    _ = target.Run(copy);
                }
            }
        }
    }
}
