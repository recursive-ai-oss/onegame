# scene3d — entities in 3D

How a game written as entities shows itself in 3D, and how tools drive and check it. Sim side in `OneGame.Core` (`FVec3`, `FQuat`, `Transform3`), view side in `OneGame.Host` (`IView3D`, `SceneWriter`, `SceneView3D`, `ModelLibrary`, `WorldAnchor`, `Render3DStage`), control in `OneGame.Control` (`camera`, `pick`, `project`). The renderer is `render3d.md`; nothing here needs more of it than `Scene3D`, `Camera3D`, `Light3D` and `ModelInstance3D`.

## Sim: synced transforms

```csharp
[Entity]
public partial class Crate : Entity, IView3D
{
    [Synced] public partial Transform3 Transform { get; private set; }          // or Timed<Transform3> for an interpolated move
    protected override void OnTick() =>
        Transform = Transform.WithRotation(FQuat.FromAxisAngle(FVec3.UnitY, Fixed.FromRatio(1, 60)) * Transform.Rotation).Translated(new FVec3(0, 0, Fixed.FromRatio(1, 32)));
    [View] public void Describe3D(SceneWriter scene) => scene.Model("crate", Transform);
}
```

- `FVec3`, `FQuat` (unit quaternion) and `Transform3` (position, rotation, scale) are `Fixed`-based `ISyncValue`s: generated codecs, deltas and snapshots take them like `FVec2`, and `Timed<Transform3>` interpolates (linear position and scale, normalized lerp rotation). Integer arithmetic only, so hashes match on every machine.
- `default` is the identity for all of them (rotation and scale are stored relative to the identity), so a new synced property is valid. Right-handed, +Y up, an unrotated thing faces -Z (`Transform3.Forward`), as the renderer does. `a * b` on quaternions applies `b` first. `Transform3.LookAt(eye, target)` builds a camera pose.
- Rules: OG0001 now also covers `System.Numerics` `Vector2/3/4`, `Quaternion`, `Plane`, `Matrix3x2/4x4` in synced state and command arguments (message names `FVec3`/`FQuat`/`Transform3`), and OG0002 covers them in sim code. A `[View]` member that assigns a synced transform (or calls something that does) is OG0008, as for any state.

## View: entity to scene

An entity that implements `IView3D` is asked once per drawn frame, in entity order, `Describe3D(SceneWriter)`; mark it `[View]`. It says:

| Call | Meaning |
|---|---|
| `Model(name, transform, pickable = true)` / `Model(name, transform, extraScale)` | a model of the `ModelLibrary`; unknown names throw `UnknownModelException` listing the known ones |
| `Instance(name, transform, tint = null)` | one copy among many of a static model: all copies of a model in a frame become one instanced batch (culled, shadowed, one draw per primitive); rest pose, uniform scale, not pickable |
| `PointLight`, `SpotLight`, `DirectionalLight` | lights (a transform gives position and facing; `Light3D` units) |
| `Camera(transform, fovDegrees, priority, near, far, orthoHeight)` | offers a camera; highest `priority` wins, first on a tie; with none the `SceneView3D.DefaultCamera` is used |

`scene.Tick` and `scene.Alpha` (0..1 between ticks; realtime runs set it, captures show whole ticks) go to `Timed<T>.ValueAt(tick, alpha)`. Describing only collects; floats are fine here, state is read-only.

`SceneView3D` (one per game, holds a `ModelLibrary`) does the work: `Prepare(world, tick, alpha)` collects; `Build(scene3d, renderer)` fills a `Scene3D` (camera, pooled `ModelInstance3D`s, one pooled `InstanceBatch3D` per instanced model, lights; models upload on first use); `Camera`, `TryProject`, `Ray`, `Pick`, `Cast` answer from the last `Prepare` with no graphics device. `Render3DStage(resources, view)` does Prepare + Build every frame on the world it is given (`FrameInfo.World`, so a replica draws what it holds), then `UpdateScene` for environment and post settings. `GameDefinition.CreateRenderStage` returns it; without a `view` the stage keeps the old hand-filled behaviour. Steady frames allocate no managed memory (test).

`SceneView3D.CameraOverride` replaces the entities' camera for tools; it is a view setting, not state, and not recorded.

### Describing from view code

A game whose entities live in a library that cannot reference the host (a rules assembly shared with servers) cannot implement `IView3D`. It sets `SceneView3D.Describer` instead: `Action<World, SceneWriter>`, called after the entities. It writes with the float forms `Model(name, in Matrix4x4, pickable)`, `PointLight(Vector3, ...)`, `DirectionalLight(Vector3, ...)` and `Camera(in Matrix4x4, ...)`, and sets `SceneWriter.Entity` before a model so picking reports that entity. Keep per-frame state keyed by `scene.Tick`: the same tick may be described several times in a frame (pinned labels ask too). Use indexed loops over `world.Entities`; `foreach` allocates.

## Picking and projection

`view.Pick(x, y, width, height)` / `view.Cast(ray)`: nearest pickable model along the ray, tested triangle by triangle at the rest pose (broad phase on the model's box), both sides. Result `PickHit(Entity, Model, Distance, Point)`. Animation and skinning are not applied to the test. `TryProject(point, w, h, out pixel, out depth)` is false for points behind the camera or outside the near/far planes; pixels have the origin at the top left.

## World-anchored UI

`element.PinToWorld(view, game, () => FVec3?, pixelOffset, pivot)` (an element placed against the screen) re-reads the point every UI frame, places the element so its `pivot` (default bottom centre) sits on the projected point, and hides it when the point is behind the camera, beyond near/far, off screen, or the reader returns null. Add it as a child of the screen root.

## Control

| Operation | Does |
|---|---|
| `camera` | no arguments: the camera in use (`position`, `forward`, `up`, `fovDegrees`, `orthoHeight`, `near`, `far`, `entity`, `overridden`). `position`, `target`, `fovDegrees`, `near`, `far`, `orthoHeight` override it; `reset: true` follows the entities again |
| `pick` | `x`,`y` pixels (`size` as for captures, default the window) or `origin`+`direction`: `hit`, `entity` (`Type#id`), `id`, `address` (`Type#n`, the form other operations take), `model`, `distance`, `point` |
| `project` | `point` → `onScreen`, `inFront`, `x`, `y`, `depth` |

They need a game whose stage is a `Render3DStage` with a view (`no_scene3d` otherwise). Captures (`capture_frame`, `capture_sequence`), `render_graph` and `info` (`stage: render3d`) are the existing operations; `GameInstance.Capture(w, h, scale, shownWorld)` draws any world, e.g. a replica's. Scenario file additions: step `camera: {position: [x,y,z], target: [x,y,z], fov, near, far, ortho-height}` or `camera: reset`; expectation `pick: {x, y, size?, entity: Type#n | none}`.

## Not built

Animated or skinned picking; physics; per-entity visibility culling (every model of the world is submitted).
