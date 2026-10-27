using System.Runtime.CompilerServices;
using System.Text.Json;
using OneGame.Host;
using ParticleGame;

// Each test makes its own GPU device; real drivers are not happy with several being created and destroyed at once.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace OneGame.Particles.Tests;

internal static class Fixture
{
    private static string SourceDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    public static string Scenarios(string folder) => Path.GetFullPath(Path.Combine(SourceDir(), "..", "ParticleGame", folder));

    public static TestCluster Start(bool threeD)
    {
        // The Vulkan validation layer must stay silent through every frame these tests draw.
        Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
        return TestCluster.Start(new ParticleGameDefinition(threeD), new TestOptions { Width = 480, Height = 270, Gpu = GpuMode.Required });
    }

    public static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();
}
