namespace OneGame.Core;

/// <summary>
/// A rotation as a quaternion in <see cref="Fixed"/> numbers. The scalar part is stored relative to one, so <c>default</c> is the identity
/// rotation and a freshly created synced property is already valid. Rotations compose with the <c>*</c> operator: <c>a * b</c> applies <c>b</c> first.
/// </summary>
public readonly struct FQuat : ISyncValue<FQuat>, ILerp<FQuat>
{
    private readonly Fixed _wMinusOne;

    /// <summary>Creates a quaternion from its components (not normalized).</summary>
    public FQuat(Fixed x, Fixed y, Fixed z, Fixed w)
    {
        X = x;
        Y = y;
        Z = z;
        _wMinusOne = w - Fixed.One;
    }

    /// <summary>No rotation.</summary>
    public static FQuat Identity => new(Fixed.Zero, Fixed.Zero, Fixed.Zero, Fixed.One);

    /// <summary>The X component of the axis part.</summary>
    public Fixed X { get; }

    /// <summary>The Y component of the axis part.</summary>
    public Fixed Y { get; }

    /// <summary>The Z component of the axis part.</summary>
    public Fixed Z { get; }

    /// <summary>The scalar part.</summary>
    public Fixed W => _wMinusOne + Fixed.One;

    /// <summary>A rotation of <paramref name="radians"/> about <paramref name="axis"/> (right-handed; the axis need not be normalized).</summary>
    public static FQuat FromAxisAngle(FVec3 axis, Fixed radians)
    {
        var n = axis.Normalized();
        var half = radians / 2;
        var s = Fixed.Sin(half);
        return new FQuat(n.X * s, n.Y * s, n.Z * s, Fixed.Cos(half));
    }

    /// <summary>A turn about +Y (positive turns anticlockwise seen from above), then about the turned X axis, then about Z.</summary>
    public static FQuat FromYawPitchRoll(Fixed yaw, Fixed pitch, Fixed roll)
        => FromAxisAngle(FVec3.UnitY, yaw) * FromAxisAngle(FVec3.UnitX, pitch) * FromAxisAngle(FVec3.UnitZ, roll);

    /// <summary>The rotation that turns -Z to look along <paramref name="forward"/> with +Y as close to up as possible.</summary>
    public static FQuat LookRotation(FVec3 forward, FVec3 up)
    {
        var f = forward.Normalized();
        if (f == FVec3.Zero)
        {
            return Identity;
        }

        var back = -f;
        var right = FVec3.Cross(up, back).Normalized();
        if (right == FVec3.Zero)
        {
            // Looking straight along the up axis: any perpendicular will do.
            right = FVec3.UnitX;
        }

        var u = FVec3.Cross(back, right);
        return FromBasis(right, u, back);
    }

    /// <summary>The quaternion for the rotation matrix with the given columns (right, up, back).</summary>
    private static FQuat FromBasis(FVec3 r, FVec3 u, FVec3 b)
    {
        // Shepperd's method: pick the largest diagonal term so the square root stays well conditioned.
        var m00 = r.X;
        var m11 = u.Y;
        var m22 = b.Z;
        var trace = m00 + m11 + m22;
        Fixed x, y, z, w;
        if (trace > Fixed.Zero)
        {
            var s = Fixed.Sqrt(trace + Fixed.One) * 2;
            w = s / 4;
            x = (u.Z - b.Y) / s;
            y = (b.X - r.Z) / s;
            z = (r.Y - u.X) / s;
        }
        else if (m00 >= m11 && m00 >= m22)
        {
            var s = Fixed.Sqrt(Fixed.One + m00 - m11 - m22) * 2;
            w = (u.Z - b.Y) / s;
            x = s / 4;
            y = (u.X + r.Y) / s;
            z = (b.X + r.Z) / s;
        }
        else if (m11 >= m22)
        {
            var s = Fixed.Sqrt(Fixed.One + m11 - m00 - m22) * 2;
            w = (b.X - r.Z) / s;
            x = (u.X + r.Y) / s;
            y = s / 4;
            z = (b.Y + u.Z) / s;
        }
        else
        {
            var s = Fixed.Sqrt(Fixed.One + m22 - m00 - m11) * 2;
            w = (r.Y - u.X) / s;
            x = (b.X + r.Z) / s;
            y = (b.Y + u.Z) / s;
            z = s / 4;
        }

        return new FQuat(x, y, z, w).Normalized();
    }

    /// <summary>The same rotation scaled to unit length; the identity for the zero quaternion.</summary>
    public FQuat Normalized()
    {
        var lenSq = (X * X) + (Y * Y) + (Z * Z) + (W * W);
        if (lenSq == Fixed.Zero)
        {
            return Identity;
        }

        var len = Fixed.Sqrt(lenSq);
        return new FQuat(X / len, Y / len, Z / len, W / len);
    }

    /// <summary>The opposite rotation (of a unit quaternion).</summary>
    public FQuat Inverse() => new(-X, -Y, -Z, W);

    /// <summary>Composes two rotations: the result applies <paramref name="b"/> first, then <paramref name="a"/>.</summary>
    public static FQuat operator *(FQuat a, FQuat b)
    {
        return new FQuat(
            (a.W * b.X) + (a.X * b.W) + (a.Y * b.Z) - (a.Z * b.Y),
            (a.W * b.Y) - (a.X * b.Z) + (a.Y * b.W) + (a.Z * b.X),
            (a.W * b.Z) + (a.X * b.Y) - (a.Y * b.X) + (a.Z * b.W),
            (a.W * b.W) - (a.X * b.X) - (a.Y * b.Y) - (a.Z * b.Z));
    }

    /// <summary>Turns a vector by this rotation (which should be normalized).</summary>
    public FVec3 Rotate(FVec3 v)
    {
        var q = new FVec3(X, Y, Z);
        var t = FVec3.Cross(q, v) * 2;
        return v + (t * W) + FVec3.Cross(q, t);
    }

    /// <summary>Blends from <paramref name="a"/> (at 0) to <paramref name="b"/> (at 1) along the shorter way, and normalizes.</summary>
    public static FQuat Lerp(FQuat a, FQuat b, Fixed t)
    {
        var dot = (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z) + (a.W * b.W);
        var sign = dot < Fixed.Zero ? -Fixed.One : Fixed.One;
        var inv = Fixed.One - t;
        return new FQuat(
            (a.X * inv) + (b.X * t * sign), (a.Y * inv) + (b.Y * t * sign), (a.Z * inv) + (b.Z * t * sign), (a.W * inv) + (b.W * t * sign)).Normalized();
    }

    /// <summary>Equality of all components.</summary>
    public static bool operator ==(FQuat a, FQuat b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z && a._wMinusOne == b._wMinusOne;

    /// <summary>Inequality of any component.</summary>
    public static bool operator !=(FQuat a, FQuat b) => !(a == b);

    /// <inheritdoc />
    public bool Equals(FQuat other) => this == other;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is FQuat q && this == q;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        X.Write(writer);
        Y.Write(writer);
        Z.Write(writer);
        _wMinusOne.Write(writer);
    }

    /// <inheritdoc />
    public static FQuat Read(ref SyncReader reader)
    {
        var x = Fixed.Read(ref reader);
        var y = Fixed.Read(ref reader);
        var z = Fixed.Read(ref reader);
        var w = Fixed.Read(ref reader);
        return new FQuat(x, y, z, w + Fixed.One);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        json.BeginArray();
        X.WriteJson(json);
        Y.WriteJson(json);
        Z.WriteJson(json);
        W.WriteJson(json);
        json.EndArray();
    }

    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y}, {Z}, {W})";
}
