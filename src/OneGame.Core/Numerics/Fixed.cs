using System.Globalization;
using System.Numerics;
using System.Text;

namespace OneGame.Core;

/// <summary>
/// Deterministic fixed-point number: a signed 64-bit integer holding the value times 2^32 (31 integer bits and 32 fractional bits). Every operation is integer
/// arithmetic, so results are bit-identical on every platform and runtime. Arithmetic wraps on overflow, like <c>unchecked</c>
/// integers. Use it for everything in synced state and in sim code; floats are for views only.
/// </summary>
public readonly struct Fixed : ISyncValue<Fixed>, ILerp<Fixed>, IComparable<Fixed>
{
    /// <summary>Number of fractional bits.</summary>
    public const int FractionBits = 32;

    private const long OneRaw = 1L << FractionBits;
    private const long PiRaw = 13493037705L;

    // atan(2^-i) scaled by 2^32, for the angle routines. Rounded from a 50-digit evaluation.
    private static readonly long[] AtanTable =
    [
        3373259426, 1991351318, 1052175346, 534100635, 268086748, 134174063, 67103403, 33553749,
        16777131, 8388597, 4194303, 2097152, 1048576, 524288, 262144, 131072,
        65536, 32768, 16384, 8192, 4096, 2048, 1024, 512,
        256, 128, 64, 32, 16, 8, 4, 2,
    ];

    private readonly long _raw;

    private Fixed(long raw) => _raw = raw;

    /// <summary>Zero.</summary>
    public static Fixed Zero => default;

    /// <summary>One.</summary>
    public static Fixed One => new(OneRaw);

    /// <summary>One half.</summary>
    public static Fixed Half => new(OneRaw / 2);

    /// <summary>The smallest positive value, 2^-32.</summary>
    public static Fixed Epsilon => new(1);

    /// <summary>The largest value.</summary>
    public static Fixed MaxValue => new(long.MaxValue);

    /// <summary>The smallest value.</summary>
    public static Fixed MinValue => new(long.MinValue);

    /// <summary>Pi, to the precision of the format.</summary>
    public static Fixed Pi => new(PiRaw);

    /// <summary>Two pi.</summary>
    public static Fixed TwoPi => new(PiRaw * 2);

    /// <summary>Pi over two.</summary>
    public static Fixed HalfPi => new(PiRaw / 2);

    /// <summary>The underlying integer, value times 2^32.</summary>
    public long Raw => _raw;

    /// <summary>Wraps an underlying integer.</summary>
    public static Fixed FromRaw(long raw) => new(raw);

    /// <summary>Converts a whole number.</summary>
    public static Fixed FromInt(int value) => new((long)value << FractionBits);

    /// <summary>Converts a whole number.</summary>
    public static Fixed FromInt(long value) => new(value << FractionBits);

    /// <summary>The quotient <paramref name="numerator"/> / <paramref name="denominator"/>, rounded toward negative infinity at the last bit.</summary>
    /// <exception cref="DivideByZeroException"><paramref name="denominator"/> is zero.</exception>
    public static Fixed FromRatio(long numerator, long denominator)
        => new((long)(((Int128)numerator << FractionBits) / denominator));

    /// <summary>Converts a double. For tests and tooling; never use it in sim code.</summary>
    public static Fixed FromDouble(double value) => new((long)Math.Round(value * OneRaw));

    /// <summary>Whole numbers convert implicitly.</summary>
    public static implicit operator Fixed(int value) => FromInt(value);

    /// <summary>The value as a double, for views and tests. Never use it in sim code.</summary>
    public double ToDouble() => _raw / (double)OneRaw;

    /// <summary>The value as a float, for views and rendering. Never use it in sim code.</summary>
    public float ToSingle() => (float)ToDouble();

    /// <summary>The whole part, rounded toward zero.</summary>
    public int ToInt() => (int)(_raw < 0 ? -(-_raw >> FractionBits) : _raw >> FractionBits);

    /// <summary>The whole part, rounded toward negative infinity.</summary>
    public int FloorToInt() => (int)(_raw >> FractionBits);

    /// <summary>The nearest whole number; halves round away from zero.</summary>
    public int RoundToInt() => Round().FloorToInt();

    /// <summary>Adds two values.</summary>
    public static Fixed operator +(Fixed a, Fixed b) => new(unchecked(a._raw + b._raw));

    /// <summary>Subtracts two values.</summary>
    public static Fixed operator -(Fixed a, Fixed b) => new(unchecked(a._raw - b._raw));

    /// <summary>Negates a value.</summary>
    public static Fixed operator -(Fixed a) => new(unchecked(-a._raw));

    /// <summary>Multiplies two values; the 128-bit product is shifted down, rounding toward negative infinity.</summary>
    public static Fixed operator *(Fixed a, Fixed b)
    {
        var high = Math.BigMul(a._raw, b._raw, out long low);
        return new(unchecked((high << FractionBits) | (long)((ulong)low >> FractionBits)));
    }

    /// <summary>Divides two values.</summary>
    /// <exception cref="DivideByZeroException"><paramref name="b"/> is zero.</exception>
    public static Fixed operator /(Fixed a, Fixed b) => new(unchecked((long)(((Int128)a._raw << FractionBits) / b._raw)));

    /// <summary>The remainder of a division, with the sign of <paramref name="a"/>.</summary>
    /// <exception cref="DivideByZeroException"><paramref name="b"/> is zero.</exception>
    public static Fixed operator %(Fixed a, Fixed b) => new(a._raw % b._raw);

    /// <summary>Equality of the underlying values.</summary>
    public static bool operator ==(Fixed a, Fixed b) => a._raw == b._raw;

    /// <summary>Inequality of the underlying values.</summary>
    public static bool operator !=(Fixed a, Fixed b) => a._raw != b._raw;

    /// <summary>Less than.</summary>
    public static bool operator <(Fixed a, Fixed b) => a._raw < b._raw;

    /// <summary>Greater than.</summary>
    public static bool operator >(Fixed a, Fixed b) => a._raw > b._raw;

    /// <summary>Less than or equal.</summary>
    public static bool operator <=(Fixed a, Fixed b) => a._raw <= b._raw;

    /// <summary>Greater than or equal.</summary>
    public static bool operator >=(Fixed a, Fixed b) => a._raw >= b._raw;

    /// <summary>The absolute value.</summary>
    public static Fixed Abs(Fixed a) => a._raw < 0 ? -a : a;

    /// <summary>-1, 0 or 1.</summary>
    public static int Sign(Fixed a) => Math.Sign(a._raw);

    /// <summary>The smaller of two values.</summary>
    public static Fixed Min(Fixed a, Fixed b) => a._raw <= b._raw ? a : b;

    /// <summary>The larger of two values.</summary>
    public static Fixed Max(Fixed a, Fixed b) => a._raw >= b._raw ? a : b;

    /// <summary>Limits a value to a range.</summary>
    public static Fixed Clamp(Fixed v, Fixed min, Fixed max) => v < min ? min : v > max ? max : v;

    /// <summary>Blends from <paramref name="a"/> (at 0) to <paramref name="b"/> (at 1). <paramref name="t"/> is not clamped.</summary>
    public static Fixed Lerp(Fixed a, Fixed b, Fixed t) => a + ((b - a) * t);

    /// <summary>The largest whole number not above the value.</summary>
    public static Fixed Floor(Fixed a) => new(a._raw & ~(OneRaw - 1));

    /// <summary>The smallest whole number not below the value.</summary>
    public static Fixed Ceiling(Fixed a) => new((a._raw + OneRaw - 1) & ~(OneRaw - 1));

    /// <summary>The whole part, rounded toward zero.</summary>
    public static Fixed Truncate(Fixed a) => a._raw < 0 ? Ceiling(a) : Floor(a);

    /// <summary>The fractional part, <c>a - Floor(a)</c>, always in [0, 1).</summary>
    public static Fixed Frac(Fixed a) => new(a._raw & (OneRaw - 1));

    /// <summary>The nearest whole number; halves round away from zero.</summary>
    public Fixed Round() => _raw < 0 ? -Floor(-this + Half) : Floor(this + Half);

    /// <summary>The square root, rounded toward zero at the last bit.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public static Fixed Sqrt(Fixed a)
    {
        if (a._raw < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(a), "Square root of a negative Fixed.");
        }

        // Integer square root of raw * 2^32, by the bit-by-bit method; the result is the root scaled by 2^32.
        UInt128 n = (UInt128)(ulong)a._raw << FractionBits;
        UInt128 result = 0;
        UInt128 bit = (UInt128)1 << 94;
        while (bit > n)
        {
            bit >>= 2;
        }

        while (bit != 0)
        {
            if (n >= result + bit)
            {
                n -= result + bit;
                result = (result >> 1) + bit;
            }
            else
            {
                result >>= 1;
            }

            bit >>= 2;
        }

        return new((long)(ulong)result);
    }

    /// <summary>The sine of an angle in radians. Absolute error is below 1e-8 for angles within a few turns.</summary>
    public static Fixed Sin(Fixed radians)
    {
        // Fold the angle into [-pi/2, pi/2] where a short Taylor series is accurate, then evaluate it with Horner's rule.
        var r = radians._raw % (PiRaw * 2);
        if (r > PiRaw)
        {
            r -= PiRaw * 2;
        }
        else if (r < -PiRaw)
        {
            r += PiRaw * 2;
        }

        if (r > PiRaw / 2)
        {
            r = PiRaw - r;
        }
        else if (r < -(PiRaw / 2))
        {
            r = -PiRaw - r;
        }

        var x = new Fixed(r);
        var x2 = x * x;
        var t = One;
        t = One - (new Fixed((x2 * t)._raw / 156));
        t = One - (new Fixed((x2 * t)._raw / 110));
        t = One - (new Fixed((x2 * t)._raw / 72));
        t = One - (new Fixed((x2 * t)._raw / 42));
        t = One - (new Fixed((x2 * t)._raw / 20));
        t = One - (new Fixed((x2 * t)._raw / 6));
        return x * t;
    }

    /// <summary>The cosine of an angle in radians.</summary>
    public static Fixed Cos(Fixed radians) => Sin(radians + HalfPi);

    /// <summary>The angle in radians, in (-pi, pi], of the point (<paramref name="x"/>, <paramref name="y"/>). Zero for the origin.</summary>
    public static Fixed Atan2(Fixed y, Fixed x)
    {
        var xr = x._raw;
        var yr = y._raw;
        if (xr == 0 && yr == 0)
        {
            return Zero;
        }

        // Scale so the larger coordinate sits near 2^61: precision is kept for tiny vectors and the CORDIC sums cannot overflow.
        var magnitude = Math.Max(Magnitude(xr), Magnitude(yr));
        var shift = BitOperations.LeadingZeroCount(magnitude) - 3;
        if (shift >= 0)
        {
            xr <<= shift;
            yr <<= shift;
        }
        else
        {
            xr >>= -shift;
            yr >>= -shift;
        }

        long z = 0;
        if (xr < 0)
        {
            z = yr >= 0 ? PiRaw : -PiRaw;
            xr = -xr;
            yr = -yr;
        }

        for (var i = 0; i < AtanTable.Length; i++)
        {
            long nx;
            long ny;
            if (yr > 0)
            {
                nx = xr + (yr >> i);
                ny = yr - (xr >> i);
                z += AtanTable[i];
            }
            else
            {
                nx = xr - (yr >> i);
                ny = yr + (xr >> i);
                z -= AtanTable[i];
            }

            xr = nx;
            yr = ny;
        }

        return new(z);
    }

    private static ulong Magnitude(long v) => v < 0 ? (ulong)(-(v + 1)) + 1 : (ulong)v;

    /// <summary>The arctangent in radians, in (-pi/2, pi/2).</summary>
    public static Fixed Atan(Fixed value) => Atan2(value, One);

    /// <inheritdoc />
    public bool Equals(Fixed other) => _raw == other._raw;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Fixed f && f._raw == _raw;

    /// <inheritdoc />
    public override int GetHashCode() => _raw.GetHashCode();

    /// <inheritdoc />
    public int CompareTo(Fixed other) => _raw.CompareTo(other._raw);

    /// <inheritdoc />
    public void Write(SyncWriter writer) => writer.WriteVarInt(_raw);

    /// <inheritdoc />
    public static Fixed Read(ref SyncReader reader) => new(reader.ReadVarInt());

    /// <inheritdoc />
    public void WriteJson(JsonOut json) => json.Raw(ToString());

    /// <summary>
    /// The value as invariant decimal text with up to nine fractional digits, rounded, trailing zeros removed. The text is
    /// stable across platforms but not exact: nine digits cannot carry all 32 fractional bits.
    /// </summary>
    public override string ToString()
    {
        var negative = _raw < 0;
        var magnitude = Magnitude(_raw);
        var whole = magnitude >> FractionBits;
        var frac = magnitude & (OneRaw - 1);
        var digits = ((frac * 1_000_000_000UL) + (1UL << 31)) >> FractionBits;
        if (digits >= 1_000_000_000UL)
        {
            whole++;
            digits -= 1_000_000_000UL;
        }

        var sb = new StringBuilder();
        if (negative && (whole != 0 || digits != 0))
        {
            sb.Append('-');
        }

        sb.Append(whole.ToString(CultureInfo.InvariantCulture));
        if (digits != 0)
        {
            sb.Append('.').Append(digits.ToString("D9", CultureInfo.InvariantCulture).TrimEnd('0'));
        }

        return sb.ToString();
    }

    /// <summary>Parses text such as <c>-12.375</c>. Digits beyond the eighteenth fractional place are ignored. Never throws.</summary>
    public static bool TryParse(string? text, out Fixed value)
    {
        value = default;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var i = 0;
        var negative = false;
        if (text[0] == '-' || text[0] == '+')
        {
            negative = text[0] == '-';
            i++;
        }

        ulong whole = 0;
        var wholeDigits = 0;
        for (; i < text.Length && text[i] is >= '0' and <= '9'; i++, wholeDigits++)
        {
            whole = (whole * 10) + (ulong)(text[i] - '0');
            if (whole > 1UL << 31)
            {
                return false;
            }
        }

        UInt128 fracNum = 0;
        UInt128 fracDen = 1;
        if (i < text.Length && text[i] == '.')
        {
            i++;
            var fracDigits = 0;
            for (; i < text.Length && text[i] is >= '0' and <= '9'; i++, fracDigits++)
            {
                if (fracDigits < 18)
                {
                    fracNum = (fracNum * 10) + (uint)(text[i] - '0');
                    fracDen *= 10;
                }
            }

            if (fracDigits == 0)
            {
                return false;
            }
        }

        if (i != text.Length || wholeDigits == 0)
        {
            return false;
        }

        var magnitude = ((UInt128)whole << FractionBits) + ((fracNum << FractionBits) / fracDen);
        if (negative ? magnitude > (UInt128)1 << 63 : magnitude >= (UInt128)1 << 63)
        {
            return false;
        }

        value = new(negative ? unchecked(-(long)(ulong)magnitude) : (long)(ulong)magnitude);
        return true;
    }
}
