using Draw2DGame;
using OneGame.Atlas;
using OneGame.Control;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Host;
using OneGame.Render2D;
using OneGame.Text;
using OneGame.UI;

namespace OneGame.Draw2D.Tests;

public class OverlayTests
{
    [Fact]
    public void ShowingThePanelDrawsIntoCapturesAndLeavesTheStateHashAlone()
    {
        using var cluster = Fixture.Start();
        cluster.Client.Step(5);
        var game = cluster.Client.Game;
        var hash = cluster.Client.Call("hash");
        // The very first capture of a game can differ from the ones after it (the coin's disc texture is not ready yet), so take one first.
        game.Capture(480, 270);
        var plain = game.Capture(480, 270);
        Assert.Equal(plain.Rgba, game.Capture(480, 270).Rgba);

        cluster.Client.Call("overlay", "{\"visible\":true}");
        var shown = game.Capture(480, 270);
        Assert.Equal(hash, cluster.Client.Call("hash"));
        Assert.NotEqual(plain.Rgba, shown.Rgba);

        // Only the top-right corner changed; the rest of the frame is the game's own.
        var changed = 0;
        for (var i = 0; i < plain.Rgba.Length; i += 4)
        {
            var px = (i / 4) % 480;
            var py = (i / 4) / 480;
            if (plain.Rgba[i] != shown.Rgba[i] || plain.Rgba[i + 1] != shown.Rgba[i + 1] || plain.Rgba[i + 2] != shown.Rgba[i + 2])
            {
                changed++;
                Assert.True(px > 100 && py < 100, $"pixel {px},{py} changed outside the panel's corner: {plain.Rgba[i]},{plain.Rgba[i + 1]},{plain.Rgba[i + 2]} -> {shown.Rgba[i]},{shown.Rgba[i + 1]},{shown.Rgba[i + 2]}");
            }
        }

        Assert.True(changed > 1000, $"only {changed} pixels changed");

        cluster.Client.Call("overlay", "{\"visible\":false}");
        Assert.Equal(plain.Rgba, game.Capture(480, 270).Rgba);

        // Running on with the panel shown ends at the same hash as running on without it.
        cluster.Client.Step(20);
        var without = cluster.Client.Call("hash");
        using var other = Fixture.Start();
        other.Client.Step(5);
        other.Client.Call("overlay", "{\"visible\":true}");
        other.Client.Step(20);
        Assert.Equal(without, other.Client.Call("hash"));
        Assert.Empty(game.TakeGpuValidationMessages());
    }

    [Fact]
    public void ADrawnPanelAllocatesOnlyWhenItsTextRefreshes()
    {
        using var cluster = Fixture.Start();
        var game = cluster.Client.Game;
        var overlay = DebugOverlay.For(game);
        overlay.Visible = true;
        overlay.Masked = false;
        var definition = new Draw2DGameDefinition();
        var registry = new EntityRegistry();
        definition.RegisterEntities(registry);
        var world = new World(1, registry);
        definition.Setup(world);
        var fonts = new FontLibrary();
        definition.DefineFonts(fonts);

        using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        using var stage = new Render2DStage(new HostResources(fonts, new FontTextMeasurer(fonts), new ArtAtlas(), GeneratedShaders.Instance)) { Overlay = overlay };
        using var target = device.CreateTexture(new TextureDesc(320, 180, GpuFormat.Rgba8UnormSrgb, TextureUsage.RenderTarget | TextureUsage.CopySource));
        var list = new DisplayList();

        void Frame() => stage.Render(device, target, new FrameInfo(320, 180, 1, world.Tick, list, world, Fixed.Half));

        for (var i = 0; i < 40; i++)
        {
            world.Step();
            Frame();
        }

        device.WaitIdle();
        long total = 0;
        var worst = 0L;
        var refreshes = 0;
        for (var i = 0; i < 150; i++)
        {
            world.Step();
            var before = GC.GetAllocatedBytesForCurrentThread();
            Frame();
            var delta = GC.GetAllocatedBytesForCurrentThread() - before;
            total += delta;
            worst = Math.Max(worst, delta);
            if (delta > 0)
            {
                refreshes++;
            }
        }

        device.WaitIdle();
        Console.WriteLine($"overlay: 150 frames allocated {total} B in {refreshes} frames (worst {worst} B)");
        // The text is rebuilt every 15th frame (ten times in 150); the renderer shapes the new strings then (about 10 KB), and every other frame allocates nothing.
        Assert.True(refreshes <= 11, $"{refreshes} frames allocated");
        Assert.True(worst < 32 * 1024, $"worst frame {worst} B");
        Assert.Empty(device.TakeValidationMessages());
    }
}
