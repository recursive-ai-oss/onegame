using OneGame.Host;
using Scene3DGame;

namespace OneGame.Scene3D.Tests;

public class SceneTests
{
    [Fact]
    public void TheScenarioFilesPassOnTheRenderer()
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
    public void TheStageIsThe3DRendererAndTheGraphListsItsPasses()
    {
        using var cluster = Fixture.Start();
        var info = Fixture.Json(cluster.Client.Call("info"));
        Assert.Equal("render3d", info.GetProperty("stage").GetString());

        var graph = cluster.Client.Call("render_graph");
        foreach (var pass in new[] { "cluster_lights", "forward", "tonemap" })
        {
            Assert.Contains($"\"{pass}\"", graph, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CameraPickAndProjectReturnJson()
    {
        using var cluster = Fixture.Start();
        cluster.Client.Step(10);
        var camera = Fixture.Json(cluster.Client.Call("camera"));
        Assert.Equal(40, camera.GetProperty("fovDegrees").GetDouble());
        Assert.Equal("Rig#5", camera.GetProperty("entity").GetString());
        Assert.False(camera.GetProperty("overridden").GetBoolean());

        var moved = Fixture.Json(cluster.Client.Call("camera", """{"position":[0,6,6],"target":[0,0,0]}"""));
        Assert.True(moved.GetProperty("overridden").GetBoolean());
        Assert.Equal(6, moved.GetProperty("position")[1].GetDouble());
        Assert.Equal(40, moved.GetProperty("fovDegrees").GetDouble(), 3);
        Assert.False(Fixture.Json(cluster.Client.Call("camera", """{"reset":true}""")).GetProperty("overridden").GetBoolean());

        // The pixel of a world point picks the entity that stands there, and a ray gives the same answer.
        var at = Fixture.Json(cluster.Client.Call("project", """{"point":[-2,0.5,0]}"""));
        Assert.True(at.GetProperty("onScreen").GetBoolean());
        var pick = Fixture.Json(cluster.Client.Call("pick", $$"""{"x":{{at.GetProperty("x").GetDouble()}},"y":{{at.GetProperty("y").GetDouble()}}}"""));
        Assert.True(pick.GetProperty("hit").GetBoolean());
        Assert.Equal("Crate#1", pick.GetProperty("address").GetString());
        Assert.Equal("box", pick.GetProperty("model").GetString());
        var ray = Fixture.Json(cluster.Client.Call("pick", """{"origin":[-2,0.5,8],"direction":[0,0,-1]}"""));
        Assert.Equal("Crate#1", ray.GetProperty("address").GetString());
        // The box is tilted a little about its X axis, so its face is not exactly half a unit from the centre.
        Assert.InRange(ray.GetProperty("distance").GetDouble(), 7.4, 7.6);
        Assert.False(Fixture.Json(cluster.Client.Call("pick", """{"origin":[-20,0.5,8],"direction":[0,0,-1]}""")).GetProperty("hit").GetBoolean());

        var bad = Assert.Throws<OneGame.Control.ControlException>(() => cluster.Client.Call("pick", """{"x":3}"""));
        Assert.Equal("bad_args", bad.Code);
        Assert.Equal("bad_args", Assert.Throws<OneGame.Control.ControlException>(() => cluster.Client.Call("camera", """{"position":[1,2]}""")).Code);
    }

    [Fact]
    public void CaptureOfA3DViewIsTheSameFromRunToRun()
    {
        using var a = Fixture.Start();
        using var b = Fixture.Start();
        a.Client.Step(25);
        b.Client.Step(25);
        var one = a.Client.Capture("640x360");
        var two = b.Client.Capture("640x360");
        Assert.Equal(0, ImageCompare.Compare(one, two).DifferingPixels);
        var small = a.Client.Capture("320x180");
        Assert.Equal("render3d", a.Client.Game.Stage.Name);
        Assert.Equal(320, small.Width);
    }
}
