# Physics

**Preview.** `OneGame.Physics` is a prerelease package built on a prerelease of OnePhysics, and its API may change before it is stable. Add it with `dotnet add package RecursiveAI.OneGame.Physics --prerelease`.

`OneGame.Physics` gives a game 2D rigid bodies, characters, triggers and ray casts that behave the same on every node. Positions are `Fixed` numbers, the physics world is part of the game world's state (it is stepped by the tick, saved with the world, restored with it, and covered by the state hash), and events reach sim code in a fixed order. It wraps [OnePhysics](https://www.nuget.org/packages/RecursiveAI.OnePhysics), which does the simulation. Reference it from a game with `RecursiveAI.OneGame.Physics` (in a checkout: a project reference to `src/OneGame.Physics`). It is not part of the default game template or `Game.targets`. It is published as a prerelease (`<engine version>-preview.N`) because OnePhysics is one; the other engine packages stay stable and never depend on it. `examples/physics` shows all of it in one file: a character that walks, jumps between platforms and pushes crates, a goal zone, and the debug overlay.

```csharp
using OneGame.Physics;
```

Everything you call from sim code takes and returns onegame types (`Fixed`, `FVec2`, `BodyRef`), and the assembly is marked `[SimSafe]`, so the sim analyzers accept it. You never write a `float`.

## Setting it up

The space is added where the game builds its world, so that a world made to load a save has it too:

```csharp
public override void RegisterSystems(World world) => PhysicsSpace2D.AddTo(world);   // gravity (0, -9.81), 60 ticks per second

public override void Setup(World world)
{
    var space = PhysicsSpace2D.Of(world)!;
    space.AddBox(new FVec2(40, 1), new FVec2(0, -1), new BodyOptions { Kind = BodyKind.Static });
    ...
}
```

`PhysicsOptions` (gravity, ticks per second, substeps, sleeping, whether hit and stay events are recorded) is passed to `AddTo` and must be the same on every node. One world unit is one metre; pick a scale for the view (the example draws 16 pixels per unit). Y is up in the simulation: flip it when drawing.

`AddTo` registers the space as a hot table named `physics` and a system that steps it once per tick, after the entities, by 1 / `TicksPerSecond` seconds (one IEEE division, the same everywhere). Commands run first, then entity `OnTick`s, then the step.

## Bodies, characters, queries

```csharp
BodyRef crate = space.AddBox(new FVec2(1, 1), new FVec2(0, 5));                        // dynamic by default
BodyRef ball = space.AddCircle(Fixed.FromRatio(1, 2), new FVec2(3, 5), new BodyOptions { Restitution = Fixed.FromRatio(1, 2) });
BodyRef zone = space.AddBox(new FVec2(4, 4), new FVec2(0, 3), new BodyOptions { Kind = BodyKind.Static, IsTrigger = true });
CharacterRef hero = space.AddCharacter(new FVec2(-4, 1), new CharacterOptions { Height = Fixed.FromInt(2) });

FVec2 p = space.Position(crate);  Fixed a = space.Angle(crate);  FVec2 v = space.Velocity(crate);
space.ApplyImpulse(crate, new FVec2(3, 0));  space.SetVelocity(ball, FVec2.Zero);  space.Remove(crate);
CharacterMove m = space.Move(hero, new FVec2(speed * dt, vy * dt));    // collide-and-slide: m.Grounded, m.HitCeiling, m.HitSide, m.Position
if (space.Raycast(origin, direction, Fixed.FromInt(20), out RayHit hit)) { /* hit.Body, hit.Point, hit.Normal, hit.Distance */ }
```

- `AddBox`, `AddCircle`, `AddCapsule`, `AddPolygon`; `BodyOptions` sets kind (static, kinematic, dynamic), angle, density, friction, restitution, layer, trigger, fixed rotation, bullet, damping, gravity scale and starting velocity.
- **Characters** are upright capsules that you move by displacement: they stop at walls, climb steps up to `StepOffset`, follow slopes up to `SlopeLimit`, ride moving ground and push dynamic bodies (`PushStrength`). Gravity and jumping are game rules: keep the vertical speed in a synced property of your entity (as `Hero` does in the example) and put it in the displacement. `StepSeconds` is the tick length. A character is a kinematic body that moves when the space steps, so `Position(character)` read in `OnTick` is where it ended the last step (`CharacterMove.Position` is where it stood when it was moved).
- **Layers.** Bodies are on one of 32 layers; `SetLayerCollision(a, b, false)` makes two layers pass through each other, and `Raycast`'s `layers` mask picks what a ray sees. The matrix is saved with the space.
- **Triggers** have no response: things pass through them, and the space reports `TriggerEnter` and `TriggerExit`.
- Not wrapped yet: joints, chains and segments, shape casts and overlap queries, per-shape materials on a body with several shapes, moving the world origin. The library's own API takes floats, so it cannot be called from sim code directly; a game that needs more asks for a wrapper.

## Events

`space.Events` holds what the last step reported: `ContactBegin`, `ContactEnd`, `TriggerEnter`, `TriggerExit`, `Sleep`, `Wake`, and with options `ContactStay` and `ContactHit`. Each carries the two bodies (`A`, `B`, as `BodyRef`s), and contacts also a point, a normal and (for hits) the approach speed. Read them in `OnTick`:

```csharp
foreach (var e in PhysicsSpace2D.Of(World!)!.Events)
{
    if (e.Kind == PhysicsEventKind.TriggerEnter && e.Involves(Beacon)) { Entered++; }
}
```

The list is the library's delivery order (by kind, then by the bodies' slots), the same on every node and on a replay. It is part of the saved state, so a restore keeps the events of the step before it and reading them after a restore gives what the original run saw. A step replaces the list: an entity that wants an event must read it in the tick after the step that produced it. Events are polled; there are no callbacks.

**Triggers and characters.** Trigger enter and exit events arrive for dynamic bodies, kinematic bodies and characters alike.

## What is synced, and how

There is one source of truth for each thing:

- **The physics world is the source of truth for everything it holds**: positions, rotations, velocities, contacts and their warm-start impulses, which bodies sleep, the layer matrix, characters, the step counter, the event list. It lives in one hidden hot table, `physics` (`PhysicsSpace2D.TableName`). Everything that holds a world holds it: a snapshot or save writes the whole physics snapshot in OnePhysics' own byte-exact format, a restore reads it back (validated before anything changes; a bad one is refused and the world is untouched), and a command-log replay or a restored world continues bit for bit.
- **References are synced values.** `BodyRef` and `CharacterRef` (a slot and a generation) are ordinary `[Synced]` properties, so an entity that owns a body keeps the reference in its own state, and a reference to a removed body is detected (`StaleBodyException`) instead of pointing at whatever took its slot. A reference taken before a save names the same body after the restore.
- **What views draw is mirrored into synced properties by the entities**, as `Crate.Position` and `Crate.Angle` are in the example: `OnTick` copies `space.Position(Body)` into a `Fixed` property. The table is `Visibility.Hidden`: replicas never receive the physics world (which for a busy world would be a whole snapshot per tick), so a client draws from the mirrored properties and holds no space of its own (`PhysicsSpace2D.Of(world)` is null there). The mirror is derived data: it is rewritten every tick, never read back by the sim.
- **The hash.** The state hash covers the whole physics snapshot like any other table, written into a reused buffer, so a steady-state tick allocates nothing. The table is hidden: replicas never receive it, and drift detection on a replica compares the view hash, which leaves it out, so the mirrored properties are what a replica is checked on.
- **Prediction.** A predicted command (`[Command(Predicted = true)]`) runs on a replica's predicted world, which has no physics space. Do not predict commands that read or change physics.

**When the save format changes.** The table writes a one-byte format version, the tick rate and then the OnePhysics snapshot. OnePhysics may change its snapshot format before its 0.2 release. A save whose table format version differs is refused as `SyncError.UnsupportedVersion`, and one whose OnePhysics snapshot the library rejects (another format version, damaged bytes) as `SyncError.InvalidValue`; both come out of the world's `ReadSnapshot` (and as `RestoreFailed` from `GameInstance.LoadSnapshot`), and nothing is loaded half-way; a save made at another `TicksPerSecond` is refused as a mismatch, because stepping it at this rate would not continue the same simulation. There is no migration for physics state between format versions: a game that must keep old saves across an upgrade of this package should keep the previous package version until it re-saves, or rebuild the physics world from its own entity data.

## Range, rounding and determinism

Positions, sizes and distances are limited to ±2^21 (2,097,152) world units, so that every `Fixed` in range is exactly a double (32 + 21 bits fit in 53) and the library places every body identically everywhere. A value outside the range throws `PhysicsRangeException` naming the limit; nothing is clamped or rounded silently. Velocities, impulses, densities and the like go through single-precision floats, which is the library's own precision: `Fixed` to `float` rounds to nearest even (one IEEE conversion), and values read back are converted to `Fixed` exactly. `PhysicsLimits.ToDouble`, `ToSingle` and `FromDouble` are the checked conversions.

OnePhysics uses only IEEE `+ - × ÷ sqrt` on its numbers and no fused operations, so a run gives the same bits on x64, arm64 and WebAssembly at any vector width. The space is created with one thread. `tools/ci/physics-probe.sh` runs a scene of boxes, balls, a polygon, a trigger and a character through 900 ticks and compares its hashes and end positions with pinned values; they are equal on x64, on arm64 (under qemu) and on .NET's WebAssembly runtime under node, and CI runs it on Linux and Windows.

## Cost

Stepping and the wrapper's calls (`Move`, reads, event walking, ray casts) allocate nothing. The library allocates a small writer object each time the world hash is computed (40 bytes, every tick, because the world hash is always on), and a step sometimes grows an internal list (176 bytes in the worst tick measured over a 40-box pile). So a tick is not strictly allocation-free, though it stays well under 256 bytes. A world hash walks the physics state once. OnePhysics is written for exact results first and is a few times slower than the fastest native engines.

## Seeing it

While the debug overlay is on (F3, or `overlay: on` in a scenario) `Draw2D.DebugVisible` is true, and a view can collect `PhysicsDebugShapes` with `space.AddDebugShapes(shapes)`: outlines of every shape (static, kinematic, awake, asleep and trigger shapes in different colours) and contact points with normals, as lines in world units (the example draws them in a few lines). Release builds never set the flag. `state_query` with `table: physics` shows the step count, the hash and every body's position, velocity and sleep state (`property: "bodies[9].awake"`).
