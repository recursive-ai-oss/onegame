using OneGame.Core;
using OneGame.Physics;

namespace OneGame.Physics.Tests;

/// <summary>Particle fluids of a physics space: determinism, saves, drains, queries, events and allocation.</summary>
public class FluidTests
{
    private static readonly BodyOptions Still = new() { Kind = BodyKind.Static };

    private static World NewWorld()
    {
        var world = new World(9, new EntityRegistry());
        PhysicsSpace2D.AddTo(world);
        return world;
    }

    // A box-shaped tank: floor and two walls, a block of water in it, an emitter dripping into it from above and a drain low in the right wall.
    private static World Tank(out PhysicsSpace2D space, out FluidRef water, out FluidDrainRef drain, out FluidEmitterRef emitter)
    {
        var world = NewWorld();
        space = PhysicsSpace2D.Of(world)!;
        space.AddBox(new FVec2(8, 1), new FVec2(0, Fixed.FromRatio(-1, 2)), Still);
        space.AddBox(new FVec2(1, 6), new FVec2(Fixed.FromRatio(-9, 2), 2), Still);
        space.AddBox(new FVec2(1, 6), new FVec2(Fixed.FromRatio(9, 2), 2), Still);
        water = space.AddFluid();
        space.FillFluidBox(water, new FVec2(-2, 0), new FVec2(2, 1));
        emitter = space.AddEmitter(water, new FluidEmitterOptions
        {
            Position = new FVec2(-1, 4),
            Direction = new FVec2(0, -1),
            Size = new FVec2(Fixed.FromRatio(1, 5), Fixed.FromRatio(1, 5)),
            Speed = Fixed.FromInt(2),
            MaxEmitted = 300,
        });
        drain = space.AddDrain(water, new FluidDrainOptions { Position = new FVec2(Fixed.FromRatio(37, 10), Fixed.FromRatio(1, 5)), Size = new FVec2(Fixed.FromRatio(3, 10), Fixed.FromRatio(3, 10)) });
        return world;
    }

    [Fact]
    public void TwoRunsGiveTheSameHashEveryTickAndTheFluidReallyMoves()
    {
        (ulong[] Hashes, int Last) Run()
        {
            var world = Tank(out var space, out var water, out _, out _);
            var hashes = new List<ulong>();
            for (var t = 0; t < 240; t++)
            {
                world.Step();
                hashes.Add(world.ComputeHash());
            }

            return ([.. hashes], space.ParticleCount(water));
        }

        var a = Run();
        var b = Run();
        Assert.Equal(a.Hashes, b.Hashes);
        Assert.Equal(a.Last, b.Last);
        Assert.True(a.Hashes.Distinct().Count() > 100);
    }

    [Fact]
    public void ASnapshotMidFlowRestoresIntoAFreshWorldAndContinuesWithTheSameHashes()
    {
        var world = Tank(out var space, out var water, out var drain, out var emitter);
        for (var t = 0; t < 60; t++)
        {
            world.Step();
        }

        var w = new SyncWriter();
        world.WriteSnapshot(w);
        var other = NewWorld();
        Assert.Equal(SyncError.None, other.ReadSnapshot(w.ToArray()));
        var otherSpace = PhysicsSpace2D.Of(other)!;
        Assert.Equal(world.ComputeHash(), other.ComputeHash());
        Assert.Equal(space.ParticleCount(water), otherSpace.ParticleCount(water));

        // The library hands out emitter and drain handles only when they are made, so a space that was loaded cannot reach them. Everything
        // that matters to a game still works through the fluid.
        Assert.False(otherSpace.Exists(drain));
        Assert.False(otherSpace.Exists(emitter));
        Assert.Throws<StaleFluidException>(() => otherSpace.Drained(drain));
        Assert.True(otherSpace.Exists(water));
        Assert.Equal(space.FluidSettings.MaxParticles, otherSpace.FluidSettings.MaxParticles);

        for (var t = 0; t < 200; t++)
        {
            world.Step();
            other.Step();
            Assert.Equal(world.ComputeHash(), other.ComputeHash());
        }

        Assert.Equal(space.FluidParticleCount, otherSpace.FluidParticleCount);
    }

    [Fact]
    public void RestoringIntoTheSameWorldRewindsTheFluidAndKeepsItsEmitterAndDrain()
    {
        var world = Tank(out var space, out var water, out var drain, out var emitter);
        for (var t = 0; t < 30; t++)
        {
            world.Step();
        }

        var w = new SyncWriter();
        world.WriteSnapshot(w);
        var drainedThen = space.Drained(drain);
        var hashes = new List<ulong>();
        for (var t = 0; t < 120; t++)
        {
            world.Step();
            hashes.Add(world.ComputeHash());
        }

        Assert.Equal(SyncError.None, world.ReadSnapshot(w.Written));
        Assert.Equal(drainedThen, space.Drained(drain));
        for (var t = 0; t < 120; t++)
        {
            world.Step();
            Assert.Equal(hashes[t], world.ComputeHash());
        }

        Assert.True(space.Emitted(emitter) > 100);
        Assert.True(space.ParticleCount(water) > 0);
    }

    [Fact]
    public void ADrainRemovesParticlesAndItsCountIsWhatSimCodeReactsTo()
    {
        var world = Tank(out var space, out var water, out var drain, out var emitter);
        var before = space.ParticleCount(water);
        var sawDrop = false;
        var last = 0;
        for (var t = 0; t < 400; t++)
        {
            world.Step();
            var drained = space.Drained(drain);
            sawDrop |= drained > last;
            last = drained;
        }

        Assert.True(sawDrop, "no particle ever reached the drain");
        Assert.Equal(before + space.Emitted(emitter) - last, space.ParticleCount(water));
    }

    [Fact]
    public void OverlapFindsTheLiquidTouchingASpotAndCountsWithoutABuffer()
    {
        var world = Tank(out var space, out var water, out _, out _);
        var oil = space.AddFluid(new FluidOptions { Preset = FluidPreset.Oil });
        space.FillFluidBox(oil, new FVec2(2, 0), new FVec2(Fixed.FromRatio(7, 2), 1));
        for (var t = 0; t < 20; t++)
        {
            world.Step();
        }

        var inWater = space.OverlapFluidCircle(new FVec2(0, Fixed.FromRatio(1, 2)), Fixed.One, default, water);
        var inOil = space.OverlapFluidCircle(new FVec2(0, Fixed.FromRatio(1, 2)), Fixed.One, default, oil);
        var any = space.OverlapFluidCircle(new FVec2(0, Fixed.FromRatio(1, 2)), Fixed.One, default);
        var nowhere = space.OverlapFluidCircle(new FVec2(0, 50), Fixed.One, default);
        Assert.True(inWater > 20);
        Assert.Equal(0, inOil);
        Assert.Equal(inWater, any);
        Assert.Equal(0, nowhere);

        Span<FluidParticle> few = stackalloc FluidParticle[4];
        var total = space.OverlapFluidCircle(new FVec2(0, Fixed.FromRatio(1, 2)), Fixed.One, few, water);
        Assert.Equal(inWater, total);
        foreach (var p in few)
        {
            Assert.Equal(water, p.Fluid);
            Assert.True(p.Position.X >= Fixed.FromInt(-3) && p.Position.X <= Fixed.FromInt(3));
        }

        var box = space.OverlapFluidBox(new FVec2(-4, 0), new FVec2(4, 5), default, oil);
        Assert.True(box > 10);
    }

    [Fact]
    public void ReadingParticlesFiltersByFluidAndFillsAShortBuffer()
    {
        var world = Tank(out var space, out var water, out _, out _);
        var oil = space.AddFluid(new FluidOptions { Preset = FluidPreset.Oil });
        var nOil = space.FillFluidCircle(oil, new FVec2(3, 1), Fixed.FromRatio(1, 2));
        world.Step();
        Assert.True(nOil > 5);
        var all = new FluidParticle[space.FluidParticleCount];
        Assert.Equal(all.Length, space.GetFluidParticles(all));
        var onlyOil = new FluidParticle[all.Length];
        Assert.Equal(nOil, space.GetFluidParticles(onlyOil, oil));
        Assert.Equal(all.Count(p => p.Fluid == oil), nOil);
        Assert.Equal(10, space.GetFluidParticles(new FluidParticle[10]));
        Assert.Equal(all.Select(p => p.Id).Distinct().Count(), all.Length);
    }

    [Fact]
    public void ABodyDroppedIntoWaterRaisesAnEnteredEventInTheSameTickOnEveryRun()
    {
        List<(int Tick, FluidBodyEventKind Kind, double Submerged)> Run()
        {
            var world = Tank(out var space, out var water, out _, out _);
            var ball = space.AddCircle(Fixed.FromRatio(1, 2), new FVec2(1, 3));
            var events = new List<(int, FluidBodyEventKind, double)>();
            for (var t = 0; t < 200; t++)
            {
                world.Step();
                foreach (var e in space.FluidEvents)
                {
                    Assert.Equal(ball, e.Body);
                    Assert.Equal(water, e.Fluid);
                    events.Add((t, e.Kind, e.Submerged.ToDouble()));
                }
            }

            var state = space.FluidState(ball, water);
            Assert.True(state.Submerged > Fixed.Zero);
            return events;
        }

        var a = Run();
        Assert.Contains(a, e => e.Kind == FluidBodyEventKind.Entered);
        Assert.Equal(a, Run());
    }

    [Fact]
    public void ReferencesAreCheckedAndAnOldOneNeverNamesTheNextEmitterInItsSlot()
    {
        var world = Tank(out var space, out var water, out var drain, out var emitter);
        world.Step();
        Assert.True(space.Exists(water));
        Assert.True(space.Exists(emitter));
        Assert.True(space.Exists(drain));
        Assert.False(space.Exists(default(FluidRef)));
        Assert.False(space.Exists(default(FluidEmitterRef)));
        Assert.Throws<StaleFluidException>(() => space.ParticleCount(default));
        Assert.Throws<StaleFluidException>(() => space.ParticleCount(new FluidRef(9)));

        space.Remove(emitter);
        Assert.False(space.Exists(emitter));
        Assert.Throws<StaleFluidException>(() => space.Emitted(emitter));
        var next = space.AddEmitter(water, new FluidEmitterOptions { Position = new FVec2(0, 4) });
        Assert.True(space.Exists(next));
        Assert.False(space.Exists(emitter));

        space.Remove(drain);
        Assert.Throws<StaleFluidException>(() => space.Drained(drain));
    }

    [Fact]
    public void ReferencesRoundTripThroughTheSyncCodec()
    {
        Tank(out _, out var water, out var drain, out var emitter);
        var w = new SyncWriter();
        water.Write(w);
        drain.Write(w);
        emitter.Write(w);
        var r = new SyncReader(w.ToArray());
        Assert.Equal(water, FluidRef.Read(ref r));
        Assert.Equal(drain, FluidDrainRef.Read(ref r));
        Assert.Equal(emitter, FluidEmitterRef.Read(ref r));
        Assert.True(r.Ok);
    }

    [Fact]
    public void FluidsWithDifferentSpacingAreRefusedAndSettingsAreSavedWithTheSpace()
    {
        var world = NewWorld();
        var space = PhysicsSpace2D.Of(world)!;
        space.AddFluid();
        Assert.Throws<ArgumentOutOfRangeException>(() => space.AddFluid(new FluidOptions { Spacing = Fixed.FromRatio(1, 5) }));

        space.SetFluidSettings(new FluidWorldSettings { Substeps = 3, Iterations = 5, MaxParticles = 777, SleepSpeed = Fixed.Zero });
        var w = new SyncWriter();
        world.WriteSnapshot(w);
        var other = NewWorld();
        Assert.Equal(SyncError.None, other.ReadSnapshot(w.ToArray()));
        var s = PhysicsSpace2D.Of(other)!.FluidSettings;
        Assert.Equal((3, 5, 777), (s.Substeps, s.Iterations, s.MaxParticles));
        Assert.Equal(Fixed.Zero, s.SleepSpeed);
        Assert.Throws<ArgumentOutOfRangeException>(() => space.SetFluidSettings(new FluidWorldSettings { Substeps = 0 }));
    }

    [Fact]
    public void FillingPastTheBudgetThrowsATypedException()
    {
        var world = NewWorld();
        var space = PhysicsSpace2D.Of(world)!;
        var f = space.AddFluid();
        space.SetFluidSettings(new FluidWorldSettings { MaxParticles = 50 });
        Assert.Throws<FluidBudgetExceededException>(() => space.FillFluidBox(f, new FVec2(0, 0), new FVec2(10, 10)));
    }

    [Fact]
    public void AFewThousandParticlesSaveWellInsideTheSnapshotLimit()
    {
        var world = NewWorld();
        var space = PhysicsSpace2D.Of(world)!;
        space.AddBox(new FVec2(40, 1), new FVec2(0, Fixed.FromRatio(-1, 2)), Still);
        var f = space.AddFluid();
        var n = space.FillFluidBox(f, new FVec2(-10, 0), new FVec2(10, 3));
        Assert.True(n > 5000, $"only {n} particles");
        for (var t = 0; t < 10; t++)
        {
            world.Step();
        }

        var w = new SyncWriter();
        world.WriteSnapshot(w);
        TestContext.Current.SendDiagnosticMessage($"{n} particles: snapshot {w.Written.Length} bytes ({w.Written.Length / (double)n:F1} B per particle)");
        Assert.True(w.Written.Length < 4 * 1024 * 1024);
        var other = NewWorld();
        Assert.Equal(SyncError.None, other.ReadSnapshot(w.ToArray()));
        Assert.Equal(world.ComputeHash(), other.ComputeHash());
    }

    [Fact]
    public void TheStateQueryShowsParticleCountsPerFluid()
    {
        var world = Tank(out var space, out _, out _, out _);
        var json = new JsonOut();
        space.WriteJson(json);
        var text = json.ToString();
        Assert.Contains("\"fluidParticles\":", text);
        Assert.Contains("\"slot\":0", text);
        Assert.Contains($"\"particles\":{space.FluidParticleCount}", text);
        var plain = NewWorld();
        var plainJson = new JsonOut();
        PhysicsSpace2D.Of(plain)!.WriteJson(plainJson);
        Assert.DoesNotContain("fluid", plainJson.ToString());
    }

    [Fact]
    public void DebugShapesShowParticlesEmittersAndDrains()
    {
        var world = Tank(out var space, out _, out _, out _);
        world.Step();
        var shapes = new PhysicsDebugShapes();
        space.AddDebugShapes(shapes);
        Assert.Contains(shapes.Lines, l => l.Rgba == PhysicsDebugShapes.FluidColor);
        Assert.Contains(shapes.Lines, l => l.Rgba == PhysicsDebugShapes.DrainColor);
        shapes.Clear();
        shapes.IncludeFluids = false;
        space.AddDebugShapes(shapes);
        Assert.DoesNotContain(shapes.Lines, l => l.Rgba == PhysicsDebugShapes.FluidColor);
    }

    [Fact]
    public void AWarmedSpaceWithFlowingFluidAllocatesNothingPerStepReadOrQuery()
    {
        var world = Tank(out var space, out var water, out var drain, out _);
        space.Warmup();
        var buffer = new FluidParticle[4096];
        var near = new FluidParticle[64];
        space.GetFluidParticles(buffer);
        space.OverlapFluidCircle(new FVec2(0, 1), Fixed.One, near, water);

        // The emitter grows the particle storage while it runs (an emitter adds particles, that is the point); once it has finished and the
        // drain keeps removing, steps, reads and the world hash are flat.
        for (var t = 0; t < 420; t++)
        {
            world.Step();
            world.ComputeHash();
        }

        var sink = 0;
        var worst = 0L;
        var worstSpaceHash = 0L;
        var worstWorldHash = 0L;
        for (var t = 0; t < 300; t++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            world.Step();
            sink += space.GetFluidParticles(buffer);
            sink += space.OverlapFluidCircle(new FVec2(0, 1), Fixed.One, near, water);
            sink += space.Drained(drain) + space.ParticleCount(water);
            sink += space.FluidEvents.Count;
            var beforeHash = GC.GetAllocatedBytesForCurrentThread();
            worst = Math.Max(worst, beforeHash - before);
            sink += (int)(world.ComputeHash() & 1);
            worstWorldHash = Math.Max(worstWorldHash, GC.GetAllocatedBytesForCurrentThread() - beforeHash);
            var h0 = GC.GetAllocatedBytesForCurrentThread();
            sink += (int)(space.Hash & 1);
            worstSpaceHash = Math.Max(worstSpaceHash, GC.GetAllocatedBytesForCurrentThread() - h0);
        }

        TestContext.Current.SendDiagnosticMessage($"fluid ticks: {space.FluidParticleCount} particles, worst tick {worst} B, world hash {worstWorldHash} B, standalone space.Hash {worstSpaceHash} B ({sink})");
        Assert.Equal(0, worst);

        // The library allocates one 112-byte object each time it snapshots or hashes while a particle fluid exists (one call per world hash here).
        Assert.True(worstWorldHash <= 112, $"world hash allocated {worstWorldHash} B");
    }
}
