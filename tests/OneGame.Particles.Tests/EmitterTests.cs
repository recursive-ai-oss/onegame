using System.Numerics;
using OneGame.Particles;

namespace OneGame.Particles.Tests;

/// <summary>The simulation alone: no renderer, no device.</summary>
public class EmitterTests
{
    private static ParticleSettings Settings() => new()
    {
        Capacity = 512,
        Rate = 100,
        Lifetime = 1f,
        Speed = new FloatRange(10, 20),
        Spread = 0.4f,
    };

    private static ParticleSample[] Run(uint seed, int steps, float dt)
    {
        var e = new ParticleEmitter(Settings(), seed);
        for (var i = 0; i < steps; i++)
        {
            e.Position = new Vector3(i, 0, 0);
            e.Step(dt);
        }

        var all = new ParticleSample[e.Count];
        for (var i = 0; i < all.Length; i++)
        {
            all[i] = e.Sample(i);
        }

        return all;
    }

    [Fact]
    public void TheSameSeedAndStepsGiveTheSameParticlesAndAnotherSeedDoesNot()
    {
        var a = Run(5, 90, 1f / 60f);
        var b = Run(5, 90, 1f / 60f);
        Assert.NotEmpty(a);
        Assert.Equal(a.Length, b.Length);
        for (var i = 0; i < a.Length; i++)
        {
            Assert.Equal(a[i].Position, b[i].Position);
            Assert.Equal(a[i].Rotation, b[i].Rotation);
        }

        var c = Run(6, 90, 1f / 60f);
        Assert.NotEqual(a[0].Position, c[0].Position);
    }

    [Fact]
    public void TheRateGivesThatManyBirthsPerSecondAtAnyFrameRate()
    {
        foreach (var hz in new[] { 20, 60, 144 })
        {
            var e = new ParticleEmitter(Settings(), 1);
            for (var i = 0; i < hz * 2; i++)
            {
                e.Step(1f / hz);
            }

            Assert.InRange(e.Spawned, 199, 200);
            // Lifetime 1 s at 100 per second: about a hundred alive.
            Assert.InRange(e.Count, 98, 101);
        }
    }

    [Fact]
    public void ParticlesDieAtTheEndOfTheirLifeAndAOneShotFinishes()
    {
        var s = Settings();
        s.Rate = 50;
        s.Loop = false;
        s.Duration = 0.5f;
        var e = new ParticleEmitter(s, 1);
        for (var i = 0; i < 30; i++)
        {
            e.Step(1f / 30f);
        }

        Assert.False(e.Emitting);
        Assert.InRange(e.Spawned, 24, 26);
        Assert.True(e.Alive);
        for (var i = 0; i < 40; i++)
        {
            e.Step(1f / 30f);
        }

        Assert.Equal(0, e.Count);
        Assert.False(e.Alive);
        Assert.InRange(e.Spawned, 24, 26);
    }

    [Fact]
    public void GravityAndDragFollowTheirFormulas()
    {
        var s = new ParticleSettings { Rate = 0, Lifetime = 10f, Speed = 0f, Spread = 0f, Gravity = new Vector3(0, 10, 0) };
        var e = new ParticleEmitter(s, 1);
        e.Burst(1);
        for (var i = 0; i < 60; i++)
        {
            e.Step(1f / 60f);
        }

        // Semi-implicit Euler: y = g dt^2 n(n+1)/2 = 5.083 against the exact 5 for one second.
        Assert.InRange(e.Sample(0).Position.Y, 4.9f, 5.2f);

        var d = new ParticleSettings { Rate = 0, Lifetime = 10f, Speed = 100f, Spread = 0f, Direction = Vector3.UnitX, Drag = 2f };
        var f = new ParticleEmitter(d, 1);
        f.Burst(1);
        for (var i = 0; i < 60; i++)
        {
            f.Step(1f / 60f);
        }

        // Speed 100 decaying at e^-2t travels 100 (1 - e^-2) / 2 = 43.2 in a second.
        Assert.InRange(f.Sample(0).Position.X, 42.5f, 44f);
    }

    [Fact]
    public void ABurstIsBornAtTheEmittersPlaceAndAMovingEmitterLeavesAStraightTrail()
    {
        var e = new ParticleEmitter(new ParticleSettings { Rate = 120, Lifetime = 5f, Speed = 0f, Spread = 0f }, 1);
        e.Teleport(new Vector3(0, 0, 0));
        for (var i = 0; i < 60; i++)
        {
            e.Position = new Vector3(i * 2, 0, 0);
            e.Step(1f / 60f);
        }

        // Particles stay where they were born, so they are spread evenly along the path, oldest first.
        var prev = float.MinValue;
        var gaps = new List<float>();
        for (var i = 0; i < e.Count; i++)
        {
            var p = e.Sample(i).Position;
            Assert.Equal(0f, p.Y, 3);
            Assert.True(p.X >= prev);
            if (i > 2)
            {
                gaps.Add(p.X - prev);   // the first step had no earlier position, so its births share one place
            }

            prev = p.X;
        }

        Assert.True(gaps.Max() - gaps.Min() < 0.05f, $"gaps {gaps.Min()}..{gaps.Max()}");

        var b = new ParticleEmitter(new ParticleSettings { Rate = 0, Lifetime = 5f }, 1) { Position = new Vector3(7, 8, 0) };
        b.Burst(5);
        Assert.Equal(5, b.Count);
        Assert.Equal(new Vector3(7, 8, 0), b.Sample(3).Position);
    }

    [Fact]
    public void LocalSpaceParticlesFollowTheEmitter()
    {
        var e = new ParticleEmitter(new ParticleSettings { Rate = 0, Lifetime = 5f, Speed = 0f, LocalSpace = true }, 1) { Position = new Vector3(1, 1, 0) };
        e.Burst(1);
        e.Position = new Vector3(10, 5, 0);
        Assert.Equal(new Vector3(10, 5, 0), e.Sample(0).Position);
    }

    [Fact]
    public void AFullEmitterDropsBirthsAndCountsThem()
    {
        var e = new ParticleEmitter(new ParticleSettings { Capacity = 10, Rate = 0, Lifetime = 5f }, 1);
        e.Burst(25);
        Assert.Equal(10, e.Count);
        Assert.Equal(15, e.Dropped);
        Assert.Equal(25, e.Spawned);
    }

    [Fact]
    public void CurvesGradientsAndSheetFramesFollowTheLife()
    {
        var curve = ParticleCurve.Of((0f, 1f), (0.5f, 3f), (1f, 0f));
        Assert.Equal(1f, curve.Evaluate(-1f));
        Assert.Equal(2f, curve.Evaluate(0.25f));
        Assert.Equal(3f, curve.Evaluate(0.5f));
        Assert.Equal(1.5f, curve.Evaluate(0.75f));
        Assert.Equal(0f, curve.Evaluate(2f));
        Assert.Throws<ArgumentException>(() => ParticleCurve.Of((0.5f, 1f), (0.5f, 2f)));
        Assert.Throws<ArgumentException>(() => ParticleCurve.Of());

        var g = ParticleGradient.Between(new Vector4(1, 0, 0, 1), new Vector4(0, 0, 1, 0));
        Assert.Equal(new Vector4(0.5f, 0, 0.5f, 0.5f), g.Evaluate(0.5f));
        Assert.Equal(new Vector4(0, 1, 0, 1), ParticleGradient.Rgb(0x00ff00));

        var s = new ParticleSettings { Rate = 0, Lifetime = 1f, Speed = 0f, SheetColumns = 2, SheetRows = 2, FrameCount = 3, AnimateFrames = true };
        var e = new ParticleEmitter(s, 1);
        e.Burst(1);
        Assert.Equal(0, e.Sample(0).Frame);
        e.Step(0.5f);
        var mid = e.Sample(0);
        Assert.Equal(1, mid.Frame);
        Assert.Equal(new Vector4(0.5f, 0f, 0.5f, 0.5f), mid.Uv);
        e.Step(0.4f);
        var late = e.Sample(0);
        Assert.Equal(2, late.Frame);
        Assert.Equal(new Vector4(0f, 0.5f, 0.5f, 0.5f), late.Uv);
    }

    [Fact]
    public void PlaneEmittersStayInThePlaneAndVolumeEmittersFillACone()
    {
        var plane = new ParticleEmitter(new ParticleSettings { Rate = 0, Lifetime = 5f, Speed = 10f, Spread = 3f, SpawnRadius = 2f }, 3);
        plane.Burst(200);
        for (var i = 0; i < plane.Count; i++)
        {
            Assert.Equal(0f, plane.Sample(i).Position.Z);
        }

        var cone = new ParticleEmitter(new ParticleSettings { Space = ParticleSpace.Volume, Rate = 0, Lifetime = 5f, Speed = 10f, Spread = 0.3f, Direction = Vector3.UnitZ }, 3);
        cone.Burst(200);
        cone.Step(0.1f);
        var spreadY = 0f;
        for (var i = 0; i < cone.Count; i++)
        {
            var p = Vector3.Normalize(cone.Sample(i).Position);
            Assert.True(p.Z > MathF.Cos(0.3f) - 0.01f, "outside the cone");
            spreadY = Math.Max(spreadY, MathF.Abs(p.Y));
        }

        Assert.True(spreadY > 0.1f, "the cone is a beam");
    }

    [Fact]
    public void PrewarmFillsAnEmitterBeforeItsFirstStep()
    {
        var s = Settings();
        s.Prewarm = 2f;
        var e = new ParticleEmitter(s, 1);
        e.Step(0f);
        Assert.InRange(e.Count, 95, 101);
    }

    [Fact]
    public void BigGapsAreSplitIntoSmallStepsAndCut()
    {
        var e = new ParticleEmitter(Settings(), 1);
        e.Step(60f);   // cut to one second
        Assert.InRange(e.Spawned, 99, 101);
    }

    [Fact]
    public void SteppingAndSamplingAllocateNothing()
    {
        // 5000 particles alive: 5000 per second with a one second life.
        var s = new ParticleSettings { Capacity = 6000, Rate = 5000, Lifetime = 1f, Speed = new FloatRange(10, 50), Spread = 1f, Gravity = new Vector3(0, 9, 0), Drag = 1f,
            SizeOverLife = ParticleCurve.Linear(1, 0), Color = ParticleGradient.FadeOut(Vector4.One), Rotation = new FloatRange(0, 6), AngularVelocity = new FloatRange(-1, 1) };
        var e = new ParticleEmitter(s, 1);
        for (var i = 0; i < 90; i++)
        {
            e.Step(1f / 60f);
        }

        Assert.True(e.Count > 4500);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var sum = 0f;
        for (var i = 0; i < 120; i++)
        {
            e.Position = new Vector3(i, 0, 0);
            e.Step(1f / 60f);
            for (var k = 0; k < e.Count; k++)
            {
                sum += e.Sample(k).Size;
            }
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(sum > 0);
        Assert.Equal(0, allocated);
    }

    [Fact]
    public void FiveThousandParticlesStepAndSampleWellInsideAFrame()
    {
        var s = new ParticleSettings { Capacity = 6000, Rate = 5000, Lifetime = 1f, Speed = new FloatRange(10, 50), Spread = 1f, Gravity = new Vector3(0, 9, 0), Drag = 1f,
            SizeOverLife = ParticleCurve.Linear(1, 0), Color = ParticleGradient.FadeOut(Vector4.One) };
        var e = new ParticleEmitter(s, 1);
        for (var i = 0; i < 90; i++)
        {
            e.Step(1f / 60f);
        }

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var sum = 0f;
        const int frames = 300;
        for (var i = 0; i < frames; i++)
        {
            e.Step(1f / 60f);
            for (var k = 0; k < e.Count; k++)
            {
                sum += e.Sample(k).Color.W;
            }
        }

        var ms = watch.Elapsed.TotalMilliseconds / frames;
        TestContext.Current.SendDiagnosticMessage($"{e.Count} live particles: step and sample {ms:F3} ms per frame");
        Assert.True(sum > 0);
        Assert.True(ms < 3, $"{ms} ms per frame");
    }
}
