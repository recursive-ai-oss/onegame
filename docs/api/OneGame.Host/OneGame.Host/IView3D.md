[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# IView3D interface

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public interface IView3D
```

Implemented by an entity that shows itself in 3D. The host calls [`IView3D.Describe3D()`](IView3D.md#m-describe3d-scenewriter) on every such entity once per drawn frame, in entity order, and builds the scene from what the entity says. Mark the implementation `[View]` so the analyzers check that it only reads state.

## Methods

| Member | Description |
| --- | --- |
| [`Describe3D(SceneWriter)`](#m-describe3d-scenewriter) | Says what this entity looks like right now: its model, lights and camera. |

### <a id="m-describe3d-scenewriter"></a>Describe3D(SceneWriter)

```csharp
void Describe3D(SceneWriter scene);
```

Says what this entity looks like right now: its model, lights and camera. Read state; never change it.

**Parameters**

- `scene` ([`SceneWriter`](SceneWriter.md)): Collects the answer. [`SceneWriter.Tick`](SceneWriter.md#m-tick) and [`SceneWriter.Alpha`](SceneWriter.md#m-alpha) say which moment to show, for `Timed<T>.ValueAt`.
