[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# FVec3 struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`ISyncValue`](ISyncValue-1.md)&lt;[`FVec3`](FVec3.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FVec3`](FVec3.md)&gt;, [`ILerp`](ILerp-1.md)&lt;[`FVec3`](FVec3.md)&gt;

```csharp
public readonly struct FVec3 : ISyncValue<FVec3>, IEquatable<FVec3>, ILerp<FVec3>
```

A three-dimensional point or vector in [`Fixed`](Fixed.md) numbers. +X is right, +Y is up and -Z is forward, as in the 3D renderer.

## Constructors

| Member | Description |
| --- | --- |
| [`FVec3(Fixed, Fixed, Fixed)`](#m-ctor-fixed-fixed-fixed) | Creates a vector. |

### <a id="m-ctor-fixed-fixed-fixed"></a>FVec3(Fixed, Fixed, Fixed)

```csharp
public FVec3(Fixed x, Fixed y, Fixed z);
```

Creates a vector.

**Parameters**

- `x` ([`Fixed`](Fixed.md))
- `y` ([`Fixed`](Fixed.md))
- `z` ([`Fixed`](Fixed.md))

## Properties

| Member | Description |
| --- | --- |
| [`Length`](#m-length) | The length. |
| [`LengthSquared`](#m-lengthsquared) | The squared length, which avoids a square root. |
| [`One`](#m-one) | All components one. |
| [`UnitX`](#m-unitx) | The +X axis. |
| [`UnitY`](#m-unity) | The +Y axis (up). |
| [`UnitZ`](#m-unitz) | The +Z axis (towards the viewer of an unrotated camera). |
| [`X`](#m-x) | Horizontal component. |
| [`Y`](#m-y) | Vertical component. |
| [`Z`](#m-z) | Depth component. |
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

### <a id="m-one"></a>One

```csharp
public static FVec3 One { get; }
```

All components one.

**Type:** [`FVec3`](FVec3.md)

### <a id="m-unitx"></a>UnitX

```csharp
public static FVec3 UnitX { get; }
```

The +X axis.

**Type:** [`FVec3`](FVec3.md)

### <a id="m-unity"></a>UnitY

```csharp
public static FVec3 UnitY { get; }
```

The +Y axis (up).

**Type:** [`FVec3`](FVec3.md)

### <a id="m-unitz"></a>UnitZ

```csharp
public static FVec3 UnitZ { get; }
```

The +Z axis (towards the viewer of an unrotated camera).

**Type:** [`FVec3`](FVec3.md)

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

### <a id="m-z"></a>Z

```csharp
public Fixed Z { get; }
```

Depth component.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-zero"></a>Zero

```csharp
public static FVec3 Zero { get; }
```

The zero vector.

**Type:** [`FVec3`](FVec3.md)

## Methods

| Member | Description |
| --- | --- |
| [`Cross(FVec3, FVec3)`](#m-cross-fvec3-fvec3) | The cross product (right-handed). |
| [`Distance(FVec3, FVec3)`](#m-distance-fvec3-fvec3) | The distance between two points. |
| [`Dot(FVec3, FVec3)`](#m-dot-fvec3-fvec3) | The dot product. |
| [`Equals(FVec3)`](#m-equals-fvec3) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Lerp(FVec3, FVec3, Fixed)`](#m-lerp-fvec3-fvec3-fixed) | Blends from `a` (at 0) to `b` (at 1). |
| [`Normalized()`](#m-normalized) | The vector scaled to length one, or zero for the zero vector. |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). |
| [`ToString()`](#m-tostring) |  |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-cross-fvec3-fvec3"></a>Cross(FVec3, FVec3)

```csharp
public static FVec3 Cross(FVec3 a, FVec3 b);
```

The cross product (right-handed).

**Parameters**

- `a` ([`FVec3`](FVec3.md))
- `b` ([`FVec3`](FVec3.md))

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-distance-fvec3-fvec3"></a>Distance(FVec3, FVec3)

```csharp
public static Fixed Distance(FVec3 a, FVec3 b);
```

The distance between two points.

**Parameters**

- `a` ([`FVec3`](FVec3.md))
- `b` ([`FVec3`](FVec3.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-dot-fvec3-fvec3"></a>Dot(FVec3, FVec3)

```csharp
public static Fixed Dot(FVec3 a, FVec3 b);
```

The dot product.

**Parameters**

- `a` ([`FVec3`](FVec3.md))
- `b` ([`FVec3`](FVec3.md))

**Returns:** [`Fixed`](Fixed.md)

### <a id="m-equals-fvec3"></a>Equals(FVec3)

```csharp
public bool Equals(FVec3 other);
```

Implements [`IEquatable<FVec3>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`FVec3`](FVec3.md))

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

### <a id="m-lerp-fvec3-fvec3-fixed"></a>Lerp(FVec3, FVec3, Fixed)

```csharp
public static FVec3 Lerp(FVec3 a, FVec3 b, Fixed t);
```

Blends from `a` (at 0) to `b` (at 1).

**Parameters**

- `a` ([`FVec3`](FVec3.md))
- `b` ([`FVec3`](FVec3.md))
- `t` ([`Fixed`](Fixed.md))

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-normalized"></a>Normalized()

```csharp
public FVec3 Normalized();
```

The vector scaled to length one, or zero for the zero vector.

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public static FVec3 Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](SyncReader.md))

**Returns:** [`FVec3`](FVec3.md)

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
| [`operator !=(FVec3, FVec3)`](#m-operator-fvec3-fvec3) | Inequality of any component. |
| [`operator *(FVec3, FVec3)`](#m-operator-fvec3-fvec3-2) | Multiplies component by component. |
| [`operator *(FVec3, Fixed)`](#m-operator-fvec3-fixed) | Scales a vector. |
| [`operator *(Fixed, FVec3)`](#m-operator-fixed-fvec3) | Scales a vector. |
| [`operator +(FVec3, FVec3)`](#m-operator-fvec3-fvec3-3) | Adds two vectors. |
| [`operator -(FVec3)`](#m-operator-fvec3) | Negates a vector. |
| [`operator -(FVec3, FVec3)`](#m-operator-fvec3-fvec3-4) | Subtracts two vectors. |
| [`operator ==(FVec3, FVec3)`](#m-operator-fvec3-fvec3-5) | Equality of all components. |

### <a id="m-operator-fvec3-fvec3"></a>operator !=(FVec3, FVec3)

```csharp
public static bool operator !=(FVec3 a, FVec3 b);
```

Inequality of any component.

**Parameters**

- `a` ([`FVec3`](FVec3.md))
- `b` ([`FVec3`](FVec3.md))

**Returns:** `bool`

### <a id="m-operator-fvec3-fvec3-2"></a>operator \*(FVec3, FVec3)

```csharp
public static FVec3 operator *(FVec3 a, FVec3 b);
```

Multiplies component by component.

**Parameters**

- `a` ([`FVec3`](FVec3.md))
- `b` ([`FVec3`](FVec3.md))

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-operator-fvec3-fixed"></a>operator \*(FVec3, Fixed)

```csharp
public static FVec3 operator *(FVec3 a, Fixed s);
```

Scales a vector.

**Parameters**

- `a` ([`FVec3`](FVec3.md))
- `s` ([`Fixed`](Fixed.md))

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-operator-fixed-fvec3"></a>operator \*(Fixed, FVec3)

```csharp
public static FVec3 operator *(Fixed s, FVec3 a);
```

Scales a vector.

**Parameters**

- `s` ([`Fixed`](Fixed.md))
- `a` ([`FVec3`](FVec3.md))

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-operator-fvec3-fvec3-3"></a>operator +(FVec3, FVec3)

```csharp
public static FVec3 operator +(FVec3 a, FVec3 b);
```

Adds two vectors.

**Parameters**

- `a` ([`FVec3`](FVec3.md))
- `b` ([`FVec3`](FVec3.md))

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-operator-fvec3"></a>operator -(FVec3)

```csharp
public static FVec3 operator -(FVec3 a);
```

Negates a vector.

**Parameters**

- `a` ([`FVec3`](FVec3.md))

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-operator-fvec3-fvec3-4"></a>operator -(FVec3, FVec3)

```csharp
public static FVec3 operator -(FVec3 a, FVec3 b);
```

Subtracts two vectors.

**Parameters**

- `a` ([`FVec3`](FVec3.md))
- `b` ([`FVec3`](FVec3.md))

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-operator-fvec3-fvec3-5"></a>operator ==(FVec3, FVec3)

```csharp
public static bool operator ==(FVec3 a, FVec3 b);
```

Equality of all components.

**Parameters**

- `a` ([`FVec3`](FVec3.md))
- `b` ([`FVec3`](FVec3.md))

**Returns:** `bool`
