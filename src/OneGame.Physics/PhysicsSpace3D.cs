using RecursiveAI.OneMath;
using RecursiveAI.OnePhysics;
using RecursiveAI.OnePhysics.Physics3D;

namespace OneGame.Physics;

/// <summary>A deterministic 3D physics space saved as a Hidden world table and stepped after entities. Coordinates are local to <see cref="Origin"/>.</summary>
public sealed partial class PhysicsSpace3D : IHotTable
{
    /// <summary>The default table name.</summary>
    public const string TableName = "physics3d";
    private const int WorldId = 1;
    private const int MaxSnapshotBytes = 64 * 1024 * 1024;
    private readonly string _name;
    private readonly int _rate;
    private readonly float _dt;
    private readonly WorldOptions3D _settings;
    private PhysicsWorld3D _world;
    private bool _warm;
    private byte[] _scratch = [];

    /// <summary>Creates a standalone space; <see cref="AddTo"/> also registers tick stepping.</summary>
    public PhysicsSpace3D(PhysicsOptions3D? options = null)
    {
        var o = options ?? new();
        if (o.TicksPerSecond is < 1 or > 1000 || o.Substeps is < 1 or > 32 || string.IsNullOrEmpty(o.Name))
            throw new ArgumentOutOfRangeException(nameof(options), "Use a name, 1..1000 ticks per second and 1..32 substeps.");
        _name = o.Name;
        _rate = o.TicksPerSecond;
        _dt = 1f / _rate;
        _settings = new WorldOptions3D
        {
            WorldId = WorldId, Gravity = Vector(o.Gravity), Substeps = o.Substeps,
            SleepEnabled = o.Sleep, ThreadCount = 1,
            Events = EventMask.Default | (o.HitEvents ? EventMask.ContactHit : 0) | (o.StayEvents ? EventMask.ContactPersist : 0),
            PairCapacity = o.PairCapacity, ContactCapacity = o.ContactCapacity,
            IslandCapacity = o.IslandCapacity, QueryScratchCapacity = o.QueryScratchCapacity,
        };
        _world = new(_settings);
    }

    private PhysicsSpace3D(PhysicsSpace3D source)
    {
        _name = source._name; _rate = source._rate; _dt = source._dt;
        _world = source._world.Fork(); _settings = _world.Options;
    }

    /// <summary>Registers a Hidden table and a post-entity step system. Add in the same order on every node.</summary>
    public static PhysicsSpace3D AddTo(World world, PhysicsOptions3D? options = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        var space = new PhysicsSpace3D(options);
        world.AddTable(space); world.AddSystem(new StepSystem(space));
        return space;
    }

    /// <summary>Finds a registered space by table name.</summary>
    public static PhysicsSpace3D? Of(World world, string name = TableName)
    {
        ArgumentNullException.ThrowIfNull(world);
        foreach (var table in world.Tables)
            if (table is PhysicsSpace3D space && space.Name == name) return space;
        return null;
    }

    /// <summary>Completed physics steps.</summary>
    public long StepCount => _world.StepCount;
    /// <summary>The fixed tick length.</summary>
    public Fixed StepSeconds => Fixed.FromRatio(1, _rate);
    /// <summary>Live body count, including character bodies.</summary>
    public int BodyCount => _world.BodyCount;
    /// <summary>Accumulated origin offset. Included in snapshots and the hash.</summary>
    public FVec3 Origin { get { var o = _world.Origin; return new(Global(o.X), Global(o.Y), Global(o.Z)); } }
    /// <summary>Hash of every saved physics section, including events and origin.</summary>
    public ulong Hash => _world.ComputeHash();
    /// <summary>Previous step's events, in canonical delivery order.</summary>
    public PhysicsEvents3D Events => new(_world);

    /// <summary>Advances one fixed tick. During rollback, suppresses callbacks but retains polled events.</summary>
    [MutatesState]
    public void Step(bool resimulating = false)
    {
        if (!_warm) Warmup();
        if (resimulating) { using var suppression = _world.SuppressCallbacks(); _world.Step(_dt); }
        else _world.Step(_dt);
    }

    /// <summary>Preallocates stepping, query and snapshot storage for the current scene.</summary>
    public void Warmup()
    {
        _world.Warmup();
        EnsureScratch();
        _warm = true;
    }

    /// <summary>Moves the local coordinate frame by an explicit deterministic offset before positions approach ±2^21. The library saves and hashes the accumulated origin.</summary>
    [MutatesState]
    public void ShiftOrigin(FVec3 offset)
    {
        var origin = _world.Origin;
        var shift = Point(offset);
        _ = Global(origin.X + shift.X); _ = Global(origin.Y + shift.Y); _ = Global(origin.Z + shift.Z);
        _world.ShiftOrigin(shift);
    }

    private static Fixed Global(double value)
    {
        if (!double.IsFinite(value) || value < int.MinValue || value >= int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), "Accumulated origin must fit signed 32-bit world units.");
        var whole = Math.Truncate(value);
        return Fixed.FromInt((int)whole) + From(value - whole);
    }

    /// <summary>Creates an independent structural fork for prediction, preserving handles and pending events.</summary>
    public PhysicsSpace3D Fork() => new(this);

    /// <summary>Adds a full-sized box at a local position.</summary>
    [MutatesState]
    public BodyRef AddBox(FVec3 size, FVec3 at, BodyOptions3D? options = null)
    {
        var o = options ?? new(); var full = Vector(size); var shape = ShapeFor(o);
        var body = MakeBody(at, o); _world.CreateBox(body, full, def: shape); return Ref(body.RawHandle);
    }

    /// <summary>Adds a sphere at a local position.</summary>
    [MutatesState]
    public BodyRef AddSphere(Fixed radius, FVec3 at, BodyOptions3D? options = null)
    {
        var o = options ?? new(); var r = Single(radius); var shape = ShapeFor(o);
        var body = MakeBody(at, o); _world.CreateSphere(body, r, shape); return Ref(body.RawHandle);
    }

    /// <summary>Adds an upright capsule; halfLength is half the straight section.</summary>
    [MutatesState]
    public BodyRef AddCapsule(Fixed halfLength, Fixed radius, FVec3 at, BodyOptions3D? options = null)
    {
        var o = options ?? new(); var h = Single(halfLength); var r = Single(radius); var shape = ShapeFor(o);
        var body = MakeBody(at, o); _world.CreateCapsule(body, h, r, shape); return Ref(body.RawHandle);
    }

    /// <summary>Whether this reference names a live body in this space.</summary>
    public bool IsAlive(BodyRef body) => _world.IsBodyAlive(Handle(body));
    /// <summary>Local position of a body.</summary>
    public FVec3 Position(BodyRef body) => From(Body(body).Position);
    /// <summary>Orientation of a body.</summary>
    public FQuat Rotation(BodyRef body) { var q = Body(body).Rotation; return new(From(q.X), From(q.Y), From(q.Z), From(q.W)); }
    /// <summary>Rigid transform with unit scale.</summary>
    public Transform3 Transform(BodyRef body) => new(Position(body), Rotation(body), new(Fixed.One, Fixed.One, Fixed.One));
    /// <summary>Linear velocity of a body.</summary>
    public FVec3 Velocity(BodyRef body) => From(Body(body).LinearVelocity);
    /// <summary>Sets linear velocity.</summary>
    [MutatesState]
    public void SetVelocity(BodyRef body, FVec3 velocity) => Body(body).SetLinearVelocity(Vector(velocity));
    /// <summary>Applies a centre-of-mass impulse.</summary>
    [MutatesState]
    public void ApplyImpulse(BodyRef body, FVec3 impulse) => Body(body).ApplyImpulse(Vector(impulse));
    /// <summary>Teleports a body, preserving velocity.</summary>
    [MutatesState]
    public void Teleport(BodyRef body, FVec3 at, FQuat rotation = default) => Body(body).SetTransform(Point(at), Quaternion(rotation));
    /// <summary>Removes a body and its shapes.</summary>
    [MutatesState]
    public void Remove(BodyRef body) => Body(body).Destroy();
    /// <summary>Changes the saved collision matrix for two layers.</summary>
    [MutatesState]
    public void SetLayerCollision(int a, int b, bool collide) => _world.SetLayerCollision(a, b, collide);

    private RecursiveAI.OnePhysics.Physics3D.Body MakeBody(FVec3 at, BodyOptions3D o)
    {
        var def = new BodyDef
        {
            Position = Point(at), Rotation = Quaternion(o.Rotation),
            Type = o.Kind switch { BodyKind.Static => BodyType.Static, BodyKind.Kinematic => BodyType.Kinematic, _ => BodyType.Dynamic },
            LinearVelocity = Vector(o.Velocity), LinearDamping = Single(o.LinearDamping), AngularDamping = Single(o.AngularDamping),
            GravityScale = Single(o.GravityScale), FixedRotation = o.FixedRotation, IsBullet = o.IsBullet,
        };
        return _world.CreateBody(def);
    }
    private static ShapeDef ShapeFor(BodyOptions3D o) => new()
    {
        Density = Single(o.Density), Friction = Single(o.Friction), Restitution = Single(o.Restitution), IsSensor = o.IsTrigger, Layer = o.Layer,
    };
    private static BodyHandle Handle(BodyRef body) => body.IsNull ? default : new(WorldId, body.Index, body.Generation);
    private RecursiveAI.OnePhysics.Physics3D.Body Body(BodyRef body)
    {
        if (!IsAlive(body)) throw new StaleBodyException($"{body} no longer exists in {Name}.");
        return _world.GetBody(Handle(body));
    }
    internal static BodyRef Ref(BodyHandle body) => body.IsNull ? default : new(body.Index, body.Generation);
    // FixedBridge excludes the endpoints; physics also accepts these exactly representable constants.
    internal static double Double(Fixed v) => v.Raw switch
    {
        PhysicsLimits.MaxCoordinateRaw => PhysicsLimits.MaxCoordinate,
        -PhysicsLimits.MaxCoordinateRaw => -PhysicsLimits.MaxCoordinate,
        _ => FixedBridge.ToDouble(v.Raw),
    };
    internal static float Single(Fixed v) => (float)Double(v);
    internal static Fixed From(double v) => v switch
    {
        PhysicsLimits.MaxCoordinate => Fixed.FromInt(PhysicsLimits.MaxCoordinate),
        -PhysicsLimits.MaxCoordinate => Fixed.FromInt(-PhysicsLimits.MaxCoordinate),
        _ => Fixed.FromRaw(FixedBridge.FromDouble(v)),
    };
    internal static FVec3 From(Vector3D p) => new(From(p.X), From(p.Y), From(p.Z));
    internal static FVec3 From(Vector3F p) => new(From(p.X), From(p.Y), From(p.Z));
    internal static Vector3D Point(FVec3 p) => new(Double(p.X), Double(p.Y), Double(p.Z));
    internal static Vector3F Vector(FVec3 p) => new(Single(p.X), Single(p.Y), Single(p.Z));
    private static QuaternionF Quaternion(FQuat q) => new(Single(q.X), Single(q.Y), Single(q.Z), Single(q.W));

    private sealed class StepSystem(PhysicsSpace3D space) : ISimSystem
    {
        public void Tick(World world) => space.Step();
    }
}
