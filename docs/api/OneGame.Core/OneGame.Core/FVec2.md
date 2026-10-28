[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# FVec2 struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`ISyncValue`](ISyncValue-1.md)&lt;[`FVec2`](FVec2.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FVec2`](FVec2.md)&gt;, [`ILerp`](ILerp-1.md)&lt;[`FVec2`](FVec2.md)&gt;

```csharp
public readonly struct FVec2 : ISyncValue<FVec2>, IEquatable<FVec2>, ILerp<FVec2>
```

A two-dimensional point or vector in [`Fixed`](Fixed.md) numbers.

## Constructors

| Member | Description |
| --- | --- |
| [`FVec2(Fixed, Fixed)`](#m-ctor-fixed-fixed) | Creates a vector. |

### <a id="m-ctor-fixed-fixed"></a>FVec2(Fixed, Fixed)

```csharp
public FVec2(Fixed x, Fixed y);
```

Creates a vector.

**Parameters**

- `x` ([`Fixed`](Fixed.md))
- `y` ([`Fixed`](Fixed.md))

## Properties

| Member | Description |
| --- | --- |
| [`Length`](#m-length) | The length. |
| [`LengthSquared`](#m-lengthsquared) | The squared length, which avoids a square root. |
| [`X`](#m-x) | Horizontal component. |
| [`Y`](#m-y) | Vertical component. |
| [`Zero`](#m-zero) | The zero vector. |

### <a id="m-length"></a>Length

```csharp
public Fixed Length { get; }
```

The length.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-lengthsquared"></a>LengthSquared

```csharp
public Fixed LengthSquared { get; }
```

The squared length, which avoids a square root.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-x"></a>X

```csharp
public Fixed X { get; }
```

Horizontal component.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-y"></a>Y

```csharp
public Fixed Y { get; }
```

Vertical component.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-zero"></a>Zero

```csharp
public static FVec2 Zero { get; }
```

The zero vector.

**Type:** [`FVec2`](FVec2.md)

## Methods

| Member | Description |
| --- | --- |
| [`Dot(FVec2, FVec2)`](#m-dot-fvec2-fvec2) | The dot product. |
| [`Equals(FVec2)`](#m-equals-fvec2) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Lerp(FVec2, FVec2, Fixed)`](#m-lerp-fvec2-fvec2-fixed) | Blends from `a` (at 0) to `b` (at 1). |
| [`Normalized()`](#m-normalized) | The vector scaled to length one, or zero for the zero vector. |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). |
| [`ToString()`](#m-tostring) |  |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-dot-fvec2-fvec2"></a>Dot(FVec2, FVec2)

```csharp
public static Fixed Dot(FVec2 a, FVec2 b);
```

The dot product.

**Parameters**

- `a` ([`FVec2`](FVec2.md))
- `b` ([`FVec2`](FVec2.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-equals-fvec2"></a>Equals(FVec2)

```csharp
public bool Equals(FVec2 other);
```

Implements [`IEquatable<FVec2>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`FVec2`](FVec2.md))

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

Overrides [`ValueType.Equals()`](https://learn.microsoft.com/dotnet/api/system.valuetype.equals).

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

Overrides [`ValueType.GetHashCode()`](https://learn.microsoft.com/dotnet/api/system.valuetype.gethashcode).

**Returns:** `int`

### <a id="m-lerp-fvec2-fvec2-fixed"></a>Lerp(FVec2, FVec2, Fixed)

```csharp
public static FVec2 Lerp(FVec2 a, FVec2 b, Fixed t);
```

Blends from `a` (at 0) to `b` (at 1).

**Parameters**

- `a` ([`FVec2`](FVec2.md))
- `b` ([`FVec2`](FVec2.md))
- `t` ([`Fixed`](Fixed.md))

**Returns:** [`FVec2`](FVec2.md)

### <a id="m-normalized"></a>Normalized()

```csharp
public FVec2 Normalized();
```

The vector scaled to length one, or zero for the zero vector.

**Returns:** [`FVec2`](FVec2.md)

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public static FVec2 Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](SyncReader.md))

**Returns:** [`FVec2`](FVec2.md)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

Overrides [`ValueType.ToString()`](https://learn.microsoft.com/dotnet/api/system.valuetype.tostring).

**Returns:** `string`

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
| [`operator !=(FVec2, FVec2)`](#m-operator-fvec2-fvec2) | Inequality of either component. |
| [`operator *(FVec2, Fixed)`](#m-operator-fvec2-fixed) | Scales a vector. |
| [`operator *(Fixed, FVec2)`](#m-operator-fixed-fvec2) | Scales a vector. |
| [`operator +(FVec2, FVec2)`](#m-operator-fvec2-fvec2-2) | Adds two vectors. |
| [`operator -(FVec2)`](#m-operator-fvec2) | Negates a vector. |
| [`operator -(FVec2, FVec2)`](#m-operator-fvec2-fvec2-3) | Subtracts two vectors. |
| [`operator ==(FVec2, FVec2)`](#m-operator-fvec2-fvec2-4) | Equality of both components. |

### <a id="m-operator-fvec2-fvec2"></a>operator !=(FVec2, FVec2)

```csharp
public static bool operator !=(FVec2 a, FVec2 b);
```

Inequality of either component.

**Parameters**

- `a` ([`FVec2`](FVec2.md))
- `b` ([`FVec2`](FVec2.md))

**Returns:** `bool`

### <a id="m-operator-fvec2-fixed"></a>operator \*(FVec2, Fixed)

```csharp
public static FVec2 operator *(FVec2 a, Fixed s);
```

Scales a vector.

**Parameters**

- `a` ([`FVec2`](FVec2.md))
- `s` ([`Fixed`](Fixed.md))

**Returns:** [`FVec2`](FVec2.md)

### <a id="m-operator-fixed-fvec2"></a>operator \*(Fixed, FVec2)

```csharp
public static FVec2 operator *(Fixed s, FVec2 a);
```

Scales a vector.

**Parameters**

- `s` ([`Fixed`](Fixed.md))
- `a` ([`FVec2`](FVec2.md))

**Returns:** [`FVec2`](FVec2.md)

### <a id="m-operator-fvec2-fvec2-2"></a>operator +(FVec2, FVec2)

```csharp
public static FVec2 operator +(FVec2 a, FVec2 b);
```

Adds two vectors.

**Parameters**

- `a` ([`FVec2`](FVec2.md))
- `b` ([`FVec2`](FVec2.md))

**Returns:** [`FVec2`](FVec2.md)

### <a id="m-operator-fvec2"></a>operator -(FVec2)

```csharp
public static FVec2 operator -(FVec2 a);
```

Negates a vector.

**Parameters**

- `a` ([`FVec2`](FVec2.md))

**Returns:** [`FVec2`](FVec2.md)

### <a id="m-operator-fvec2-fvec2-3"></a>operator -(FVec2, FVec2)

```csharp
public static FVec2 operator -(FVec2 a, FVec2 b);
```

Subtracts two vectors.

**Parameters**

- `a` ([`FVec2`](FVec2.md))
- `b` ([`FVec2`](FVec2.md))

**Returns:** [`FVec2`](FVec2.md)

### <a id="m-operator-fvec2-fvec2-4"></a>operator ==(FVec2, FVec2)

```csharp
public static bool operator ==(FVec2 a, FVec2 b);
```

Equality of both components.

**Parameters**

- `a` ([`FVec2`](FVec2.md))
- `b` ([`FVec2`](FVec2.md))

**Returns:** `bool`
