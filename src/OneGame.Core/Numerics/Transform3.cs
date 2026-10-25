namespace OneGame.Core;

/// <summary>
/// Where something is in a 3D world: a position, a rotation and a scale, all in <see cref="Fixed"/> numbers so they can be synced and
/// simulated deterministically. Declare a synced property of this type (or <c>Timed&lt;Transform3&gt;</c> for a move that is interpolated
/// in views). The <c>default</c> value is the identity transform: the zero rotation means none and a zero scale means one.
/// </summary>
public readonly struct Transform3 : ISyncValue<Transform3>, ILerp<Transform3>
{
    private readonly FVec3 _scaleMinusOne;

    /// <summary>Creates a transform.</summary>
    public Transform3(FVec3 position, FQuat rotation, FVec3 scale)
    {
        Position = position;
        Rotation = rotation;
        _scaleMinusOne = scale - FVec3.One;
    }

    /// <summary>No move, no rotation, scale one.</summary>
    public static Transform3 Identity => default;

    /// <summary>A transform that only moves.</summary>
    public static Transform3 At(FVec3 position) => new(position, FQuat.Identity, FVec3.One);

    /// <summary>A transform that moves and rotates.</summary>
    public static Transform3 At(FVec3 position, FQuat rotation) => new(position, rotation, FVec3.One);

    /// <summary>Where the origin of the thing is.</summary>
    public FVec3 Position { get; }

    /// <summary>How it is turned; the zero value means no rotation.</summary>
    public FQuat Rotation { get; }

    /// <summary>Its size on each axis. (Stored relative to one so that <c>default</c> is the identity.)</summary>
    public FVec3 Scale => _scaleMinusOne + FVec3.One;

    /// <summary>The direction it faces (-Z turned by <see cref="Rotation"/>).</summary>
    public FVec3 Forward => Rotation.Rotate(-FVec3.UnitZ);

    /// <summary>Its right (+X turned by <see cref="Rotation"/>).</summary>
    public FVec3 Right => Rotation.Rotate(FVec3.UnitX);

    /// <summary>Its up (+Y turned by <see cref="Rotation"/>).</summary>
    public FVec3 Up => Rotation.Rotate(FVec3.UnitY);

    /// <summary>The same transform at another position.</summary>
    public Transform3 WithPosition(FVec3 position) => new(position, Rotation, Scale);

    /// <summary>The same transform turned differently.</summary>
    public Transform3 WithRotation(FQuat rotation) => new(Position, rotation, Scale);

    /// <summary>The same transform at another scale.</summary>
    public Transform3 WithScale(FVec3 scale) => new(Position, Rotation, scale);

    /// <summary>The same transform moved by <paramref name="delta"/> in world space.</summary>
    public Transform3 Translated(FVec3 delta) => new(Position + delta, Rotation, Scale);

    /// <summary>Takes a point from this thing's own space to the world.</summary>
    public FVec3 TransformPoint(FVec3 local) => Position + Rotation.Rotate(local * Scale);

    /// <summary>A transform at <paramref name="eye"/> that faces <paramref name="target"/>, with +Y as close to up as possible.</summary>
    public static Transform3 LookAt(FVec3 eye, FVec3 target) => At(eye, FQuat.LookRotation(target - eye, FVec3.UnitY));

    /// <summary>Blends position and scale linearly and rotation along the shorter arc.</summary>
    public static Transform3 Lerp(Transform3 a, Transform3 b, Fixed t)
        => new(FVec3.Lerp(a.Position, b.Position, t), FQuat.Lerp(a.Rotation, b.Rotation, t), FVec3.Lerp(a.Scale, b.Scale, t));

    /// <summary>Equality of all parts.</summary>
    public static bool operator ==(Transform3 a, Transform3 b) => a.Position == b.Position && a.Rotation == b.Rotation && a._scaleMinusOne == b._scaleMinusOne;

    /// <summary>Inequality of any part.</summary>
    public static bool operator !=(Transform3 a, Transform3 b) => !(a == b);

    /// <inheritdoc />
    public bool Equals(Transform3 other) => this == other;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Transform3 t && this == t;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Position, Rotation, _scaleMinusOne);

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        Position.Write(writer);
        Rotation.Write(writer);
        _scaleMinusOne.Write(writer);
    }

    /// <inheritdoc />
    public static Transform3 Read(ref SyncReader reader)
    {
        var p = FVec3.Read(ref reader);
        var r = FQuat.Read(ref reader);
        var s = FVec3.Read(ref reader);
        return new Transform3(p, r, s + FVec3.One);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        json.BeginObject();
        json.Name("position");
        Position.WriteJson(json);
        json.Name("rotation");
        Rotation.WriteJson(json);
        json.Name("scale");
        Scale.WriteJson(json);
        json.EndObject();
    }

    /// <inheritdoc />
    public override string ToString() => $"{{pos {Position}, rot {Rotation}, scale {Scale}}}";
}
