namespace OneGame.Particles;

/// <summary>One live particle as a renderer needs it, from <see cref="ParticleEmitter.Sample"/>.</summary>
public readonly struct ParticleSample
{
    /// <summary>Where the particle is, in world units.</summary>
    public Vector3 Position { get; init; }

    /// <summary>Width and height in world units, after the size curve.</summary>
    public float Size { get; init; }

    /// <summary>Rotation in radians.</summary>
    public float Rotation { get; init; }

    /// <summary>sRGB colour with straight alpha from the gradient (not including <see cref="ParticleSettings.Emission"/>).</summary>
    public Vector4 Color { get; init; }

    /// <summary>The sheet frame shown, counting row by row.</summary>
    public int Frame { get; init; }

    /// <summary>The frame's rectangle in the texture as (u, v, width, height), 0 to 1, origin top-left.</summary>
    public Vector4 Uv { get; init; }

    /// <summary>How far through its life the particle is, 0 to 1.</summary>
    public float Life { get; init; }
}

/// <summary>
/// A source of particles and the particles it made. Create one when a view sets up, move <see cref="Position"/> to where it should be each frame,
/// and hand it to a renderer glue (<c>Draw2D.Particles</c>, <c>SceneWriter.Particles</c>), which steps and draws it. It is view-only: it reads
/// no simulation state and uses its own random stream, so the same seed and the same sequence of step sizes give the same
/// particles. All storage is allocated here; stepping and sampling allocate nothing.
/// </summary>
/// <example>
/// <code>
/// // In a [View] method, once: create the emitter and keep it in a view-side field.
/// _sparks ??= new ParticleEmitter(new ParticleSettings { Rate = 60, Lifetime = 0.8f, Speed = new FloatRange(30, 90), Spread = MathF.PI, Size = 4f }, seed: 7, name: "sparks");
///
/// // Each frame: draw it at a world position; this also steps it by the frame's time.
/// d.Particles(_sparks, new Vector2(X.ToSingle(), Y.ToSingle()));
/// </code>
/// </example>
public sealed class ParticleEmitter
{
    // A long frame (a hitch, a breakpoint) must not fling particles across the world or spawn thousands at once, and a frame drawn after a
    // long gap (a capture many ticks after the last one) still gets a plausible stream: time is applied in sub-steps no longer than this.
    private const float MaxStep = 1f;
    private const float SubStep = 1f / 30f;

    private readonly float[] _px, _py, _pz, _vx, _vy, _vz, _age, _life, _size, _rot, _spin;
    private readonly int[] _frame;
    private ParticleRandom _rng;
    private int _count;
    private float _acc;
    private float _elapsed;
    private bool _emitting = true;
    private bool _stepped;
    private Vector3 _previous;

    /// <summary>Makes an emitter.</summary>
    /// <param name="settings">How it behaves. Its <see cref="ParticleSettings.Capacity"/> is fixed now.</param>
    /// <param name="seed">Seed of the emitter's random stream; fixed seeds make captures repeatable.</param>
    /// <param name="name">A name for the harness (<c>particles</c> operation and scenario expectation).</param>
    public ParticleEmitter(ParticleSettings settings, uint seed = 1, string name = "")
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentOutOfRangeException.ThrowIfLessThan(settings.Capacity, 1);
        Settings = settings;
        Name = name ?? "";
        _rng = new ParticleRandom(seed);
        var n = settings.Capacity;
        _px = new float[n];
        _py = new float[n];
        _pz = new float[n];
        _vx = new float[n];
        _vy = new float[n];
        _vz = new float[n];
        _age = new float[n];
        _life = new float[n];
        _size = new float[n];
        _rot = new float[n];
        _spin = new float[n];
        _frame = new int[n];
    }

    /// <summary>The settings, which can be changed while it runs.</summary>
    public ParticleSettings Settings { get; }

    /// <summary>The name shown to the harness.</summary>
    public string Name { get; set; }

    /// <summary>Where the emitter is. Set it every frame to follow a view; in world space the particles stay behind, so a moving emitter leaves a trail.</summary>
    public Vector3 Position { get; set; }

    /// <summary>How many particles are alive.</summary>
    public int Count => _count;

    /// <summary>The most particles that can be alive.</summary>
    public int Capacity => _px.Length;

    /// <summary>True while the emitter is still producing particles from its rate.</summary>
    public bool Emitting => _emitting;

    /// <summary>True while it is emitting or any particle is alive; a one-shot effect is finished when this turns false.</summary>
    public bool Alive => _emitting || _count > 0;

    /// <summary>How many particles have been born in total.</summary>
    public long Spawned { get; private set; }

    /// <summary>How many births were dropped because the emitter was full. A non-zero value means the capacity is too small for the effect.</summary>
    public long Dropped { get; private set; }

    /// <summary>Seconds of stepping since the last <see cref="Play"/>.</summary>
    public float Elapsed => _elapsed;

    /// <summary>Starts (or restarts) emitting from the rate. Live particles stay.</summary>
    public void Play()
    {
        _emitting = true;
        _elapsed = 0;
        _acc = 0;
    }

    /// <summary>Stops emitting; particles already alive live out their lives.</summary>
    public void Stop() => _emitting = false;

    /// <summary>Removes every live particle at once.</summary>
    public void Clear() => _count = 0;

    /// <summary>Restarts the random stream.</summary>
    /// <param name="seed">The new seed.</param>
    public void Reseed(uint seed) => _rng.Reseed(seed);

    /// <summary>Moves the emitter without a trail: the next step spawns from the new place only.</summary>
    /// <param name="position">The new position.</param>
    public void Teleport(Vector3 position)
    {
        Position = position;
        _previous = position;
    }

    /// <summary>Emits <paramref name="count"/> particles right now at the current position: an explosion, a hit spark, a pickup.</summary>
    /// <param name="count">How many to make. Births past the capacity are dropped and counted in <see cref="Dropped"/>.</param>
    public void Burst(int count)
    {
        var at = Settings.LocalSpace ? Vector3.Zero : Position;
        for (var i = 0; i < count; i++)
        {
            Spawn(at, 0f);
        }
    }

    /// <summary>Moves the emitter to a place and emits <paramref name="count"/> particles there right now.</summary>
    /// <param name="count">How many to make.</param>
    /// <param name="position">Where the burst happens; the emitter stays there.</param>
    public void Burst(int count, Vector3 position)
    {
        Position = position;
        Burst(count);
    }

    /// <summary>Moves time forward: ages and moves the particles, retires the old ones and makes new ones from the rate.</summary>
    /// <param name="seconds">Time since the last step. It is applied in sub-steps of at most 1/30 s and values above one second are cut to one; zero or less does nothing.</param>
    public void Step(float seconds)
    {
        if (!_stepped && Settings.Prewarm > 0f)
        {
            // First sight of the emitter: show it as if it had been running for a while, from where it stands.
            _previous = Position;
            _stepped = true;
            var warm = Math.Min(Settings.Prewarm, 30f);
            for (var t = 0f; t < warm; t += SubStep)
            {
                StepOnce(Math.Min(SubStep, warm - t), Position, Position);
            }
        }

        if (!(seconds > 0f))
        {
            return;
        }

        var total = Math.Min(seconds, MaxStep);
        var now = Position;
        var from = _stepped ? _previous : now;
        _stepped = true;
        _previous = now;
        var parts = (int)MathF.Ceiling(total / SubStep);
        for (var k = 0; k < parts; k++)
        {
            StepOnce(total / parts, Vector3.Lerp(from, now, k / (float)parts), Vector3.Lerp(from, now, (k + 1) / (float)parts));
        }
    }

    private void StepOnce(float dt, Vector3 from, Vector3 now)
    {
        var s = Settings;
        var g = s.Gravity;
        var decay = s.Drag > 0f ? MathF.Exp(-s.Drag * dt) : 1f;

        // Existing particles move, and the survivors are packed down in order, so older particles always draw first.
        var w = 0;
        for (var i = 0; i < _count; i++)
        {
            var age = _age[i] + dt;
            if (age >= _life[i])
            {
                continue;
            }

            var vx = (_vx[i] + (g.X * dt)) * decay;
            var vy = (_vy[i] + (g.Y * dt)) * decay;
            var vz = (_vz[i] + (g.Z * dt)) * decay;
            _px[w] = _px[i] + (vx * dt);
            _py[w] = _py[i] + (vy * dt);
            _pz[w] = _pz[i] + (vz * dt);
            _vx[w] = vx;
            _vy[w] = vy;
            _vz[w] = vz;
            _age[w] = age;
            _life[w] = _life[i];
            _size[w] = _size[i];
            _rot[w] = _rot[i] + (_spin[i] * dt);
            _spin[w] = _spin[i];
            _frame[w] = _frame[i];
            w++;
        }

        _count = w;

        if (!_emitting)
        {
            return;
        }

        var window = dt;
        if (!s.Loop)
        {
            var left = s.Duration - _elapsed;
            if (left <= 0f)
            {
                _emitting = false;
                return;
            }

            window = Math.Min(dt, left);
        }

        _elapsed += dt;
        if (s.Rate > 0f)
        {
            var before = _acc;
            _acc += s.Rate * window;
            var births = (int)_acc;
            _acc -= births;
            for (var j = 1; j <= births; j++)
            {
                // The j-th birth happened this far into the step; the particle has lived the rest of it and the emitter was part way along its move.
                var t = (j - before) / s.Rate;
                var at = s.LocalSpace ? Vector3.Zero : Vector3.Lerp(from, now, t / dt);
                Spawn(at, dt - t);
            }
        }

        if (!s.Loop && _elapsed >= s.Duration)
        {
            _emitting = false;
        }
    }

    /// <summary>One live particle as a renderer needs it.</summary>
    /// <param name="index">0 to <see cref="Count"/> - 1; lower indices are older.</param>
    /// <returns>The particle.</returns>
    public ParticleSample Sample(int index)
    {
        if ((uint)index >= (uint)_count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var s = Settings;
        var t = Math.Clamp(_age[index] / _life[index], 0f, 1f);
        var pos = new Vector3(_px[index], _py[index], _pz[index]);
        if (s.LocalSpace)
        {
            pos += Position;
        }

        var cols = Math.Max(s.SheetColumns, 1);
        var rows = Math.Max(s.SheetRows, 1);
        var frames = s.FrameCount > 0 ? Math.Min(s.FrameCount, cols * rows) : cols * rows;
        var frame = s.AnimateFrames ? Math.Min((int)(t * frames), frames - 1) : _frame[index] % frames;
        return new ParticleSample
        {
            Position = pos,
            Size = _size[index] * s.SizeOverLife.Evaluate(t),
            Rotation = _rot[index],
            Color = s.Color.Evaluate(t),
            Frame = frame,
            Uv = new Vector4((frame % cols) / (float)cols, (frame / cols) / (float)rows, 1f / cols, 1f / rows),
            Life = t,
        };
    }

    private void Spawn(Vector3 at, float age)
    {
        var s = Settings;
        Spawned++;
        // Every random draw happens whether or not the particle fits, so a full emitter does not shift the stream.
        var life = Math.Max(_rng.Next(s.Lifetime), 0.001f);
        var speed = _rng.Next(s.Speed);
        var offset = SpawnOffset(s);
        var dir = LaunchDirection(s);
        var size = _rng.Next(s.Size);
        var rot = _rng.Next(s.Rotation);
        var spin = _rng.Next(s.AngularVelocity);
        var cols = Math.Max(s.SheetColumns, 1);
        var rows = Math.Max(s.SheetRows, 1);
        var frames = s.FrameCount > 0 ? Math.Min(s.FrameCount, cols * rows) : cols * rows;
        var frame = (int)(_rng.NextSingle() * frames);

        if (age >= life)
        {
            return;
        }

        if (_count == _px.Length)
        {
            Dropped++;
            return;
        }

        var v = dir * speed;
        var g = s.Gravity;
        var p = at + offset + (v * age) + (g * (0.5f * age * age));
        v += g * age;
        var i = _count++;
        _px[i] = p.X;
        _py[i] = p.Y;
        _pz[i] = p.Z;
        _vx[i] = v.X;
        _vy[i] = v.Y;
        _vz[i] = v.Z;
        _age[i] = age;
        _life[i] = life;
        _size[i] = size;
        _rot[i] = rot + (spin * age);
        _spin[i] = spin;
        _frame[i] = frame;
    }

    private Vector3 SpawnOffset(ParticleSettings s)
    {
        if (s.SpawnRadius <= 0f)
        {
            return Vector3.Zero;
        }

        if (s.Space == ParticleSpace.Plane)
        {
            var a = _rng.NextSingle() * MathF.Tau;
            var r = s.SpawnRadius * MathF.Sqrt(_rng.NextSingle());
            return new Vector3(MathF.Cos(a) * r, MathF.Sin(a) * r, 0f);
        }

        var d = RandomOnSphere();
        return d * (s.SpawnRadius * MathF.Cbrt(_rng.NextSingle()));
    }

    private Vector3 RandomOnSphere()
    {
        var z = (_rng.NextSingle() * 2f) - 1f;
        var a = _rng.NextSingle() * MathF.Tau;
        var r = MathF.Sqrt(Math.Max(0f, 1f - (z * z)));
        return new Vector3(r * MathF.Cos(a), r * MathF.Sin(a), z);
    }

    private Vector3 LaunchDirection(ParticleSettings s)
    {
        var d = s.Direction;
        if (s.Space == ParticleSpace.Plane)
        {
            var angle = MathF.Atan2(d.Y, d.X) + (((_rng.NextSingle() * 2f) - 1f) * s.Spread);
            return new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0f);
        }

        d = d.LengthSquared() > 0f ? Vector3.Normalize(d) : Vector3.UnitY;
        var spread = Math.Clamp(s.Spread, 0f, MathF.PI);
        var cos = 1f - (_rng.NextSingle() * (1f - MathF.Cos(spread)));
        var sin = MathF.Sqrt(Math.Max(0f, 1f - (cos * cos)));
        var phi = _rng.NextSingle() * MathF.Tau;
        var helper = MathF.Abs(d.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX;
        var u = Vector3.Normalize(Vector3.Cross(d, helper));
        var v = Vector3.Cross(d, u);
        return (d * cos) + (((u * MathF.Cos(phi)) + (v * MathF.Sin(phi))) * sin);
    }
}
