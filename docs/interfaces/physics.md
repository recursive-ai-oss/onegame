# Physics table integration

## 3D

`PhysicsSpace3D.AddTo(world, options)` registers a Hidden table named `physics3d`
and a post-entity tick system. `PhysicsOptions3D` chooses gravity, fixed tick rate,
substeps and sleeping. All simulation-facing values are `Fixed`, `FVec3`,
`FQuat` and `Transform3`; conversion uses OneMath `FixedBridge`.

Bodies use `BodyRef`, characters use `CharacterRef` (scoped to their space), and
vehicles use `VehicleRef`. Boxes, spheres and capsules support `BodyOptions3D`.
Position, rotation, velocity, impulses, teleport, removal, layer filtering,
raycasts, sphere casts and sphere overlaps are exposed without floating-point
arguments. Characters move by displacement; vehicles attach a standard car to a
chassis and expose drive controls and wheel state. `Events` polls the previous
step in the library's canonical order, without callbacks.

Origin policy: the simulation authority explicitly calls `ShiftOrigin(offset)`
before local coordinates approach ±2^21. Offsets must lie on the physics 2^-31 grid (even `Fixed.Raw`);
the accumulated origin stays within the signed 32-bit world-unit range. Positions passed to and returned from
the API are local to `Origin`; entities may separately mirror global positions.
The offset is a deterministic command/tick decision, never a camera decision.
The physics snapshot includes the accumulated origin and the state hash includes
it. Full saves, forks, deltas and compressed snapshots preserve it. Conversion uses FixedBridge, with explicit exact inclusive ±2^21 endpoints.

`Write`/`Read` serialize a bounded full snapshot and tick rate. `CopyFrom` restores
that state; `Fork` uses the physics structural fork for standalone prediction.
`Step(resimulating: true)` suppresses callbacks while retaining polled events.
The Hidden table is not automatically predicted by network replicas: the game
owns and reconciles its prediction fork. Full snapshots, delta/compressed codecs
and topology-compatible region snapshots are exposed directly on the space.
The sync stack has no per-table delta hook; these methods are not automatic wire
compression. Region restores require the same tick, origin and topology. They refresh
broadphase proxies rather than importing their history. Call
`CanonicalizeBroadphase` on both peers after a region repair to discard proxy
history in the same order before comparing full hashes.

`FindDifferences` returns the library's named snapshot difference report as text
for replay and drift tooling; it names body slots, generations and fields.
Normal table serialization remains byte-exact, including pending events.

`PhysicsDebug3D.Draw(space, scene.Debug, view.DebugVisible)` feeds the render
 graph's debug drawing. Call in `Render3DStage.UpdateScene` after the view's Build;
 the view clears its list each frame. The adapter emits triangle wireframes,
 sphere/capsule outlines, character ground arrows, wheels/suspension, joints,
 contact points and normal arrows. Off means no geometry collection or emission.
 Colours distinguish static, kinematic, awake, sleeping and trigger shapes.
 The destination is not cleared by the adapter; its caller owns frame lifetime.

All integration is contained in Physics. Core and Sync remain physics-free and
use their existing full-table serialization and recording format. Physics
`Hash` uses the library's in-place `ComputeHash`; world hashes include the bounded
snapshot bytes through the ordinary table contract. Warmup reserves snapshot
storage; the world serializer warms on its first hash. `Snapshot` and
`DescribeDifferences` let tooling retain and compare physics checkpoints without
adding diagnostic payloads to engine recordings or replica messages.
