# particles — emitters and their renderers

`OneGame.Particles` (no dependencies beyond the base library) is the simulation: `ParticleEmitter` over `ParticleSettings`, with `ParticleCurve`, `ParticleGradient` and `FloatRange` for values over a life. It knows nothing about the GPU. Renderers read live particles through `emitter.Sample(i)` (`ParticleSample`: world position, size, rotation, sRGB straight-alpha colour, sheet frame and texture rectangle, life fraction). User guide: `docs/particles.md`.

## Simulation

- **View-only.** An emitter holds no reference to a world, uses its own random stream (`ParticleRandom`, seeded by the constructor) and is never written to by the simulation. Hashes, snapshots and replays are unaffected; a test runs the same commands with and without particles and compares the simulation hashes in 2D and 3D.
- **Storage** is struct-of-arrays sized by `Capacity` at construction; births past it are dropped (`Dropped`). `Step`, `Burst` and `Sample` allocate nothing. Live particles stay in birth order (survivors are packed down), so older ones draw first.
- **Time.** `Step(seconds)` applies time in sub-steps of at most 1/30 s and cuts a call to 1 s; zero or negative does nothing. Births are spaced by `Rate` across each sub-step: a particle born part-way through a step is advanced by the remaining time, and the emitter's position is interpolated from where it was at the previous step, so a moving emitter leaves an even trail at any frame rate. Integration is semi-implicit Euler: `v += g dt; v *= exp(-drag dt); p += v dt`. `Prewarm` runs the emitter at its first step. Rate and gravity changes apply to the next step.
- **Launch.** `Space = Plane`: angle = atan2(Direction) ± `Spread`; spawn offset in a disc. `Volume`: uniform on a cone of half-angle `Spread` about `Direction` (cos θ uniform), spawn offset in a ball. `LocalSpace` stores positions relative to the emitter and adds `Position` when sampling.
- **Frames.** `SheetColumns` x `SheetRows` cells read row by row, `FrameCount` of them; a particle shows one random cell, or with `AnimateFrames` the cell its life fraction reaches.
- Determinism holds for one seed, one sequence of step sizes and one platform's maths library; it is not a cross-machine bit-exact contract (the simulation's hash does not depend on it).

## Renderer glue

- **2D:** `Draw2D.Particles(emitter, position, texture | path, z)` (`Draw2D.Particles.cs`). The first call per emitter per frame sets `Position`, steps by `Draw2D.DeltaTime` (view time, from the shown tick) and emits one `Sprite` per particle on the current layer (scale = size / cell pixels; additive emitters use `Renderer2D.AdditiveMaterial`, see `render2d.md`). The default texture is a 64 px soft spot. `Draw2D.ParticleEmitters` lists the emitters shown in the last frame.
- **3D:** `SceneWriter.Particles(emitter, position | Transform3, texture)` steps with `SceneWriter.DeltaTime` (view time of the prepared frame; zero when the same moment is prepared again, so camera/pick operations that call `Prepare` do not advance particles). `SceneView3D.Build` turns them into pooled `BillboardBatch3D`s, one per texture, with linear colour times `Emission` (`render3d.md`, Billboards). `SceneView3D.ParticleEmitters` lists them.
- Sorting and blending in 3D: see `render3d.md`; alpha particles are sorted per batch, additive ones are order independent. Particles are not lit and cast no shadows.

## Harness

- Control operation `particles` (`{name?}`): `{stage, emitters, live, items:[{name, live, capacity, emitting, spawned, dropped, blend, space, position}]}`. A headless game draws frames only on request, so the operation draws one at the window's size when none has been drawn at the current tick (`no_particles` when the stage is not a `Render2DStage`).
- Scenario expectation `particles: {emitter?, count?, min?, max?}` checks live particles (all emitters, or the named one) in the frame just drawn.
- `tests/ParticleGame` (`--3d` for the 3D variant) is the reference game; its scenarios hold the capture references.

## Not built

Ribbon trails, collision, particle lights and shadows, GPU simulation, soft (depth-faded) particles, stretched or velocity-aligned quads, sub-emitters, particle-to-particle forces. Over-lifetime curves are the package's own (`ParticleCurve`), not shared with any tween API.
