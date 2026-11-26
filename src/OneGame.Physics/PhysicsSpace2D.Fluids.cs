using System.Numerics;
using RecursiveAI.OneMath;
using RecursiveAI.OnePhysics;
using RecursiveAI.OnePhysics.Fluids;
using RecursiveAI.OnePhysics.Physics2D;

namespace OneGame.Physics;

// Particle fluids of the space: the library's position-based fluids behind game types. The fluids, their particles, emitters, drains and
// settings are inside the library's snapshot and hash, so saves, restores, replays and the state hash cover them with no extra bytes.
public sealed partial class PhysicsSpace2D
{
    // Reads and overlaps go through this buffer; it grows to the largest span a caller passes and is sized by Warmup.
    private FluidParticleInfo[] _fluidScratch = [];

    // The library gives out emitter and drain handles only when they are created, so the space keeps them. Keys are (fluid slot, index);
    // the serial in a reference tells a reused slot from the one a reference was made for. Looked up, never iterated.
    private readonly Dictionary<long, (int Serial, FluidEmitter Handle)> _emitters = [];
    private readonly Dictionary<long, (int Serial, FluidDrain Handle)> _drains = [];
    private int _serial;

    private static ParticleFluidDef LiquidDef(float spacing)
    {
        var def = ParticleFluidDef.Water(spacing);
        def.Viscosity = 0.05f;
        def.TensileCorrection = 0.1f;
        def.Relaxation = 0.03f;
        return def;
    }

    /// <summary>The number of particle fluids added with <see cref="AddFluid"/>.</summary>
    public int FluidCount => _world.ParticleFluidCount;

    /// <summary>The number of particles of all fluids.</summary>
    public int FluidParticleCount => _world.FluidParticleCount;

    /// <summary>The wet and dry events of the last step. Read them in <c>OnTick</c>, before the system steps again.</summary>
    public FluidBodyEvents FluidEvents => new(_world);

    /// <summary>The settings shared by all fluids of the space (a copy; assign through <see cref="SetFluidSettings"/>).</summary>
    public FluidWorldSettings FluidSettings
    {
        get
        {
            var s = _world.FluidSettings;
            return new FluidWorldSettings
            {
                Substeps = s.Substeps,
                Iterations = s.Iterations,
                MaxParticles = s.MaxParticles,
                SleepSpeed = PhysicsLimits.FromSingle(s.SleepSpeed),
                TimeToSleep = PhysicsLimits.FromSingle(s.TimeToSleep),
                WakeBodySpeed = PhysicsLimits.FromSingle(s.WakeBodySpeed),
            };
        }
    }

    /// <summary>Sets the settings shared by all fluids. They are saved with the space, so every node must set the same values at setup.</summary>
    /// <param name="settings">The new settings.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
    [MutatesState]
    public void SetFluidSettings(FluidWorldSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _world.FluidSettings = new RecursiveAI.OnePhysics.FluidSettings
        {
            Substeps = settings.Substeps,
            Iterations = settings.Iterations,
            MaxParticles = settings.MaxParticles,
            SleepSpeed = PhysicsLimits.ToSingle(settings.SleepSpeed, "SleepSpeed"),
            TimeToSleep = PhysicsLimits.ToSingle(settings.TimeToSleep, "TimeToSleep"),
            WakeBodySpeed = PhysicsLimits.ToSingle(settings.WakeBodySpeed, "WakeBodySpeed"),
        };
    }

    // ---------------------------------------------------------------- making fluids

    /// <summary>Adds a particle fluid. It has no particles until you fill a region or add an emitter.</summary>
    /// <param name="options">The material and spacing; null is water at 0.1 spacing.</param>
    /// <returns>A reference to keep in a synced property.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range, or the spacing differs from the space's other fluids.</exception>
    /// <exception cref="PhysicsRangeException">A value is outside ±2^21.</exception>
    [MutatesState]
    public FluidRef AddFluid(FluidOptions? options = null)
    {
        var o = options ?? new FluidOptions();
        var spacing = PhysicsLimits.ToSingle(o.Spacing, "Spacing");
        var def = o.Preset switch
        {
            FluidPreset.Oil => ParticleFluidDef.Oil(spacing),
            FluidPreset.Honey => ParticleFluidDef.Honey(spacing),
            FluidPreset.Sand => ParticleFluidDef.Sand(spacing),
            FluidPreset.Gravel => ParticleFluidDef.Gravel(spacing),
            FluidPreset.Snow => ParticleFluidDef.Snow(spacing),
            FluidPreset.Mud => ParticleFluidDef.Mud(spacing),
            FluidPreset.Clay => ParticleFluidDef.Clay(spacing),
            FluidPreset.Dough => ParticleFluidDef.Dough(spacing),
            FluidPreset.Liquid => LiquidDef(spacing),
            _ => ParticleFluidDef.Water(spacing),
        };
        if (o.Viscosity is { } viscosity)
        {
            def.Viscosity = PhysicsLimits.ToSingle(viscosity, "Viscosity");
        }

        if (o.RestDensity is { } density)
        {
            def.RestDensity = PhysicsLimits.ToSingle(density, "RestDensity");
        }

        if (o.Friction is { } friction)
        {
            def.Friction = PhysicsLimits.ToSingle(friction, "Friction");
        }

        if (o.GravityScale is { } gravity)
        {
            def.GravityScale = PhysicsLimits.ToSingle(gravity, "GravityScale");
        }

        if (o.MaxSpeed is { } maxSpeed)
        {
            def.MaxSpeed = PhysicsLimits.ToSingle(maxSpeed, "MaxSpeed");
        }

        if (o.Layer is { } layer)
        {
            def.Layer = layer;
        }

        if (o.SurfaceTension is { } tension)
        {
            def.TensileCorrection = PhysicsLimits.ToSingle(tension, "SurfaceTension");
        }

        if (o.Softness is { } softness)
        {
            def.Relaxation = PhysicsLimits.ToSingle(softness, "Softness");
        }

        if (o.Vorticity is { } vorticity)
        {
            def.Vorticity = PhysicsLimits.ToSingle(vorticity, "Vorticity");
        }

        if (o.KernelRadiusScale is { } kernel)
        {
            def.KernelRadiusScale = PhysicsLimits.ToSingle(kernel, "KernelRadiusScale");
        }

        if (o.DynamicViscosity is { } dynamic)
        {
            def.DynamicViscosity = PhysicsLimits.ToSingle(dynamic, "DynamicViscosity");
        }

        return new FluidRef(_world.AddParticleFluid(def).Index + 1);
    }

    /// <summary>Whether the reference names a fluid of this space.</summary>
    /// <param name="fluid">The reference.</param>
    public bool Exists(FluidRef fluid) => !fluid.IsNull && fluid.Slot < _world.ParticleFluidCount && _world.GetParticleFluid(fluid.Slot).IsValid;

    /// <summary>The number of particles of one fluid.</summary>
    /// <param name="fluid">The fluid.</param>
    /// <exception cref="StaleFluidException">The fluid does not exist.</exception>
    public int ParticleCount(FluidRef fluid) => Resolve(fluid).ParticleCount;

    /// <summary>Fills a box with particles on the fluid's lattice (rows from the lower corner, half a spacing inside it).</summary>
    /// <param name="fluid">The fluid.</param>
    /// <param name="min">The lower left corner.</param>
    /// <param name="max">The upper right corner.</param>
    /// <param name="velocity">The particles' starting velocity.</param>
    /// <returns>How many particles were added.</returns>
    /// <exception cref="StaleFluidException">The fluid does not exist.</exception>
    /// <exception cref="FluidBudgetExceededException">The box holds more than <see cref="FluidWorldSettings.MaxParticles"/> allows.</exception>
    /// <exception cref="PhysicsRangeException">A value is outside ±2^21.</exception>
    [MutatesState]
    public int FillFluidBox(FluidRef fluid, FVec2 min, FVec2 max, FVec2 velocity = default)
    {
        var f = Resolve(fluid);
        try
        {
            return _world.FillFluidBox(f, Position2(min), Position2(max), ToVector(velocity, "velocity"));
        }
        catch (RecursiveAI.OnePhysics.FluidBudgetException e)
        {
            throw new FluidBudgetExceededException(e.Message);
        }
    }

    /// <summary>Fills a disc with lattice particles whose centres lie inside it.</summary>
    /// <param name="fluid">The fluid.</param>
    /// <param name="centre">The disc centre.</param>
    /// <param name="radius">The disc radius.</param>
    /// <param name="velocity">The particles' starting velocity.</param>
    /// <returns>How many particles were added.</returns>
    /// <exception cref="StaleFluidException">The fluid does not exist.</exception>
    /// <exception cref="FluidBudgetExceededException">The disc holds more than <see cref="FluidWorldSettings.MaxParticles"/> allows.</exception>
    /// <exception cref="PhysicsRangeException">A value is outside ±2^21.</exception>
    [MutatesState]
    public int FillFluidCircle(FluidRef fluid, FVec2 centre, Fixed radius, FVec2 velocity = default)
    {
        var f = Resolve(fluid);
        try
        {
            return _world.FillFluidSphere(f, Position2(centre), PhysicsLimits.ToDouble(radius, nameof(radius)), ToVector(velocity, "velocity"));
        }
        catch (RecursiveAI.OnePhysics.FluidBudgetException e)
        {
            throw new FluidBudgetExceededException(e.Message);
        }
    }

    /// <summary>Adds one particle.</summary>
    /// <param name="fluid">The fluid.</param>
    /// <param name="at">Where it starts.</param>
    /// <param name="velocity">Its starting velocity.</param>
    /// <exception cref="StaleFluidException">The fluid does not exist.</exception>
    /// <exception cref="FluidBudgetExceededException">The budget is used up.</exception>
    /// <exception cref="PhysicsRangeException">A value is outside ±2^21.</exception>
    [MutatesState]
    public void AddFluidParticle(FluidRef fluid, FVec2 at, FVec2 velocity = default)
    {
        var f = Resolve(fluid);
        try
        {
            _world.AddFluidParticle(f, Position2(at), ToVector(velocity, "velocity"));
        }
        catch (RecursiveAI.OnePhysics.FluidBudgetException e)
        {
            throw new FluidBudgetExceededException(e.Message);
        }
    }

    // ---------------------------------------------------------------- emitters and drains

    /// <summary>Adds an emitter: a nozzle that streams particles into the fluid every step.</summary>
    /// <param name="fluid">The fluid.</param>
    /// <param name="options">The nozzle.</param>
    /// <returns>A reference to keep in a synced property.</returns>
    /// <exception cref="StaleFluidException">The fluid does not exist.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
    [MutatesState]
    public FluidEmitterRef AddEmitter(FluidRef fluid, FluidEmitterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var f = Resolve(fluid);
        var def = new FluidEmitterDef();
        ApplyEmitter(def, options);
        var handle = f.AddEmitter(def);
        var serial = ++_serial;
        _emitters[Key(fluid, handle.Index)] = (serial, handle);
        return new FluidEmitterRef(fluid, handle.Index, serial);
    }

    /// <summary>Turns an emitter on or off.</summary>
    /// <param name="emitter">The emitter.</param>
    /// <param name="enabled">Whether it emits.</param>
    /// <exception cref="StaleFluidException">The emitter does not exist.</exception>
    [MutatesState]
    public void SetEnabled(FluidEmitterRef emitter, bool enabled)
    {
        var h = Resolve(emitter);
        var def = h.Definition.Clone();
        def.Enabled = enabled;
        h.Definition = def;
    }

    /// <summary>Removes an emitter. The particles it made stay.</summary>
    /// <param name="emitter">The emitter.</param>
    /// <exception cref="StaleFluidException">The emitter does not exist.</exception>
    [MutatesState]
    public void Remove(FluidEmitterRef emitter)
    {
        Resolve(emitter).Destroy();
        _emitters.Remove(Key(emitter.Fluid, emitter.Index));
    }

    /// <summary>Whether the reference names an emitter that exists.</summary>
    /// <param name="emitter">The reference.</param>
    public bool Exists(FluidEmitterRef emitter) =>
        !emitter.IsNull && _emitters.TryGetValue(Key(emitter.Fluid, emitter.Index), out var e) && e.Serial == emitter.Serial && e.Handle.IsValid;

    /// <summary>How many particles an emitter has made so far.</summary>
    /// <param name="emitter">The emitter.</param>
    /// <exception cref="StaleFluidException">The emitter does not exist.</exception>
    public int Emitted(FluidEmitterRef emitter) => Resolve(emitter).Emitted;

    /// <summary>Adds a drain: a volume that removes the fluid's particles at the end of every step.</summary>
    /// <param name="fluid">The fluid.</param>
    /// <param name="options">The volume.</param>
    /// <returns>A reference to keep in a synced property.</returns>
    /// <exception cref="StaleFluidException">The fluid does not exist.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A value is out of range.</exception>
    [MutatesState]
    public FluidDrainRef AddDrain(FluidRef fluid, FluidDrainOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var f = Resolve(fluid);
        var def = new FluidDrainDef
        {
            Position = new Vector3D(PhysicsLimits.ToDouble(options.Position.X, "drain position"), PhysicsLimits.ToDouble(options.Position.Y, "drain position"), 0),
            Kind = options.Shape == FluidVolumeShape.Circle ? FluidVolumeKind.Sphere : FluidVolumeKind.Box,
            Size = Size3(options.Size, "drain size"),
            Enabled = options.Enabled,
        };
        var handle = f.AddDrain(def);
        var serial = ++_serial;
        _drains[Key(fluid, handle.Index)] = (serial, handle);
        return new FluidDrainRef(fluid, handle.Index, serial);
    }

    /// <summary>Turns a drain on or off.</summary>
    /// <param name="drain">The drain.</param>
    /// <param name="enabled">Whether it removes particles.</param>
    /// <exception cref="StaleFluidException">The drain does not exist.</exception>
    [MutatesState]
    public void SetEnabled(FluidDrainRef drain, bool enabled)
    {
        var h = Resolve(drain);
        var def = h.Definition.Clone();
        def.Enabled = enabled;
        h.Definition = def;
    }

    /// <summary>Removes a drain.</summary>
    /// <param name="drain">The drain.</param>
    /// <exception cref="StaleFluidException">The drain does not exist.</exception>
    [MutatesState]
    public void Remove(FluidDrainRef drain)
    {
        Resolve(drain).Destroy();
        _drains.Remove(Key(drain.Fluid, drain.Index));
    }

    /// <summary>Whether the reference names a drain that exists.</summary>
    /// <param name="drain">The reference.</param>
    public bool Exists(FluidDrainRef drain) =>
        !drain.IsNull && _drains.TryGetValue(Key(drain.Fluid, drain.Index), out var d) && d.Serial == drain.Serial && d.Handle.IsValid;

    /// <summary>How many particles a drain has removed so far. Compare it from tick to tick to react to particles reaching the drain.</summary>
    /// <param name="drain">The drain.</param>
    /// <exception cref="StaleFluidException">The drain does not exist.</exception>
    public int Drained(FluidDrainRef drain) => Resolve(drain).Removed;

    // ---------------------------------------------------------------- reading and queries

    /// <summary>Copies the particles into a buffer, in the library's storage order (which changes from step to step; use <see cref="FluidParticle.Id"/> to follow one). Allocates nothing once the buffer's size has been seen once.</summary>
    /// <param name="into">Receives the particles; when it is shorter than <see cref="FluidParticleCount"/> the rest are left out.</param>
    /// <param name="fluid">Only this fluid's particles; the default value means all fluids.</param>
    /// <returns>How many particles were written.</returns>
    /// <exception cref="StaleFluidException"><paramref name="fluid"/> is not null and does not exist.</exception>
    public int GetFluidParticles(Span<FluidParticle> into, FluidRef fluid = default)
    {
        if (!fluid.IsNull)
        {
            Resolve(fluid);
        }

        if (into.IsEmpty)
        {
            return 0;
        }

        // A single fluid's particles can sit anywhere in storage, so read everything when filtering.
        var want = fluid.IsNull ? into.Length : Math.Max(into.Length, _world.FluidParticleCount);
        var scratch = Scratch(want);
        var read = _world.GetFluidParticles(scratch.AsSpan(0, want), 0);
        var written = 0;
        for (var i = 0; i < read && written < into.Length; i++)
        {
            ref readonly var p = ref scratch[i];
            if (fluid.IsNull || p.Fluid == fluid.Slot)
            {
                into[written++] = Convert(in p);
            }
        }

        return written;
    }

    /// <summary>
    /// Copies just the particle positions, as floats in sim units (y up), for drawing: the cheapest read, and the one <c>FluidSurface2D</c> takes.
    /// For views only; sim code reads <see cref="GetFluidParticles"/>. Allocates nothing once the buffer's size has been seen once.
    /// </summary>
    /// <param name="into">Receives the positions; when it is shorter than the particle count the rest are left out.</param>
    /// <param name="fluid">Only this fluid's particles; the default value means all fluids.</param>
    /// <returns>How many positions were written.</returns>
    /// <exception cref="StaleFluidException"><paramref name="fluid"/> is not null and does not exist.</exception>
    public int GetFluidPositions(Span<Vector2> into, FluidRef fluid = default)
    {
        if (!fluid.IsNull)
        {
            Resolve(fluid);
        }

        if (into.IsEmpty)
        {
            return 0;
        }

        var want = fluid.IsNull ? into.Length : Math.Max(into.Length, _world.FluidParticleCount);
        var scratch = Scratch(want);
        var read = _world.GetFluidParticles(scratch.AsSpan(0, want), 0);
        var written = 0;
        for (var i = 0; i < read && written < into.Length; i++)
        {
            ref readonly var p = ref scratch[i];
            if (fluid.IsNull || p.Fluid == fluid.Slot)
            {
                into[written++] = new Vector2((float)p.Position.X, (float)p.Position.Y);
            }
        }

        return written;
    }

    /// <summary>Finds the particles whose centres lie within a distance of a point: "is liquid touching this cup".</summary>
    /// <param name="centre">The point.</param>
    /// <param name="radius">The distance.</param>
    /// <param name="into">Receives the particles found, in storage order (may be empty to count only).</param>
    /// <param name="fluid">Only this fluid; the default value means all fluids.</param>
    /// <returns>How many particles are within the distance, even when <paramref name="into"/> holds fewer.</returns>
    /// <exception cref="StaleFluidException"><paramref name="fluid"/> is not null and does not exist.</exception>
    /// <exception cref="PhysicsRangeException">A value is outside ±2^21.</exception>
    public int OverlapFluidCircle(FVec2 centre, Fixed radius, Span<FluidParticle> into, FluidRef fluid = default)
    {
        if (!fluid.IsNull)
        {
            Resolve(fluid);
        }

        var scratch = Scratch(into.Length);
        _world.OverlapFluidSphere(Position2(centre), PhysicsLimits.ToSingle(radius, nameof(radius)), scratch.AsSpan(0, into.Length), out var total, fluid.IsNull ? -1 : fluid.Slot);
        var n = Math.Min(total, into.Length);
        for (var i = 0; i < n; i++)
        {
            into[i] = Convert(in scratch[i]);
        }

        return total;
    }

    /// <summary>Finds the particles inside a box (edges included).</summary>
    /// <param name="min">The lower left corner.</param>
    /// <param name="max">The upper right corner.</param>
    /// <param name="into">Receives the particles found, in storage order (may be empty to count only).</param>
    /// <param name="fluid">Only this fluid; the default value means all fluids.</param>
    /// <returns>How many particles are inside, even when <paramref name="into"/> holds fewer.</returns>
    /// <exception cref="StaleFluidException"><paramref name="fluid"/> is not null and does not exist.</exception>
    /// <exception cref="PhysicsRangeException">A value is outside ±2^21.</exception>
    public int OverlapFluidBox(FVec2 min, FVec2 max, Span<FluidParticle> into, FluidRef fluid = default)
    {
        if (!fluid.IsNull)
        {
            Resolve(fluid);
        }

        var scratch = Scratch(into.Length);
        _world.OverlapFluidBox(Position2(min), Position2(max), scratch.AsSpan(0, into.Length), out var total, fluid.IsNull ? -1 : fluid.Slot);
        var n = Math.Min(total, into.Length);
        for (var i = 0; i < n; i++)
        {
            into[i] = Convert(in scratch[i]);
        }

        return total;
    }

    /// <summary>The fluid's density at a point in kg per cubic metre: about its rest density inside it and half that on a flat free surface.</summary>
    /// <param name="at">The point.</param>
    /// <param name="fluid">Which fluid; the default value adds up all fluids.</param>
    /// <exception cref="StaleFluidException"><paramref name="fluid"/> is not null and does not exist.</exception>
    public Fixed FluidDensityAt(FVec2 at, FluidRef fluid = default)
    {
        if (!fluid.IsNull)
        {
            Resolve(fluid);
        }

        return PhysicsLimits.FromSingle(_world.FluidDensityAt(Position2(at), fluid.IsNull ? -1 : fluid.Slot));
    }

    /// <summary>How much of a body a fluid covers after the last step, and the force it puts on it. Zero for a body the fluid has not met.</summary>
    /// <param name="body">The body.</param>
    /// <param name="fluid">The fluid.</param>
    /// <exception cref="StaleBodyException">The body does not exist.</exception>
    /// <exception cref="StaleFluidException">The fluid does not exist.</exception>
    public BodyFluidState FluidState(BodyRef body, FluidRef fluid)
    {
        var f = Resolve(fluid);
        var s = _world.GetFluidState(Resolve(body), f);
        return new BodyFluidState(PhysicsLimits.FromSingle(s.SubmergedFraction), PhysicsLimits.FromSingle(s.WettedArea), new FVec2(PhysicsLimits.FromSingle(s.Force.X), PhysicsLimits.FromSingle(s.Force.Y)));
    }

    // ---------------------------------------------------------------- helpers

    private void WarmFluids()
    {
        var n = _world.FluidParticleCount;
        if (n > 0)
        {
            _ = Scratch(n);
        }
    }

    private FluidParticleInfo[] Scratch(int length)
    {
        if (_fluidScratch.Length < length)
        {
            _fluidScratch = new FluidParticleInfo[Math.Max(length, _fluidScratch.Length * 2)];
        }

        return _fluidScratch;
    }

    private static FluidParticle Convert(in FluidParticleInfo p) =>
        new(p.Id, new FluidRef(p.Fluid + 1), new FVec2(PhysicsLimits.FromDouble(p.Position.X), PhysicsLimits.FromDouble(p.Position.Y)), new FVec2(PhysicsLimits.FromSingle(p.Velocity.X), PhysicsLimits.FromSingle(p.Velocity.Y)));

    private static long Key(FluidRef fluid, int index) => ((long)fluid.Id << 32) | (uint)index;

    private static Vector3F Size3(FVec2 size, string name)
    {
        var x = PhysicsLimits.ToSingle(size.X, name);
        var y = PhysicsLimits.ToSingle(size.Y, name);
        return new Vector3F(x, y, x);
    }

    private static void ApplyEmitter(FluidEmitterDef def, FluidEmitterOptions o)
    {
        def.Position = new Vector3D(PhysicsLimits.ToDouble(o.Position.X, "emitter position"), PhysicsLimits.ToDouble(o.Position.Y, "emitter position"), 0);
        def.Direction = new Vector3F(PhysicsLimits.ToSingle(o.Direction.X, "emitter direction"), PhysicsLimits.ToSingle(o.Direction.Y, "emitter direction"), 0f);
        def.Kind = o.Shape == FluidVolumeShape.Circle ? FluidVolumeKind.Sphere : FluidVolumeKind.Box;
        def.Size = Size3(o.Size, "emitter size");
        def.Speed = PhysicsLimits.ToSingle(o.Speed, "emitter speed");
        def.MaxEmitted = o.MaxEmitted;
        def.Enabled = o.Enabled;
    }

    private ParticleFluid Resolve(FluidRef fluid)
    {
        if (fluid.IsNull || fluid.Slot >= _world.ParticleFluidCount)
        {
            throw new StaleFluidException($"{fluid} does not exist in this space (never added here, or the space was restored from a save without it).");
        }

        var f = _world.GetParticleFluid(fluid.Slot);
        if (!f.IsValid)
        {
            throw new StaleFluidException($"{fluid} does not exist in this space.");
        }

        return f;
    }

    private FluidEmitter Resolve(FluidEmitterRef emitter)
    {
        if (!Exists(emitter))
        {
            throw new StaleFluidException($"{emitter} does not exist in this space (removed, never created here, or the space was loaded from a save: emitters can only be reached through the space that created them).");
        }

        return _emitters[Key(emitter.Fluid, emitter.Index)].Handle;
    }

    private FluidDrain Resolve(FluidDrainRef drain)
    {
        if (!Exists(drain))
        {
            throw new StaleFluidException($"{drain} does not exist in this space (removed, never created here, or the space was loaded from a save: drains can only be reached through the space that created them).");
        }

        return _drains[Key(drain.Fluid, drain.Index)].Handle;
    }
}
