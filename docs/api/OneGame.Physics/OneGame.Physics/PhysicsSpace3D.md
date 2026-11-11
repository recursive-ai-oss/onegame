[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsSpace3D class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`IHotTable`](../../OneGame.Core/OneGame.Core/IHotTable.md)

```csharp
public sealed class PhysicsSpace3D : IHotTable
```

A deterministic 3D physics space saved as a Hidden world table and stepped after entities. Coordinates are local to [`PhysicsSpace3D.Origin`](PhysicsSpace3D.md#m-origin).

## Constructors

| Member | Description |
| --- | --- |
| [`PhysicsSpace3D(PhysicsOptions3D?)`](#m-ctor-physicsoptions3d) | Creates a standalone space; [`PhysicsSpace3D.AddTo()`](PhysicsSpace3D.md#m-addto-world-physicsoptions3d) also registers tick stepping. |

### <a id="m-ctor-physicsoptions3d"></a>PhysicsSpace3D(PhysicsOptions3D?)

```csharp
public PhysicsSpace3D(PhysicsOptions3D? options = null);
```

Creates a standalone space; [`PhysicsSpace3D.AddTo()`](PhysicsSpace3D.md#m-addto-world-physicsoptions3d) also registers tick stepping.

**Parameters**

- `options` ([`PhysicsOptions3D`](PhysicsOptions3D.md)?)

## Fields

| Member | Description |
| --- | --- |
| [`TableName`](#m-tablename) | The default table name. |

### <a id="m-tablename"></a>TableName

```csharp
public const string TableName = "physics3d";
```

The default table name.

**Type:** `string`

## Properties

| Member | Description |
| --- | --- |
| [`BodyCount`](#m-bodycount) | Live body count, including character bodies. |
| [`Count`](#m-count) | The number of rows. |
| [`Events`](#m-events) | Previous step's events, in canonical delivery order. |
| [`Hash`](#m-hash) | Hash of every saved physics section, including events and origin. |
| [`Name`](#m-name) | The table's name in snapshots; unique within a world. |
| [`Origin`](#m-origin) | Accumulated origin offset. |
| [`StepCount`](#m-stepcount) | Completed physics steps. |
| [`StepSeconds`](#m-stepseconds) | The fixed tick length. |
| [`Visibility`](#m-visibility) | Whether subscribers receive the table: [`Visibility.Everyone`](../../OneGame.Core/OneGame.Core/Visibility.md#m-everyone) or [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden). |

### <a id="m-bodycount"></a>BodyCount

```csharp
public int BodyCount { get; }
```

Live body count, including character bodies.

**Type:** `int`

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of rows.

**Type:** `int`

### <a id="m-events"></a>Events

```csharp
public PhysicsEvents3D Events { get; }
```

Previous step's events, in canonical delivery order.

**Type:** [`PhysicsEvents3D`](PhysicsEvents3D.md)

### <a id="m-hash"></a>Hash

```csharp
public ulong Hash { get; }
```

Hash of every saved physics section, including events and origin.

**Type:** `ulong`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The table's name in snapshots; unique within a world.

**Type:** `string`

### <a id="m-origin"></a>Origin

```csharp
public FVec3 Origin { get; }
```

Accumulated origin offset. Included in snapshots and the hash.

**Type:** [`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)

### <a id="m-stepcount"></a>StepCount

```csharp
public long StepCount { get; }
```

Completed physics steps.

**Type:** `long`

### <a id="m-stepseconds"></a>StepSeconds

```csharp
public Fixed StepSeconds { get; }
```

The fixed tick length.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-visibility"></a>Visibility

```csharp
public Visibility Visibility { get; }
```

Whether subscribers receive the table: [`Visibility.Everyone`](../../OneGame.Core/OneGame.Core/Visibility.md#m-everyone) or [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden).

**Type:** [`Visibility`](../../OneGame.Core/OneGame.Core/Visibility.md)

## Methods

| Member | Description |
| --- | --- |
| [`AddBox(FVec3, FVec3, BodyOptions3D?)`](#m-addbox-fvec3-fvec3-bodyoptions3d) | Adds a full-sized box at a local position. |
| [`AddCapsule(Fixed, Fixed, FVec3, BodyOptions3D?)`](#m-addcapsule-fixed-fixed-fvec3-bodyoptions3d) | Adds an upright capsule; halfLength is half the straight section. |
| [`AddCharacter(FVec3, CharacterOptions3D?)`](#m-addcharacter-fvec3-characteroptions3d) | Adds an upright character; the position is its capsule centre. |
| [`AddSphere(Fixed, FVec3, BodyOptions3D?)`](#m-addsphere-fixed-fvec3-bodyoptions3d) | Adds a sphere at a local position. |
| [`AddTo(World, PhysicsOptions3D?)`](#m-addto-world-physicsoptions3d) | Registers a Hidden table and a post-entity step system. |
| [`AddVehicle(BodyRef)`](#m-addvehicle-bodyref) | Attaches the standard four-wheel car suspension and drivetrain to a dynamic chassis. |
| [`ApplyImpulse(BodyRef, FVec3)`](#m-applyimpulse-bodyref-fvec3) | Applies a centre-of-mass impulse. |
| [`CanonicalizeBroadphase()`](#m-canonicalizebroadphase) | Discards broadphase history in canonical shape order. |
| [`Clear()`](#m-clear) | Removes every row. |
| [`CopyFrom(IHotTable)`](#m-copyfrom-ihottable) | Makes this table hold exactly the rows of `source`, a table of the same type. |
| [`DescribeDifferences(ReadOnlySpan<byte>)`](#m-describedifferences-readonlyspan-byte) | Returns named differences against a physics snapshot; invalid data produces an unavailable message. |
| [`Drive(VehicleRef, Fixed, Fixed, Fixed, Fixed)`](#m-drive-vehicleref-fixed-fixed-fixed-fixed) | Sets throttle (-1..1), steering (-1..1), brake (0..1) and handbrake (0..1). |
| [`FindDifferences(PhysicsSpace3D)`](#m-finddifferences-physicsspace3d) | Returns a named SnapshotDiffReport summary for drift/replay tooling, including body slots, generations and fields. |
| [`Fork()`](#m-fork) | Creates an independent structural fork for prediction, preserving handles and pending events. |
| [`Grounded(CharacterRef)`](#m-grounded-characterref) | Whether a character touches walkable ground. |
| [`IsAlive(BodyRef)`](#m-isalive-bodyref) | Whether this reference names a live body in this space. |
| [`Move(CharacterRef, FVec3)`](#m-move-characterref-fvec3) | Moves a character by displacement over one fixed tick. |
| [`Of(World, string)`](#m-of-world-string) | Finds a registered space by table name. |
| [`OverlapSphere(FVec3, Fixed, out RayHit3D, uint)`](#m-overlapsphere-fvec3-fixed-rayhit3d-uint) | Finds a matching body overlapping a sphere. |
| [`Position(BodyRef)`](#m-position-bodyref) | Local position of a body. |
| [`Position(CharacterRef)`](#m-position-characterref) | Local character position. |
| [`Raycast(FVec3, FVec3, Fixed, out RayHit3D, uint)`](#m-raycast-fvec3-fvec3-fixed-rayhit3d-uint) | Casts a ray and returns the nearest matching body. |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Replaces all rows. |
| [`Remove(BodyRef)`](#m-remove-bodyref) | Removes a body and its shapes. |
| [`Remove(CharacterRef)`](#m-remove-characterref) | Removes a character and its capsule. |
| [`Remove(VehicleRef)`](#m-remove-vehicleref) | Removes a vehicle's suspension and drivetrain, retaining its chassis body. |
| [`Restore(ReadOnlySpan<byte>)`](#m-restore-readonlyspan-byte) | Restores a full physics snapshot; invalid input leaves the space unchanged. |
| [`RestoreCompressed(ReadOnlySpan<byte>)`](#m-restorecompressed-readonlyspan-byte) | Restores a compressed snapshot with a 64 MiB decoded size limit. |
| [`RestoreDelta(ReadOnlySpan<byte>, ReadOnlySpan<byte>)`](#m-restoredelta-readonlyspan-byte-readonlyspan-byte) | Restores a delta with a 64 MiB decoded size limit. |
| [`RestoreRegion(ReadOnlySpan<byte>)`](#m-restoreregion-readonlyspan-byte) | Restores a region at the same tick, origin and topology. |
| [`Rotation(BodyRef)`](#m-rotation-bodyref) | Orientation of a body. |
| [`SetLayerCollision(int, int, bool)`](#m-setlayercollision-int-int-bool) | Changes the saved collision matrix for two layers. |
| [`SetVelocity(BodyRef, FVec3)`](#m-setvelocity-bodyref-fvec3) | Sets linear velocity. |
| [`ShiftOrigin(FVec3)`](#m-shiftorigin-fvec3) | Moves the local coordinate frame by an explicit deterministic offset before positions approach ±2^21. |
| [`Snapshot()`](#m-snapshot) | Captures an independent full physics snapshot (not the table envelope). |
| [`SnapshotCompressed()`](#m-snapshotcompressed) | Creates a compressed full physics snapshot. |
| [`SnapshotDelta(ReadOnlySpan<byte>)`](#m-snapshotdelta-readonlyspan-byte) | Creates a delta against a full physics snapshot. |
| [`SnapshotRegion(FVec3, FVec3)`](#m-snapshotregion-fvec3-fvec3) | Captures bodies in inclusive local bounds and their constraint closure. |
| [`SphereCast(FVec3, Fixed, FVec3, Fixed, out RayHit3D, uint)`](#m-spherecast-fvec3-fixed-fvec3-fixed-rayhit3d-uint) | Sweeps a sphere and returns the nearest matching body. |
| [`Step(bool)`](#m-step-bool) | Advances one fixed tick. |
| [`Teleport(CharacterRef, FVec3)`](#m-teleport-characterref-fvec3) | Teleports a character to a local position. |
| [`Teleport(BodyRef, FVec3, FQuat)`](#m-teleport-bodyref-fvec3-fquat) | Teleports a body, preserving velocity. |
| [`Transform(BodyRef)`](#m-transform-bodyref) | Rigid transform with unit scale. |
| [`Velocity(BodyRef)`](#m-velocity-bodyref) | Linear velocity of a body. |
| [`Warmup()`](#m-warmup) | Preallocates stepping, query and snapshot storage for the current scene. |
| [`Wheel(VehicleRef, int)`](#m-wheel-vehicleref-int) | Current state of a wheel (zero-based). |
| [`WheelCount(VehicleRef)`](#m-wheelcount-vehicleref) | Number of wheels attached to a vehicle. |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes all rows. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the rows as a JSON array of objects. |

### <a id="m-addbox-fvec3-fvec3-bodyoptions3d"></a>AddBox(FVec3, FVec3, BodyOptions3D?)

```csharp
public BodyRef AddBox(FVec3 size, FVec3 at, BodyOptions3D? options = null);
```

Adds a full-sized box at a local position.

**Parameters**

- `size` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))
- `at` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))
- `options` ([`BodyOptions3D`](BodyOptions3D.md)?)

**Returns:** [`BodyRef`](BodyRef.md)

### <a id="m-addcapsule-fixed-fixed-fvec3-bodyoptions3d"></a>AddCapsule(Fixed, Fixed, FVec3, BodyOptions3D?)

```csharp
public BodyRef AddCapsule(Fixed halfLength, Fixed radius, FVec3 at, BodyOptions3D? options = null);
```

Adds an upright capsule; halfLength is half the straight section.

**Parameters**

- `halfLength` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md))
- `radius` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md))
- `at` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))
- `options` ([`BodyOptions3D`](BodyOptions3D.md)?)

**Returns:** [`BodyRef`](BodyRef.md)

### <a id="m-addcharacter-fvec3-characteroptions3d"></a>AddCharacter(FVec3, CharacterOptions3D?)

```csharp
public CharacterRef AddCharacter(FVec3 at, CharacterOptions3D? options = null);
```

Adds an upright character; the position is its capsule centre. SlopeLimitRadians is passed directly to the physics controller.

**Parameters**

- `at` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))
- `options` ([`CharacterOptions3D`](CharacterOptions3D.md)?)

**Returns:** [`CharacterRef`](CharacterRef.md)

### <a id="m-addsphere-fixed-fvec3-bodyoptions3d"></a>AddSphere(Fixed, FVec3, BodyOptions3D?)

```csharp
public BodyRef AddSphere(Fixed radius, FVec3 at, BodyOptions3D? options = null);
```

Adds a sphere at a local position.

**Parameters**

- `radius` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md))
- `at` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))
- `options` ([`BodyOptions3D`](BodyOptions3D.md)?)

**Returns:** [`BodyRef`](BodyRef.md)

### <a id="m-addto-world-physicsoptions3d"></a>AddTo(World, PhysicsOptions3D?)

```csharp
public static PhysicsSpace3D AddTo(World world, PhysicsOptions3D? options = null);
```

Registers a Hidden table and a post-entity step system. Add in the same order on every node.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))
- `options` ([`PhysicsOptions3D`](PhysicsOptions3D.md)?)

**Returns:** [`PhysicsSpace3D`](PhysicsSpace3D.md)

### <a id="m-addvehicle-bodyref"></a>AddVehicle(BodyRef)

```csharp
public VehicleRef AddVehicle(BodyRef chassis);
```

Attaches the standard four-wheel car suspension and drivetrain to a dynamic chassis. Chassis size should be roughly (2, 0.5, 4), forward +Z.

**Parameters**

- `chassis` ([`BodyRef`](BodyRef.md))

**Returns:** [`VehicleRef`](VehicleRef.md)

### <a id="m-applyimpulse-bodyref-fvec3"></a>ApplyImpulse(BodyRef, FVec3)

```csharp
public void ApplyImpulse(BodyRef body, FVec3 impulse);
```

Applies a centre-of-mass impulse.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md))
- `impulse` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))

### <a id="m-canonicalizebroadphase"></a>CanonicalizeBroadphase()

```csharp
public void CanonicalizeBroadphase();
```

Discards broadphase history in canonical shape order. Call between steps on both peers after a region repair before comparing full hashes. Allocates tooling buffers.

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Removes every row.

### <a id="m-copyfrom-ihottable"></a>CopyFrom(IHotTable)

```csharp
public void CopyFrom(IHotTable source);
```

Makes this table hold exactly the rows of `source`, a table of the same type. Generated tables copy the columns in place without allocating once they have room; the default goes through the encoded form.

**Parameters**

- `source` ([`IHotTable`](../../OneGame.Core/OneGame.Core/IHotTable.md))

### <a id="m-describedifferences-readonlyspan-byte"></a>DescribeDifferences(ReadOnlySpan&lt;byte&gt;)

```csharp
public string DescribeDifferences(ReadOnlySpan<byte> expected);
```

Returns named differences against a physics snapshot; invalid data produces an unavailable message.

**Parameters**

- `expected` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** `string`

### <a id="m-drive-vehicleref-fixed-fixed-fixed-fixed"></a>Drive(VehicleRef, Fixed, Fixed, Fixed, Fixed)

```csharp
public void Drive(VehicleRef vehicle, Fixed throttle, Fixed steering = default, Fixed brake = default, Fixed handbrake = default);
```

Sets throttle (-1..1), steering (-1..1), brake (0..1) and handbrake (0..1).

**Parameters**

- `vehicle` ([`VehicleRef`](VehicleRef.md))
- `throttle` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md))
- `steering` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md))
- `brake` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md))
- `handbrake` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md))

### <a id="m-finddifferences-physicsspace3d"></a>FindDifferences(PhysicsSpace3D)

```csharp
public string FindDifferences(PhysicsSpace3D other);
```

Returns a named SnapshotDiffReport summary for drift/replay tooling, including body slots, generations and fields.

**Parameters**

- `other` ([`PhysicsSpace3D`](PhysicsSpace3D.md))

**Returns:** `string`

### <a id="m-fork"></a>Fork()

```csharp
public PhysicsSpace3D Fork();
```

Creates an independent structural fork for prediction, preserving handles and pending events.

**Returns:** [`PhysicsSpace3D`](PhysicsSpace3D.md)

### <a id="m-grounded-characterref"></a>Grounded(CharacterRef)

```csharp
public bool Grounded(CharacterRef character);
```

Whether a character touches walkable ground.

**Parameters**

- `character` ([`CharacterRef`](CharacterRef.md))

**Returns:** `bool`

### <a id="m-isalive-bodyref"></a>IsAlive(BodyRef)

```csharp
public bool IsAlive(BodyRef body);
```

Whether this reference names a live body in this space.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md))

**Returns:** `bool`

### <a id="m-move-characterref-fvec3"></a>Move(CharacterRef, FVec3)

```csharp
public CharacterMove3D Move(CharacterRef character, FVec3 displacement);
```

Moves a character by displacement over one fixed tick.

**Parameters**

- `character` ([`CharacterRef`](CharacterRef.md))
- `displacement` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))

**Returns:** [`CharacterMove3D`](CharacterMove3D.md)

### <a id="m-of-world-string"></a>Of(World, string)

```csharp
public static PhysicsSpace3D? Of(World world, string name = "physics3d");
```

Finds a registered space by table name.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))
- `name` (`string`)

**Returns:** [`PhysicsSpace3D`](PhysicsSpace3D.md)?

### <a id="m-overlapsphere-fvec3-fixed-rayhit3d-uint"></a>OverlapSphere(FVec3, Fixed, out RayHit3D, uint)

```csharp
public bool OverlapSphere(FVec3 center, Fixed radius, out RayHit3D hit, uint layers = 4294967295);
```

Finds a matching body overlapping a sphere.

**Parameters**

- `center` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))
- `radius` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md))
- `hit` ([`RayHit3D`](RayHit3D.md))
- `layers` (`uint`)

**Returns:** `bool`

### <a id="m-position-bodyref"></a>Position(BodyRef)

```csharp
public FVec3 Position(BodyRef body);
```

Local position of a body.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md))

**Returns:** [`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)

### <a id="m-position-characterref"></a>Position(CharacterRef)

```csharp
public FVec3 Position(CharacterRef character);
```

Local character position.

**Parameters**

- `character` ([`CharacterRef`](CharacterRef.md))

**Returns:** [`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)

### <a id="m-raycast-fvec3-fvec3-fixed-rayhit3d-uint"></a>Raycast(FVec3, FVec3, Fixed, out RayHit3D, uint)

```csharp
public bool Raycast(FVec3 origin, FVec3 direction, Fixed maxDistance, out RayHit3D hit, uint layers = 4294967295);
```

Casts a ray and returns the nearest matching body.

**Parameters**

- `origin` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))
- `direction` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))
- `maxDistance` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md))
- `hit` ([`RayHit3D`](RayHit3D.md))
- `layers` (`uint`)

**Returns:** `bool`

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public void Read(ref SyncReader reader);
```

Replaces all rows. All rows are decoded before any is stored, so an error leaves the table unchanged.

**Parameters**

- `reader` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

### <a id="m-remove-bodyref"></a>Remove(BodyRef)

```csharp
public void Remove(BodyRef body);
```

Removes a body and its shapes.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md))

### <a id="m-remove-characterref"></a>Remove(CharacterRef)

```csharp
public void Remove(CharacterRef character);
```

Removes a character and its capsule.

**Parameters**

- `character` ([`CharacterRef`](CharacterRef.md))

### <a id="m-remove-vehicleref"></a>Remove(VehicleRef)

```csharp
public void Remove(VehicleRef vehicle);
```

Removes a vehicle's suspension and drivetrain, retaining its chassis body.

**Parameters**

- `vehicle` ([`VehicleRef`](VehicleRef.md))

### <a id="m-restore-readonlyspan-byte"></a>Restore(ReadOnlySpan&lt;byte&gt;)

```csharp
public void Restore(ReadOnlySpan<byte> snapshot);
```

Restores a full physics snapshot; invalid input leaves the space unchanged.

**Parameters**

- `snapshot` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

### <a id="m-restorecompressed-readonlyspan-byte"></a>RestoreCompressed(ReadOnlySpan&lt;byte&gt;)

```csharp
public void RestoreCompressed(ReadOnlySpan<byte> compressed);
```

Restores a compressed snapshot with a 64 MiB decoded size limit.

**Parameters**

- `compressed` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

### <a id="m-restoredelta-readonlyspan-byte-readonlyspan-byte"></a>RestoreDelta(ReadOnlySpan&lt;byte&gt;, ReadOnlySpan&lt;byte&gt;)

```csharp
public void RestoreDelta(ReadOnlySpan<byte> baseline, ReadOnlySpan<byte> delta);
```

Restores a delta with a 64 MiB decoded size limit.

**Parameters**

- `baseline` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)
- `delta` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

### <a id="m-restoreregion-readonlyspan-byte"></a>RestoreRegion(ReadOnlySpan&lt;byte&gt;)

```csharp
public void RestoreRegion(ReadOnlySpan<byte> region);
```

Restores a region at the same tick, origin and topology. Invalid input leaves the space unchanged.

**Parameters**

- `region` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

### <a id="m-rotation-bodyref"></a>Rotation(BodyRef)

```csharp
public FQuat Rotation(BodyRef body);
```

Orientation of a body.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md))

**Returns:** [`FQuat`](../../OneGame.Core/OneGame.Core/FQuat.md)

### <a id="m-setlayercollision-int-int-bool"></a>SetLayerCollision(int, int, bool)

```csharp
public void SetLayerCollision(int a, int b, bool collide);
```

Changes the saved collision matrix for two layers.

**Parameters**

- `a` (`int`)
- `b` (`int`)
- `collide` (`bool`)

### <a id="m-setvelocity-bodyref-fvec3"></a>SetVelocity(BodyRef, FVec3)

```csharp
public void SetVelocity(BodyRef body, FVec3 velocity);
```

Sets linear velocity.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md))
- `velocity` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))

### <a id="m-shiftorigin-fvec3"></a>ShiftOrigin(FVec3)

```csharp
public void ShiftOrigin(FVec3 offset);
```

Moves the local coordinate frame by an explicit deterministic offset before positions approach ±2^21. The library saves and hashes the accumulated origin.

**Parameters**

- `offset` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))

### <a id="m-snapshot"></a>Snapshot()

```csharp
public byte[] Snapshot();
```

Captures an independent full physics snapshot (not the table envelope).

**Returns:** `byte[]`

### <a id="m-snapshotcompressed"></a>SnapshotCompressed()

```csharp
public byte[] SnapshotCompressed();
```

Creates a compressed full physics snapshot. Tooling operation; allocates.

**Returns:** `byte[]`

### <a id="m-snapshotdelta-readonlyspan-byte"></a>SnapshotDelta(ReadOnlySpan&lt;byte&gt;)

```csharp
public byte[] SnapshotDelta(ReadOnlySpan<byte> baseline);
```

Creates a delta against a full physics snapshot. Tooling operation; allocates.

**Parameters**

- `baseline` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** `byte[]`

### <a id="m-snapshotregion-fvec3-fvec3"></a>SnapshotRegion(FVec3, FVec3)

```csharp
public byte[] SnapshotRegion(FVec3 minimum, FVec3 maximum);
```

Captures bodies in inclusive local bounds and their constraint closure. Tooling operation; allocates.

**Parameters**

- `minimum` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))
- `maximum` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))

**Returns:** `byte[]`

### <a id="m-spherecast-fvec3-fixed-fvec3-fixed-rayhit3d-uint"></a>SphereCast(FVec3, Fixed, FVec3, Fixed, out RayHit3D, uint)

```csharp
public bool SphereCast(FVec3 origin, Fixed radius, FVec3 direction, Fixed maxDistance, out RayHit3D hit, uint layers = 4294967295);
```

Sweeps a sphere and returns the nearest matching body.

**Parameters**

- `origin` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))
- `radius` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md))
- `direction` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))
- `maxDistance` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md))
- `hit` ([`RayHit3D`](RayHit3D.md))
- `layers` (`uint`)

**Returns:** `bool`

### <a id="m-step-bool"></a>Step(bool)

```csharp
public void Step(bool resimulating = false);
```

Advances one fixed tick. During rollback, suppresses callbacks but retains polled events.

**Parameters**

- `resimulating` (`bool`)

### <a id="m-teleport-characterref-fvec3"></a>Teleport(CharacterRef, FVec3)

```csharp
public void Teleport(CharacterRef character, FVec3 at);
```

Teleports a character to a local position.

**Parameters**

- `character` ([`CharacterRef`](CharacterRef.md))
- `at` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))

### <a id="m-teleport-bodyref-fvec3-fquat"></a>Teleport(BodyRef, FVec3, FQuat)

```csharp
public void Teleport(BodyRef body, FVec3 at, FQuat rotation = default);
```

Teleports a body, preserving velocity.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md))
- `at` ([`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md))
- `rotation` ([`FQuat`](../../OneGame.Core/OneGame.Core/FQuat.md))

### <a id="m-transform-bodyref"></a>Transform(BodyRef)

```csharp
public Transform3 Transform(BodyRef body);
```

Rigid transform with unit scale.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md))

**Returns:** [`Transform3`](../../OneGame.Core/OneGame.Core/Transform3.md)

### <a id="m-velocity-bodyref"></a>Velocity(BodyRef)

```csharp
public FVec3 Velocity(BodyRef body);
```

Linear velocity of a body.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md))

**Returns:** [`FVec3`](../../OneGame.Core/OneGame.Core/FVec3.md)

### <a id="m-warmup"></a>Warmup()

```csharp
public void Warmup();
```

Preallocates stepping, query and snapshot storage for the current scene.

### <a id="m-wheel-vehicleref-int"></a>Wheel(VehicleRef, int)

```csharp
public PhysicsWheel3D Wheel(VehicleRef vehicle, int index);
```

Current state of a wheel (zero-based).

**Parameters**

- `vehicle` ([`VehicleRef`](VehicleRef.md))
- `index` (`int`)

**Returns:** [`PhysicsWheel3D`](PhysicsWheel3D.md)

### <a id="m-wheelcount-vehicleref"></a>WheelCount(VehicleRef)

```csharp
public int WheelCount(VehicleRef vehicle);
```

Number of wheels attached to a vehicle.

**Parameters**

- `vehicle` ([`VehicleRef`](VehicleRef.md))

**Returns:** `int`

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
public void Write(SyncWriter writer);
```

Writes all rows.

**Parameters**

- `writer` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md))

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
public void WriteJson(JsonOut json);
```

Writes the rows as a JSON array of objects.

**Parameters**

- `json` ([`JsonOut`](../../OneGame.Core/OneGame.Core/JsonOut.md))
