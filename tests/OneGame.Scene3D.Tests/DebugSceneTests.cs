using System.Numerics;
using OneGame.Core;
using OneGame.Host;
using OneGame.Render3D;

namespace OneGame.Scene3D.Tests;

[Entity]
public partial class DebugProp : Entity, IView3D
{
    [View]
    public void Describe3D(SceneWriter scene)
    {
        scene.DebugLine(new FVec3(-2, 0, 0), new FVec3(2, 0, 0), new Vector4(1, 0, 0, 1), 8);
        scene.DebugPoint(new Vector3(-1, 1, 0), new Vector4(0, 1, 0, 1), 12);
        scene.DebugArrow(FVec3.Zero, new FVec3(1, 1, 0), new Vector4(0, 0, 1, 1), 4);
    }
}

public class DebugSceneTests
{
    private sealed class DebugGame : GameDefinition
    {
        public SceneView3D View { get; } = new();
        public override string Name => "Debug geometry";
        public override void DefineScreens(ScreenSet screens) => screens.Add("main", _ => OneGame.UI.Ui.Column());
        public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.OneGame_Scene3D_Tests.EntityTypes.Register(registry);
        public override void Setup(World world) => world.Add(new DebugProp());
        public override void DefineFonts(OneGame.Text.FontLibrary fonts) => new Scene3DGame.Scene3DGameDefinition().DefineFonts(fonts);
        public override IRenderStage CreateRenderStage(HostResources resources) => new Render3DStage(resources, View);
    }

    [Fact]
    public void EntityGeometryIsGatedAndOverlayTogglesBothDrawingPaths()
    {
        Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
        var definition = new DebugGame();
        using var cluster = TestCluster.Start(definition, new TestOptions { Width = 240, Height = 180, Gpu = GpuMode.Required });
        var stage = Assert.IsType<Render3DStage>(cluster.Client.Game.Stage);
        cluster.Client.Call("overlay", "{\"visible\":false}");
        Assert.False(definition.View.DebugVisible);
        Assert.False(stage.Draw.DebugVisible);
        var off = cluster.Client.Capture("240x180");
        Assert.Equal(0, stage.Scene.Debug.Count);
        cluster.Client.Call("overlay", "{\"visible\":true}");
        Assert.True(definition.View.DebugVisible);
        Assert.True(stage.Draw.DebugVisible);
        cluster.Client.Capture("240x180");
        Assert.Equal(3, stage.Scene.Debug.Count);
        Assert.Contains("debug3d", cluster.Client.Call("render_graph"), StringComparison.Ordinal);
        cluster.Client.Call("overlay", "{\"visible\":false}");
        // Isolate primitive pixels from the panel's own pixels.
        definition.View.DebugVisible = true;
        var on = cluster.Client.Capture("240x180");
        Assert.True(ImageCompare.Compare(off, on).DifferingPixels > 100);
        definition.View.DebugVisible = false;
        var offAgain = cluster.Client.Capture("240x180");
        Assert.Equal(0, stage.Scene.Debug.Count);
        Assert.Equal(0, ImageCompare.Compare(off, offAgain).DifferingPixels);
        Assert.Empty(cluster.Client.Game.TakeGpuValidationMessages());
    }
}
