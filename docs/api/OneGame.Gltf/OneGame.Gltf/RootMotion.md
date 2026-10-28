[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# RootMotion static class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public static class RootMotion
```

Turns a clip's root bone movement into numbers the sim can use.

## Methods

| Member | Description |
| --- | --- |
| [`Bake(AnimationClip, int, int)`](#m-bake-animationclip-int-int) | Bakes the translation of a root node into a fixed-point step per tick. |

### <a id="m-bake-animationclip-int-int"></a>Bake(AnimationClip, int, int)

```csharp
public static RootMotionTrack Bake(AnimationClip clip, int rootNode, int ticksPerSecond = 60);
```

Bakes the translation of a root node into a fixed-point step per tick. Run it once at load time (it uses floats; the result is plain [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) data). The steps are the differences of positions rounded to fixed point, so they add up exactly to the clip's total movement with no drift over many loops. The clip is stretched to a whole number of ticks.

**Parameters**

- `clip` ([`AnimationClip`](AnimationClip.md)): The clip.
- `rootNode` (`int`): The node that carries the movement. Its parents should not rotate or scale; the node's own translation is used as written.
- `ticksPerSecond` (`int`): The simulation rate.

**Returns:** [`RootMotionTrack`](../../OneGame.Core/OneGame.Core/RootMotionTrack.md) — One step per tick, in the model's axes (-Z is forward). A clip that does not animate the node gives all zeros.
