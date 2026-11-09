[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# PhysicsDebug3D class

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)

```csharp
public sealed class PhysicsDebug3D
```

Reusable view-side adapter from 3D physics debug geometry to the render graph's debug primitives. Collects nothing while the debug switch is off.

## Constructors

| Member | Description |
| --- | --- |
| [`PhysicsDebug3D()`](#m-ctor) |  |

### <a id="m-ctor"></a>PhysicsDebug3D()

```csharp
public PhysicsDebug3D();
```

## Methods

| Member | Description |
| --- | --- |
| [`Draw(PhysicsSpace3D, DebugDraw3D, bool, DebugDepth3D)`](#m-draw-physicsspace3d-debugdraw3d-bool-debugdepth3d) | Adds wire shapes, character capsules, wheels, suspension, joints and contact normals. |

### <a id="m-draw-physicsspace3d-debugdraw3d-bool-debugdepth3d"></a>Draw(PhysicsSpace3D, DebugDraw3D, bool, DebugDepth3D)

```csharp
public void Draw(PhysicsSpace3D space, DebugDraw3D drawing, bool debugVisible, DebugDepth3D mode = 0);
```

Adds wire shapes, character capsules, wheels, suspension, joints and contact normals. Call after the scene view's Build, passing its DebugVisible switch. Does not clear the destination or mutate physics state.

**Parameters**

- `space` ([`PhysicsSpace3D`](PhysicsSpace3D.md)): Authority space to inspect.
- `drawing` ([`DebugDraw3D`](../../OneGame.Render3D/OneGame.Render3D/DebugDraw3D.md)): Scene debug list receiving the primitives.
- `debugVisible` (`bool`): The host view's debug switch; false emits nothing.
- `mode` ([`DebugDepth3D`](../../OneGame.Render3D/OneGame.Render3D/DebugDepth3D.md)): Depth-tested by default; AlwaysOnTop is useful when inspecting occluded shapes.
