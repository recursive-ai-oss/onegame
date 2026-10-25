namespace OneGame.Core;

/// <summary>A three-dimensional point or vector in <see cref="Fixed"/> numbers. +X is right, +Y is up and -Z is forward, as in the 3D renderer.</summary>
public readonly struct FVec3 : ISyncValue<FVec3>, ILerp<FVec3>
{
    /// <summary>Creates a vector.</summary>
    public FVec3(Fixed x, Fixed y, Fixed z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>The zero vector.</summary>
    public static FVec3 Zero => default;

    /// <summary>All components one.</summary>
    public static FVec3 One => new(Fixed.One, Fixed.One, Fixed.One);

    /// <summary>The +X axis.</summary>
    public static FVec3 UnitX => new(Fixed.One, Fixed.Zero, Fixed.Zero);

    /// <summary>The +Y axis (up).</summary>
    public static FVec3 UnitY => new(Fixed.Zero, Fixed.One, Fixed.Zero);

    /// <summary>The +Z axis (towards the viewer of an unrotated camera).</summary>
    public static FVec3 UnitZ => new(Fixed.Zero, Fixed.Zero, Fixed.One);

    /// <summary>Horizontal component.</summary>
    public Fixed X { get; }

    /// <summary>Vertical component.</summary>
    public Fixed Y { get; }

    /// <summary>Depth component.</summary>
    public Fixed Z { get; }

    /// <summary>The squared length, which avoids a square root.</summary>
    public Fixed LengthSquared => (X * X) + (Y * Y) + (Z * Z);

    /// <summary>The length.</summary>
    public Fixed Length => Fixed.Sqrt(LengthSquared);

    /// <summary>The vector scaled to length one, or zero for the zero vector.</summary>
    public FVec3 Normalized()
    {
        var len = Length;
        return len == Fixed.Zero ? Zero : new FVec3(X / len, Y / len, Z / len);
    }

    /// <summary>The dot product.</summary>
    public static Fixed Dot(FVec3 a, FVec3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

    /// <summary>The cross product (right-handed).</summary>
    public static FVec3 Cross(FVec3 a, FVec3 b) => new((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

    /// <summary>The distance between two points.</summary>
    public static Fixed Distance(FVec3 a, FVec3 b) => (a - b).Length;

    /// <summary>Adds two vectors.</summary>
    public static FVec3 operator +(FVec3 a, FVec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>Subtracts two vectors.</summary>
    public static FVec3 operator -(FVec3 a, FVec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>Negates a vector.</summary>
    public static FVec3 operator -(FVec3 a) => new(-a.X, -a.Y, -a.Z);

    /// <summary>Scales a vector.</summary>
    public static FVec3 operator *(FVec3 a, Fixed s) => new(a.X * s, a.Y * s, a.Z * s);

    /// <summary>Scales a vector.</summary>
    public static FVec3 operator *(Fixed s, FVec3 a) => new(a.X * s, a.Y * s, a.Z * s);

    /// <summary>Multiplies component by component.</summary>
    public static FVec3 operator *(FVec3 a, FVec3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    /// <summary>Equality of all components.</summary>
    public static bool operator ==(FVec3 a, FVec3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;

    /// <summary>Inequality of any component.</summary>
    public static bool operator !=(FVec3 a, FVec3 b) => !(a == b);

    /// <summary>Blends from <paramref name="a"/> (at 0) to <paramref name="b"/> (at 1).</summary>
    public static FVec3 Lerp(FVec3 a, FVec3 b, Fixed t) => new(Fixed.Lerp(a.X, b.X, t), Fixed.Lerp(a.Y, b.Y, t), Fixed.Lerp(a.Z, b.Z, t));

    /// <inheritdoc />
    public bool Equals(FVec3 other) => this == other;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is FVec3 v && this == v;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        X.Write(writer);
        Y.Write(writer);
        Z.Write(writer);
    }

    /// <inheritdoc />
    public static FVec3 Read(ref SyncReader reader)
    {
        var x = Fixed.Read(ref reader);
        var y = Fixed.Read(ref reader);
        var z = Fixed.Read(ref reader);
        return new FVec3(x, y, z);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        json.BeginArray();
        X.WriteJson(json);
        Y.WriteJson(json);
        Z.WriteJson(json);
        json.EndArray();
    }

    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y}, {Z})";
}
