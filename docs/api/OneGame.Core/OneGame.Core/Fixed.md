[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# Fixed struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`ISyncValue`](ISyncValue-1.md)&lt;[`Fixed`](Fixed.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Fixed`](Fixed.md)&gt;, [`ILerp`](ILerp-1.md)&lt;[`Fixed`](Fixed.md)&gt;, [`IComparable`](https://learn.microsoft.com/dotnet/api/system.icomparable-1)&lt;[`Fixed`](Fixed.md)&gt;

```csharp
public readonly struct Fixed : ISyncValue<Fixed>, IEquatable<Fixed>, ILerp<Fixed>, IComparable<Fixed>
```

Deterministic fixed-point number: a signed 64-bit integer holding the value times 2^32 (31 integer bits and 32 fractional bits). Every operation is integer arithmetic, so results are bit-identical on every platform and runtime. Arithmetic wraps on overflow, like `unchecked` integers. Use it for everything in synced state and in sim code; floats are for views only.

## Fields

| Member | Description |
| --- | --- |
| [`FractionBits`](#m-fractionbits) | Number of fractional bits. |

### <a id="m-fractionbits"></a>FractionBits

```csharp
public const int FractionBits = 32;
```

Number of fractional bits.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Epsilon`](#m-epsilon) | The smallest positive value, 2^-32. |
| [`Half`](#m-half) | One half. |
| [`HalfPi`](#m-halfpi) | Pi over two. |
| [`MaxValue`](#m-maxvalue) | The largest value. |
| [`MinValue`](#m-minvalue) | The smallest value. |
| [`One`](#m-one) | One. |
| [`Pi`](#m-pi) | Pi, to the precision of the format. |
| [`Raw`](#m-raw) | The underlying integer, value times 2^32. |
| [`TwoPi`](#m-twopi) | Two pi. |
| [`Zero`](#m-zero) | Zero. |

### <a id="m-epsilon"></a>Epsilon

```csharp
public static Fixed Epsilon { get; }
```

The smallest positive value, 2^-32.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-half"></a>Half

```csharp
public static Fixed Half { get; }
```

One half.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-halfpi"></a>HalfPi

```csharp
public static Fixed HalfPi { get; }
```

Pi over two.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-maxvalue"></a>MaxValue

```csharp
public static Fixed MaxValue { get; }
```

The largest value.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-minvalue"></a>MinValue

```csharp
public static Fixed MinValue { get; }
```

The smallest value.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-one"></a>One

```csharp
public static Fixed One { get; }
```

One.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-pi"></a>Pi

```csharp
public static Fixed Pi { get; }
```

Pi, to the precision of the format.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-raw"></a>Raw

```csharp
public long Raw { get; }
```

The underlying integer, value times 2^32.

**Type:** `long`

### <a id="m-twopi"></a>TwoPi

```csharp
public static Fixed TwoPi { get; }
```

Two pi.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-zero"></a>Zero

```csharp
public static Fixed Zero { get; }
```

Zero.

**Type:** [`Fixed`](Fixed.md)

## Methods

| Member | Description |
| --- | --- |
| [`Abs(Fixed)`](#m-abs-fixed) | The absolute value. |
| [`Atan(Fixed)`](#m-atan-fixed) | The arctangent in radians, in (-pi/2, pi/2). |
| [`Atan2(Fixed, Fixed)`](#m-atan2-fixed-fixed) | The angle in radians, in (-pi, pi\], of the point (`x`, `y`). |
| [`Ceiling(Fixed)`](#m-ceiling-fixed) | The smallest whole number not below the value. |
| [`Clamp(Fixed, Fixed, Fixed)`](#m-clamp-fixed-fixed-fixed) | Limits a value to a range. |
| [`CompareTo(Fixed)`](#m-compareto-fixed) |  |
| [`Cos(Fixed)`](#m-cos-fixed) | The cosine of an angle in radians. |
| [`Equals(Fixed)`](#m-equals-fixed) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`Floor(Fixed)`](#m-floor-fixed) | The largest whole number not above the value. |
| [`FloorToInt()`](#m-floortoint) | The whole part, rounded toward negative infinity. |
| [`Frac(Fixed)`](#m-frac-fixed) | The fractional part, `a - Floor(a)`, always in \[0, 1). |
| [`FromDouble(double)`](#m-fromdouble-double) | Converts a double. |
| [`FromInt(int)`](#m-fromint-int) | Converts a whole number. |
| [`FromInt(long)`](#m-fromint-long) | Converts a whole number. |
| [`FromRatio(long, long)`](#m-fromratio-long-long) | The quotient `numerator` / `denominator`, rounded toward negative infinity at the last bit. |
| [`FromRaw(long)`](#m-fromraw-long) | Wraps an underlying integer. |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Lerp(Fixed, Fixed, Fixed)`](#m-lerp-fixed-fixed-fixed) | Blends from `a` (at 0) to `b` (at 1). |
| [`Max(Fixed, Fixed)`](#m-max-fixed-fixed) | The larger of two values. |
| [`Min(Fixed, Fixed)`](#m-min-fixed-fixed) | The smaller of two values. |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). |
| [`Round()`](#m-round) | The nearest whole number; halves round away from zero. |
| [`RoundToInt()`](#m-roundtoint) | The nearest whole number; halves round away from zero. |
| [`Sign(Fixed)`](#m-sign-fixed) | -1, 0 or 1. |
| [`Sin(Fixed)`](#m-sin-fixed) | The sine of an angle in radians. |
| [`Sqrt(Fixed)`](#m-sqrt-fixed) | The square root, rounded toward zero at the last bit. |
| [`ToDouble()`](#m-todouble) | The value as a double, for views and tests. |
| [`ToInt()`](#m-toint) | The whole part, rounded toward zero. |
| [`ToSingle()`](#m-tosingle) | The value as a float, for views and rendering. |
| [`ToString()`](#m-tostring) | The value as invariant decimal text with up to nine fractional digits, rounded, trailing zeros removed. |
| [`Truncate(Fixed)`](#m-truncate-fixed) | The whole part, rounded toward zero. |
| [`TryParse(string?, out Fixed)`](#m-tryparse-string-fixed) | Parses text such as `-12.375`. |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-abs-fixed"></a>Abs(Fixed)

```csharp
public static Fixed Abs(Fixed a);
```

The absolute value.

**Parameters**

- `a` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-atan-fixed"></a>Atan(Fixed)

```csharp
public static Fixed Atan(Fixed value);
```

The arctangent in radians, in (-pi/2, pi/2).

**Parameters**

- `value` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-atan2-fixed-fixed"></a>Atan2(Fixed, Fixed)

```csharp
public static Fixed Atan2(Fixed y, Fixed x);
```

The angle in radians, in (-pi, pi\], of the point (`x`, `y`). Zero for the origin.

**Parameters**

- `y` ([`Fixed`](Fixed.md))
- `x` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-ceiling-fixed"></a>Ceiling(Fixed)

```csharp
public static Fixed Ceiling(Fixed a);
```

The smallest whole number not below the value.

**Parameters**

- `a` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-clamp-fixed-fixed-fixed"></a>Clamp(Fixed, Fixed, Fixed)

```csharp
public static Fixed Clamp(Fixed v, Fixed min, Fixed max);
```

Limits a value to a range.

**Parameters**

- `v` ([`Fixed`](Fixed.md))
- `min` ([`Fixed`](Fixed.md))
- `max` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-compareto-fixed"></a>CompareTo(Fixed)

```csharp
public int CompareTo(Fixed other);
```

**Parameters**

- `other` ([`Fixed`](Fixed.md))

**Returns:** `int`

### <a id="m-cos-fixed"></a>Cos(Fixed)

```csharp
public static Fixed Cos(Fixed radians);
```

The cosine of an angle in radians.

**Parameters**

- `radians` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-equals-fixed"></a>Equals(Fixed)

```csharp
public bool Equals(Fixed other);
```

**Parameters**

- `other` ([`Fixed`](Fixed.md))

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-floor-fixed"></a>Floor(Fixed)

```csharp
public static Fixed Floor(Fixed a);
```

The largest whole number not above the value.

**Parameters**

- `a` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-floortoint"></a>FloorToInt()

```csharp
public int FloorToInt();
```

The whole part, rounded toward negative infinity.

**Returns:** `int`

### <a id="m-frac-fixed"></a>Frac(Fixed)

```csharp
public static Fixed Frac(Fixed a);
```

The fractional part, `a - Floor(a)`, always in \[0, 1).

**Parameters**

- `a` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-fromdouble-double"></a>FromDouble(double)

```csharp
public static Fixed FromDouble(double value);
```

Converts a double. For tests and tooling; never use it in sim code.

**Parameters**

- `value` (`double`)

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-fromint-int"></a>FromInt(int)

```csharp
public static Fixed FromInt(int value);
```

Converts a whole number.

**Parameters**

- `value` (`int`)

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-fromint-long"></a>FromInt(long)

```csharp
public static Fixed FromInt(long value);
```

Converts a whole number.

**Parameters**

- `value` (`long`)

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-fromratio-long-long"></a>FromRatio(long, long)

```csharp
public static Fixed FromRatio(long numerator, long denominator);
```

The quotient `numerator` / `denominator`, rounded toward negative infinity at the last bit.

**Parameters**

- `numerator` (`long`)
- `denominator` (`long`)

**Returns:** [`Fixed`](Fixed.md)

**Exceptions**

- [`DivideByZeroException`](https://learn.microsoft.com/dotnet/api/system.dividebyzeroexception): `denominator` is zero.

### <a id="m-fromraw-long"></a>FromRaw(long)

```csharp
public static Fixed FromRaw(long raw);
```

Wraps an underlying integer.

**Parameters**

- `raw` (`long`)

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

**Returns:** `int`

### <a id="m-lerp-fixed-fixed-fixed"></a>Lerp(Fixed, Fixed, Fixed)

```csharp
public static Fixed Lerp(Fixed a, Fixed b, Fixed t);
```

Blends from `a` (at 0) to `b` (at 1). `t` is not clamped.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))
- `t` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-max-fixed-fixed"></a>Max(Fixed, Fixed)

```csharp
public static Fixed Max(Fixed a, Fixed b);
```

The larger of two values.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-min-fixed-fixed"></a>Min(Fixed, Fixed)

```csharp
public static Fixed Min(Fixed a, Fixed b);
```

The smaller of two values.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public static Fixed Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](SyncReader.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-round"></a>Round()

```csharp
public Fixed Round();
```

The nearest whole number; halves round away from zero.

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-roundtoint"></a>RoundToInt()

```csharp
public int RoundToInt();
```

The nearest whole number; halves round away from zero.

**Returns:** `int`

### <a id="m-sign-fixed"></a>Sign(Fixed)

```csharp
public static int Sign(Fixed a);
```

-1, 0 or 1.

**Parameters**

- `a` ([`Fixed`](Fixed.md))

**Returns:** `int`

### <a id="m-sin-fixed"></a>Sin(Fixed)

```csharp
public static Fixed Sin(Fixed radians);
```

The sine of an angle in radians. Absolute error is below 1e-8 for angles within a few turns.

**Parameters**

- `radians` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-sqrt-fixed"></a>Sqrt(Fixed)

```csharp
public static Fixed Sqrt(Fixed a);
```

The square root, rounded toward zero at the last bit.

**Parameters**

- `a` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The value is negative.

### <a id="m-todouble"></a>ToDouble()

```csharp
public double ToDouble();
```

The value as a double, for views and tests. Never use it in sim code.

**Returns:** `double`

### <a id="m-toint"></a>ToInt()

```csharp
public int ToInt();
```

The whole part, rounded toward zero.

**Returns:** `int`

### <a id="m-tosingle"></a>ToSingle()

```csharp
public float ToSingle();
```

The value as a float, for views and rendering. Never use it in sim code.

**Returns:** `float`

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

The value as invariant decimal text with up to nine fractional digits, rounded, trailing zeros removed. The text is stable across platforms but not exact: nine digits cannot carry all 32 fractional bits.

**Returns:** `string`

### <a id="m-truncate-fixed"></a>Truncate(Fixed)

```csharp
public static Fixed Truncate(Fixed a);
```

The whole part, rounded toward zero.

**Parameters**

- `a` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-tryparse-string-fixed"></a>TryParse(string?, out Fixed)

```csharp
public static bool TryParse(string? text, out Fixed value);
```

Parses text such as `-12.375`. Digits beyond the eighteenth fractional place are ignored. Never throws.

**Parameters**

- `text` (`string?`)
- `value` ([`Fixed`](Fixed.md))

**Returns:** `bool`

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
public void Write(SyncWriter writer);
```

Writes the value in the sync format.

**Parameters**

- `writer` ([`SyncWriter`](SyncWriter.md))

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
public void WriteJson(JsonOut json);
```

Writes the value as JSON for inspection.

**Parameters**

- `json` ([`JsonOut`](JsonOut.md))

## Operators

| Member | Description |
| --- | --- |
| [`implicit operator Fixed(int)`](#m-implicit-operator-fixed-int) | Whole numbers convert implicitly. |
| [`operator !=(Fixed, Fixed)`](#m-operator-fixed-fixed) | Inequality of the underlying values. |
| [`operator %(Fixed, Fixed)`](#m-operator-fixed-fixed-2) | The remainder of a division, with the sign of `a`. |
| [`operator *(Fixed, Fixed)`](#m-operator-fixed-fixed-3) | Multiplies two values; the 128-bit product is shifted down, rounding toward negative infinity. |
| [`operator +(Fixed, Fixed)`](#m-operator-fixed-fixed-4) | Adds two values. |
| [`operator -(Fixed)`](#m-operator-fixed) | Negates a value. |
| [`operator -(Fixed, Fixed)`](#m-operator-fixed-fixed-5) | Subtracts two values. |
| [`operator /(Fixed, Fixed)`](#m-operator-fixed-fixed-6) | Divides two values. |
| [`operator <(Fixed, Fixed)`](#m-operator-fixed-fixed-7) | Less than. |
| [`operator <=(Fixed, Fixed)`](#m-operator-fixed-fixed-8) | Less than or equal. |
| [`operator ==(Fixed, Fixed)`](#m-operator-fixed-fixed-9) | Equality of the underlying values. |
| [`operator >(Fixed, Fixed)`](#m-operator-fixed-fixed-10) | Greater than. |
| [`operator >=(Fixed, Fixed)`](#m-operator-fixed-fixed-11) | Greater than or equal. |

### <a id="m-implicit-operator-fixed-int"></a>implicit operator Fixed(int)

```csharp
public static implicit operator Fixed(int value);
```

Whole numbers convert implicitly.

**Parameters**

- `value` (`int`)

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-operator-fixed-fixed"></a>operator !=(Fixed, Fixed)

```csharp
public static bool operator !=(Fixed a, Fixed b);
```

Inequality of the underlying values.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** `bool`

### <a id="m-operator-fixed-fixed-2"></a>operator %(Fixed, Fixed)

```csharp
public static Fixed operator %(Fixed a, Fixed b);
```

The remainder of a division, with the sign of `a`.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

**Exceptions**

- [`DivideByZeroException`](https://learn.microsoft.com/dotnet/api/system.dividebyzeroexception): `b` is zero.

### <a id="m-operator-fixed-fixed-3"></a>operator \*(Fixed, Fixed)

```csharp
public static Fixed operator *(Fixed a, Fixed b);
```

Multiplies two values; the 128-bit product is shifted down, rounding toward negative infinity.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-operator-fixed-fixed-4"></a>operator +(Fixed, Fixed)

```csharp
public static Fixed operator +(Fixed a, Fixed b);
```

Adds two values.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-operator-fixed"></a>operator -(Fixed)

```csharp
public static Fixed operator -(Fixed a);
```

Negates a value.

**Parameters**

- `a` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-operator-fixed-fixed-5"></a>operator -(Fixed, Fixed)

```csharp
public static Fixed operator -(Fixed a, Fixed b);
```

Subtracts two values.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-operator-fixed-fixed-6"></a>operator /(Fixed, Fixed)

```csharp
public static Fixed operator /(Fixed a, Fixed b);
```

Divides two values.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** [`Fixed`](Fixed.md)

**Exceptions**

- [`DivideByZeroException`](https://learn.microsoft.com/dotnet/api/system.dividebyzeroexception): `b` is zero.

### <a id="m-operator-fixed-fixed-7"></a>operator &lt;(Fixed, Fixed)

```csharp
public static bool operator <(Fixed a, Fixed b);
```

Less than.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** `bool`

### <a id="m-operator-fixed-fixed-8"></a>operator &lt;=(Fixed, Fixed)

```csharp
public static bool operator <=(Fixed a, Fixed b);
```

Less than or equal.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** `bool`

### <a id="m-operator-fixed-fixed-9"></a>operator ==(Fixed, Fixed)

```csharp
public static bool operator ==(Fixed a, Fixed b);
```

Equality of the underlying values.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** `bool`

### <a id="m-operator-fixed-fixed-10"></a>operator &gt;(Fixed, Fixed)

```csharp
public static bool operator >(Fixed a, Fixed b);
```

Greater than.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** `bool`

### <a id="m-operator-fixed-fixed-11"></a>operator &gt;=(Fixed, Fixed)

```csharp
public static bool operator >=(Fixed a, Fixed b);
```

Greater than or equal.

**Parameters**

- `a` ([`Fixed`](Fixed.md))
- `b` ([`Fixed`](Fixed.md))

**Returns:** `bool`
