namespace OneGame.Core;

/// <summary>A two-dimensional point or vector in <see cref="Fixed"/> numbers.</summary>
public readonly struct FVec2 : ISyncValue<FVec2>, ILerp<FVec2>
{
    /// <summary>Creates a vector.</summary>
    public FVec2(Fixed x, Fixed y)
    {
        X = x;
        Y = y;
    }

    /// <summary>The zero vector.</summary>
    public static FVec2 Zero => default;

    /// <summary>Horizontal component.</summary>
    public Fixed X { get; }

    /// <summary>Vertical component.</summary>
    public Fixed Y { get; }

    /// <summary>The squared length, which avoids a square root.</summary>
    public Fixed LengthSquared => (X * X) + (Y * Y);

    /// <summary>The length.</summary>
    public Fixed Length => Fixed.Sqrt(LengthSquared);

    /// <summary>The vector scaled to length one, or zero for the zero vector.</summary>
    public FVec2 Normalized()
    {
        var len = Length;
        return len == Fixed.Zero ? Zero : new FVec2(X / len, Y / len);
    }

    /// <summary>The dot product.</summary>
    public static Fixed Dot(FVec2 a, FVec2 b) => (a.X * b.X) + (a.Y * b.Y);

    /// <summary>Adds two vectors.</summary>
    public static FVec2 operator +(FVec2 a, FVec2 b) => new(a.X + b.X, a.Y + b.Y);

    /// <summary>Subtracts two vectors.</summary>
    public static FVec2 operator -(FVec2 a, FVec2 b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>Negates a vector.</summary>
    public static FVec2 operator -(FVec2 a) => new(-a.X, -a.Y);

    /// <summary>Scales a vector.</summary>
    public static FVec2 operator *(FVec2 a, Fixed s) => new(a.X * s, a.Y * s);

    /// <summary>Scales a vector.</summary>
    public static FVec2 operator *(Fixed s, FVec2 a) => new(a.X * s, a.Y * s);

    /// <summary>Equality of both components.</summary>
    public static bool operator ==(FVec2 a, FVec2 b) => a.X == b.X && a.Y == b.Y;

    /// <summary>Inequality of either component.</summary>
    public static bool operator !=(FVec2 a, FVec2 b) => !(a == b);

    /// <summary>Blends from <paramref name="a"/> (at 0) to <paramref name="b"/> (at 1).</summary>
    public static FVec2 Lerp(FVec2 a, FVec2 b, Fixed t) => new(Fixed.Lerp(a.X, b.X, t), Fixed.Lerp(a.Y, b.Y, t));

    /// <inheritdoc />
    public bool Equals(FVec2 other) => this == other;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is FVec2 v && this == v;

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <inheritdoc />
    public void Write(SyncWriter writer)
    {
        X.Write(writer);
        Y.Write(writer);
    }

    /// <inheritdoc />
    public static FVec2 Read(ref SyncReader reader)
    {
        var x = Fixed.Read(ref reader);
        var y = Fixed.Read(ref reader);
        return new FVec2(x, y);
    }

    /// <inheritdoc />
    public void WriteJson(JsonOut json)
    {
        json.BeginArray();
        X.WriteJson(json);
        Y.WriteJson(json);
        json.EndArray();
    }

    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y})";
}
