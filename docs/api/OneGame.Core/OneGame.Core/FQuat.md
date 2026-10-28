[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# FQuat struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`ISyncValue`](ISyncValue-1.md)&lt;[`FQuat`](FQuat.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`FQuat`](FQuat.md)&gt;, [`ILerp`](ILerp-1.md)&lt;[`FQuat`](FQuat.md)&gt;

```csharp
public readonly struct FQuat : ISyncValue<FQuat>, IEquatable<FQuat>, ILerp<FQuat>
```

A rotation as a quaternion in [`Fixed`](Fixed.md) numbers. The scalar part is stored relative to one, so `default` is the identity rotation and a freshly created synced property is already valid. Rotations compose with the `*` operator: `a * b` applies `b` first.

## Constructors

| Member | Description |
| --- | --- |
| [`FQuat(Fixed, Fixed, Fixed, Fixed)`](#m-ctor-fixed-fixed-fixed-fixed) | Creates a quaternion from its components (not normalized). |

### <a id="m-ctor-fixed-fixed-fixed-fixed"></a>FQuat(Fixed, Fixed, Fixed, Fixed)

```csharp
public FQuat(Fixed x, Fixed y, Fixed z, Fixed w);
```

Creates a quaternion from its components (not normalized).

**Parameters**

- `x` ([`Fixed`](Fixed.md))
- `y` ([`Fixed`](Fixed.md))
- `z` ([`Fixed`](Fixed.md))
- `w` ([`Fixed`](Fixed.md))

## Properties

| Member | Description |
| --- | --- |
| [`Identity`](#m-identity) | No rotation. |
| [`W`](#m-w) | The scalar part. |
| [`X`](#m-x) | The X component of the axis part. |
| [`Y`](#m-y) | The Y component of the axis part. |
| [`Z`](#m-z) | The Z component of the axis part. |

### <a id="m-identity"></a>Identity

```csharp
public static FQuat Identity { get; }
```

No rotation.

**Type:** [`FQuat`](FQuat.md)

### <a id="m-w"></a>W

```csharp
public Fixed W { get; }
```

The scalar part.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-x"></a>X

```csharp
public Fixed X { get; }
```

The X component of the axis part.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-y"></a>Y

```csharp
public Fixed Y { get; }
```

The Y component of the axis part.

**Type:** [`Fixed`](Fixed.md)

### <a id="m-z"></a>Z

```csharp
public Fixed Z { get; }
```

The Z component of the axis part.

**Type:** [`Fixed`](Fixed.md)

## Methods

| Member | Description |
| --- | --- |
| [`Equals(FQuat)`](#m-equals-fquat) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`FromAxisAngle(FVec3, Fixed)`](#m-fromaxisangle-fvec3-fixed) | A rotation of `radians` about `axis` (right-handed; the axis need not be normalized). |
| [`FromYawPitchRoll(Fixed, Fixed, Fixed)`](#m-fromyawpitchroll-fixed-fixed-fixed) | A turn about +Y (positive turns anticlockwise seen from above), then about the turned X axis, then about Z. |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Inverse()`](#m-inverse) | The opposite rotation (of a unit quaternion). |
| [`Lerp(FQuat, FQuat, Fixed)`](#m-lerp-fquat-fquat-fixed) | Blends from `a` (at 0) to `b` (at 1) along the shorter way, and normalizes. |
| [`LookRotation(FVec3, FVec3)`](#m-lookrotation-fvec3-fvec3) | The rotation that turns -Z to look along `forward` with +Y as close to up as possible. |
| [`Normalized()`](#m-normalized) | The same rotation scaled to unit length; the identity for the zero quaternion. |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). |
| [`Rotate(FVec3)`](#m-rotate-fvec3) | Turns a vector by this rotation (which should be normalized). |
| [`ToString()`](#m-tostring) |  |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-equals-fquat"></a>Equals(FQuat)

```csharp
public bool Equals(FQuat other);
```

**Parameters**

- `other` ([`FQuat`](FQuat.md))

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-fromaxisangle-fvec3-fixed"></a>FromAxisAngle(FVec3, Fixed)

```csharp
public static FQuat FromAxisAngle(FVec3 axis, Fixed radians);
```

A rotation of `radians` about `axis` (right-handed; the axis need not be normalized).

**Parameters**

- `axis` ([`FVec3`](FVec3.md))
- `radians` ([`Fixed`](Fixed.md))

**Returns:** [`FQuat`](FQuat.md)

### <a id="m-fromyawpitchroll-fixed-fixed-fixed"></a>FromYawPitchRoll(Fixed, Fixed, Fixed)

```csharp
public static FQuat FromYawPitchRoll(Fixed yaw, Fixed pitch, Fixed roll);
```

A turn about +Y (positive turns anticlockwise seen from above), then about the turned X axis, then about Z.

**Parameters**

- `yaw` ([`Fixed`](Fixed.md))
- `pitch` ([`Fixed`](Fixed.md))
- `roll` ([`Fixed`](Fixed.md))

**Returns:** [`FQuat`](FQuat.md)

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

**Returns:** `int`

### <a id="m-inverse"></a>Inverse()

```csharp
public FQuat Inverse();
```

The opposite rotation (of a unit quaternion).

**Returns:** [`FQuat`](FQuat.md)

### <a id="m-lerp-fquat-fquat-fixed"></a>Lerp(FQuat, FQuat, Fixed)

```csharp
public static FQuat Lerp(FQuat a, FQuat b, Fixed t);
```

Blends from `a` (at 0) to `b` (at 1) along the shorter way, and normalizes.

**Parameters**

- `a` ([`FQuat`](FQuat.md))
- `b` ([`FQuat`](FQuat.md))
- `t` ([`Fixed`](Fixed.md))

**Returns:** [`FQuat`](FQuat.md)

### <a id="m-lookrotation-fvec3-fvec3"></a>LookRotation(FVec3, FVec3)

```csharp
public static FQuat LookRotation(FVec3 forward, FVec3 up);
```

The rotation that turns -Z to look along `forward` with +Y as close to up as possible.

**Parameters**

- `forward` ([`FVec3`](FVec3.md))
- `up` ([`FVec3`](FVec3.md))

**Returns:** [`FQuat`](FQuat.md)

### <a id="m-normalized"></a>Normalized()

```csharp
public FQuat Normalized();
```

The same rotation scaled to unit length; the identity for the zero quaternion.

**Returns:** [`FQuat`](FQuat.md)

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public static FQuat Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](SyncReader.md))

**Returns:** [`FQuat`](FQuat.md)

### <a id="m-rotate-fvec3"></a>Rotate(FVec3)

```csharp
public FVec3 Rotate(FVec3 v);
```

Turns a vector by this rotation (which should be normalized).

**Parameters**

- `v` ([`FVec3`](FVec3.md))

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

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
| [`operator !=(FQuat, FQuat)`](#m-operator-fquat-fquat) | Inequality of any component. |
| [`operator *(FQuat, FQuat)`](#m-operator-fquat-fquat-2) | Composes two rotations: the result applies `b` first, then `a`. |
| [`operator ==(FQuat, FQuat)`](#m-operator-fquat-fquat-3) | Equality of all components. |

### <a id="m-operator-fquat-fquat"></a>operator !=(FQuat, FQuat)

```csharp
public static bool operator !=(FQuat a, FQuat b);
```

Inequality of any component.

**Parameters**

- `a` ([`FQuat`](FQuat.md))
- `b` ([`FQuat`](FQuat.md))

**Returns:** `bool`

### <a id="m-operator-fquat-fquat-2"></a>operator \*(FQuat, FQuat)

```csharp
public static FQuat operator *(FQuat a, FQuat b);
```

Composes two rotations: the result applies `b` first, then `a`.

**Parameters**

- `a` ([`FQuat`](FQuat.md))
- `b` ([`FQuat`](FQuat.md))

**Returns:** [`FQuat`](FQuat.md)

### <a id="m-operator-fquat-fquat-3"></a>operator ==(FQuat, FQuat)

```csharp
public static bool operator ==(FQuat a, FQuat b);
```

Equality of all components.

**Parameters**

- `a` ([`FQuat`](FQuat.md))
- `b` ([`FQuat`](FQuat.md))

**Returns:** `bool`
