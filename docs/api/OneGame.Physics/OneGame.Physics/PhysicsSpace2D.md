[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsSpace2D class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`IHotTable`](../../OneGame.Core/OneGame.Core/IHotTable.md)

```csharp
public sealed class PhysicsSpace2D : IHotTable
```

A 2D physics world that is part of the game world's state. It is a hot table of the [`World`](../../OneGame.Core/OneGame.Core/World.md) (so snapshots, saves, the command-log replay and the state hash include it) and it registers a system that steps it once per tick, after the entities.

```csharp
// Setup (the same on every node):
var space = PhysicsSpace2D.AddTo(world);
space.AddBox(new FVec2(40, 1), new FVec2(0, -1), new BodyOptions { Kind = BodyKind.Static });
// in a command or OnTick:
var space = PhysicsSpace2D.Of(World!)!;
Body = space.AddCircle(Fixed.FromRatio(1, 2), new FVec2(0, 5));
Position = space.Position(Body);   // copy what views need into synced properties
```

Everything sim code touches takes and returns onegame types ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md), [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md), [`BodyRef`](BodyRef.md)). The library underneath works in doubles (positions) and floats (everything else); conversions are exact for positions and round to nearest even for the rest, so every node computes the same bits (see `docs/physics.md`).

The space is authority state: the table is [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden), so replicas do not receive it. Entities mirror what views draw into ordinary synced properties.

## Constructors

| Member | Description |
| --- | --- |
| [`PhysicsSpace2D(PhysicsOptions?)`](#m-ctor-physicsoptions) | Creates a space that is not yet part of any world. |

### <a id="m-ctor-physicsoptions"></a>PhysicsSpace2D(PhysicsOptions?)

```csharp
public PhysicsSpace2D(PhysicsOptions? options = null);
```

Creates a space that is not yet part of any world. Most games use [`PhysicsSpace2D.AddTo()`](PhysicsSpace2D.md#m-addto-world-physicsoptions).

**Parameters**

- `options` ([`PhysicsOptions`](PhysicsOptions.md)?): Settings; the same on every node. Null uses the defaults.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): An option is out of range.

## Fields

| Member | Description |
| --- | --- |
| [`TableName`](#m-tablename) | The default table name. |

### <a id="m-tablename"></a>TableName

```csharp
public const string TableName = "physics";
```

The default table name.

**Type:** `string`

## Properties

| Member | Description |
| --- | --- |
| [`BodyCount`](#m-bodycount) | The number of bodies (characters' bodies included). |
| [`Count`](#m-count) | The number of rows. |
| [`Events`](#m-events) | The events of the last step. |
| [`Hash`](#m-hash) | The hash of the physics state and its events; the same on every node when the states are equal. |
| [`Name`](#m-name) | The table's name in snapshots; unique within a world. |
| [`StepCount`](#m-stepcount) | How many steps the space has taken. |
| [`StepSeconds`](#m-stepseconds) | The seconds one step advances, 1 / [`PhysicsOptions.TicksPerSecond`](PhysicsOptions.md#m-tickspersecond). |
| [`Visibility`](#m-visibility) | Whether subscribers receive the table: [`Visibility.Everyone`](../../OneGame.Core/OneGame.Core/Visibility.md#m-everyone) or [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden). |

### <a id="m-bodycount"></a>BodyCount

```csharp
public int BodyCount { get; }
```

The number of bodies (characters' bodies included).

**Type:** `int`

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of rows.

**Type:** `int`

### <a id="m-events"></a>Events

```csharp
public PhysicsEvents Events { get; }
```

The events of the last step. Read them in `OnTick`, before the system steps again.

**Type:** [`PhysicsEvents`](PhysicsEvents.md)

### <a id="m-hash"></a>Hash

```csharp
public ulong Hash { get; }
```

The hash of the physics state and its events; the same on every node when the states are equal. The world hash includes it.

**Type:** `ulong`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The table's name in snapshots; unique within a world.

**Type:** `string`

### <a id="m-stepcount"></a>StepCount

```csharp
public long StepCount { get; }
```

How many steps the space has taken.

**Type:** `long`

### <a id="m-stepseconds"></a>StepSeconds

```csharp
public Fixed StepSeconds { get; }
```

The seconds one step advances, 1 / [`PhysicsOptions.TicksPerSecond`](PhysicsOptions.md#m-tickspersecond).

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-visibility"></a>Visibility

```csharp
public Visibility Visibility { get; }
```

Whether subscribers receive the table: [`Visibility.Everyone`](../../OneGame.Core/OneGame.Core/Visibility.md#m-everyone) or [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden).

Hidden: replicas never receive the space; they get what entities mirror into synced properties.

**Type:** [`Visibility`](../../OneGame.Core/OneGame.Core/Visibility.md)

## Methods

| Member | Description |
| --- | --- |
| [`AddBox(FVec2, FVec2, BodyOptions?)`](#m-addbox-fvec2-fvec2-bodyoptions) | Adds a box. |
| [`AddCapsule(Fixed, Fixed, FVec2, BodyOptions?)`](#m-addcapsule-fixed-fixed-fvec2-bodyoptions) | Adds an upright capsule (a box with half-circle ends) whose centre starts at `at`. |
| [`AddCharacter(FVec2, CharacterOptions?)`](#m-addcharacter-fvec2-characteroptions) | Adds a character: an upright capsule moved by [`PhysicsSpace2D.Move()`](PhysicsSpace2D.md#m-move-characterref-fvec2) with collide-and-slide. |
| [`AddCircle(Fixed, FVec2, BodyOptions?)`](#m-addcircle-fixed-fvec2-bodyoptions) | Adds a circle. |
| [`AddDebugShapes(PhysicsDebugShapes)`](#m-adddebugshapes-physicsdebugshapes) | Adds the outlines of every shape, the contact points and the characters' capsules to `shapes`, for a view to draw while the debug overlay is on. |
| [`AddPolygon(ReadOnlySpan<FVec2>, FVec2, BodyOptions?)`](#m-addpolygon-readonlyspan-fvec2-fvec2-bodyoptions) | Adds a convex polygon of up to eight points given relative to `at`. |
| [`AddTo(World, PhysicsOptions?)`](#m-addto-world-physicsoptions) | Makes a space and adds it to a world as a table and as a system (it steps after the entities, once per tick). |
| [`Angle(BodyRef)`](#m-angle-bodyref) | The body's rotation in radians, counter-clockwise. |
| [`AngularVelocity(BodyRef)`](#m-angularvelocity-bodyref) | The body's angular velocity in radians per second. |
| [`ApplyForce(BodyRef, FVec2)`](#m-applyforce-bodyref-fvec2) | Applies a force to the body's centre for the next step. |
| [`ApplyImpulse(BodyRef, FVec2)`](#m-applyimpulse-bodyref-fvec2) | Gives the body an instant push at its centre (velocity change = impulse / mass). |
| [`BodyOf(CharacterRef)`](#m-bodyof-characterref) | The body of a character, as events and queries name it. |
| [`Clear()`](#m-clear) | Removes every row. |
| [`CopyFrom(IHotTable)`](#m-copyfrom-ihottable) | Makes this table hold exactly the rows of `source`, a table of the same type. |
| [`Exists(BodyRef)`](#m-exists-bodyref) | Whether the reference names a body that exists. |
| [`Exists(CharacterRef)`](#m-exists-characterref) | Whether the reference names a character that exists. |
| [`IsAwake(BodyRef)`](#m-isawake-bodyref) | Whether a dynamic body is awake (static bodies report false). |
| [`IsGrounded(CharacterRef)`](#m-isgrounded-characterref) | Whether the character stood on walkable ground after its last move. |
| [`Move(CharacterRef, FVec2)`](#m-move-characterref-fvec2) | Moves a character by a displacement this tick with collide-and-slide: it stops at walls, climbs small steps, follows slopes up to its limit and rides moving ground. |
| [`Of(World, string)`](#m-of-world-string) | The world's space with this name, or null. |
| [`Position(BodyRef)`](#m-position-bodyref) | Where the body's origin is (the centre for the shapes added here). |
| [`Position(CharacterRef)`](#m-position-characterref) | The character's capsule centre. |
| [`Raycast(FVec2, FVec2, Fixed, out RayHit, uint)`](#m-raycast-fvec2-fvec2-fixed-rayhit-uint) | Casts a ray and finds the nearest body it meets. |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Replaces all rows. |
| [`Remove(BodyRef)`](#m-remove-bodyref) | Removes a body. |
| [`Remove(CharacterRef)`](#m-remove-characterref) | Removes a character and its body. |
| [`SetLayerCollision(int, int, bool)`](#m-setlayercollision-int-int-bool) | Sets whether bodies on two layers collide (all pairs collide at first). |
| [`SetTransform(BodyRef, FVec2, Fixed)`](#m-settransform-bodyref-fvec2-fixed) | Teleports the body. |
| [`SetVelocity(BodyRef, FVec2)`](#m-setvelocity-bodyref-fvec2) | Sets the body's linear velocity (waking it). |
| [`Step()`](#m-step) | Advances the space by one tick. |
| [`Teleport(CharacterRef, FVec2)`](#m-teleport-characterref-fvec2) | Moves a character to a place without sweeping (a respawn). |
| [`Velocity(BodyRef)`](#m-velocity-bodyref) | The body's linear velocity. |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes all rows. |
| [`WriteHash(SyncWriter)`](#m-writehash-syncwriter) | Writes what the world hash and state digests cover. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the rows as a JSON array of objects. |

### <a id="m-addbox-fvec2-fvec2-bodyoptions"></a>AddBox(FVec2, FVec2, BodyOptions?)

```csharp
public BodyRef AddBox(FVec2 size, FVec2 at, BodyOptions? options = null);
```

Adds a box.

**Parameters**

- `size` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Full width and height.
- `at` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where its centre starts.
- `options` ([`BodyOptions`](BodyOptions.md)?): Behaviour; null is a dynamic body with the defaults.

**Returns:** [`BodyRef`](BodyRef.md) — A reference to keep in a synced property.

**Exceptions**

- [`PhysicsRangeException`](PhysicsRangeException.md): A value is outside ±2^21.

### <a id="m-addcapsule-fixed-fixed-fvec2-bodyoptions"></a>AddCapsule(Fixed, Fixed, FVec2, BodyOptions?)

```csharp
public BodyRef AddCapsule(Fixed halfLength, Fixed radius, FVec2 at, BodyOptions? options = null);
```

Adds an upright capsule (a box with half-circle ends) whose centre starts at `at`.

**Parameters**

- `halfLength` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): Half the length of the straight part.
- `radius` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): The radius of the ends.
- `at` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where its centre starts.
- `options` ([`BodyOptions`](BodyOptions.md)?): Behaviour; null is a dynamic body with the defaults.

**Returns:** [`BodyRef`](BodyRef.md) — A reference to keep in a synced property.

**Exceptions**

- [`PhysicsRangeException`](PhysicsRangeException.md): A value is outside ±2^21.

### <a id="m-addcharacter-fvec2-characteroptions"></a>AddCharacter(FVec2, CharacterOptions?)

```csharp
public CharacterRef AddCharacter(FVec2 at, CharacterOptions? options = null);
```

Adds a character: an upright capsule moved by [`PhysicsSpace2D.Move()`](PhysicsSpace2D.md#m-move-characterref-fvec2) with collide-and-slide.

**Parameters**

- `at` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where its centre starts.
- `options` ([`CharacterOptions`](CharacterOptions.md)?): Size and abilities; null is a 1.8 tall person.

**Returns:** [`CharacterRef`](CharacterRef.md) — A reference to keep in a synced property.

**Exceptions**

- [`PhysicsRangeException`](PhysicsRangeException.md): A value is outside ±2^21.

### <a id="m-addcircle-fixed-fvec2-bodyoptions"></a>AddCircle(Fixed, FVec2, BodyOptions?)

```csharp
public BodyRef AddCircle(Fixed radius, FVec2 at, BodyOptions? options = null);
```

Adds a circle.

**Parameters**

- `radius` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): Its radius.
- `at` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where its centre starts.
- `options` ([`BodyOptions`](BodyOptions.md)?): Behaviour; null is a dynamic body with the defaults.

**Returns:** [`BodyRef`](BodyRef.md) — A reference to keep in a synced property.

**Exceptions**

- [`PhysicsRangeException`](PhysicsRangeException.md): A value is outside ±2^21.

### <a id="m-adddebugshapes-physicsdebugshapes"></a>AddDebugShapes(PhysicsDebugShapes)

```csharp
public void AddDebugShapes(PhysicsDebugShapes shapes);
```

Adds the outlines of every shape, the contact points and the characters' capsules to `shapes`, for a view to draw while the debug overlay is on.

**Parameters**

- `shapes` ([`PhysicsDebugShapes`](PhysicsDebugShapes.md)): Receives the geometry (call [`PhysicsDebugShapes.Clear()`](PhysicsDebugShapes.md#m-clear) first).

### <a id="m-addpolygon-readonlyspan-fvec2-fvec2-bodyoptions"></a>AddPolygon(ReadOnlySpan&lt;FVec2&gt;, FVec2, BodyOptions?)

```csharp
public BodyRef AddPolygon(ReadOnlySpan<FVec2> points, FVec2 at, BodyOptions? options = null);
```

Adds a convex polygon of up to eight points given relative to `at`.

**Parameters**

- `points` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)&gt;): The corners.
- `at` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where its origin starts.
- `options` ([`BodyOptions`](BodyOptions.md)?): Behaviour; null is a dynamic body with the defaults.

**Returns:** [`BodyRef`](BodyRef.md) — A reference to keep in a synced property.

**Exceptions**

- [`PhysicsRangeException`](PhysicsRangeException.md): A value is outside ±2^21.
- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The points do not make a polygon.

### <a id="m-addto-world-physicsoptions"></a>AddTo(World, PhysicsOptions?)

```csharp
public static PhysicsSpace2D AddTo(World world, PhysicsOptions? options = null);
```

Makes a space and adds it to a world as a table and as a system (it steps after the entities, once per tick).

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The world; add the space before any save is loaded, in the same order on every node.
- `options` ([`PhysicsOptions`](PhysicsOptions.md)?): Settings; the same on every node.

**Returns:** [`PhysicsSpace2D`](PhysicsSpace2D.md) — The space.

**Exceptions**

- [`DuplicateTypeException`](../../OneGame.Core/OneGame.Core/DuplicateTypeException.md): The world already has a table with this name.

### <a id="m-angle-bodyref"></a>Angle(BodyRef)

```csharp
public Fixed Angle(BodyRef body);
```

The body's rotation in radians, counter-clockwise.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): The body.

**Returns:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The body does not exist.

### <a id="m-angularvelocity-bodyref"></a>AngularVelocity(BodyRef)

```csharp
public Fixed AngularVelocity(BodyRef body);
```

The body's angular velocity in radians per second.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): The body.

**Returns:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The body does not exist.

### <a id="m-applyforce-bodyref-fvec2"></a>ApplyForce(BodyRef, FVec2)

```csharp
public void ApplyForce(BodyRef body, FVec2 force);
```

Applies a force to the body's centre for the next step.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): The body.
- `force` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): The force.

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The body does not exist.

### <a id="m-applyimpulse-bodyref-fvec2"></a>ApplyImpulse(BodyRef, FVec2)

```csharp
public void ApplyImpulse(BodyRef body, FVec2 impulse);
```

Gives the body an instant push at its centre (velocity change = impulse / mass).

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): The body.
- `impulse` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): The impulse.

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The body does not exist.

### <a id="m-bodyof-characterref"></a>BodyOf(CharacterRef)

```csharp
public BodyRef BodyOf(CharacterRef character);
```

The body of a character, as events and queries name it.

**Parameters**

- `character` ([`CharacterRef`](CharacterRef.md)): The character.

**Returns:** [`BodyRef`](BodyRef.md)

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The character does not exist.

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

### <a id="m-exists-bodyref"></a>Exists(BodyRef)

```csharp
public bool Exists(BodyRef body);
```

Whether the reference names a body that exists.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): The reference.

**Returns:** `bool`

### <a id="m-exists-characterref"></a>Exists(CharacterRef)

```csharp
public bool Exists(CharacterRef character);
```

Whether the reference names a character that exists.

**Parameters**

- `character` ([`CharacterRef`](CharacterRef.md)): The reference.

**Returns:** `bool`

### <a id="m-isawake-bodyref"></a>IsAwake(BodyRef)

```csharp
public bool IsAwake(BodyRef body);
```

Whether a dynamic body is awake (static bodies report false).

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): The body.

**Returns:** `bool`

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The body does not exist.

### <a id="m-isgrounded-characterref"></a>IsGrounded(CharacterRef)

```csharp
public bool IsGrounded(CharacterRef character);
```

Whether the character stood on walkable ground after its last move.

**Parameters**

- `character` ([`CharacterRef`](CharacterRef.md)): The character.

**Returns:** `bool`

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The character does not exist.

### <a id="m-move-characterref-fvec2"></a>Move(CharacterRef, FVec2)

```csharp
public CharacterMove Move(CharacterRef character, FVec2 displacement);
```

Moves a character by a displacement this tick with collide-and-slide: it stops at walls, climbs small steps, follows slopes up to its limit and rides moving ground. Gravity and jumps are yours: keep the vertical speed in a synced property and include it in the displacement.

**Parameters**

- `character` ([`CharacterRef`](CharacterRef.md)): The character.
- `displacement` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): How far to move this tick.

**Returns:** [`CharacterMove`](CharacterMove.md) — Where it ended and what it touches.

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The character does not exist.

### <a id="m-of-world-string"></a>Of(World, string)

```csharp
public static PhysicsSpace2D? Of(World world, string name = "physics");
```

The world's space with this name, or null.

**Parameters**

- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): The world.
- `name` (`string`): The table name.

**Returns:** [`PhysicsSpace2D`](PhysicsSpace2D.md)?

### <a id="m-position-bodyref"></a>Position(BodyRef)

```csharp
public FVec2 Position(BodyRef body);
```

Where the body's origin is (the centre for the shapes added here).

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): The body.

**Returns:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The body does not exist.

### <a id="m-position-characterref"></a>Position(CharacterRef)

```csharp
public FVec2 Position(CharacterRef character);
```

The character's capsule centre.

**Parameters**

- `character` ([`CharacterRef`](CharacterRef.md)): The character.

**Returns:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The character does not exist.

### <a id="m-raycast-fvec2-fvec2-fixed-rayhit-uint"></a>Raycast(FVec2, FVec2, Fixed, out RayHit, uint)

```csharp
public bool Raycast(FVec2 origin, FVec2 direction, Fixed maxDistance, out RayHit hit, uint layers = 4294967295);
```

Casts a ray and finds the nearest body it meets. Triggers are skipped.

**Parameters**

- `origin` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where the ray starts.
- `direction` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Which way it goes (any length except zero).
- `maxDistance` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): How far it reaches.
- `hit` ([`RayHit`](RayHit.md)): What it met.
- `layers` (`uint`): Which layers count, as a bit mask (bit i = layer i). Default all.

**Returns:** `bool` — True when something was hit.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The direction is zero or the distance is not positive.

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

Removes a body.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): The body.

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The body does not exist.

### <a id="m-remove-characterref"></a>Remove(CharacterRef)

```csharp
public void Remove(CharacterRef character);
```

Removes a character and its body.

**Parameters**

- `character` ([`CharacterRef`](CharacterRef.md)): The character.

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The character does not exist.

### <a id="m-setlayercollision-int-int-bool"></a>SetLayerCollision(int, int, bool)

```csharp
public void SetLayerCollision(int layerA, int layerB, bool collide);
```

Sets whether bodies on two layers collide (all pairs collide at first). Part of the saved state.

**Parameters**

- `layerA` (`int`): A layer, 0 to 31.
- `layerB` (`int`): A layer, 0 to 31.
- `collide` (`bool`): Whether they collide.

### <a id="m-settransform-bodyref-fvec2-fixed"></a>SetTransform(BodyRef, FVec2, Fixed)

```csharp
public void SetTransform(BodyRef body, FVec2 position, Fixed angle);
```

Teleports the body.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): The body.
- `position` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): The new position.
- `angle` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): The new rotation in radians.

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The body does not exist.

### <a id="m-setvelocity-bodyref-fvec2"></a>SetVelocity(BodyRef, FVec2)

```csharp
public void SetVelocity(BodyRef body, FVec2 velocity);
```

Sets the body's linear velocity (waking it).

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): The body.
- `velocity` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): The new velocity.

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The body does not exist.

### <a id="m-step"></a>Step()

```csharp
public void Step();
```

Advances the space by one tick. The system added by [`PhysicsSpace2D.AddTo()`](PhysicsSpace2D.md#m-addto-world-physicsoptions) calls this; call it yourself only for a space you did not add to a world.

### <a id="m-teleport-characterref-fvec2"></a>Teleport(CharacterRef, FVec2)

```csharp
public void Teleport(CharacterRef character, FVec2 position);
```

Moves a character to a place without sweeping (a respawn).

**Parameters**

- `character` ([`CharacterRef`](CharacterRef.md)): The character.
- `position` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): The new capsule centre.

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The character does not exist.

### <a id="m-velocity-bodyref"></a>Velocity(BodyRef)

```csharp
public FVec2 Velocity(BodyRef body);
```

The body's linear velocity.

**Parameters**

- `body` ([`BodyRef`](BodyRef.md)): The body.

**Returns:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)

**Exceptions**

- [`StaleBodyException`](StaleBodyException.md): The body does not exist.

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
public void Write(SyncWriter writer);
```

Writes all rows.

The whole physics snapshot, byte-exact. Written for saves, replays and state digests, not once per tick: the world hash uses [`PhysicsSpace2D.WriteHash()`](PhysicsSpace2D.md#m-writehash-syncwriter).

**Parameters**

- `writer` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md))

### <a id="m-writehash-syncwriter"></a>WriteHash(SyncWriter)

```csharp
public void WriteHash(SyncWriter writer);
```

Writes what the world hash and state digests cover. The default is the whole table; a table whose full encoding is large can write a digest of itself here (the hash must change whenever the state does).

**Parameters**

- `writer` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md)): Receives the bytes.

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
public void WriteJson(JsonOut json);
```

Writes the rows as a JSON array of objects.

**Parameters**

- `json` ([`JsonOut`](../../OneGame.Core/OneGame.Core/JsonOut.md))
