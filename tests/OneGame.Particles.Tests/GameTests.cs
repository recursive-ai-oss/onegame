using ParticleGame;

namespace OneGame.Particles.Tests;

/// <summary>Particles through the game host: captures against references, the harness surface, and the proof that they never touch the simulation.</summary>
public class GameTests
{
    private static void RunFolder(bool threeD, string folder)
    {
        using var cluster = Fixture.Start(threeD);
        var dir = Fixture.Scenarios(folder);
        foreach (var file in Directory.GetFiles(dir, "*.yaml").Order(StringComparer.Ordinal))
        {
            var result = cluster.Run(file, new ScenarioRunOptions { BaseDirectory = dir, ArtifactsDirectory = Path.Combine(cluster.OutputDirectory, "art") });
            Assert.True(result.Passed, file + "\n" + string.Join("\n", result.Expectations.Where(e => !e.Passed).Select(e => e.Message)) + string.Join("\n", result.Steps.Where(st => !st.Ok).Select(st => st.Error)));
        }

        Assert.Empty(cluster.Client.Game.TakeGpuValidationMessages());
    }

    [Fact]
    public void The2DScenarioPassesWithCaptureReferencesAndNoValidationMessages() => RunFolder(false, "scenarios2d");

    [Fact]
    public void The3DScenarioPassesWithCaptureReferencesAndNoValidationMessages() => RunFolder(true, "scenarios3d");

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheParticlesOperationListsEmittersWithTheirCounts(bool threeD)
    {
        using var cluster = Fixture.Start(threeD);
        cluster.Client.Step(5);
        var report = Fixture.Json(cluster.Client.Call("particles"));
        Assert.Equal(threeD ? "render3d" : "render2d", report.GetProperty("stage").GetString());
        Assert.Equal(3, report.GetProperty("emitters").GetInt32());
        var items = report.GetProperty("items").EnumerateArray().ToDictionary(i => i.GetProperty("name").GetString()!);
        Assert.Equal(["pop", "smoke", "sparks"], items.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("additive", items["sparks"].GetProperty("blend").GetString());
        Assert.Equal("alpha", items["smoke"].GetProperty("blend").GetString());
        Assert.Equal(threeD ? "volume" : "plane", items["sparks"].GetProperty("space").GetString());
        Assert.InRange(items["sparks"].GetProperty("live").GetInt32(), 55, 90);
        Assert.Equal(0, items["sparks"].GetProperty("dropped").GetInt64());
        Assert.Equal(report.GetProperty("live").GetInt32(), items.Values.Sum(i => i.GetProperty("live").GetInt32()));

        var one = Fixture.Json(cluster.Client.Call("particles", "{\"name\":\"smoke\"}"));
        Assert.Equal(1, one.GetProperty("emitters").GetInt32());
        Assert.Equal(items["smoke"].GetProperty("live").GetInt32(), one.GetProperty("live").GetInt32());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheSimulationHashIsTheSameWithAndWithoutParticles(bool threeD)
    {
        string[] Run(bool particles)
        {
            Fx.Enabled = particles;
            try
            {
                using var cluster = Fixture.Start(threeD);
                var dir = Fixture.Scenarios(threeD ? "scenarios-hash3d" : "scenarios-hash");
                var hashes = new List<string>();
                foreach (var file in Directory.GetFiles(dir, "*.yaml").Order(StringComparer.Ordinal))
                {
                    var result = cluster.Run(file, new ScenarioRunOptions { BaseDirectory = dir, ArtifactsDirectory = Path.Combine(cluster.OutputDirectory, "art") });
                    Assert.True(result.Passed, string.Join("\n", result.Steps.Where(st => !st.Ok).Select(st => st.Error)));
                    hashes.Add(result.Hash);
                }

                var live = Fixture.Json(cluster.Client.Call("particles")).GetProperty("live").GetInt32();
                Assert.Equal(particles, live > 0);
                return [.. hashes, cluster.Client.Game.World.ComputeHash().ToString("x16")];
            }
            finally
            {
                Fx.Enabled = true;
            }
        }

        var with = Run(true);
        var without = Run(false);
        Assert.Equal(without, with);
        Assert.NotEmpty(with);
    }
}
