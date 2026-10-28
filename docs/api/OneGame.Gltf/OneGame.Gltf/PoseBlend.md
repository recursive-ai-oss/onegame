[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# PoseBlend static class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public static class PoseBlend
```

Combines poses. All operations work in place on buffers you own and allocate nothing.

## Methods

| Member | Description |
| --- | --- |
| [`Lerp(NodePose, NodePose, NodePose, float, PoseMask?)`](#m-lerp-nodepose-nodepose-nodepose-float-posemask) | Blends two poses: `dst = a + (b - a) * t` per node. |

### <a id="m-lerp-nodepose-nodepose-nodepose-float-posemask"></a>Lerp(NodePose, NodePose, NodePose, float, PoseMask?)

```csharp
public static void Lerp(NodePose dst, NodePose a, NodePose b, float t, PoseMask? mask = null);
```

Blends two poses: `dst = a + (b - a) * t` per node. Translations, scales and morph weights are interpolated linearly; rotations take the shorter way and are normalized (a good match for poses that are near each other).

**Parameters**

- `dst` ([`NodePose`](NodePose.md)): Receives the result; it may be `a` or `b`.
- `a` ([`NodePose`](NodePose.md)): The pose at weight 0.
- `b` ([`NodePose`](NodePose.md)): The pose at weight 1.
- `t` (`float`): The weight of `b`, clamped to 0..1.
- `mask` ([`PoseMask`](PoseMask.md)?): Optional per-node limit on `t`.
