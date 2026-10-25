using OneGame.Net.Tests.Fuzzing;

namespace OneGame.Net.Tests;

public class NetFuzzRegressionTests
{
    private static string Root => Path.Combine(AppContext.BaseDirectory, "testdata", "fuzz-net");

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
        var t = NetFuzzTargets.All.Single(x => x.Name == target);
        _ = t.Run(File.ReadAllBytes(Path.Combine(Root, relativePath)));
    }

    [Fact]
    public void EveryTargetAcceptsItsOwnSeeds()
    {
        foreach (var t in NetFuzzTargets.All)
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
