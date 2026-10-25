using OneGame.Gltf.Tests.Fuzzing;

namespace OneGame.Gltf.Tests;

public class GltfFuzzRegressionTests
{
    private static string Root => Path.Combine(AppContext.BaseDirectory, "testdata", "fuzz-gltf");

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
        var t = GltfFuzzTargets.All.Single(x => x.Name == target);
        _ = t.Run(File.ReadAllBytes(Path.Combine(Root, relativePath)));
    }

    [Fact]
    public void EveryTargetAcceptsItsOwnSeeds()
    {
        foreach (var t in GltfFuzzTargets.All)
        {
            var any = false;
            foreach (var seed in t.Seeds())
            {
                any = true;
                // The uri seeds include refused ones on purpose; every other seed is a valid file.
                if (t.Name != "gltf-uri")
                {
                    Assert.True(t.Run(seed), t.Name);
                }
                else
                {
                    _ = t.Run(seed);
                }
            }

            Assert.True(any, t.Name);
        }
    }

    [Fact]
    public void TheRichSeedUsesEveryPartOfTheModel()
    {
        var m = GltfLoader.Load(System.Text.Encoding.UTF8.GetBytes(GltfFuzzTargets.KitchenSinkEmbedded()));
        Assert.Single(m.Skins);
        Assert.Single(m.Animations);
        Assert.Equal(4, m.Animations[0].Samplers.Length);
        Assert.Equal(2, m.Meshes[0].Primitives.Length);
        Assert.Single(m.Meshes[0].Primitives[0].MorphTargets);
        Assert.NotNull(m.Meshes[0].Primitives[0].Colors);
        Assert.Single(m.Lights);
        Assert.Single(m.Cameras);
        Assert.Equal(new System.Numerics.Vector3(0, 0, 0), m.Meshes[0].Primitives[1].Positions[0]);
        Assert.Equal(new System.Numerics.Vector3(5, 5, 5), m.Meshes[0].Primitives[1].Positions[1]);
    }
}
