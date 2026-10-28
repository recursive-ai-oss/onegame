namespace OneGame.Physics;

/// <summary>How a body takes part in the simulation.</summary>
public enum BodyKind : byte
{
    /// <summary>Never moves: ground, walls, platforms.</summary>
    Static = 0,

    /// <summary>Moves only as the game tells it (by velocity or teleport); pushes dynamic bodies and is never pushed.</summary>
    Kinematic = 1,

    /// <summary>Moves under gravity, forces and contacts.</summary>
    Dynamic = 2,
}

/// <summary>
/// The settings of a <see cref="PhysicsSpace2D"/>. They are fixed when the space is created, and they must be the same on every node:
/// build the space the same way everywhere (the same code in <c>Setup</c>), as for any other level data.
/// </summary>
public sealed class PhysicsOptions
{
    /// <summary>The table's name in snapshots; unique within a world. Default <c>physics</c>.</summary>
    public string Name { get; set; } = "physics";

    /// <summary>Gravity in world units per second squared. Default (0, -9.81), so one world unit is one metre; scale the world units the same way for the view.</summary>
    public FVec2 Gravity { get; set; } = new(Fixed.Zero, Fixed.FromRatio(-981, 100));

    /// <summary>The sim's ticks per second; one tick is one physics step of 1/TicksPerSecond seconds. Default 60. Changing it changes every result.</summary>
    public int TicksPerSecond { get; set; } = 60;

    /// <summary>Solver substeps per step (more is steadier stacking and costs more). Default 4.</summary>
    public int Substeps { get; set; } = 4;

    /// <summary>Whether resting bodies fall asleep (cheaper; any contact or touch wakes them). Default true.</summary>
    public bool Sleep { get; set; } = true;

    /// <summary>Whether <see cref="PhysicsEventKind.ContactHit"/> events are recorded: contacts that approach faster than one unit per second. Default false.</summary>
    public bool HitEvents { get; set; }

    /// <summary>Whether <see cref="PhysicsEventKind.ContactStay"/> events are recorded (one per touching contact per step). Default false.</summary>
    public bool StayEvents { get; set; }
}

/// <summary>What a new body is made of and how it behaves. Everything has a usable default: a dynamic body of density 1, friction 0.6, no bounce.</summary>
public sealed class BodyOptions
{
    /// <summary>Static, kinematic or dynamic. Default dynamic.</summary>
    public BodyKind Kind { get; set; } = BodyKind.Dynamic;

    /// <summary>Starting rotation, in radians, counter-clockwise.</summary>
    public Fixed Angle { get; set; }

    /// <summary>Mass per area. Zero gives a body that has unit mass whatever its size. Default 1.</summary>
    public Fixed Density { get; set; } = Fixed.One;

    /// <summary>Sliding grip, 0 (ice) and up. Contacts use the geometric mean of the two bodies. Default 0.6.</summary>
    public Fixed Friction { get; set; } = Fixed.FromRatio(6, 10);

    /// <summary>Bounciness, 0 (none) to 1 (perfect). Default 0.</summary>
    public Fixed Restitution { get; set; }

    /// <summary>Which of the 32 layers the body is on; see <see cref="PhysicsSpace2D.SetLayerCollision"/>. Default 0.</summary>
    public int Layer { get; set; }

    /// <summary>A trigger detects overlaps (<see cref="PhysicsEventKind.TriggerEnter"/>/<see cref="PhysicsEventKind.TriggerExit"/>) and has no contact response: things pass through it.</summary>
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
    public FVec2 Velocity { get; set; }
}

/// <summary>The shape and abilities of a character: an upright capsule that is moved by collide-and-slide and not by forces.</summary>
public sealed class CharacterOptions
{
    /// <summary>Capsule radius. Default 0.3.</summary>
    public Fixed Radius { get; set; } = Fixed.FromRatio(3, 10);

    /// <summary>Total height of the capsule, rounded ends included. Default 1.8.</summary>
    public Fixed Height { get; set; } = Fixed.FromRatio(18, 10);

    /// <summary>Steepest slope it can stand on, in degrees from flat. Default 45.</summary>
    public Fixed SlopeLimit { get; set; } = Fixed.FromInt(45);

    /// <summary>Tallest step it climbs without jumping. Default 0.3.</summary>
    public Fixed StepOffset { get; set; } = Fixed.FromRatio(3, 10);

    /// <summary>How hard it pushes dynamic bodies it walks into (0 does not push them). Default 1.</summary>
    public Fixed PushStrength { get; set; } = Fixed.One;

    /// <summary>The layer of its capsule; see <see cref="PhysicsSpace2D.SetLayerCollision"/>. Default 0.</summary>
    public int Layer { get; set; }
}
