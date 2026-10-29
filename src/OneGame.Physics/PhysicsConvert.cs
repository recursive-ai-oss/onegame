// Everything here gives the same result on every machine: the physics library uses only exact IEEE operations, positions cross the
// boundary as exact doubles, and other values are rounded to single precision by the same IEEE rule everywhere. Sim code may call all of it.
[assembly: OneGame.Core.SimSafe]

namespace OneGame.Physics;

/// <summary>The limits of the fixed-point to physics conversion, and the checked conversions themselves.</summary>
public static class PhysicsLimits
{
    /// <summary>Largest absolute coordinate or length, in world units: 2^21. Inside it a <see cref="Fixed"/> (32 fraction bits) is exactly a double (53 bits), which is what the physics world needs to treat positions identically everywhere.</summary>
    public const int MaxCoordinate = 1 << 21;

    /// <summary>The same limit as a raw <see cref="Fixed"/> value.</summary>
    public const long MaxCoordinateRaw = (long)MaxCoordinate << Fixed.FractionBits;

    /// <summary>Throws <see cref="PhysicsRangeException"/> unless <paramref name="value"/> is within ±<see cref="MaxCoordinate"/>.</summary>
    /// <param name="value">The coordinate or length.</param>
    /// <param name="paramName">Name reported in the exception.</param>
    /// <exception cref="PhysicsRangeException">The value is out of range.</exception>
    public static void Check(Fixed value, string paramName)
    {
        var raw = value.Raw;
        if (raw < -MaxCoordinateRaw || raw > MaxCoordinateRaw)
        {
            throw new PhysicsRangeException(paramName, value);
        }
    }

    /// <summary>The value as a double, exactly. Throws when it is outside the range.</summary>
    /// <param name="value">The value.</param>
    /// <param name="paramName">Name reported in the exception.</param>
    /// <exception cref="PhysicsRangeException">The value is out of range.</exception>
    public static double ToDouble(Fixed value, string paramName = "value")
    {
        Check(value, paramName);
        return value.Raw / (double)(1L << Fixed.FractionBits);
    }

    /// <summary>The value as a single-precision float, rounded to nearest even (the physics world's own precision). Throws when it is outside the range.</summary>
    /// <param name="value">The value.</param>
    /// <param name="paramName">Name reported in the exception.</param>
    /// <exception cref="PhysicsRangeException">The value is out of range.</exception>
    public static float ToSingle(Fixed value, string paramName = "value") => (float)ToDouble(value, paramName);

    /// <summary>A double as the nearest <see cref="Fixed"/>, ties to even. Throws when it is not finite or is outside the range.</summary>
    /// <param name="value">The value.</param>
    /// <exception cref="PhysicsRangeException">The value is out of range.</exception>
    public static Fixed FromDouble(double value)
    {
        // 2^21 * 2^32 = 2^53: the scaled value is an exact integer-valued double inside the long range.
        if (!double.IsFinite(value) || Math.Abs(value) > MaxCoordinate)
        {
            throw new PhysicsRangeException(nameof(value), value);
        }

        return Fixed.FromRaw((long)Math.Round(value * (1L << Fixed.FractionBits), MidpointRounding.ToEven));
    }

    internal static Fixed FromSingle(float value) => FromDouble(value);
}

/// <summary>A value was outside what the physics world can convert exactly (±2^21 world units), or was not finite.</summary>
public sealed class PhysicsRangeException : ArgumentOutOfRangeException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="paramName">The offending argument.</param>
    /// <param name="value">Its value.</param>
    public PhysicsRangeException(string paramName, Fixed value)
        : base(paramName, value.ToString(), $"World value {value} is outside the physics range of ±{PhysicsLimits.MaxCoordinate} units. Keep positions and sizes inside it (move far-away content into a separate space).")
    {
    }

    internal PhysicsRangeException(string paramName, double value)
        : base(paramName, null, "The physics world produced a value outside ±" + PhysicsLimits.MaxCoordinate.ToString(System.Globalization.CultureInfo.InvariantCulture) + " units or not finite (a body left the world).")
    {
    }
}

/// <summary>A body or character reference no longer names anything in the space (it was removed, or the space was restored from a save that does not have it).</summary>
public sealed class StaleBodyException : InvalidOperationException
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">What happened.</param>
    public StaleBodyException(string message)
        : base(message)
    {
    }
}
