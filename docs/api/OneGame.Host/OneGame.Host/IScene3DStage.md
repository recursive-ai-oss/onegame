[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# IScene3DStage interface

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public interface IScene3DStage
```

What a render stage that draws a 3D scene shows, for tools that ask about it (the `camera` and `pick` operations).

## Properties

| Member | Description |
| --- | --- |
| [`Manual`](#m-manual) | The probe for a scene the game fills by hand, so the `camera`, `pick` and `project` operations work without a [`SceneView3D`](SceneView3D.md); null for a stage that has a view or draws no 3D scene. |
| [`View`](#m-view) | The view that builds the scene from the world's [`IView3D`](IView3D.md) entities; null for a stage whose game fills the scene by hand. |

### <a id="m-manual"></a>Manual

```csharp
ManualScene3D? Manual { get; }
```

The probe for a scene the game fills by hand, so the `camera`, `pick` and `project` operations work without a [`SceneView3D`](SceneView3D.md); null for a stage that has a view or draws no 3D scene.

**Type:** [`ManualScene3D`](ManualScene3D.md)?

### <a id="m-view"></a>View

```csharp
SceneView3D? View { get; }
```

The view that builds the scene from the world's [`IView3D`](IView3D.md) entities; null for a stage whose game fills the scene by hand.

**Type:** [`SceneView3D`](SceneView3D.md)?
