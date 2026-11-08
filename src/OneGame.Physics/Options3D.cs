namespace OneGame.Physics;

/// <summary>Fixed tick settings of a 3D physics space; use identical settings on every node.</summary>
public sealed class PhysicsOptions3D
{
    /// <summary>The table's name in snapshots; unique within a world. Default <c>physics3d</c>.</summary>
    public string Name { get; set; } = "physics3d";

    /// <summary>Gravity in world units per second squared. Default (0, -9.81, 0), so one world unit is one metre; scale the world units the same way for the view.</summary>
    public FVec3 Gravity { get; set; } = new(Fixed.Zero, Fixed.FromRatio(-981, 100), Fixed.Zero);

    /// <summary>The sim's ticks per second; one tick is one physics step of 1/TicksPerSecond seconds. Default 60. Changing it changes every result.</summary>
    public int TicksPerSecond { get; set; } = 60;

    /// <summary>Solver substeps per step (more is steadier stacking and costs more). Default 4.</summary>
    public int Substeps { get; set; } = 4;

    /// <summary>Whether resting bodies fall asleep (cheaper; any contact or touch wakes them). Default true.</summary>
    public bool Sleep { get; set; } = true;

    /// <summary>Whether <see cref="PhysicsEventKind3D.ContactHit"/> events are recorded: contacts that approach faster than one unit per second. Default false.</summary>
    public bool HitEvents { get; set; }

    /// <summary>Whether <see cref="PhysicsEventKind3D.ContactStay"/> events are recorded (one per touching contact per step). Default false.</summary>
    public bool StayEvents { get; set; }

    /// <summary>Room for this many touching shape pairs, reserved up front so a step never grows its tables. 0 sizes them from the body count when <see cref="PhysicsSpace3D.Warmup"/> runs. Never changes a result.</summary>
    public int PairCapacity { get; set; }

    /// <summary>Room for this many contacts (see <see cref="PairCapacity"/>). 0 sizes from the body count.</summary>
    public int ContactCapacity { get; set; }

    /// <summary>Room for this many islands of touching bodies (see <see cref="PairCapacity"/>). 0 sizes from the body count.</summary>
    public int IslandCapacity { get; set; }

    /// <summary>Hits a ray cast or overlap can return before its scratch grows. 0 keeps the library's default of 64.</summary>
    public int QueryScratchCapacity { get; set; }
}

/// <summary>What a new body is made of and how it behaves. Everything has a usable default: a dynamic body of density 1, friction 0.6, no bounce.</summary>
public sealed class BodyOptions3D
{
    /// <summary>Static, kinematic or dynamic. Default dynamic.</summary>
    public BodyKind Kind { get; set; } = BodyKind.Dynamic;

    /// <summary>Starting orientation.</summary>
    public FQuat Rotation { get; set; }

    /// <summary>Mass per volume. Zero gives a body that has unit mass whatever its size. Default 1.</summary>
    public Fixed Density { get; set; } = Fixed.One;

    /// <summary>Sliding grip, 0 (ice) and up. Contacts use the geometric mean of the two bodies. Default 0.6.</summary>
    public Fixed Friction { get; set; } = Fixed.FromRatio(6, 10);

    /// <summary>Bounciness, 0 (none) to 1 (perfect). Default 0.</summary>
    public Fixed Restitution { get; set; }

    /// <summary>Which of the 32 layers the body is on; see <see cref="PhysicsSpace3D.SetLayerCollision"/>. Default 0.</summary>
    public int Layer { get; set; }

    /// <summary>A trigger detects overlaps (<see cref="PhysicsEventKind3D.TriggerEnter"/>/<see cref="PhysicsEventKind3D.TriggerExit"/>) and has no contact response: things pass through it.</summary>
    public bool IsTrigger { get; set; }

    /// <summary>Whether the body never rotates (upright things).</summary>
    public bool FixedRotation { get; set; }

    /// <summary>Whether fast small bodies are swept against other moving bodies too, so they cannot tunnel through them.</summary>
    public bool IsBullet { get; set; }

    /// <summary>Slows the body's linear motion by this fraction per second. Default 0.</summary>
    public Fixed LinearDamping { get; set; }

    /// <summary>Slows the body's rotation by this fraction per second. Default 0.</summary>
    public Fixed AngularDamping { get; set; }

    /// <summary>Multiplies gravity for this body. Default 1.</summary>
    public Fixed GravityScale { get; set; } = Fixed.One;

    /// <summary>Starting velocity.</summary>
    public FVec3 Velocity { get; set; }
}

