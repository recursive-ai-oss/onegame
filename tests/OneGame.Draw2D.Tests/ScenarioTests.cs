using System.Text.Json;
using OneGame.Host;

namespace OneGame.Draw2D.Tests;

public class ScenarioTests
{
    [Fact]
    public void TheScenarioFilesPassWithCaptureReferences()
    {
        using var cluster = Fixture.Start();
        foreach (var file in Directory.GetFiles(Fixture.Scenarios, "*.yaml").Order(StringComparer.Ordinal))
        {
            var result = cluster.Run(file, new ScenarioRunOptions { BaseDirectory = Fixture.Scenarios, ArtifactsDirectory = Path.Combine(cluster.OutputDirectory, "art") });
            Assert.True(result.Passed, file + "\n" + string.Join("\n", result.Expectations.Where(e => !e.Passed).Select(e => e.Message)) + string.Join("\n", result.Steps.Where(st => !st.Ok).Select(st => st.Error)));
        }

        Assert.Empty(cluster.Client.Game.TakeGpuValidationMessages());
    }

    [Fact]
    public void TheDrawnFrameIsInTheCaptureAndTheDrawOpCountsIt()
    {
        using var cluster = Fixture.Start();
        cluster.Client.Step(3);
        var shot = cluster.Client.Game.Capture(480, 270);
        Assert.Equal(480, shot.Width);
        var frame = JsonDocument.Parse(cluster.Client.Call("draw_view")).RootElement;
        Assert.Equal("render2d", frame.GetProperty("stage").GetString());
        // Floor fill and outline (1 + 4), marker line, coin disc, 16+ ring pieces, the hero: at least 24 sprites, and the two text lines.
        Assert.True(frame.GetProperty("sprites").GetInt32() >= 24);
        Assert.Equal(2, frame.GetProperty("texts").GetInt32());
        Assert.Equal(2, frame.GetProperty("zoom").GetDouble());
        // The camera looks at the hero, who is at the origin: the middle pixel is world (0, 0) and the origin is the middle pixel.
        var at = JsonDocument.Parse(cluster.Client.Call("draw_view", "{\"screen\":[240,135],\"world\":[0,0]}")).RootElement;
        Assert.Equal(0, at.GetProperty("world")[0].GetDouble(), 0.5);
        Assert.Equal(135, at.GetProperty("screen")[1].GetDouble(), 0.5);
    }
}
