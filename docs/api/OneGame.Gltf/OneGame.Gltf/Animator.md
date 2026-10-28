[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# Animator class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public sealed class Animator
```

The happy path for animating a character: states that play clips or blends, parameters that steer them, transitions with crossfades, and the finished [`Animator.Pose`](Animator.md#m-pose) to hand to the scene. A view sets parameters from synced state and calls [`Animator.UpdateTo()`](Animator.md#m-updateto-double) once per frame. It reads state only; it must not be used from sim code (rule OG0023). After construction an update allocates nothing.

**Example**

```csharp
var anim = new Animator(rig, rootNode: rig.Node("Root"));
anim.AddFloat("speed").AddTrigger("jump");
anim.AddState("move", new BlendSpace1DMotion(new BlendSpace1D(0, 1, 2), idle, walk, run) { XParameter = "speed" });
anim.AddState("jump", new ClipMotion(jump), loop: false);
anim.Transition("move", "jump").WhenTrigger("jump").Fade(0.1f);
anim.Transition("jump", "move").AtExit().Fade(0.15f);
// each frame:  anim.SetFloat("speed", gait); anim.UpdateTo(time); scene.Model("hero", transform, anim.Pose);
```

## Constructors

| Member | Description |
| --- | --- |
| [`Animator(Rig, int)`](#m-ctor-rig-int) | Creates an animator for a rig. |

### <a id="m-ctor-rig-int"></a>Animator(Rig, int)

```csharp
public Animator(Rig rig, int rootNode = -1);
```

Creates an animator for a rig.

**Parameters**

- `rig` ([`Rig`](Rig.md)): The skeleton and clips.
- `rootNode` (`int`): The node whose translation carries root motion, or -1 for none. Unless [`Animator.KeepRootMotion`](Animator.md#m-keeprootmotion) is set, its translation is held at rest so the character stays under its entity while the sim moves it.

## Properties

| Member | Description |
| --- | --- |
| [`CurrentState`](#m-currentstate) | The name of the state being played (the one being entered during a crossfade), or empty before the first update. |
| [`IsFading`](#m-isfading) | True while a crossfade is running. |
| [`KeepRootMotion`](#m-keeprootmotion) | True to leave the root node's own movement in the pose (for a character that has no sim moving it). |
| [`Pose`](#m-pose) | The finished pose of the last update: pass it to `SceneWriter.Model` or copy it with [`NodePose.CopyFrom()`](NodePose.md#m-copyfrom-nodepose). |
| [`RootNode`](#m-rootnode) | The root node, or -1. |
| [`StateProgress`](#m-stateprogress) | Progress of the current state (see [`AnimState.Progress`](AnimState.md#m-progress)). |
| [`States`](#m-states) | The state names in the order they were added. |

### <a id="m-currentstate"></a>CurrentState

```csharp
public string CurrentState { get; }
```

The name of the state being played (the one being entered during a crossfade), or empty before the first update.

**Type:** `string`

### <a id="m-isfading"></a>IsFading

```csharp
public bool IsFading { get; }
```

True while a crossfade is running.

**Type:** `bool`

### <a id="m-keeprootmotion"></a>KeepRootMotion

```csharp
public bool KeepRootMotion { get; set; }
```

True to leave the root node's own movement in the pose (for a character that has no sim moving it).

**Type:** `bool`

### <a id="m-pose"></a>Pose

```csharp
public NodePose Pose { get; }
```

The finished pose of the last update: pass it to `SceneWriter.Model` or copy it with [`NodePose.CopyFrom()`](NodePose.md#m-copyfrom-nodepose).

**Type:** [`NodePose`](NodePose.md)

### <a id="m-rootnode"></a>RootNode

```csharp
public int RootNode { get; }
```

The root node, or -1.

**Type:** `int`

### <a id="m-stateprogress"></a>StateProgress

```csharp
public float StateProgress { get; }
```

Progress of the current state (see [`AnimState.Progress`](AnimState.md#m-progress)).

**Type:** `float`

### <a id="m-states"></a>States

```csharp
public IEnumerable<string> States { get; }
```

The state names in the order they were added.

**Type:** [`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)`<string>`

## Methods

| Member | Description |
| --- | --- |
| [`AddBool(string)`](#m-addbool-string) | Declares a bool parameter (initially false). |
| [`AddFloat(string)`](#m-addfloat-string) | Declares a float parameter (initially 0). |
| [`AddState(string, Motion, bool, float)`](#m-addstate-string-motion-bool-float) | Adds a state. |
| [`AddTrigger(string)`](#m-addtrigger-string) | Declares a trigger: a flag that stays set until a transition that waits for it fires. |
| [`CrossFade(string, float)`](#m-crossfade-string-float) | Starts a crossfade into a state at once, whatever the transitions say. |
| [`GetBool(string)`](#m-getbool-string) | Reads a bool parameter or whether a trigger is set. |
| [`GetFloat(string)`](#m-getfloat-string) | Reads a float parameter. |
| [`MoveFloat(string, float, float)`](#m-movefloat-string-float-float) | Moves a float parameter towards a goal at a steady rate while the animator updates, so a gait that changes from 1 to 2 blends over time instead of jumping. |
| [`Play(string, float)`](#m-play-string-float) | Cuts to a state at a point in it, with no crossfade, and recomputes [`Animator.Pose`](Animator.md#m-pose). |
| [`SetBool(string, bool)`](#m-setbool-string-bool) | Sets a bool parameter. |
| [`SetFloat(string, float)`](#m-setfloat-string-float) | Sets a float parameter at once. |
| [`Transition(string?, string)`](#m-transition-string-string) | Adds a transition between two states, or from every state when `from` is null. |
| [`Trigger(string)`](#m-trigger-string) | Sets a trigger. |
| [`Update(float)`](#m-update-float) | Advances by a time step and recomputes [`Animator.Pose`](Animator.md#m-pose). |
| [`UpdateTo(double)`](#m-updateto-double) | Advances to a running time. |

### <a id="m-addbool-string"></a>AddBool(string)

```csharp
public Animator AddBool(string name);
```

Declares a bool parameter (initially false).

**Parameters**

- `name` (`string`)

**Returns:** [`Animator`](Animator.md)

### <a id="m-addfloat-string"></a>AddFloat(string)

```csharp
public Animator AddFloat(string name);
```

Declares a float parameter (initially 0).

**Parameters**

- `name` (`string`)

**Returns:** [`Animator`](Animator.md)

### <a id="m-addstate-string-motion-bool-float"></a>AddState(string, Motion, bool, float)

```csharp
public AnimState AddState(string name, Motion motion, bool loop = true, float speed = 1f);
```

Adds a state. The first state added is where the animator starts.

**Parameters**

- `name` (`string`): A unique name.
- `motion` ([`Motion`](Motion.md)): What it plays. Parameters it names must be declared first.
- `loop` (`bool`): Whether it repeats.
- `speed` (`float`): Playback speed factor.

**Returns:** [`AnimState`](AnimState.md)

### <a id="m-addtrigger-string"></a>AddTrigger(string)

```csharp
public Animator AddTrigger(string name);
```

Declares a trigger: a flag that stays set until a transition that waits for it fires.

**Parameters**

- `name` (`string`)

**Returns:** [`Animator`](Animator.md)

### <a id="m-crossfade-string-float"></a>CrossFade(string, float)

```csharp
public void CrossFade(string state, float seconds);
```

Starts a crossfade into a state at once, whatever the transitions say.

**Parameters**

- `state` (`string`): The state's name.
- `seconds` (`float`): Fade length.

### <a id="m-getbool-string"></a>GetBool(string)

```csharp
public bool GetBool(string name);
```

Reads a bool parameter or whether a trigger is set.

**Parameters**

- `name` (`string`)

**Returns:** `bool`

### <a id="m-getfloat-string"></a>GetFloat(string)

```csharp
public float GetFloat(string name);
```

Reads a float parameter.

**Parameters**

- `name` (`string`)

**Returns:** `float`

### <a id="m-movefloat-string-float-float"></a>MoveFloat(string, float, float)

```csharp
public void MoveFloat(string name, float goal, float perSecond);
```

Moves a float parameter towards a goal at a steady rate while the animator updates, so a gait that changes from 1 to 2 blends over time instead of jumping. Call it every frame with the current goal.

**Parameters**

- `name` (`string`): The parameter.
- `goal` (`float`): The value to reach.
- `perSecond` (`float`): The most the value changes in a second.

### <a id="m-play-string-float"></a>Play(string, float)

```csharp
public void Play(string state, float progress = 0f);
```

Cuts to a state at a point in it, with no crossfade, and recomputes [`Animator.Pose`](Animator.md#m-pose).

**Parameters**

- `state` (`string`): The state's name.
- `progress` (`float`): Passes through the state's motion so far (0.5 = halfway through the first pass).

**Exceptions**

- [`UnknownAnimatorNameException`](UnknownAnimatorNameException.md): There is no such state.

### <a id="m-setbool-string-bool"></a>SetBool(string, bool)

```csharp
public void SetBool(string name, bool value);
```

Sets a bool parameter.

**Parameters**

- `name` (`string`)
- `value` (`bool`)

**Exceptions**

- [`UnknownAnimatorNameException`](UnknownAnimatorNameException.md): There is no such parameter.

### <a id="m-setfloat-string-float"></a>SetFloat(string, float)

```csharp
public void SetFloat(string name, float value);
```

Sets a float parameter at once.

**Parameters**

- `name` (`string`)
- `value` (`float`)

**Exceptions**

- [`UnknownAnimatorNameException`](UnknownAnimatorNameException.md): There is no such parameter.

### <a id="m-transition-string-string"></a>Transition(string?, string)

```csharp
public AnimTransition Transition(string? from, string to);
```

Adds a transition between two states, or from every state when `from` is null. The first transition whose conditions hold fires.

**Parameters**

- `from` (`string?`): The state to leave, or null.
- `to` (`string`): The state to enter.

**Returns:** [`AnimTransition`](AnimTransition.md)

**Exceptions**

- [`UnknownAnimatorNameException`](UnknownAnimatorNameException.md): A state name does not exist.

### <a id="m-trigger-string"></a>Trigger(string)

```csharp
public void Trigger(string name);
```

Sets a trigger.

**Parameters**

- `name` (`string`)

**Exceptions**

- [`UnknownAnimatorNameException`](UnknownAnimatorNameException.md): There is no such trigger.

### <a id="m-update-float"></a>Update(float)

```csharp
public void Update(float dt);
```

Advances by a time step and recomputes [`Animator.Pose`](Animator.md#m-pose).

**Parameters**

- `dt` (`float`): Seconds, not negative.

### <a id="m-updateto-double"></a>UpdateTo(double)

```csharp
public void UpdateTo(double seconds);
```

Advances to a running time. Short gaps (up to a quarter second) are played in fixed 1/60 s steps, so the result depends on the time and the parameters, not on the frame rate. A longer gap (a capture after many ticks, a paused window), a time that goes backwards (a restore) or the first call does not replay what was missed: the clock moves, parameters being moved by [`Animator.MoveFloat()`](Animator.md#m-movefloat-string-float-float) settle on their goals, and the pose is made from where the animator stands. To show an exact moment of a state after such a gap, call [`Animator.Play()`](Animator.md#m-play-string-float).

**Parameters**

- `seconds` (`double`): A running time, for example `(scene.Tick + scene.Alpha) / 60.0`; the same value again changes nothing.
