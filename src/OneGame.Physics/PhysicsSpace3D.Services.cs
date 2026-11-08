using RecursiveAI.OnePhysics;
using RecursiveAI.OnePhysics.Physics3D;

namespace OneGame.Physics;

/// <summary>Result of a 3D character displacement.</summary>
/// <param name="Position">Local position after the move.</param>
/// <param name="Grounded">Touches walkable ground.</param>
/// <param name="HitCeiling">Upward movement was blocked.</param>
/// <param name="HitSide">Sideways movement was blocked.</param>
public readonly record struct CharacterMove3D(FVec3 Position, bool Grounded, bool HitCeiling, bool HitSide);

/// <summary>One wheel's view and traction state, in local coordinates.</summary>
/// <param name="Center">Wheel centre.</param>
/// <param name="Radius">Wheel radius.</param>
/// <param name="Grounded">Whether the suspension cast touches ground.</param>
/// <param name="ContactPoint">Ground contact point.</param>
/// <param name="SuspensionLength">Current suspension length.</param>
/// <param name="SpinAngle">Wheel spin in radians.</param>
/// <param name="SteerAngle">Wheel steering in radians.</param>
public readonly record struct PhysicsWheel3D(FVec3 Center, Fixed Radius, bool Grounded, FVec3 ContactPoint, Fixed SuspensionLength, Fixed SpinAngle, Fixed SteerAngle);

/// <summary>A nearest 3D query hit.</summary>
/// <param name="Body">Body hit.</param>
/// <param name="Point">Local hit point.</param>
/// <param name="Normal">Surface normal.</param>
/// <param name="Distance">Distance along the cast.</param>
public readonly record struct RayHit3D(BodyRef Body, FVec3 Point, FVec3 Normal, Fixed Distance);

public sealed partial class PhysicsSpace3D
{
    /// <summary>Adds an upright character; the position is its capsule centre. SlopeLimit is in degrees, as in 2D.</summary>
    [MutatesState]
    public CharacterRef AddCharacter(FVec3 at, CharacterOptions? options = null)
    {
        var o = options ?? new();
        var def = new CharacterDef
        {
            Radius = Single(o.Radius), Height = Single(o.Height), StepOffset = Single(o.StepOffset),
            SlopeLimitRadians = (float)(Double(o.SlopeLimit) * (Math.PI / 180)),
            PushStrength = Single(o.PushStrength), Layer = o.Layer,
        };
        var c = _world.AddCharacter(Point(at), def);
        return new(c.RawHandle.Index, c.RawHandle.Generation);
    }
    private Character Character(CharacterRef reference)
    {
        var handle = reference.IsNull ? default : new CharacterHandle(WorldId, reference.Index, reference.Generation);
        if (!_world.IsCharacterAlive(handle)) throw new StaleBodyException($"{reference} no longer exists in {Name}.");
        return _world.GetCharacter(handle);
    }
    /// <summary>Local character position.</summary>
    public FVec3 Position(CharacterRef character) => From(Character(character).Position);
    /// <summary>Whether a character touches walkable ground.</summary>
    public bool Grounded(CharacterRef character) => Character(character).IsGrounded;
    /// <summary>Moves a character by displacement over one fixed tick.</summary>
    [MutatesState]
    public CharacterMove3D Move(CharacterRef character, FVec3 displacement)
    {
        var c = Character(character); var contact = c.Move(Vector(displacement), _dt);
        return new(From(c.Position), c.IsGrounded, (contact & CharacterContact.Above) != 0, (contact & CharacterContact.Sides) != 0);
    }
    /// <summary>Teleports a character to a local position.</summary>
    [MutatesState]
    public void Teleport(CharacterRef character, FVec3 at) { var c = Character(character); c.Position = Point(at); }
    /// <summary>Removes a character and its capsule.</summary>
    [MutatesState]
    public void Remove(CharacterRef character) => Character(character).Destroy();

    /// <summary>Attaches the standard four-wheel car suspension and drivetrain to a dynamic chassis. Chassis size should be roughly (2, 0.5, 4), forward +Z.</summary>
    [MutatesState]
    public VehicleRef AddVehicle(BodyRef chassis)
    {
        var v = _world.AddVehicle(Body(chassis), VehicleDef.Car);
        return new(v.RawHandle.Index, v.RawHandle.Generation);
    }
    private Vehicle Vehicle(VehicleRef reference)
    {
        var handle = reference.IsNull ? default : new VehicleHandle(WorldId, reference.Index, reference.Generation);
        if (!_world.IsVehicleAlive(handle)) throw new StaleBodyException($"{reference} no longer exists in {Name}.");
        return _world.GetVehicle(handle);
    }
    /// <summary>Sets throttle (-1..1), steering (-1..1), brake (0..1) and handbrake (0..1).</summary>
    [MutatesState]
    public void Drive(VehicleRef vehicle, Fixed throttle, Fixed steering = default, Fixed brake = default, Fixed handbrake = default)
        => Vehicle(vehicle).Drive(Single(throttle), Single(steering), Single(brake), Single(handbrake));
    /// <summary>Number of wheels attached to a vehicle.</summary>
    public int WheelCount(VehicleRef vehicle) => Vehicle(vehicle).WheelCount;
    /// <summary>Current state of a wheel (zero-based).</summary>
    public PhysicsWheel3D Wheel(VehicleRef vehicle, int index)
    {
        var w = Vehicle(vehicle).GetWheel(index);
        return new(From(w.Center), From(w.Radius), w.IsInContact, From(w.ContactPoint), From(w.SuspensionLength), From(w.SpinAngle), From(w.SteerAngle));
    }
    /// <summary>Removes a vehicle's suspension and drivetrain, retaining its chassis body.</summary>
    [MutatesState]
    public void Remove(VehicleRef vehicle) => Vehicle(vehicle).Destroy();

    /// <summary>Casts a ray and returns the nearest matching body.</summary>
    public bool Raycast(FVec3 origin, FVec3 direction, Fixed maxDistance, out RayHit3D hit, uint layers = uint.MaxValue)
    {
        var filter = QueryFilter3D.Layers(layers);
        var found = _world.Raycast(Point(origin), Vector(direction), out var q, Single(maxDistance), filter);
        hit = found ? Hit(q) : default; return found;
    }
    /// <summary>Sweeps a sphere and returns the nearest matching body.</summary>
    public bool SphereCast(FVec3 origin, Fixed radius, FVec3 direction, Fixed maxDistance, out RayHit3D hit, uint layers = uint.MaxValue)
    {
        var filter = QueryFilter3D.Layers(layers);
        var found = _world.SphereCast(Point(origin), Single(radius), Vector(direction), out var q, Single(maxDistance), filter);
        hit = found ? Hit(q) : default; return found;
    }
    /// <summary>Finds a matching body overlapping a sphere.</summary>
    public bool OverlapSphere(FVec3 center, Fixed radius, out RayHit3D hit, uint layers = uint.MaxValue)
    {
        var filter = QueryFilter3D.Layers(layers);
        var found = _world.OverlapSphere(Point(center), Single(radius), out var q, filter);
        hit = found ? Hit(q) : default; return found;
    }
    private static RayHit3D Hit(QueryHit3D q) => new(Ref(q.Body.RawHandle), From(q.Point), From(q.Normal), From(q.Distance));
}
