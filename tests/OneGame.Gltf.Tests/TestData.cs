using OneGame.Gltf;

namespace OneGame.Gltf.Tests;

/// <summary>Finds the committed models and, when configured, the downloaded corpus.</summary>
internal static class TestData
{
    /// <summary>Folder of the committed models.</summary>
    internal static string Committed => Path.Combine(AppContext.BaseDirectory, "testdata", "gltf");

    /// <summary>Folder made by tools/fetch-gltf-samples.sh, or null when the environment variable is not set.</summary>
    internal static string? Corpus => Environment.GetEnvironmentVariable("ONEGAME_GLTF_SAMPLES") is { Length: > 0 } d && Directory.Exists(d) ? d : null;

    /// <summary>Resolves a model path (such as <c>Box/glTF-Binary/Box.glb</c>) in the committed set, then in the corpus.</summary>
    internal static string Path_(string relative)
    {
        var committed = Path.Combine(Committed, relative);
        if (File.Exists(committed))
        {
            return committed;
        }

        if (Corpus is { } corpus && File.Exists(Path.Combine(corpus, relative)))
        {
            return Path.Combine(corpus, relative);
        }

        Assert.Skip($"{relative} is not committed and ONEGAME_GLTF_SAMPLES does not hold it.");
        return "";
    }

    internal static GltfModel Load(string relative, GltfLoadOptions? options = null) => GltfLoader.LoadFile(Path_(relative), options);

    /// <summary>Every .gltf and .glb under the committed set and the corpus, as relative paths with forward slashes.</summary>
    internal static IEnumerable<string> AllModels()
    {
        var seen = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var root in new[] { Committed, Corpus }.OfType<string>())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var f in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (f.EndsWith(".gltf", StringComparison.Ordinal) || f.EndsWith(".glb", StringComparison.Ordinal))
                {
                    seen.Add(Path.GetRelativePath(root, f).Replace('\\', '/'));
                }
            }
        }

        return seen;
    }
}
