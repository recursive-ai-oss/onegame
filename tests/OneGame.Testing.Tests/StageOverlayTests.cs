using OneGame.Host;
using OneGame.UI;

namespace OneGame.Testing.Tests;

/// <summary>A game whose stage adds its own overlay: a magenta box with an outline and a line of text, above the UI.</summary>
public sealed class OverlayStageGame : ShopGame
{
    public override string Name => "OverlayStageGame";

    public override void DefineFonts(OneGame.Text.FontLibrary fonts) =>
        fonts.Add(File.ReadAllBytes(Path.Combine(GpuPaths.RepoRoot, "testdata", "text", "fonts", "NotoSans-Subset-Regular.ttf")));

    public override IRenderStage CreateRenderStage(HostResources resources)
    {
        var stage = new Render2DStage(resources);
        stage.AddOverlay(new Mark());
        return stage;
    }

    private sealed class Mark : IFrameOverlay
    {
        public void Draw(OverlayCanvas canvas)
        {
            canvas.Fill(240, 150, 60, 30, new Color(255, 0, 255, 255));
            canvas.Outline(236, 146, 68, 38, 2, new Color(0, 255, 0, 255));
            canvas.Text("mark", 150, 160, 16, new Color(255, 255, 0, 255));
        }
    }
}

/// <summary>A stage's own overlay and the development panel draw together, and neither replaces the other.</summary>
public class StageOverlayTests
{
    private static uint At(CapturedFrame f, int x, int y) => BitConverter.ToUInt32(f.Rgba, ((y * f.Width) + x) * 4);

    [Fact]
    public void AGameOverlayAndTheDebugPanelAreBothDrawnAndShowInCaptures()
    {
        Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
        using var cluster = TestCluster.Start(new OverlayStageGame(), new TestOptions { Width = 320, Height = 200, Gpu = GpuMode.Required });
        var c = cluster.Client;
        var stage = Assert.IsType<Render2DStage>(c.Game.Stage);
        c.Step(2);
        var off = c.Capture("320x200");
        Assert.Equal(0xFFFF00FFu, At(off, 270, 165));
        Assert.Equal(0xFF00FF00u, At(off, 237, 165));
        var yellow = 0;
        for (var y = 150; y < 175; y++)
        {
            for (var x = 150; x < 200; x++)
            {
                yellow += At(off, x, y) is var p && (p & 0xFF) > 200 && ((p >> 8) & 0xFF) > 200 && ((p >> 16) & 0xFF) < 100 ? 1 : 0;
            }
        }

        Assert.True(yellow > 20, $"the text should be drawn in its colour; {yellow} yellow pixels");

        // Turning the panel on (the control layer installs it in the stage's own slot) leaves the game's overlay in place and adds the panel.
        c.Call("overlay", """{"visible":true}""");
        c.Step(2);
        var on = c.Capture("320x200");
        Assert.NotNull(stage.Overlay);
        Assert.Single(stage.Overlays);
        Assert.Equal(0xFFFF00FFu, At(on, 270, 165));
        Assert.Equal(0xFF00FF00u, At(on, 237, 165));
        var panelPixelsChanged = 0;
        for (var y = 0; y < 60; y++)
        {
            for (var x = 0; x < 160; x++)
            {
                panelPixelsChanged += At(on, x, y) != At(off, x, y) ? 1 : 0;
            }
        }

        Assert.True(panelPixelsChanged > 500, $"the panel should be visible; {panelPixelsChanged} pixels changed");
        Assert.True(stage.RemoveOverlay(stage.Overlays[0]));
        c.Step(1);
        Assert.NotEqual(0xFFFF00FFu, At(c.Capture("320x200"), 270, 165));
        Assert.Empty(c.Game.TakeGpuValidationMessages());
    }
}
