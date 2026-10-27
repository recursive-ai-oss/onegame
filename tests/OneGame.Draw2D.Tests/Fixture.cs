using System.Runtime.CompilerServices;
using Draw2DGame;
using OneGame.Host;

// Each test makes its own GPU device; real drivers are not happy with several being created and destroyed at once.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace OneGame.Draw2D.Tests;

internal static class Fixture
{
    private static string SourceDir([CallerFilePath] string path = "") => Path.GetDirectoryName(path)!;

    public static string Scenarios => Path.GetFullPath(Path.Combine(SourceDir(), "..", "Draw2DGame", "scenarios"));

    public static TestCluster Start()
    {
        // The Vulkan validation layer must stay silent through every frame these tests draw.
        Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
        return TestCluster.Start(new Draw2DGameDefinition(), new TestOptions { Width = 480, Height = 270, Gpu = GpuMode.Required });
    }
}
