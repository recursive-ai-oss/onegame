using OneGame.Control;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Host;
using OneGame.Render3D;
using Physics3DExample;

namespace OneGame.Physics.Tests;

public class Debug3DTests
{
    [Fact]
    public void AdapterIsGatedAndDoesNotChangeState()
    {
        var space = new PhysicsSpace3D();
        space.AddBox(new FVec3(1, 1, 1), new FVec3(0, 2, 0));
        space.AddCharacter(new FVec3(3, 1, 0));
        var chassis = space.AddBox(new FVec3(2, Fixed.One, 4), new FVec3(6, 1, 0));
        space.AddVehicle(chassis); space.Step();
        var before = space.Hash; var debug = new PhysicsDebug3D(); var drawing = new DebugDraw3D();
        debug.Draw(space, drawing, false); Assert.Equal(0, drawing.Count);
        debug.Draw(space, drawing, true); Assert.True(drawing.Count > 100);
        Assert.Equal(before, space.Hash);
        var count = drawing.Count; drawing.Clear(); debug.Draw(space, drawing, true);
        Assert.Equal(count, drawing.Count); Assert.Equal(before, space.Hash);
    }

    [Fact]
    public void HarnessOverlayCapturesPhysicsAndHasNoValidationMessages()
    {
        Assert.SkipUnless(GpuBackends.IsAvailable, "needs a graphics device (lavapipe is sufficient)");
        using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        using (var game = GameInstance.Create(new Physics3DGame(), new GameOptions { Headless = true, Width = 320, Height = 180, Gpu = GpuMode.Required, Device = device }))
        {
            var control = new ControlSurface(game);
            control.Call("step", """{"frames":80}""");
            var before = game.World.ComputeHash();
            var bare = game.Capture(320, 180);
            Assert.Equal(0, ((Render3DStage)game.Stage).Scene.Debug.Count);
            control.Call("overlay", """{"visible":true}""");
            var captured = game.Capture(320, 180);
            Assert.True(((Render3DStage)game.Stage).Scene.Debug.Count > 100);
            Assert.False(bare.Rgba.AsSpan().SequenceEqual(captured.Rgba));
            Assert.Equal(before, game.World.ComputeHash());
            Assert.Contains(game.DescribeRenderGraph()!.Passes, p => p.Name == "forward + debug3d");
            if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, "physics3d.png"), Png.Encode(captured));
            }
            control.Call("overlay", """{"visible":false}""");
            game.Capture(320, 180);
            Assert.Equal(0, ((Render3DStage)game.Stage).Scene.Debug.Count);
        }
        device.WaitIdle();
        Assert.Empty(device.TakeValidationMessages());
    }
}
