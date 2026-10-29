[API reference](../index.md) › OneGame.Physics

# OneGame.Physics

Preview, built on a preview of OnePhysics: the API may change before it is stable. 2D rigid bodies, characters, triggers and ray casts for deterministic simulations on fixed-point positions, stepped by the sim tick, with state that is saved, restored and hashed with the world.

**Package:** `RecursiveAI.OneGame.Physics`

```sh
dotnet add package RecursiveAI.OneGame.Physics --prerelease
```

## OneGame.Physics

| Type | Kind | Description |
| --- | --- | --- |
| [`BodyKind`](OneGame.Physics/BodyKind.md) | enum | How a body takes part in the simulation. |
| [`BodyOptions`](OneGame.Physics/BodyOptions.md) | class | What a new body is made of and how it behaves. |
| [`BodyRef`](OneGame.Physics/BodyRef.md) | struct | Names a body in a [`PhysicsSpace2D`](OneGame.Physics/PhysicsSpace2D.md): a slot and a generation, so a reference to a removed body is detected instead of silently pointing at the body that took its slot. |
| [`CharacterMove`](OneGame.Physics/CharacterMove.md) | struct | The result of [`PhysicsSpace2D.Move()`](OneGame.Physics/PhysicsSpace2D.md#m-move-characterref-fvec2). |
| [`CharacterOptions`](OneGame.Physics/CharacterOptions.md) | class | The shape and abilities of a character: an upright capsule that is moved by collide-and-slide and not by forces. |
| [`CharacterRef`](OneGame.Physics/CharacterRef.md) | struct | Names a character in a [`PhysicsSpace2D`](OneGame.Physics/PhysicsSpace2D.md), like [`BodyRef`](OneGame.Physics/BodyRef.md) names a body. |
| [`PhysicsDebugLine`](OneGame.Physics/PhysicsDebugLine.md) | struct | A line of debug geometry in world units. |
| [`PhysicsDebugShapes`](OneGame.Physics/PhysicsDebugShapes.md) | class | Debug geometry for a [`PhysicsSpace2D`](OneGame.Physics/PhysicsSpace2D.md): every shape's outline (colour tells static, kinematic, awake, asleep and trigger apart), contact points with their normals, as plain lines in world units. |
| [`PhysicsEvent`](OneGame.Physics/PhysicsEvent.md) | struct | One event of the last step, resolved to game types. |
| [`PhysicsEventKind`](OneGame.Physics/PhysicsEventKind.md) | enum | What happened between two bodies during a step. |
| [`PhysicsEvents`](OneGame.Physics/PhysicsEvents.md) | struct | The events of the last step, in the order the physics world delivered them: by kind, then by the bodies' slots. |
| [`PhysicsEvents.Enumerator`](OneGame.Physics/PhysicsEvents.Enumerator.md) | struct | Walks the events without allocating. |
| [`PhysicsLimits`](OneGame.Physics/PhysicsLimits.md) | static class | The limits of the fixed-point to physics conversion, and the checked conversions themselves. |
| [`PhysicsOptions`](OneGame.Physics/PhysicsOptions.md) | class | The settings of a [`PhysicsSpace2D`](OneGame.Physics/PhysicsSpace2D.md). |
| [`PhysicsRangeException`](OneGame.Physics/PhysicsRangeException.md) | class | A value was outside what the physics world can convert exactly (±2^21 world units), or was not finite. |
| [`PhysicsSpace2D`](OneGame.Physics/PhysicsSpace2D.md) | class | A 2D physics world that is part of the game world's state. |
| [`RayHit`](OneGame.Physics/RayHit.md) | struct | What a ray met. |
| [`StaleBodyException`](OneGame.Physics/StaleBodyException.md) | class | A body or character reference no longer names anything in the space (it was removed, or the space was restored from a save that does not have it). |
