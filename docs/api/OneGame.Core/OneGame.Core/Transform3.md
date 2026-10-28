[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# Transform3 struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`ISyncValue`](ISyncValue-1.md)&lt;[`Transform3`](Transform3.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Transform3`](Transform3.md)&gt;, [`ILerp`](ILerp-1.md)&lt;[`Transform3`](Transform3.md)&gt;

```csharp
public readonly struct Transform3 : ISyncValue<Transform3>, IEquatable<Transform3>, ILerp<Transform3>
```

Where something is in a 3D world: a position, a rotation and a scale, all in [`Fixed`](Fixed.md) numbers so they can be synced and simulated deterministically. Declare a synced property of this type (or `Timed<Transform3>` for a move that is interpolated in views). The `default` value is the identity transform: the zero rotation means none and a zero scale means one.

## Constructors

| Member | Description |
| --- | --- |
| [`Transform3(FVec3, FQuat, FVec3)`](#m-ctor-fvec3-fquat-fvec3) | Creates a transform. |

### <a id="m-ctor-fvec3-fquat-fvec3"></a>Transform3(FVec3, FQuat, FVec3)

```csharp
public Transform3(FVec3 position, FQuat rotation, FVec3 scale);
```

Creates a transform.

**Parameters**

- `position` ([`FVec3`](FVec3.md))
- `rotation` ([`FQuat`](FQuat.md))
- `scale` ([`FVec3`](FVec3.md))

## Properties

| Member | Description |
| --- | --- |
| [`Forward`](#m-forward) | The direction it faces (-Z turned by [`Transform3.Rotation`](Transform3.md#m-rotation)). |
| [`Identity`](#m-identity) | No move, no rotation, scale one. |
| [`Position`](#m-position) | Where the origin of the thing is. |
| [`Right`](#m-right) | Its right (+X turned by [`Transform3.Rotation`](Transform3.md#m-rotation)). |
| [`Rotation`](#m-rotation) | How it is turned; the zero value means no rotation. |
| [`Scale`](#m-scale) | Its size on each axis. |
| [`Up`](#m-up) | Its up (+Y turned by [`Transform3.Rotation`](Transform3.md#m-rotation)). |

### <a id="m-forward"></a>Forward

```csharp
public FVec3 Forward { get; }
```

The direction it faces (-Z turned by [`Transform3.Rotation`](Transform3.md#m-rotation)).

**Type:** [`FVec3`](FVec3.md)

### <a id="m-identity"></a>Identity

```csharp
public static Transform3 Identity { get; }
```

No move, no rotation, scale one.

**Type:** [`Transform3`](Transform3.md)

### <a id="m-position"></a>Position

```csharp
public FVec3 Position { get; }
```

Where the origin of the thing is.

**Type:** [`FVec3`](FVec3.md)

### <a id="m-right"></a>Right

```csharp
public FVec3 Right { get; }
```

Its right (+X turned by [`Transform3.Rotation`](Transform3.md#m-rotation)).

**Type:** [`FVec3`](FVec3.md)

### <a id="m-rotation"></a>Rotation

```csharp
public FQuat Rotation { get; }
```

How it is turned; the zero value means no rotation.

**Type:** [`FQuat`](FQuat.md)

### <a id="m-scale"></a>Scale

```csharp
public FVec3 Scale { get; }
```

Its size on each axis. (Stored relative to one so that `default` is the identity.)

**Type:** [`FVec3`](FVec3.md)

### <a id="m-up"></a>Up

```csharp
public FVec3 Up { get; }
```

Its up (+Y turned by [`Transform3.Rotation`](Transform3.md#m-rotation)).

**Type:** [`FVec3`](FVec3.md)

## Methods

| Member | Description |
| --- | --- |
| [`At(FVec3)`](#m-at-fvec3) | A transform that only moves. |
| [`At(FVec3, FQuat)`](#m-at-fvec3-fquat) | A transform that moves and rotates. |
| [`Equals(Transform3)`](#m-equals-transform3) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Lerp(Transform3, Transform3, Fixed)`](#m-lerp-transform3-transform3-fixed) | Blends position and scale linearly and rotation along the shorter arc. |
| [`LookAt(FVec3, FVec3)`](#m-lookat-fvec3-fvec3) | A transform at `eye` that faces `target`, with +Y as close to up as possible. |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). |
| [`ToString()`](#m-tostring) |  |
| [`TransformPoint(FVec3)`](#m-transformpoint-fvec3) | Takes a point from this thing's own space to the world. |
| [`Translated(FVec3)`](#m-translated-fvec3) | The same transform moved by `delta` in world space. |
| [`WithPosition(FVec3)`](#m-withposition-fvec3) | The same transform at another position. |
| [`WithRotation(FQuat)`](#m-withrotation-fquat) | The same transform turned differently. |
| [`WithScale(FVec3)`](#m-withscale-fvec3) | The same transform at another scale. |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-at-fvec3"></a>At(FVec3)

```csharp
public static Transform3 At(FVec3 position);
```

A transform that only moves.

**Parameters**

- `position` ([`FVec3`](FVec3.md))

**Returns:** [`Transform3`](Transform3.md)

### <a id="m-at-fvec3-fquat"></a>At(FVec3, FQuat)

```csharp
public static Transform3 At(FVec3 position, FQuat rotation);
```

A transform that moves and rotates.

**Parameters**

- `position` ([`FVec3`](FVec3.md))
- `rotation` ([`FQuat`](FQuat.md))

**Returns:** [`Transform3`](Transform3.md)

### <a id="m-equals-transform3"></a>Equals(Transform3)

```csharp
public bool Equals(Transform3 other);
```

Implements [`IEquatable<Transform3>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`Transform3`](Transform3.md))

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

### <a id="m-lerp-transform3-transform3-fixed"></a>Lerp(Transform3, Transform3, Fixed)

```csharp
public static Transform3 Lerp(Transform3 a, Transform3 b, Fixed t);
```

Blends position and scale linearly and rotation along the shorter arc.

**Parameters**

- `a` ([`Transform3`](Transform3.md))
- `b` ([`Transform3`](Transform3.md))
- `t` ([`Fixed`](Fixed.md))

**Returns:** [`Transform3`](Transform3.md)

### <a id="m-lookat-fvec3-fvec3"></a>LookAt(FVec3, FVec3)

```csharp
public static Transform3 LookAt(FVec3 eye, FVec3 target);
```

A transform at `eye` that faces `target`, with +Y as close to up as possible.

**Parameters**

- `eye` ([`FVec3`](FVec3.md))
- `target` ([`FVec3`](FVec3.md))

**Returns:** [`Transform3`](Transform3.md)

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public static Transform3 Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](SyncReader.md))

**Returns:** [`Transform3`](Transform3.md)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

Overrides [`ValueType.ToString()`](https://learn.microsoft.com/dotnet/api/system.valuetype.tostring).

**Returns:** `string`

### <a id="m-transformpoint-fvec3"></a>TransformPoint(FVec3)

```csharp
public FVec3 TransformPoint(FVec3 local);
```

Takes a point from this thing's own space to the world.

**Parameters**

- `local` ([`FVec3`](FVec3.md))

**Returns:** [`FVec3`](FVec3.md)

### <a id="m-translated-fvec3"></a>Translated(FVec3)

```csharp
public Transform3 Translated(FVec3 delta);
```

The same transform moved by `delta` in world space.

**Parameters**

- `delta` ([`FVec3`](FVec3.md))

**Returns:** [`Transform3`](Transform3.md)

### <a id="m-withposition-fvec3"></a>WithPosition(FVec3)

```csharp
public Transform3 WithPosition(FVec3 position);
```

The same transform at another position.

**Parameters**

- `position` ([`FVec3`](FVec3.md))

**Returns:** [`Transform3`](Transform3.md)

### <a id="m-withrotation-fquat"></a>WithRotation(FQuat)

```csharp
public Transform3 WithRotation(FQuat rotation);
```

The same transform turned differently.

**Parameters**

- `rotation` ([`FQuat`](FQuat.md))

**Returns:** [`Transform3`](Transform3.md)

### <a id="m-withscale-fvec3"></a>WithScale(FVec3)

```csharp
public Transform3 WithScale(FVec3 scale);
```

The same transform at another scale.

**Parameters**

- `scale` ([`FVec3`](FVec3.md))

**Returns:** [`Transform3`](Transform3.md)

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
| [`operator !=(Transform3, Transform3)`](#m-operator-transform3-transform3) | Inequality of any part. |
| [`operator ==(Transform3, Transform3)`](#m-operator-transform3-transform3-2) | Equality of all parts. |

### <a id="m-operator-transform3-transform3"></a>operator !=(Transform3, Transform3)

```csharp
public static bool operator !=(Transform3 a, Transform3 b);
```

Inequality of any part.

**Parameters**

- `a` ([`Transform3`](Transform3.md))
- `b` ([`Transform3`](Transform3.md))

**Returns:** `bool`

### <a id="m-operator-transform3-transform3-2"></a>operator ==(Transform3, Transform3)

```csharp
public static bool operator ==(Transform3 a, Transform3 b);
```

Equality of all parts.

**Parameters**

- `a` ([`Transform3`](Transform3.md))
- `b` ([`Transform3`](Transform3.md))

**Returns:** `bool`
