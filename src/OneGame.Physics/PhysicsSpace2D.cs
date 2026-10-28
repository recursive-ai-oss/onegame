using RecursiveAI.OneMath;
using RecursiveAI.OnePhysics.Core;
using RecursiveAI.OnePhysics.Physics2D;

namespace OneGame.Physics;

/// <summary>
/// A 2D physics world that is part of the game world's state. It is a hot table of the <see cref="World"/> (so snapshots, saves, the
/// command-log replay and the state hash include it) and it registers a system that steps it once per tick, after the entities.
/// <code>
/// // Setup (the same on every node):
/// var space = PhysicsSpace2D.AddTo(world);
/// space.AddBox(new FVec2(40, 1), new FVec2(0, -1), new BodyOptions { Kind = BodyKind.Static });
/// // in a command or OnTick:
/// var space = PhysicsSpace2D.Of(World!)!;
/// Body = space.AddCircle(Fixed.FromRatio(1, 2), new FVec2(0, 5));
/// Position = space.Position(Body);   // copy what views need into synced properties
/// </code>
/// Everything sim code touches takes and returns onegame types (<see cref="Fixed"/>, <see cref="FVec2"/>, <see cref="BodyRef"/>). The library
/// underneath works in doubles (positions) and floats (everything else); conversions are exact for positions and round to nearest even
/// for the rest, so every node computes the same bits (see <c>docs/physics.md</c>).
/// <para>
/// The space is authority state: the table is <see cref="Visibility.Hidden"/>, so replicas do not receive it. Entities mirror what
/// views draw into ordinary synced properties.
/// </para>
/// </summary>
public sealed class PhysicsSpace2D : IHotTable
{
    /// <summary>The default table name.</summary>
    public const string TableName = "physics";

    // Bumped when the bytes this table writes around the physics snapshot change.
    private const byte FormatVersion = 1;

    // The largest physics snapshot a save may hold. The library validates the bytes; this only bounds the allocation.
    private const int MaxSnapshotBytes = 64 * 1024 * 1024;

    // Every space uses the same world id so that references survive a restore into another instance.
    private const int WorldId = 1;

    private readonly string _name;
    private readonly PhysicsOptions _options;
    private readonly float _dt;
    private PhysicsWorld2D _world;

    /// <summary>Creates a space that is not yet part of any world. Most games use <see cref="AddTo"/>.</summary>
    /// <param name="options">Settings; the same on every node. Null uses the defaults.</param>
    /// <exception cref="ArgumentOutOfRangeException">An option is out of range.</exception>
    public PhysicsSpace2D(PhysicsOptions? options = null)
    {
        _options = options ?? new PhysicsOptions();
        if (_options.TicksPerSecond is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(options), _options.TicksPerSecond, "TicksPerSecond must be between 1 and 1000.");
        }

        if (_options.Substeps is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(options), _options.Substeps, "Substeps must be between 1 and 32.");
        }

        if (string.IsNullOrEmpty(_options.Name))
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The table needs a name.");
        }

        _name = _options.Name;
        _dt = 1f / _options.TicksPerSecond;
        _world = Create();
    }

    private PhysicsWorld2D Create()
    {
        var mask = EventMask.Default;
        if (_options.HitEvents)
        {
            mask |= EventMask.ContactHit;
        }

        var world = new PhysicsWorld2D(new WorldOptions2D
        {
            Gravity = new Vector2F(PhysicsLimits.ToSingle(_options.Gravity.X, "Gravity"), PhysicsLimits.ToSingle(_options.Gravity.Y, "Gravity")),
            Substeps = _options.Substeps,
            SleepEnabled = _options.Sleep,
            ThreadCount = 1, // threads would add nothing to the result and the sim may not start any
            WorldId = WorldId,
            Events = mask,
        });
        return world;
    }

    /// <summary>Makes a space and adds it to a world as a table and as a system (it steps after the entities, once per tick).</summary>
    /// <param name="world">The world; add the space before any save is loaded, in the same order on every node.</param>
    /// <param name="options">Settings; the same on every node.</param>
    /// <returns>The space.</returns>
    /// <exception cref="DuplicateTypeException">The world already has a table with this name.</exception>
    public static PhysicsSpace2D AddTo(World world, PhysicsOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        var space = new PhysicsSpace2D(options);
        world.AddTable(space);
        world.AddSystem(new StepSystem(space));
        return space;
    }

    /// <summary>The world's space with this name, or null.</summary>
    /// <param name="world">The world.</param>
    /// <param name="name">The table name.</param>
    public static PhysicsSpace2D? Of(World world, string name = TableName)
    {
        ArgumentNullException.ThrowIfNull(world);
        var tables = world.Tables;
        for (var i = 0; i < tables.Count; i++)
        {
            if (tables[i] is PhysicsSpace2D s && s._name == name)
            {
                return s;
            }
        }

        return null;
    }

    /// <summary>How many steps the space has taken.</summary>
    public long StepCount => _world.StepCount;

    /// <summary>The seconds one step advances, 1 / <see cref="PhysicsOptions.TicksPerSecond"/>.</summary>
    public Fixed StepSeconds => Fixed.FromRatio(1, _options.TicksPerSecond);

    /// <summary>The number of bodies (characters' bodies included).</summary>
    public int BodyCount => Bodies();

    /// <summary>The events of the last step. Read them in <c>OnTick</c>, before the system steps again.</summary>
    public PhysicsEvents Events => new(_world);

    /// <summary>The hash of the physics state and its events; the same on every node when the states are equal. The world hash includes it.</summary>
    public ulong Hash
    {
        get
        {
            var h = StateHash.Combine(0, _world.ComputeHash());
            foreach (ref readonly var e in _world.Events)
            {
                h = StateHash.Combine(h, (ulong)e.Kind);
                h = StateHash.Combine(h, (ulong)(uint)e.BodyA.Index | ((ulong)(uint)e.BodyA.Generation << 32));
                h = StateHash.Combine(h, (ulong)(uint)e.BodyB.Index | ((ulong)(uint)e.BodyB.Generation << 32));
            }

            return h;
        }
    }

    // ---------------------------------------------------------------- stepping

    /// <summary>Advances the space by one tick. The system added by <see cref="AddTo"/> calls this; call it yourself only for a space you did not add to a world.</summary>
    [MutatesState]
    public void Step() => _world.Step(_dt);

    // ---------------------------------------------------------------- creating things

    /// <summary>Adds a box.</summary>
    /// <param name="size">Full width and height.</param>
    /// <param name="at">Where its centre starts.</param>
    /// <param name="options">Behaviour; null is a dynamic body with the defaults.</param>
    /// <returns>A reference to keep in a synced property.</returns>
    /// <exception cref="PhysicsRangeException">A value is outside ±2^21.</exception>
    [MutatesState]
    public BodyRef AddBox(FVec2 size, FVec2 at, BodyOptions? options = null)
    {
        var o = options ?? new BodyOptions();
        var halfWidth = 0.5f * PhysicsLimits.ToSingle(size.X, "box size");
        var halfHeight = 0.5f * PhysicsLimits.ToSingle(size.Y, "box size");
        var shape = ShapeFor(o);
        var body = MakeBody(at, o);
        _world.CreateBox(body, halfWidth, halfHeight, default, 0f, 0f, shape);
        return Ref(body);
    }

    /// <summary>Adds a circle.</summary>
    /// <param name="radius">Its radius.</param>
    /// <param name="at">Where its centre starts.</param>
    /// <param name="options">Behaviour; null is a dynamic body with the defaults.</param>
    /// <returns>A reference to keep in a synced property.</returns>
    /// <exception cref="PhysicsRangeException">A value is outside ±2^21.</exception>
    [MutatesState]
    public BodyRef AddCircle(Fixed radius, FVec2 at, BodyOptions? options = null)
    {
        var o = options ?? new BodyOptions();
        var body = MakeBody(at, o);
        _world.CreateCircle(body, PhysicsLimits.ToSingle(radius, "circle radius"), default, ShapeFor(o));
        return Ref(body);
    }

    /// <summary>Adds an upright capsule (a box with half-circle ends) whose centre starts at <paramref name="at"/>.</summary>
    /// <param name="halfLength">Half the length of the straight part.</param>
    /// <param name="radius">The radius of the ends.</param>
    /// <param name="at">Where its centre starts.</param>
    /// <param name="options">Behaviour; null is a dynamic body with the defaults.</param>
    /// <returns>A reference to keep in a synced property.</returns>
    /// <exception cref="PhysicsRangeException">A value is outside ±2^21.</exception>
    [MutatesState]
    public BodyRef AddCapsule(Fixed halfLength, Fixed radius, FVec2 at, BodyOptions? options = null)
    {
        var o = options ?? new BodyOptions();
        var body = MakeBody(at, o);
        var half = PhysicsLimits.ToSingle(halfLength, "half length");
        _world.CreateCapsule(body, new Vector2F(0f, -half), new Vector2F(0f, half), PhysicsLimits.ToSingle(radius, "circle radius"), ShapeFor(o));
        return Ref(body);
    }

    /// <summary>Adds a convex polygon of up to eight points given relative to <paramref name="at"/>.</summary>
    /// <param name="points">The corners.</param>
    /// <param name="at">Where its origin starts.</param>
    /// <param name="options">Behaviour; null is a dynamic body with the defaults.</param>
    /// <returns>A reference to keep in a synced property.</returns>
    /// <exception cref="PhysicsRangeException">A value is outside ±2^21.</exception>
    /// <exception cref="ArgumentException">The points do not make a polygon.</exception>
    [MutatesState]
    public BodyRef AddPolygon(ReadOnlySpan<FVec2> points, FVec2 at, BodyOptions? options = null)
    {
        var o = options ?? new BodyOptions();
        var pts = new Vector2F[points.Length];
        for (var i = 0; i < pts.Length; i++)
        {
            pts[i] = new Vector2F(PhysicsLimits.ToSingle(points[i].X, "polygon point"), PhysicsLimits.ToSingle(points[i].Y, "polygon point"));
        }

        var shape = ShapeFor(o);
        var body = MakeBody(at, o);
        try
        {
            _world.CreatePolygon(body, pts, 0f, shape);
        }
        catch
        {
            body.Destroy();
            throw;
        }

        return Ref(body);
    }

    /// <summary>Adds a character: an upright capsule moved by <see cref="Move"/> with collide-and-slide.</summary>
    /// <param name="at">Where its centre starts.</param>
    /// <param name="options">Size and abilities; null is a 1.8 tall person.</param>
    /// <returns>A reference to keep in a synced property.</returns>
    /// <exception cref="PhysicsRangeException">A value is outside ±2^21.</exception>
    [MutatesState]
    public CharacterRef AddCharacter(FVec2 at, CharacterOptions? options = null)
    {
        var o = options ?? new CharacterOptions();
        var def = new CharacterDef
        {
            Radius = PhysicsLimits.ToSingle(o.Radius, "Radius"),
            Height = PhysicsLimits.ToSingle(o.Height, "Height"),
            SlopeLimit = PhysicsLimits.ToSingle(o.SlopeLimit, "SlopeLimit"),
            StepOffset = PhysicsLimits.ToSingle(o.StepOffset, "StepOffset"),
            PushStrength = PhysicsLimits.ToSingle(o.PushStrength, "PushStrength"),
            Layer = o.Layer,
        };
        var character = _world.AddCharacter(Position2(at), def);
        return new CharacterRef(character.Index, character.Generation);
    }

    /// <summary>Removes a body.</summary>
    /// <param name="body">The body.</param>
    /// <exception cref="StaleBodyException">The body does not exist.</exception>
    [MutatesState]
    public void Remove(BodyRef body) => Resolve(body).Destroy();

    /// <summary>Removes a character and its body.</summary>
    /// <param name="character">The character.</param>
    /// <exception cref="StaleBodyException">The character does not exist.</exception>
    [MutatesState]
    public void Remove(CharacterRef character) => Resolve(character).Destroy();

    /// <summary>Sets whether bodies on two layers collide (all pairs collide at first). Part of the saved state.</summary>
    /// <param name="layerA">A layer, 0 to 31.</param>
    /// <param name="layerB">A layer, 0 to 31.</param>
    /// <param name="collide">Whether they collide.</param>
    [MutatesState]
    public void SetLayerCollision(int layerA, int layerB, bool collide) => _world.SetLayerCollision(layerA, layerB, collide);

    // ---------------------------------------------------------------- bodies

    /// <summary>Whether the reference names a body that exists.</summary>
    /// <param name="body">The reference.</param>
    public bool Exists(BodyRef body) => !body.IsNull && _world.IsBodyAlive(new BodyHandle(_world.Id, body.Index, body.Generation));

    /// <summary>Whether the reference names a character that exists.</summary>
    /// <param name="character">The reference.</param>
    public bool Exists(CharacterRef character) => !character.IsNull && _world.IsCharacterAlive(character.Index, character.Generation);

    /// <summary>Where the body's origin is (the centre for the shapes added here).</summary>
    /// <param name="body">The body.</param>
    /// <exception cref="StaleBodyException">The body does not exist.</exception>
    public FVec2 Position(BodyRef body) => FromVector(Resolve(body).Position);

    /// <summary>The body's rotation in radians, counter-clockwise.</summary>
    /// <param name="body">The body.</param>
    /// <exception cref="StaleBodyException">The body does not exist.</exception>
    public Fixed Angle(BodyRef body) => PhysicsLimits.FromSingle(Resolve(body).Angle);

    /// <summary>The body's linear velocity.</summary>
    /// <param name="body">The body.</param>
    /// <exception cref="StaleBodyException">The body does not exist.</exception>
    public FVec2 Velocity(BodyRef body)
    {
        var v = Resolve(body).LinearVelocity;
        return new FVec2(PhysicsLimits.FromSingle(v.X), PhysicsLimits.FromSingle(v.Y));
    }

    /// <summary>The body's angular velocity in radians per second.</summary>
    /// <param name="body">The body.</param>
    /// <exception cref="StaleBodyException">The body does not exist.</exception>
    public Fixed AngularVelocity(BodyRef body) => PhysicsLimits.FromSingle(Resolve(body).AngularVelocity);

    /// <summary>Whether a dynamic body is awake (static bodies report false).</summary>
    /// <param name="body">The body.</param>
    /// <exception cref="StaleBodyException">The body does not exist.</exception>
    public bool IsAwake(BodyRef body) => Resolve(body).IsAwake;

    /// <summary>Sets the body's linear velocity (waking it).</summary>
    /// <param name="body">The body.</param>
    /// <param name="velocity">The new velocity.</param>
    /// <exception cref="StaleBodyException">The body does not exist.</exception>
    [MutatesState]
    public void SetVelocity(BodyRef body, FVec2 velocity)
    {
        var b = Resolve(body);
        b.LinearVelocity = ToVector(velocity, "velocity"); // also wakes the body
    }

    /// <summary>Gives the body an instant push at its centre (velocity change = impulse / mass).</summary>
    /// <param name="body">The body.</param>
    /// <param name="impulse">The impulse.</param>
    /// <exception cref="StaleBodyException">The body does not exist.</exception>
    [MutatesState]
    public void ApplyImpulse(BodyRef body, FVec2 impulse) => Resolve(body).ApplyImpulse(ToVector(impulse, "impulse"));

    /// <summary>Applies a force to the body's centre for the next step.</summary>
    /// <param name="body">The body.</param>
    /// <param name="force">The force.</param>
    /// <exception cref="StaleBodyException">The body does not exist.</exception>
    [MutatesState]
    public void ApplyForce(BodyRef body, FVec2 force) => Resolve(body).ApplyForce(ToVector(force, "force"));

    /// <summary>Teleports the body.</summary>
    /// <param name="body">The body.</param>
    /// <param name="position">The new position.</param>
    /// <param name="angle">The new rotation in radians.</param>
    /// <exception cref="StaleBodyException">The body does not exist.</exception>
    [MutatesState]
    public void SetTransform(BodyRef body, FVec2 position, Fixed angle) => Resolve(body).SetTransform(Position2(position), PhysicsLimits.ToSingle(angle, "angle value"));

    // ---------------------------------------------------------------- characters

    /// <summary>Moves a character by a displacement this tick with collide-and-slide: it stops at walls, climbs small steps, follows slopes up to its limit and rides moving ground. Gravity and jumps are yours: keep the vertical speed in a synced property and include it in the displacement.</summary>
    /// <param name="character">The character.</param>
    /// <param name="displacement">How far to move this tick.</param>
    /// <returns>Where it ended and what it touches.</returns>
    /// <exception cref="StaleBodyException">The character does not exist.</exception>
    [MutatesState]
    public CharacterMove Move(CharacterRef character, FVec2 displacement)
    {
        var c = Resolve(character);
        var contact = c.Move(ToVector(displacement, "displacement"), _dt);
        return new CharacterMove(
            c.IsGrounded,
            (contact & CharacterContact.Above) != 0,
            (contact & CharacterContact.Sides) != 0,
            FromVector(c.Position));
    }

    /// <summary>The character's capsule centre.</summary>
    /// <param name="character">The character.</param>
    /// <exception cref="StaleBodyException">The character does not exist.</exception>
    public FVec2 Position(CharacterRef character) => FromVector(Resolve(character).Position);

    /// <summary>Moves a character to a place without sweeping (a respawn).</summary>
    /// <param name="character">The character.</param>
    /// <param name="position">The new capsule centre.</param>
    /// <exception cref="StaleBodyException">The character does not exist.</exception>
    [MutatesState]
    public void Teleport(CharacterRef character, FVec2 position) => Resolve(character).Position = Position2(position);

    /// <summary>Whether the character stood on walkable ground after its last move.</summary>
    /// <param name="character">The character.</param>
    /// <exception cref="StaleBodyException">The character does not exist.</exception>
    public bool IsGrounded(CharacterRef character) => Resolve(character).IsGrounded;

    /// <summary>The body of a character, as events and queries name it.</summary>
    /// <param name="character">The character.</param>
    /// <exception cref="StaleBodyException">The character does not exist.</exception>
    public BodyRef BodyOf(CharacterRef character) => Ref(Resolve(character).Body);

    // ---------------------------------------------------------------- queries

    /// <summary>Casts a ray and finds the nearest body it meets. Triggers are skipped.</summary>
    /// <param name="origin">Where the ray starts.</param>
    /// <param name="direction">Which way it goes (any length except zero).</param>
    /// <param name="maxDistance">How far it reaches.</param>
    /// <param name="hit">What it met.</param>
    /// <param name="layers">Which layers count, as a bit mask (bit i = layer i). Default all.</param>
    /// <returns>True when something was hit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The direction is zero or the distance is not positive.</exception>
    public bool Raycast(FVec2 origin, FVec2 direction, Fixed maxDistance, out RayHit hit, uint layers = uint.MaxValue)
    {
        if (direction.X == Fixed.Zero && direction.Y == Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(direction), "A ray needs a direction.");
        }

        if (maxDistance <= Fixed.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDistance), "A ray needs a positive length.");
        }

        var filter = QueryFilter2D.Layers(layers);
        if (!_world.Raycast(Position2(origin), ToVector(direction, "direction"), out var q, PhysicsLimits.ToSingle(maxDistance, "ray length"), in filter))
        {
            hit = default;
            return false;
        }

        hit = new RayHit(Ref(q.Body), FromVector(q.Point), new FVec2(PhysicsLimits.FromSingle(q.Normal.X), PhysicsLimits.FromSingle(q.Normal.Y)), PhysicsLimits.FromSingle(q.Distance));
        return true;
    }

    // ---------------------------------------------------------------- debug

    /// <summary>Adds the outlines of every shape, the contact points and the characters' capsules to <paramref name="shapes"/>, for a view to draw while the debug overlay is on.</summary>
    /// <param name="shapes">Receives the geometry (call <see cref="PhysicsDebugShapes.Clear"/> first).</param>
    public void AddDebugShapes(PhysicsDebugShapes shapes)
    {
        ArgumentNullException.ThrowIfNull(shapes);
        shapes.Collect(_world);
    }

    // ---------------------------------------------------------------- IHotTable

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc />
    public int Count => Bodies();

    /// <inheritdoc />
    /// <remarks>Hidden: replicas never receive the space; they get what entities mirror into synced properties.</remarks>
    public Visibility Visibility => Visibility.Hidden;

    // Reused between calls so a steady-state snapshot allocates nothing.
    private byte[] _scratch = [];

    private static ReadOnlySpan<byte> SnapshotInto(PhysicsWorld2D world, ref byte[] scratch)
    {
        var size = world.GetSnapshotSize();
        if (scratch.Length < size)
        {
            scratch = new byte[Math.Max(size, scratch.Length * 2)];
        }

        if (!world.TrySnapshot(scratch, out var written))
        {
            throw new InvalidOperationException("The physics snapshot did not fit the buffer sized for it.");
        }

        return scratch.AsSpan(0, written);
    }

    /// <inheritdoc />
    public void Clear() => _world = Create();

    /// <inheritdoc />
    /// <remarks>The whole physics snapshot, byte-exact.</remarks>
    public void Write(SyncWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte(FormatVersion);
        writer.WriteInt32(_options.TicksPerSecond);
        writer.WriteBytes(SnapshotInto(_world, ref _scratch));
    }

    /// <inheritdoc />
    public void Read(ref SyncReader reader)
    {
        var version = reader.ReadByte();
        var rate = reader.ReadInt32();
        var bytes = reader.ReadBytes(MaxSnapshotBytes);
        if (!reader.Ok)
        {
            return;
        }

        if (version != FormatVersion)
        {
            reader.Fail(SyncError.UnsupportedVersion);
            return;
        }

        if (rate != _options.TicksPerSecond)
        {
            // The save was made at another tick rate: stepping it at this one would not continue the same simulation.
            reader.Fail(SyncError.Mismatch);
            return;
        }

        try
        {
            _world.Restore(bytes); // validated first; on a problem the world is unchanged
        }
        catch (PhysicsException)
        {
            // Includes SnapshotFormatException: a snapshot from a build of the library whose format differs, or damaged bytes.
            reader.Fail(SyncError.InvalidValue);
        }
        catch (ArgumentException)
        {
            reader.Fail(SyncError.InvalidValue);
        }
    }

    /// <inheritdoc />
    public void CopyFrom(IHotTable source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source is not PhysicsSpace2D other)
        {
            throw new InvalidOperationException("A physics table can only copy from another physics table.");
        }

        _world.Restore(SnapshotInto(other._world, ref _scratch));
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        ArgumentNullException.ThrowIfNull(json);
        json.BeginObject();
        json.Name("steps");
        json.Value(_world.StepCount);
        json.Name("hash");
        json.Value(Hash);
        json.Name("bodies");
        json.BeginArray();
        var bodies = _world.Core.Bodies;
        for (var i = 0; i < bodies.HighWater; i++)
        {
            if (!bodies.Handles.IsLive(i))
            {
                continue;
            }

            var handle = new BodyHandle(_world.Id, i, bodies.Handles.GenerationOf(i));
            var body = _world.GetBody(handle);
            var p = body.Position;
            var v = body.LinearVelocity;
            json.BeginObject();
            json.Name("ref");
            Ref(body).WriteJson(json);
            json.Name("kind");
            json.Value(body.Type.ToString());
            json.Name("x");
            json.Raw(PhysicsLimits.FromDouble(p.X).ToString());
            json.Name("y");
            json.Raw(PhysicsLimits.FromDouble(p.Y).ToString());
            json.Name("vx");
            json.Raw(PhysicsLimits.FromSingle(v.X).ToString());
            json.Name("vy");
            json.Raw(PhysicsLimits.FromSingle(v.Y).ToString());
            json.Name("awake");
            json.Value(body.IsAwake);
            json.EndObject();
        }

        json.EndArray();
        json.EndObject();
    }

    // ---------------------------------------------------------------- helpers

    private int Bodies()
    {
        var n = 0;
        var bodies = _world.Core.Bodies;
        for (var i = 0; i < bodies.HighWater; i++)
        {
            if (bodies.Handles.IsLive(i))
            {
                n++;
            }
        }

        return n;
    }

    private Body MakeBody(FVec2 at, BodyOptions o)
    {
        var def = new BodyDef
        {
            Type = o.Kind switch { BodyKind.Static => BodyType.Static, BodyKind.Kinematic => BodyType.Kinematic, _ => BodyType.Dynamic },
            Position = Position2(at),
            Angle = PhysicsLimits.ToSingle(o.Angle, "Angle"),
            LinearVelocity = ToVector(o.Velocity, "Velocity"),
            LinearDamping = PhysicsLimits.ToSingle(o.LinearDamping, "LinearDamping"),
            AngularDamping = PhysicsLimits.ToSingle(o.AngularDamping, "AngularDamping"),
            GravityScale = PhysicsLimits.ToSingle(o.GravityScale, "GravityScale"),
            FixedRotation = o.FixedRotation,
            IsBullet = o.IsBullet,
        };
        return _world.CreateBody(in def);
    }

    private static ShapeDef ShapeFor(BodyOptions o) => new()
    {
        Density = PhysicsLimits.ToSingle(o.Density, "Density"),
        Friction = PhysicsLimits.ToSingle(o.Friction, "Friction"),
        Restitution = PhysicsLimits.ToSingle(o.Restitution, "Restitution"),
        IsSensor = o.IsTrigger,
        Layer = o.Layer,
    };

    private static BodyRef Ref(Body body) => new(body.RawHandle.Index, body.RawHandle.Generation);

    private static Vector2D Position2(FVec2 p) => new(PhysicsLimits.ToDouble(p.X, "position"), PhysicsLimits.ToDouble(p.Y, "position"));

    private static Vector2F ToVector(FVec2 v, string name) => new(PhysicsLimits.ToSingle(v.X, name), PhysicsLimits.ToSingle(v.Y, name));

    private static FVec2 FromVector(Vector2D v) => new(PhysicsLimits.FromDouble(v.X), PhysicsLimits.FromDouble(v.Y));

    private Body Resolve(BodyRef body)
    {
        if (!Exists(body))
        {
            throw new StaleBodyException($"{body} does not exist in this space (removed, never created here, or the space was restored from a save without it).");
        }

        return _world.GetBody(new BodyHandle(_world.Id, body.Index, body.Generation));
    }

    private Character Resolve(CharacterRef character)
    {
        if (!Exists(character))
        {
            throw new StaleBodyException($"{character} does not exist in this space (removed, never created here, or the space was restored from a save without it).");
        }

        return _world.GetCharacter(character.Index, character.Generation);
    }

    private sealed class StepSystem(PhysicsSpace2D space) : ISimSystem
    {
        public void Tick(World world) => space.Step();
    }
}

/// <summary>The result of <see cref="PhysicsSpace2D.Move"/>.</summary>
/// <param name="Grounded">The character ended the move standing on walkable ground.</param>
/// <param name="HitCeiling">It touched something above it.</param>
/// <param name="HitSide">It touched something to its side.</param>
/// <param name="Position">Where the capsule centre ended.</param>
public readonly record struct CharacterMove(bool Grounded, bool HitCeiling, bool HitSide, FVec2 Position);

/// <summary>What a ray met.</summary>
/// <param name="Body">The body that was hit.</param>
/// <param name="Point">Where the ray touched it.</param>
/// <param name="Normal">The surface direction there, pointing back at the ray's origin side (zero if the ray began inside).</param>
/// <param name="Distance">How far from the origin.</param>
public readonly record struct RayHit(BodyRef Body, FVec2 Point, FVec2 Normal, Fixed Distance);
