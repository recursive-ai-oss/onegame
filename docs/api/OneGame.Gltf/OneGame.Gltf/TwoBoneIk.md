[API reference](../../index.md) › [OneGame.Gltf](../index.md) › OneGame.Gltf

# TwoBoneIk static class

**Namespace:** `OneGame.Gltf`  
**Package:** [`RecursiveAI.OneGame.Gltf`](../index.md)

```csharp
public static class TwoBoneIk
```

Two-bone inverse kinematics for a leg or an arm: turns the upper and middle joints so the end joint reaches a target.

## Methods

| Member | Description |
| --- | --- |
| [`Solve(Rig, NodePose, int, int, int, Vector3, Vector3)`](#m-solve-rig-nodepose-int-int-int-vector3-vector3) | Changes the local rotations of `upper` and `mid` so that `end` sits at `target`, or as close as the bones' lengths allow (the limb then points straight at it). |

### <a id="m-solve-rig-nodepose-int-int-int-vector3-vector3"></a>Solve(Rig, NodePose, int, int, int, Vector3, Vector3)

```csharp
public static bool Solve(Rig rig, NodePose pose, int upper, int mid, int end, Vector3 target, Vector3 pole);
```

Changes the local rotations of `upper` and `mid` so that `end` sits at `target`, or as close as the bones' lengths allow (the limb then points straight at it). The bend goes towards `pole`. Call it after the clips and blends, on the final pose. Bones should have uniform scale.

**Parameters**

- `rig` ([`Rig`](Rig.md)): The rig of the pose.
- `pose` ([`NodePose`](NodePose.md)): The pose to change.
- `upper` (`int`): The first joint (hip or shoulder).
- `mid` (`int`): The middle joint (knee or elbow), a child of `upper`.
- `end` (`int`): The end joint (ankle or wrist), a child of `mid`.
- `target` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): Where the end joint should be, in the model's root space.
- `pole` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): A point in the model's root space the middle joint bends towards (a knee goes forward).

**Returns:** `bool` — True when the target was within reach.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The joints are not a chain.
