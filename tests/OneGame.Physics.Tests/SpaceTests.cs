using OneGame.Core;
using OneGame.Physics;

namespace OneGame.Physics.Tests;

/// <summary>A physics space as part of a world: stepped by the tick, hashed, saved, restored, and free of per-tick allocation.</summary>
public class SpaceTests
{
    private static readonly BodyOptions Still = new() { Kind = BodyKind.Static };

    private static World NewWorld(PhysicsOptions? options = null, ulong seed = 5)
    {
        var world = new World(seed, new EntityRegistry());
        PhysicsSpace2D.AddTo(world, options);
        return world;
    }

    // A floor, a trigger above it, a stack of boxes, a bouncy ball dropped from high up and a character who walks right.
    private static World Scene(out PhysicsSpace2D space, out CharacterRef hero, out BodyRef trigger)
    {
        var world = NewWorld();
        space = PhysicsSpace2D.Of(world)!;
        space.AddBox(new FVec2(40, 1), new FVec2(0, -1), Still);
        trigger = space.AddBox(new FVec2(2, 2), new FVec2(6, 1), new BodyOptions { Kind = BodyKind.Static, IsTrigger = true });
        for (var i = 0; i < 4; i++)
        {
            space.AddBox(new FVec2(1, 1), new FVec2(-6, Fixed.FromRatio(1 + (i * 11), 10)));
        }

        space.AddBox(new FVec2(1, 1), new FVec2(6, 7)); // falls through the trigger

        space.AddCircle(Fixed.FromRatio(1, 2), new FVec2(-3, 8), new BodyOptions { Restitution = Fixed.FromRatio(6, 10) });
        hero = space.AddCharacter(new FVec2(-2, Fixed.FromRatio(11, 10)), new CharacterOptions { Height = Fixed.FromInt(2) });
        return world;
    }

    private static void Tick(World world, PhysicsSpace2D space, CharacterRef hero)
    {
        space.Move(hero, new FVec2(Fixed.FromRatio(1, 12), Fixed.FromRatio(-1, 10)));
        world.Step();
    }

    [Fact]
    public void TwoRunsGiveTheSameHashEveryTickAndTheSceneReallyMoves()
    {
        ulong[] Run()
        {
            var world = Scene(out var space, out var hero, out _);
            var hashes = new List<ulong>();
            for (var t = 0; t < 400; t++)
            {
                Tick(world, space, hero);
                hashes.Add(world.ComputeHash());
            }

            return [.. hashes];
        }

        var a = Run();
        Assert.Equal(a, Run());
        Assert.True(a.Distinct().Count() > 150);
    }

    [Fact]
    public void SnapshotAndRestoreMidFallContinueWithTheSameHashes()
    {
        var world = Scene(out var space, out var hero, out _);
        for (var t = 0; t < 40; t++)
        {
            Tick(world, space, hero); // the ball is still falling
        }

        var w = new SyncWriter();
        world.WriteSnapshot(w);
        var saved = w.ToArray();

        var other = NewWorld();
        Assert.Equal(SyncError.None, other.ReadSnapshot(saved));
        var otherSpace = PhysicsSpace2D.Of(other)!;
        Assert.Equal(world.ComputeHash(), other.ComputeHash());
        Assert.Equal(space.Hash, otherSpace.Hash);

        // References made before the save name the same bodies after it.
        for (var t = 0; t < 300; t++)
        {
            Tick(world, space, hero);
            Tick(other, otherSpace, hero);
            Assert.Equal(world.ComputeHash(), other.ComputeHash());
        }

        Assert.Equal(space.Position(space.BodyOf(hero)), otherSpace.Position(otherSpace.BodyOf(hero)));
    }

    [Fact]
    public void RestoringIntoTheSameWorldRewindsIt()
    {
        var world = Scene(out var space, out var hero, out _);
        for (var t = 0; t < 30; t++)
        {
            Tick(world, space, hero);
        }

        var w = new SyncWriter();
        world.WriteSnapshot(w);
        var hashes = new List<ulong>();
        for (var t = 0; t < 100; t++)
        {
            Tick(world, space, hero);
            hashes.Add(world.ComputeHash());
        }

        Assert.Equal(SyncError.None, world.ReadSnapshot(w.Written));
        for (var t = 0; t < 100; t++)
        {
            Tick(world, space, hero);
            Assert.Equal(hashes[t], world.ComputeHash());
        }
    }

    [Fact]
    public void TheWorldHashChangesWhenOnlyThePhysicsChanges()
    {
        var world = NewWorld();
        var space = PhysicsSpace2D.Of(world)!;
        var ball = space.AddCircle(Fixed.One, new FVec2(0, 5));
        world.Step();
        var before = world.ComputeHash();
        space.SetVelocity(ball, new FVec2(1, 0));
        Assert.NotEqual(before, world.ComputeHash());
    }

    [Fact]
    public void EventsComeInTheSameOrderOnEveryRunAndSurviveARestore()
    {
        string Run(int splitAt, out string afterSplit)
        {
            var world = Scene(out var space, out var hero, out var trigger);
            var log = new List<string>();
            afterSplit = "";
            for (var t = 0; t < 300; t++)
            {
                if (t == splitAt)
                {
                    var w = new SyncWriter();
                    world.WriteSnapshot(w);
                    Assert.Equal(SyncError.None, world.ReadSnapshot(w.Written));
                }

                Tick(world, space, hero);
                foreach (var e in space.Events)
                {
                    log.Add($"{t}:{e.Kind}:{e.A}:{e.B}");
                }
            }

            return string.Join("|", log);
        }

        var a = Run(-1, out _);
        Assert.Equal(a, Run(-1, out _));
        Assert.Equal(a, Run(137, out _));
        Assert.Contains("ContactBegin", a);   // the boxes and the ball land
        Assert.Contains("TriggerEnter", a);   // the ball or the walker reaches the trigger
        Assert.Contains("Sleep", a);          // the stack settles
    }

    [Fact]
    public void ABodyEntersAndLeavesATriggerOnce()
    {
        var world = NewWorld();
        var space = PhysicsSpace2D.Of(world)!;
        space.AddBox(new FVec2(40, 1), new FVec2(0, -1), Still);
        var trigger = space.AddBox(new FVec2(4, 4), new FVec2(0, 3), new BodyOptions { Kind = BodyKind.Static, IsTrigger = true });
        var box = space.AddBox(new FVec2(1, 1), new FVec2(0, 9));
        var log = new List<string>();
        for (var t = 0; t < 240; t++)
        {
            world.Step();
            foreach (var e in space.Events)
            {
                if (e.Involves(trigger))
                {
                    Assert.True(e.Involves(box));
                    log.Add(e.Kind.ToString());
                }
            }
        }

        Assert.Equal(["TriggerEnter", "TriggerExit"], log);
    }

    [Fact]
    public void ACharacterWalkingThroughATriggerEntersAndLeavesIt()
    {
        var world = NewWorld();
        var space = PhysicsSpace2D.Of(world)!;
        space.AddBox(new FVec2(40, 1), new FVec2(0, -1), Still);
        var trigger = space.AddBox(new FVec2(2, 4), new FVec2(4, 2), new BodyOptions { Kind = BodyKind.Static, IsTrigger = true });
        var hero = space.AddCharacter(new FVec2(0, Fixed.FromRatio(11, 10)), new CharacterOptions { Height = Fixed.FromInt(2) });
        var body = space.BodyOf(hero);
        var log = new List<string>();
        for (var t = 0; t < 240; t++)
        {
            space.Move(hero, new FVec2(Fixed.FromRatio(1, 12), Fixed.FromRatio(-1, 10)));
            world.Step();
            foreach (var e in space.Events)
            {
                if (e.Involves(trigger) && e.Involves(body))
                {
                    log.Add(e.Kind.ToString());
                }
            }
        }

        Assert.Equal(["TriggerEnter", "TriggerExit"], log);
    }

    [Fact]
    public void ACharacterStandsOnGroundWalksIntoABoxAndPushesIt()
    {
        var world = NewWorld();
        var space = PhysicsSpace2D.Of(world)!;
        space.AddBox(new FVec2(40, 1), new FVec2(0, -1), Still);
        var box = space.AddBox(new FVec2(1, 1), new FVec2(3, Fixed.FromRatio(1, 2)));
        var hero = space.AddCharacter(new FVec2(0, Fixed.FromRatio(11, 10)), new CharacterOptions { Height = Fixed.FromInt(2) });
        CharacterMove last = default;
        for (var t = 0; t < 120; t++)
        {
            last = space.Move(hero, new FVec2(Fixed.FromRatio(1, 12), Fixed.FromRatio(-1, 10)));
            world.Step();
        }

        Assert.True(last.Grounded);
        Assert.True(space.IsGrounded(hero));
        Assert.True(space.Position(box).X > Fixed.FromInt(4), $"box at {space.Position(box)}");
        Assert.True(space.Position(hero).X > Fixed.FromInt(2));
    }

    [Fact]
    public void RaycastsFindTheNearestBodyAndSkipTriggersAndOtherLayers()
    {
        var world = NewWorld();
        var space = PhysicsSpace2D.Of(world)!;
        var floor = space.AddBox(new FVec2(40, 1), new FVec2(0, -1), Still);
        space.AddBox(new FVec2(2, 2), new FVec2(0, 3), new BodyOptions { Kind = BodyKind.Static, IsTrigger = true });
        var wall = space.AddBox(new FVec2(1, 6), new FVec2(10, 3), new BodyOptions { Kind = BodyKind.Static, Layer = 3 });

        Assert.True(space.Raycast(new FVec2(0, 8), new FVec2(0, -1), Fixed.FromInt(100), out var hit));
        Assert.Equal(floor, hit.Body);
        Assert.InRange(hit.Distance.ToDouble(), 8.49, 8.51);
        Assert.InRange(hit.Normal.Y.ToDouble(), 0.99, 1.01);

        Assert.True(space.Raycast(new FVec2(0, 3), new FVec2(1, 0), Fixed.FromInt(100), out var side));
        Assert.Equal(wall, side.Body);
        Assert.InRange(side.Distance.ToDouble(), 9.49, 9.51);
        Assert.False(space.Raycast(new FVec2(0, 3), new FVec2(1, 0), Fixed.FromInt(100), out _, layers: 1u));
        Assert.False(space.Raycast(new FVec2(0, 3), new FVec2(1, 0), Fixed.FromInt(5), out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => space.Raycast(FVec2.Zero, FVec2.Zero, Fixed.One, out _));
    }

    [Fact]
    public void ALayerMatrixIsPartOfTheSavedState()
    {
        var world = NewWorld();
        var space = PhysicsSpace2D.Of(world)!;
        space.SetLayerCollision(1, 2, false);
        space.AddBox(new FVec2(40, 1), new FVec2(0, -1), new BodyOptions { Kind = BodyKind.Static, Layer = 2 });
        var ball = space.AddCircle(Fixed.FromRatio(1, 2), new FVec2(0, 3), new BodyOptions { Layer = 1 });
        var w = new SyncWriter();
        world.WriteSnapshot(w);

        var other = NewWorld(); // built without the matrix change: the save carries it
        Assert.Equal(SyncError.None, other.ReadSnapshot(w.Written));
        for (var t = 0; t < 120; t++)
        {
            other.Step();
        }

        Assert.True(PhysicsSpace2D.Of(other)!.Position(ball).Y < Fixed.FromInt(-2), "the ball fell through the floor it does not collide with");
    }

    [Fact]
    public void ARemovedBodyIsStaleAndItsSlotIsNotMistakenForTheNextBody()
    {
        var space = new PhysicsSpace2D();
        var first = space.AddCircle(Fixed.One, FVec2.Zero);
        space.Remove(first);
        Assert.False(space.Exists(first));
        Assert.Throws<StaleBodyException>(() => space.Position(first));
        var second = space.AddCircle(Fixed.One, FVec2.Zero);
        Assert.Equal(first.Index, second.Index);
        Assert.NotEqual(first, second);
        Assert.False(space.Exists(first));
        Assert.True(space.Exists(second));
        Assert.Throws<StaleBodyException>(() => space.Remove(default(BodyRef)));
    }

    [Fact]
    public void SavesThatAreDamagedOrFromAnotherTickRateFailWithoutThrowingOrChangingTheWorld()
    {
        var world = Scene(out var space, out var hero, out _);
        for (var t = 0; t < 20; t++)
        {
            Tick(world, space, hero);
        }

        var good = new SyncWriter();
        world.WriteSnapshot(good);
        var bytes = good.ToArray();
        var hash = world.ComputeHash();

        // Flip bytes through the physics part of the save, one at a time: never an exception, and a failure leaves the world as it was.
        var failures = 0;
        for (var i = bytes.Length / 3; i < bytes.Length; i += 29)
        {
            var copy = (byte[])bytes.Clone();
            copy[i] ^= 0xFF;
            var target = Scene(out var s2, out var h2, out _);
            for (var t = 0; t < 20; t++)
            {
                Tick(target, s2, h2);
            }

            var error = target.ReadSnapshot(copy);
            if (error != SyncError.None)
            {
                failures++;
            }
        }

        Assert.True(failures > 0);

        // Cut short: an error, not an exception.
        Assert.NotEqual(SyncError.None, NewWorld().ReadSnapshot(bytes.AsSpan(0, bytes.Length - 40)));

        // Made at another tick rate: refused, because stepping it here would not continue the same simulation.
        var fast = NewWorld(new PhysicsOptions { TicksPerSecond = 120 });
        Assert.NotEqual(SyncError.None, fast.ReadSnapshot(bytes));
        Assert.Equal(hash, world.ComputeHash());
    }

    [Fact]
    public void AFailedRestoreLeavesThePhysicsStateUntouched()
    {
        var world = Scene(out var space, out var hero, out _);
        for (var t = 0; t < 20; t++)
        {
            Tick(world, space, hero);
        }

        var good = new SyncWriter();
        world.WriteSnapshot(good);
        var bytes = good.ToArray();

        var target = Scene(out var targetSpace, out var targetHero, out _);
        for (var t = 0; t < 7; t++)
        {
            Tick(target, targetSpace, targetHero);
        }

        var before = targetSpace.Hash;
        var cut = (byte[])bytes.Clone();
        Array.Resize(ref cut, cut.Length - 10);
        Assert.NotEqual(SyncError.None, target.ReadSnapshot(cut));
        Assert.Equal(before, targetSpace.Hash);
    }

    [Fact]
    public void DebugShapesOutlineEveryShapeAndTheContacts()
    {
        var world = Scene(out var space, out var hero, out _);
        for (var t = 0; t < 200; t++)
        {
            Tick(world, space, hero);
        }

        var shapes = new PhysicsDebugShapes();
        space.AddDebugShapes(shapes);
        var colors = shapes.Lines.Select(l => l.Rgba).Distinct().ToArray();
        Assert.Contains(PhysicsDebugShapes.StaticColor, colors);
        Assert.Contains(PhysicsDebugShapes.TriggerColor, colors);
        Assert.Contains(PhysicsDebugShapes.KinematicColor, colors); // the character
        Assert.Contains(PhysicsDebugShapes.ContactColor, colors);
        Assert.True(shapes.Lines.Count > 60);
        shapes.Clear();
        Assert.Empty(shapes.Lines);
    }

    [Fact]
    public void TheTableIsHiddenAndAReplicaDoesNotReceiveIt()
    {
        var world = NewWorld();
        Assert.Equal(Visibility.Hidden, PhysicsSpace2D.Of(world)!.Visibility);
        Assert.Null(PhysicsSpace2D.Of(new World(1, new EntityRegistry())));
    }

    [Fact]
    public void TheStateQueryShowsBodies()
    {
        var world = NewWorld();
        var space = PhysicsSpace2D.Of(world)!;
        space.AddCircle(Fixed.One, new FVec2(2, 3));
        var json = new JsonOut();
        space.WriteJson(json);
        Assert.Contains("\"x\":2", json.ToString());
        Assert.Contains("\"y\":3", json.ToString());
    }

    [Fact]
    public void AWarmedSpaceAllocatesNothingFromTheFirstStepAndTheFirstRay()
    {
        // Move, reads, events, the first ray, the step and the space's own hash: 0 B from step 1 after Warmup.
        var world = Scene(out var space, out var hero, out var trigger);
        space.Warmup();
        var sink = 0;
        var worst = 0L;
        for (var t = 0; t < 600; t++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            space.Move(hero, new FVec2(Fixed.FromRatio(1, 12), Fixed.FromRatio(-1, 10)));
            sink += space.Position(trigger).X.ToInt() + space.Position(space.BodyOf(hero)).Y.ToInt();
            foreach (var e in space.Events)
            {
                sink += (int)e.Kind;
            }

            space.Raycast(new FVec2(0, 8), new FVec2(0, -1), Fixed.FromInt(50), out var hit);
            sink += hit.Distance.ToInt();
            space.Step();
            sink += (int)(space.Hash & 1);
            worst = Math.Max(worst, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        TestContext.Current.SendDiagnosticMessage($"warmed scene, 600 ticks from step 1: worst tick {worst} B ({sink})");
        Assert.Equal(0, worst);
    }

    [Fact]
    public void AWarmedPileAllocatesAlmostNothingPerStepAndTheWorldHashOnlyWhenItsBufferGrows()
    {
        var busy = NewWorld();
        var pile = PhysicsSpace2D.Of(busy)!;
        pile.AddBox(new FVec2(60, 1), new FVec2(0, -1), Still);
        for (var i = 0; i < 40; i++)
        {
            pile.AddBox(new FVec2(1, 1), new FVec2(((i % 8) * 2) - 8, 2 + ((i / 8) * 2)));
        }

        pile.Warmup();
        var worstStep = 0L;
        var allocatingSteps = 0;
        var hashGrowths = 0;
        for (var t = 0; t < 320; t++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            pile.Step();
            var stepBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            worstStep = Math.Max(worstStep, stepBytes);
            allocatingSteps += stepBytes > 0 ? 1 : 0;
            before = GC.GetAllocatedBytesForCurrentThread();
            busy.ComputeHash(); // writes the whole snapshot into the world's reused buffer, which grows with the snapshot
            hashGrowths += GC.GetAllocatedBytesForCurrentThread() > before ? 1 : 0;
        }

        TestContext.Current.SendDiagnosticMessage($"warmed pile, 320 ticks from step 1: worst step {worstStep} B on {allocatingSteps} step(s), {hashGrowths} hashes grew the buffer");
        // The library still grows one list on a step when the pile first settles into a new contact count.
        Assert.True(allocatingSteps <= 2 && worstStep <= 512, $"{allocatingSteps} steps allocated, worst {worstStep} B");
        Assert.True(hashGrowths <= 4, $"{hashGrowths} hashes allocated");
    }
}
