using System.Runtime.CompilerServices;
using System.Text.Json;
using OneGame.Host;
using Scene3DGame;

// Each test makes its own GPU device; real drivers are not happy with several being created and destroyed at once.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace OneGame.Scene3D.Tests;

internal static class Fixture
{
    private static string SourceDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    public static string Scenarios => Path.GetFullPath(Path.Combine(SourceDir(), "..", "Scene3DGame", "scenarios"));

    public static TestCluster Start(int width = 640, int height = 360)
    {
        // The Vulkan validation layer must stay silent through every frame these tests draw.
        Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
        return TestCluster.Start(new Scene3DGameDefinition(), new TestOptions { Width = width, Height = height, Gpu = GpuMode.Required });
    }

    public static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();
}
