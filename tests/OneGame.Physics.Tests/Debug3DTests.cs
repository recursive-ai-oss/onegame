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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HarnessOverlayCapturesPhysicsAndHasNoValidationMessages(bool headless)
    {
        Assert.SkipUnless(headless || Environment.GetEnvironmentVariable("ONEGAME_PHYSICS_WINDOWED") == "1", "windowed capture is opt-in under a test display");
        Assert.SkipUnless(Environment.GetEnvironmentVariable("ONEGAME_PHYSICS_GPU_TESTS") != "0" && GpuBackends.IsAvailable, "needs a graphics device (lavapipe is sufficient)");
        using var device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        using (var game = GameInstance.Create(new Physics3DGame(), new GameOptions { Headless = headless, Backend = OneGame.Platform.PlatformBackendKind.X11, DisplayName = Environment.GetEnvironmentVariable("DISPLAY"), Width = 320, Height = 180, Gpu = GpuMode.Required, Device = device }))
        {
            var control = new ControlSurface(game);
            control.Call("step", """{"frames":80}""");
            var before = game.World.ComputeHash();
            var bare = game.Capture(320, 180);
            Assert.Equal(0, ((Render3DStage)game.Stage).Scene.Debug.Count);
            control.Call("overlay", """{"visible":true}""");
            Assert.Equal(before, game.World.ComputeHash());
            if (!headless) { game.Step(2); before = game.World.ComputeHash(); Assert.True(game.PresentedFrames > 0); }
            var captured = game.Capture(320, 180);
            Assert.NotNull(captured);
            Assert.True(((Render3DStage)game.Stage).Scene.Debug.Count > 100);
            Assert.False(bare.Rgba.AsSpan().SequenceEqual(captured.Rgba));
            Assert.Equal(before, game.World.ComputeHash());
            Assert.Contains(game.DescribeRenderGraph()!.Passes, p => p.Name == "forward + debug3d");
            if (Environment.GetEnvironmentVariable("ONEGAME_CAPTURE_DIR") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, headless ? "physics3d.png" : "physics3d-windowed.png"), Png.Encode(captured));
            }
            control.Call("overlay", """{"visible":false}""");
            game.Capture(320, 180);
            Assert.Equal(0, ((Render3DStage)game.Stage).Scene.Debug.Count);
        }
        device.WaitIdle();
        var messages = device.TakeValidationMessages();
        Assert.True(messages.Length == 0, string.Join("\n", messages));
    }
}
