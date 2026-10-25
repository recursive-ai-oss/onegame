using System.Numerics;
using Lanterns.Views;
using OneGame.Core;
using OneGame.Gpu;
using OneGame.Gpu.Vulkan;
using OneGame.Host;
using OneGame.Render3D;
using Scene = OneGame.Render3D.Scene3D;

namespace Lanterns.Tests;

/// <summary>The game in 3D: what is drawn comes from the replicas, moves smoothly, follows the player over the bridge, and costs no allocation.</summary>
public class View3DTests
{
    private static void Walk(Session s, int x, int y)
    {
        if (s.Me is { } me)
        {
            s.Steer(Math.Sign(x - me.X) * (Math.Abs(x - me.X) > Rules.Speed ? 1 : 0), Math.Sign(y - me.Y) * (Math.Abs(y - me.Y) > Rules.Speed ? 1 : 0), every: true);
        }
    }

    // ---- smoothing ---------------------------------------------------------------------------------------------------

    [Fact]
    public void ACorrectionBlendsInOverSeveralTicksInsteadOfPopping()
    {
        var motion = new Motion();
        const float step = 2 * Layout.Unit;
        var x = 5f;
        long tick = 100;
        Vector2 Frame()
        {
            motion.Begin(++tick);
            var t = motion.Touch(7, new Vector2(x, 4f), Rules.IslandA, 1, 0);
            motion.End();
            return t.Shown;
        }

        _ = Frame();
        var last = Frame();
        float steady = 0;
        for (var i = 0; i < 30; i++)
        {
            x += step;
            var now = Frame();
            steady = MathF.Max(steady, Vector2.Distance(now, last));
            last = now;
        }

        // Steady walking: the drawn walker moves one step per tick and trails the truth by a fraction of a step.
        Assert.InRange(steady, step * 0.9f, step * 1.1f);
        Assert.True(x - last.X < step * 2, $"the drawn walker trails by {x - last.X} m");

        // The authority disagrees: the predicted position jumps 0.8 m. The state pops; the picture must not.
        const float pop = 0.8f;
        x += pop;
        var worst = 0f;
        for (var i = 0; i < 20; i++)
        {
            var now = Frame();
            worst = MathF.Max(worst, Vector2.Distance(now, last));
            last = now;
            x += step;
        }

        Assert.True(worst < step + (Motion.Chase * pop) + 0.01f, $"the drawn walker moved {worst} m in one tick; a correction of {pop} m must be spread out");
        Assert.True(worst > step, "the correction should be visible as a quicker move, not swallowed");
        Assert.True(x - step - last.X < step * 2, "the drawn walker did not catch up");
    }

    [Fact]
    public void AWalkerHandedToTheOtherIslandGlidesAcrossTheBridgeAtAFixedSpeed()
    {
        var motion = new Motion();
        long tick = 0;
        motion.Begin(++tick);
        var from = new Vector2(Layout.X(Rules.IslandA, 380), 4f);
        var to = new Vector2(Layout.X(Rules.IslandB, 4), 4f);
        motion.Touch(1, from, Rules.IslandA, 1, 0);
        motion.End();
        var last = from;
        var worst = 0f;
        var ticks = 0;
        while (Vector2.Distance(last, to) > 0.01f && ticks < 100)
        {
            motion.Begin(++tick);
            var t = motion.Touch(1, to, Rules.IslandB, 0, 0);
            motion.End();
            worst = MathF.Max(worst, Vector2.Distance(t.Shown, last));
            last = t.Shown;
            ticks++;
        }

        Assert.True(worst <= Motion.GlideStep + 1e-3f, $"{worst} m in one tick");
        Assert.InRange(ticks, 25, 40);
    }

    [Fact]
    public void ThePredictedWalkersCorrectionIsContinuousOnTheScreen()
    {
        using var r = new Realms();
        var game = r.Client("p1", Rules.IslandA);
        var lanterns = (LanternsGame)game.Definition;
        var s = Session.For(game);
        r.Run(90);
        Assert.NotNull(s.Me);
        var owner = s.Me!.Owner;

        // Walk east. Half way, the server moves the player 2.5 m: the client's prediction is rebuilt from that and its state jumps.
        Vector2? shown = null;
        Vector2? raw = null;
        float worstDrawn = 0, worstRaw = 0;
        var popped = false;
        var after = 0;
        r.Run(75, i =>
        {
            s.Steer(1, 0, every: true);
            if (i == 20)
            {
                var truth = r.Players().Single();
                truth.X += 60;
                popped = true;
            }

            lanterns.View.Prepare(game.World, game.World.Tick, Fixed.Zero);
            var track = lanterns.Scene.Motion.Get(owner);
            if (track is null)
            {
                return;
            }

            if (shown is { } was && raw is { } rawWas && popped)
            {
                worstDrawn = MathF.Max(worstDrawn, Vector2.Distance(track.Shown, was));
                worstRaw = MathF.Max(worstRaw, Vector2.Distance(track.Target, rawWas));
                after++;
            }

            shown = track.Shown;
            raw = track.Target;
        });

        Assert.True(after > 30);
        // The state really jumped (2.5 m, the walking step being 0.08) and the picture moved at most a fraction of that in any one tick.
        Assert.True(worstRaw > 2f, $"the predicted state moved {worstRaw} m: the test did not provoke a correction");
        Assert.True(worstDrawn < 0.2f + (Motion.Chase * worstRaw), $"the drawn walker moved {worstDrawn} m in one tick");
        Assert.True(worstDrawn < worstRaw * 0.6f, $"drawn {worstDrawn} m against {worstRaw} m of state");
    }

    // ---- the whole thing in 3D --------------------------------------------------------------------------------------

    [Fact]
    public void CrossingTheBridgeIsSeenInThreeDimensionsAndTheCameraFollows()
    {
        using var r = new Realms();
        var game = r.Client("p1", Rules.IslandA, GpuMode.Required);
        var lanterns = (LanternsGame)game.Definition;
        var scene = lanterns.Scene;
        var s = Session.For(game);
        r.Run(90);
        Assert.NotNull(s.Me);
        var owner = s.Me!.Owner;
        var dir = Path.Combine(Path.GetTempPath(), "lanterns-3d");
        Directory.CreateDirectory(dir);
        var shots = new Dictionary<string, CapturedFrame>();
        Vector2? focus = null;
        float worstCamera = 0, worstWalker = 0;
        Vector2? walker = null;
        var gliding = 0;
        var crossed = false;

        void Shoot(string name)
        {
            var frame = game.Capture(640, 360);
            shots[name] = frame;
            Png.Save(frame, Path.Combine(dir, name + ".png"));
            // My walker is on screen, wherever the camera has gone.
            var me = scene.Motion.Get(owner)!;
            Assert.True(lanterns.View.TryProject(new Vector3(me.Shown.X, 0.5f, me.Shown.Y), 640, 360, out var px, out _), name + ": my walker is not in front of the camera");
            Assert.InRange(px.X, 0, 640);
            Assert.InRange(px.Y, 0, 360);
        }

        r.RunUntil(() => crossed && gliding == 0 && s.Scope == Rules.IslandB && scene.Motion.Get(owner) is { Glide: false } && Vector2.Distance(scene.Motion.Camera.Shown, scene.Motion.Get(owner)!.Shown) < 0.2f, 60_000, "to arrive on the other island", i =>
        {
            Walk(s, s.Scope == Rules.IslandA ? Rules.Width + 8 : 100, 96);
            lanterns.View.Prepare(game.World, game.World.Tick, Fixed.Zero);
            var me = scene.Motion.Get(owner);
            if (me is null)
            {
                return;
            }

            if (focus is { } f)
            {
                worstCamera = MathF.Max(worstCamera, Vector2.Distance(scene.Motion.Camera.Shown, f));
            }

            if (walker is { } w)
            {
                worstWalker = MathF.Max(worstWalker, Vector2.Distance(me.Shown, w));
            }

            focus = scene.Motion.Camera.Shown;
            walker = me.Shown;
            if (me.Shown.X > Layout.X(Rules.IslandA, Rules.Width - 40) && !shots.ContainsKey("before") && s.Scope == Rules.IslandA)
            {
                Shoot("before");
            }

            if (me.Glide && !shots.ContainsKey("glide"))
            {
                Shoot("glide");
            }

            crossed |= s.Scope == Rules.IslandB;
            gliding = me.Glide ? 1 : 0;
        });

        Shoot("after");
        Assert.Equal(Rules.IslandB, s.Scope);
        Assert.Contains("before", shots.Keys);
        Assert.Contains("glide", shots.Keys);

        // The three views are different pictures, none of them empty: lit lanterns, sea, islands.
        foreach (var (name, frame) in shots)
        {
            var colours = new HashSet<int>();
            for (var i = 0; i < frame.Rgba.Length; i += 4 * 17)
            {
                colours.Add((frame.Rgba[i] >> 3) | ((frame.Rgba[i + 1] >> 3) << 5) | ((frame.Rgba[i + 2] >> 3) << 10));
            }

            Assert.True(colours.Count > 200, $"{name} has {colours.Count} colours");
        }

        Assert.NotEqual(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(shots["before"].Rgba)), Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(shots["after"].Rgba)));
        Assert.True(worstCamera <= Motion.MaxCameraStep + 1e-3f, $"the camera moved {worstCamera} m in one tick");
        Assert.True(worstWalker <= Motion.GlideStep + 0.1f, $"the walker moved {worstWalker} m in one tick");
        Console.WriteLine($"captures in {dir}; camera {worstCamera:F2} m/tick at most, walker {worstWalker:F2} m/tick at most");
    }

    [Fact]
    public void SteadyFramesDrawnFromTheWorldAllocateNothing()
    {
        var game = new LanternsGame();
        var registry = new EntityRegistry();
        game.RegisterEntities(registry);
        var world = new World(1, registry);
        game.RegisterSystems(world);
        game.Setup(world);

        Environment.SetEnvironmentVariable("ONEGAME_GPU_VALIDATION", "1");
        using var device = VulkanGpu.CreateDevice(new GpuDeviceOptions { Validation = true, RequireValidation = true });
        var renderer = new Renderer3D(device);
        var target = new Offscreen3D(renderer, 320, 180);
        var scene = new Scene { Samples = 1 };
        scene.Ssao.Enabled = true; // and the moon casts shadows: the allocation claim covers the shadow and ambient occlusion passes too
        var view = game.View;

        long prepared = 0, built = 0;
        void Frame(long tick)
        {
            var a = GC.GetAllocatedBytesForCurrentThread();
            view.Prepare(world, tick, Fixed.Half);
            var b = GC.GetAllocatedBytesForCurrentThread();
            view.Build(scene, renderer);
            var c = GC.GetAllocatedBytesForCurrentThread();
            target.Render(scene);
            prepared += b - a;
            built += c - b;
        }

        // Both walkers hold six lanterns, the most that is drawn, and the island fills with lanterns: every pool has reached its size.
        foreach (var p in world.Entities.OfType<Player>())
        {
            p.Change(0, 2);
            p.Change(1, 2);
            p.Change(2, 2);
        }

        for (var i = 0; i < 600; i++)
        {
            world.Step();
            Frame(world.Tick);
        }

        device.WaitIdle();
        world.Step();
        Frame(world.Tick);

        prepared = built = 0;
        long frames = 0;
        var where = new List<string>();
        for (var i = 0; i < 120; i++)
        {
            world.Step();
            var a = GC.GetAllocatedBytesForCurrentThread();
            Frame(world.Tick);
            var delta = GC.GetAllocatedBytesForCurrentThread() - a;
            frames += delta;
            if (delta > 0)
            {
                where.Add($"frame {i}: {delta}");
            }
        }

        device.WaitIdle();
        Assert.True(frames == 0, $"{frames} bytes over 120 frames (describing {prepared}, building {built}): {string.Join(", ", where.Take(6))}");
        var props = scene.Batches.Sum(b => b.Count);
        Assert.True(scene.Models.Count > 20 && props > 40, $"{scene.Models.Count} models and {props} instanced props drawn");
        Assert.True(scene.Lights.Any(l => l.CastShadows), "the moon should cast shadows");
        Assert.True(scene.Lights.Count >= 5, $"{scene.Lights.Count} lights");
    }
}
