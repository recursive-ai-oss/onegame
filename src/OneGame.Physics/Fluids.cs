using System.Globalization;

namespace OneGame.Physics;

/// <summary>The material a particle fluid starts from. Each is a tuned set of viscosity, density, friction and cohesion; <see cref="FluidOptions"/> overrides single values.</summary>
public enum FluidPreset : byte
{
    /// <summary>Thin, free-flowing liquid (1000 kg/m³).</summary>
    Water = 0,

    /// <summary>A lighter, slightly thicker liquid.</summary>
    Oil = 1,

    /// <summary>A very thick liquid that creeps.</summary>
    Honey = 2,

    /// <summary>Dry grains that pile up at a repose angle.</summary>
    Sand = 3,

    /// <summary>Coarse grains.</summary>
    Gravel = 4,

    /// <summary>Loose packed snow.</summary>
    Snow = 5,

    /// <summary>Wet, sticky grains.</summary>
    Mud = 6,

    /// <summary>A plastic paste that holds a shape.</summary>
    Clay = 7,

    /// <summary>An elastic paste.</summary>
    Dough = 8,

    /// <summary>Water tuned to pour in a thin stream and pool flat at small spacing: a stiffer density constraint, some viscosity and surface tension so it does not bounce or scatter like balls. The starting point for a few thousand fine particles.</summary>
    Liquid = 9,
}

/// <summary>The shape of an emitter's nozzle or a drain's volume.</summary>
public enum FluidVolumeShape : byte
{
    /// <summary>A rectangle: the size is the half extents.</summary>
    Box = 0,

    /// <summary>A disc: the size's X is the radius.</summary>
    Circle = 1,
}

/// <summary>
/// How to make a particle fluid with <see cref="PhysicsSpace2D.AddFluid"/>: a preset, the particle spacing, and optional overrides.
/// Every fluid in one space shares the same spacing.
/// </summary>
public sealed class FluidOptions
{
    /// <summary>The material to start from. Default water.</summary>
    public FluidPreset Preset { get; set; }

    /// <summary>The distance between neighbouring particles at rest, in world units (one particle is a disc of about this size). Smaller is finer and costs more particles. Default 0.1. All fluids in a space must use the same value.</summary>
    public Fixed Spacing { get; set; } = Fixed.FromRatio(1, 10);

    /// <summary>Overrides the preset's viscosity (how fast neighbouring particles even out their speeds); null keeps the preset's.</summary>
    public Fixed? Viscosity { get; set; }

    /// <summary>Overrides the rest density in kg per cubic metre (water is 1000); null keeps the preset's.</summary>
    public Fixed? RestDensity { get; set; }

    /// <summary>Overrides the friction against bodies; null keeps the preset's.</summary>
    public Fixed? Friction { get; set; }

    /// <summary>Overrides how strongly gravity pulls on the fluid (1 is normal); null keeps the preset's.</summary>
    public Fixed? GravityScale { get; set; }

    /// <summary>Overrides the particle speed limit in units per second; null keeps the preset's.</summary>
    public Fixed? MaxSpeed { get; set; }

    /// <summary>Overrides the collision layer (0 to 31) the fluid meets bodies on; null keeps the preset's.</summary>
    public int? Layer { get; set; }

    /// <summary>Overrides the pressure against clumping at the free surface, 0 to 10; it also gives a little surface tension that keeps a stream together. Null keeps the preset's.</summary>
    public Fixed? SurfaceTension { get; set; }

    /// <summary>Overrides how soft the density constraint is, 0.000001 to 100. Lower is stiffer (less squashing, more cost and noise); higher is softer and steadier. Null keeps the preset's.</summary>
    public Fixed? Softness { get; set; }

    /// <summary>Overrides the swirl put back after damping, 0 to 100 (0 is off); null keeps the preset's.</summary>
    public Fixed? Vorticity { get; set; }

    /// <summary>Overrides the smoothing radius as a multiple of <see cref="Spacing"/>, 1.2 to 4. Larger is smoother and slower; null keeps the preset's.</summary>
    public Fixed? KernelRadiusScale { get; set; }

    /// <summary>Overrides the shear viscosity in Pa·s, 0 to 100,000, applied implicitly so a jet coils and a column slumps (honey, lava); null keeps the preset's.</summary>
    public Fixed? DynamicViscosity { get; set; }
}

/// <summary>
/// The settings that apply to every particle fluid of a space (see <see cref="PhysicsSpace2D.FluidSettings"/>). They are saved with the
/// space. More substeps and iterations make the fluid stiffer and costlier.
/// </summary>
public sealed class FluidWorldSettings
{
    /// <summary>Fluid substeps per tick, 1 to 16. Default 2.</summary>
    public int Substeps { get; set; } = 2;

    /// <summary>Density-constraint iterations per substep, 1 to 64. Default 4.</summary>
    public int Iterations { get; set; } = 4;

    /// <summary>The most particles that may exist, 0 to 16,777,216. Emitters stop at it; filling past it throws <see cref="FluidBudgetExceededException"/>. Default 262,144.</summary>
    public int MaxParticles { get; set; } = 262144;

    /// <summary>A fluid whose fastest particle stays below this speed for <see cref="TimeToSleep"/> seconds sleeps until something touches it. 0 turns sleeping off. Default 0.02.</summary>
    public Fixed SleepSpeed { get; set; } = Fixed.FromRatio(2, 100);

    /// <summary>Seconds below <see cref="SleepSpeed"/> before the fluid sleeps. Default 1.</summary>
    public Fixed TimeToSleep { get; set; } = Fixed.One;

    /// <summary>A sleeping body that the fluid pushes wakes only when one step changes its speed by more than this. Default 0.05.</summary>
    public Fixed WakeBodySpeed { get; set; } = Fixed.FromRatio(5, 100);
}

/// <summary>An emitter: a nozzle that streams new particles into a fluid. See <see cref="PhysicsSpace2D.AddEmitter"/>.</summary>
public sealed class FluidEmitterOptions
{
    /// <summary>The nozzle centre.</summary>
    public FVec2 Position { get; set; }

    /// <summary>The direction of the flow (any length except zero). Default right.</summary>
    public FVec2 Direction { get; set; } = new(Fixed.One, Fixed.Zero);

    /// <summary>The nozzle shape.</summary>
    public FluidVolumeShape Shape { get; set; }

    /// <summary>Half extents across the flow for a box, the radius in X for a circle. Default 0.2 by 0.2.</summary>
    public FVec2 Size { get; set; } = new(Fixed.FromRatio(1, 5), Fixed.FromRatio(1, 5));

    /// <summary>The exit speed in units per second, 0 to 1000. Default 3.</summary>
    public Fixed Speed { get; set; } = Fixed.FromInt(3);

    /// <summary>The most particles this nozzle emits over its life; 0 is unlimited.</summary>
    public int MaxEmitted { get; set; }

    /// <summary>Whether the nozzle runs. Default true.</summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>A drain: a volume that removes the particles of its fluid at the end of every step. See <see cref="PhysicsSpace2D.AddDrain"/>.</summary>
public sealed class FluidDrainOptions
{
    /// <summary>The volume's centre.</summary>
    public FVec2 Position { get; set; }

    /// <summary>The volume shape.</summary>
    public FluidVolumeShape Shape { get; set; }

    /// <summary>Half extents for a box, the radius in X for a circle. Default 0.5 by 0.5.</summary>
    public FVec2 Size { get; set; } = new(Fixed.FromRatio(1, 2), Fixed.FromRatio(1, 2));

    /// <summary>Whether the drain removes particles. Default true.</summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>One particle of a fluid, in game types. Views copy these out each frame and draw them.</summary>
public readonly struct FluidParticle
{
    internal FluidParticle(int id, FluidRef fluid, FVec2 position, FVec2 velocity)
    {
        Id = id;
        Fluid = fluid;
        Position = position;
        Velocity = velocity;
    }

    /// <summary>The particle's id; it stays the same for the particle's life while the order of particles in a read changes from step to step.</summary>
    public int Id { get; }

    /// <summary>The fluid the particle belongs to.</summary>
    public FluidRef Fluid { get; }

    /// <summary>Where the particle is.</summary>
    public FVec2 Position { get; }

    /// <summary>How fast it moves.</summary>
    public FVec2 Velocity { get; }
}

/// <summary>What a fluid does to one body: how much of it is covered and the average force on it.</summary>
/// <param name="Submerged">The covered fraction of the body's area, 0 to 1.</param>
/// <param name="WettedArea">How much of the body's surface the particles touch.</param>
/// <param name="Force">The average force the particles put on the body.</param>
public readonly record struct BodyFluidState(Fixed Submerged, Fixed WettedArea, FVec2 Force);

/// <summary>What happened between a body and a fluid.</summary>
public enum FluidBodyEventKind : byte
{
    /// <summary>A dry body became wet.</summary>
    Entered = 0,

    /// <summary>A wet body became dry.</summary>
    Left = 1,
}

/// <summary>A body becoming wet or dry in a fluid, from <see cref="PhysicsSpace2D.FluidEvents"/>.</summary>
public readonly struct FluidBodyEvent
{
    internal FluidBodyEvent(FluidBodyEventKind kind, BodyRef body, FluidRef fluid, Fixed submerged)
    {
        Kind = kind;
        Body = body;
        Fluid = fluid;
        Submerged = submerged;
    }

    /// <summary>Whether the body became wet or dry.</summary>
    public FluidBodyEventKind Kind { get; }

    /// <summary>The body.</summary>
    public BodyRef Body { get; }

    /// <summary>The fluid.</summary>
    public FluidRef Fluid { get; }

    /// <summary>The covered fraction of the body when the event fired, 0 to 1.</summary>
    public Fixed Submerged { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Kind} {Body} {Fluid}";
}

/// <summary>
/// The wet and dry events of the last step, ordered by body then fluid, the same on every node. Read them in <c>OnTick</c>; the next step
/// replaces them. Particles reaching a drain raise no event: compare <see cref="PhysicsSpace2D.Drained"/> from tick to tick.
/// </summary>
public readonly struct FluidBodyEvents
{
    private readonly RecursiveAI.OnePhysics.Physics2D.PhysicsWorld2D? _world;

    internal FluidBodyEvents(RecursiveAI.OnePhysics.Physics2D.PhysicsWorld2D? world)
    {
        _world = world;
    }

    /// <summary>The number of events.</summary>
    public int Count => _world is null ? 0 : _world.FluidEvents.Length;

    /// <summary>The event at an index, 0 to <see cref="Count"/> - 1.</summary>
    /// <param name="index">The index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the list.</exception>
    public FluidBodyEvent this[int index]
    {
        get
        {
            var all = _world is null ? default : _world.FluidEvents;
            if ((uint)index >= (uint)all.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "There is no fluid event at this index.");
            }

            return Convert(all[index]);
        }
    }

    /// <summary>Returns an enumerator over the events.</summary>
    public Enumerator GetEnumerator() => new(this);

    private static FluidBodyEvent Convert(in RecursiveAI.OnePhysics.FluidEvent e) =>
        new(e.Kind == RecursiveAI.OnePhysics.FluidEventKind.Enter ? FluidBodyEventKind.Entered : FluidBodyEventKind.Left,
            e.Body.Generation == 0 ? default : new BodyRef(e.Body.Index, e.Body.Generation),
            new FluidRef(e.Fluid + 1),
            PhysicsLimits.FromSingle(e.SubmergedFraction));

    /// <summary>Walks the events without allocating.</summary>
    public struct Enumerator
    {
        private readonly FluidBodyEvents _events;
        private int _position;

        internal Enumerator(FluidBodyEvents events)
        {
            _events = events;
            _position = -1;
        }

        /// <summary>The event the enumerator stands on.</summary>
        public readonly FluidBodyEvent Current => _events[_position];

        /// <summary>Moves to the next event.</summary>
        /// <returns>False when there are no more.</returns>
        public bool MoveNext() => ++_position < _events.Count;
    }
}

/// <summary>A reference to a fluid, a body-ref-like value that fits a synced property. The default value names nothing.</summary>
public readonly struct FluidRef : ISyncValue<FluidRef>
{
    internal FluidRef(int id)
    {
        Id = id;
    }

    internal int Id { get; }

    /// <summary>The fluid's slot in the space, 0 for the first fluid added. Particle reads carry the same number (<see cref="FluidParticle.Fluid"/>).</summary>
    public int Slot => Id - 1;

    /// <summary>True for the default value, which names nothing (and, as a filter, means every fluid).</summary>
    public bool IsNull => Id == 0;

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteInt32(Id);
    }

    /// <inheritdoc />
    public static FluidRef Read(ref SyncReader reader)
    {
        var id = reader.ReadInt32();
        if (!reader.Ok)
        {
            return default;
        }

        if (id < 0)
        {
            reader.Fail(SyncError.InvalidValue);
            return default;
        }

        return new FluidRef(id);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        ArgumentNullException.ThrowIfNull(json);
        json.Value(ToString());
    }

    /// <inheritdoc />
    public bool Equals(FluidRef other) => Id == other.Id;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is FluidRef other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Id;

    /// <summary>Two references are equal when they name the same fluid.</summary>
    public static bool operator ==(FluidRef left, FluidRef right) => left.Equals(right);

    /// <summary>Two references differ when they name different fluids.</summary>
    public static bool operator !=(FluidRef left, FluidRef right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => IsNull ? "fluid:none" : string.Create(CultureInfo.InvariantCulture, $"fluid:{Slot}");
}

/// <summary>
/// A reference to an emitter of a fluid. It is checked: after <see cref="PhysicsSpace2D.Remove(FluidEmitterRef)"/> it names nothing, even if the slot is used again.
/// An emitter made in this process stays reachable through it across restores into the same space; a space that was only loaded from a save
/// cannot reach emitters it did not create (see <c>docs/physics.md</c>).
/// </summary>
public readonly struct FluidEmitterRef : ISyncValue<FluidEmitterRef>
{
    internal FluidEmitterRef(FluidRef fluid, int index, int serial)
    {
        Fluid = fluid;
        Index = index;
        Serial = serial;
    }

    /// <summary>The fluid the emitter belongs to.</summary>
    public FluidRef Fluid { get; }

    /// <summary>The emitter's slot in its fluid.</summary>
    public int Index { get; }

    internal int Serial { get; }

    /// <summary>True for the default value, which names nothing.</summary>
    public bool IsNull => Serial == 0;

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteInt32(Fluid.Id);
        writer.WriteInt32(Index);
        writer.WriteInt32(Serial);
    }

    /// <inheritdoc />
    public static FluidEmitterRef Read(ref SyncReader reader)
    {
        var fluid = reader.ReadInt32();
        var index = reader.ReadInt32();
        var serial = reader.ReadInt32();
        if (!reader.Ok)
        {
            return default;
        }

        if (fluid < 0 || index < 0 || serial < 0 || (serial == 0 && (fluid != 0 || index != 0)))
        {
            reader.Fail(SyncError.InvalidValue);
            return default;
        }

        return new FluidEmitterRef(new FluidRef(fluid), index, serial);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        ArgumentNullException.ThrowIfNull(json);
        json.Value(ToString());
    }

    /// <inheritdoc />
    public bool Equals(FluidEmitterRef other) => Fluid == other.Fluid && Index == other.Index && Serial == other.Serial;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is FluidEmitterRef other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Fluid, Index, Serial);

    /// <summary>Two references are equal when they name the same emitter.</summary>
    public static bool operator ==(FluidEmitterRef left, FluidEmitterRef right) => left.Equals(right);

    /// <summary>Two references differ when they name different emitters.</summary>
    public static bool operator !=(FluidEmitterRef left, FluidEmitterRef right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => IsNull ? "emitter:none" : string.Create(CultureInfo.InvariantCulture, $"emitter:{Fluid.Slot}.{Index}");
}

/// <summary>A reference to a drain of a fluid, checked like <see cref="FluidEmitterRef"/>.</summary>
public readonly struct FluidDrainRef : ISyncValue<FluidDrainRef>
{
    internal FluidDrainRef(FluidRef fluid, int index, int serial)
    {
        Fluid = fluid;
        Index = index;
        Serial = serial;
    }

    /// <summary>The fluid the drain belongs to.</summary>
    public FluidRef Fluid { get; }

    /// <summary>The drain's slot in its fluid.</summary>
    public int Index { get; }

    internal int Serial { get; }

    /// <summary>True for the default value, which names nothing.</summary>
    public bool IsNull => Serial == 0;

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteInt32(Fluid.Id);
        writer.WriteInt32(Index);
        writer.WriteInt32(Serial);
    }

    /// <inheritdoc />
    public static FluidDrainRef Read(ref SyncReader reader)
    {
        var fluid = reader.ReadInt32();
        var index = reader.ReadInt32();
        var serial = reader.ReadInt32();
        if (!reader.Ok)
        {
            return default;
        }

        if (fluid < 0 || index < 0 || serial < 0 || (serial == 0 && (fluid != 0 || index != 0)))
        {
            reader.Fail(SyncError.InvalidValue);
            return default;
        }

        return new FluidDrainRef(new FluidRef(fluid), index, serial);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        ArgumentNullException.ThrowIfNull(json);
        json.Value(ToString());
    }

    /// <inheritdoc />
    public bool Equals(FluidDrainRef other) => Fluid == other.Fluid && Index == other.Index && Serial == other.Serial;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is FluidDrainRef other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Fluid, Index, Serial);

    /// <summary>Two references are equal when they name the same drain.</summary>
    public static bool operator ==(FluidDrainRef left, FluidDrainRef right) => left.Equals(right);

    /// <summary>Two references differ when they name different drains.</summary>
    public static bool operator !=(FluidDrainRef left, FluidDrainRef right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => IsNull ? "drain:none" : string.Create(CultureInfo.InvariantCulture, $"drain:{Fluid.Slot}.{Index}");
}

/// <summary>A fluid, emitter or drain reference no longer names anything in the space.</summary>
public sealed class StaleFluidException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What happened.</param>
    public StaleFluidException(string message)
        : base(message)
    {
    }
}

/// <summary>Adding particles would pass <see cref="FluidWorldSettings.MaxParticles"/>.</summary>
public sealed class FluidBudgetExceededException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What happened.</param>
    public FluidBudgetExceededException(string message)
        : base(message)
    {
    }
}
