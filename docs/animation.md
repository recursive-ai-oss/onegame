# Animation

`OneGame.Gltf` plays the clips of a glTF model, blends them, steers them with parameters and a small state machine, and hands the finished pose to the 3D scene. `examples/character` is a complete game that walks, runs and jumps; this page is its guide.

## The happy path: an `Animator`

An animator belongs to one drawn character. A view sets parameters from synced state and updates it once per frame:

```csharp
// once (the model, the rig and the animator's states are plain data)
var rig = new Rig(model);
var anim = new Animator(rig, rootNode: rig.Node("Root"));
anim.AddFloat("speed").AddTrigger("jump");
anim.AddState("move", new BlendSpace1DMotion(new BlendSpace1D(0, 1, 2), rig.Clip("idle"), rig.Clip("walk"), rig.Clip("run")) { XParameter = "speed" });
anim.AddState("jump", new ClipMotion(rig.Clip("jump")), loop: false);
anim.Transition("move", "jump").WhenTrigger("jump").Fade(0.1f);
anim.Transition("jump", "move").AtExit(0.9f).Fade(0.1f);

// in the entity: the animator lives in a [View] field, and Describe3D reads state and draws
[View] private Animator? _anim;
[View] public void Describe3D(SceneWriter scene)
{
    _anim ??= MakeAnimator();
    _anim.MoveFloat("speed", Gait, 4f);                       // glides to the gait instead of jumping
    _anim.UpdateTo((scene.Tick + scene.Alpha.ToSingle()) / 60.0);
    scene.Model("hero", Transform, _anim.Pose);               // the pose is copied into the scene
}
```

- **States** play a `Motion`: a `ClipMotion`, a `BlendSpace1DMotion` (one input, for example speed) or a `BlendSpace2DMotion` (two, for example direction by speed). A motion reads its inputs from the parameters named in `XParameter` and `YParameter`. The first state added is where the animator starts.
- **Parameters** are floats (`SetFloat`, or `MoveFloat` to glide to a goal at a rate), bools and triggers. A trigger stays set until a transition that waits for it fires; that transition clears it, so one trigger starts one transition.
- **Transitions** leave one state (or every state, with `null`) for another when all their conditions hold: `WhenFloat`, `WhenBool`, `WhenTrigger`, and `AtExit(progress)` to wait until the state has played that many passes. `Fade(seconds)` crossfades linearly; the outgoing state keeps playing while it fades. The first transition that holds fires; a transition cannot interrupt a fade.
- `UpdateTo(time)` advances in fixed 1/60 s steps, so the result follows the time and the parameters, not the frame rate. Gaps over a quarter of a second (a capture after many ticks, a restore) are not replayed: the clock moves, gliding parameters settle, and the pose is made from where the animator stands. When a view must show an exact moment of a state after such a gap, it cuts to it with `Play(state, progress)`, as the example does for the jump.
- With a `rootNode`, the root bone's own translation is held at rest in `Pose` (unless `KeepRootMotion`), so the character stays under its entity while the sim moves the entity.
- After construction an update allocates nothing (tested, including blends, fades and IK).

## The low level

Everything above is built from parts you can use directly:

| Part | What it does |
|---|---|
| `Rig` | Rest pose, node and clip lookup by name, `Sample(clip, seconds, pose)` (from the rest pose), `ComputeWorld(pose)`. |
| `NodePose` | Translation, rotation, scale and morph weights per node; `CopyFrom`. Make poses once with `rig.CreatePose()` and reuse them. |
| `PoseBlend.Lerp(dst, a, b, t, mask?)` | Blends two poses in place (rotations take the shorter way). `PoseMask.Subtree(model, node)` limits it to a body part, for example the upper body. |
| `Crossfade` | A clock for a fade: `Advance(dt)`, `Weight` (0 to 1, linear), `Done`. |
| `BlendSpace1D`, `BlendSpace2D` | Weights for samples along a line or over a plane. 1D: the two neighbours blend and the ends clamp. 2D: a Delaunay triangulation made once; a point inside blends the corners of its triangle, a point outside counts as the nearest point of the outline. Weights add up to 1. |
| `Motion` | Clip and blend-space motions: `Duration` at the current inputs, `Sample(rig, phase, dst, scratch)`. Phase is a share of the length, so clips of different lengths stay in step. |
| `TwoBoneIk.Solve(rig, pose, upper, mid, end, target, pole)` | Turns a leg or arm so the end joint reaches a target (model space); out of reach it straightens towards it. Call it on the final pose. |
| `RootMotion.Bake(clip, rootNode, ticksPerSecond)` | The root bone's movement as a `RootMotionTrack` (below). |

A by-hand frame is: `rig.Sample(walk, t, a); rig.Sample(run, t, b); PoseBlend.Lerp(pose, a, b, 0.3f); TwoBoneIk.Solve(...); scene.Model("hero", transform, pose);`.

Models need their parts in a skin (`GltfModel.Skins`); the renderer skins on the GPU. `scene.Model(name, transform, pose)` copies the pose when the frame is built. Picking still tests the rest pose.

## Root motion and determinism

**Animation is view code. The sim never runs it.** An `Animator`, a `Rig` and poses use floats and playback state that is not part of the world, so they belong in `[View]` members and fields, where they read synced state and never write it. Using any `OneGame.Gltf` type in sim code is error OG0023.

When the animation should decide how far the character moves, the movement goes through the sim as fixed-point numbers instead: at load time `RootMotion.Bake(clip, rootNode)` turns the root bone's translation into a `RootMotionTrack` (in `OneGame.Core`: one `FVec3` step per tick, summing exactly to the clip's total movement, so a thousand loops do not drift). The sim keeps an integer tick counter in synced state and applies `track.Step(transform, tick)` each tick, which every node, replica, save and replay computes identically; the view plays the same clip, with the root held at rest, so the feet match the ground (the test measures a planted foot sliding under 2 mm on the walk and 5 mm on the run). This is the only way root motion reaches the sim: a baked track the sim steps through, never the animator. The baking itself runs once on the loading machine (floats), and a test pins the walk's total to the exact raw value so a platform that disagrees is noticed.

A view that missed ticks keeps itself honest by deriving what it shows from state (`Jumps` changed, so fire the trigger; `JumpTick` says how far in, so `Play`). Animator state is not saved, replicated or hashed.

## Making a model

Any glTF with a skin and animations works. `examples/character/art` is a small program that writes the example's figure (boxes on a 14-joint skeleton with idle, walk, run and jump clips) as plain C#, with the legs of walk and run solved by IK against a foot path that moves back at exactly the root's speed while planted: read it to see how clips that need no foot sliding are made.

Not built: layers (play two animators and blend their poses by hand with a mask), additive clips, retargeting, animation events, curves for morph targets beyond what the clip carries, IK beyond two bones.
