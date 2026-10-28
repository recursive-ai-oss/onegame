[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# Render3DStage class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Inherits:** [`Render2DStage`](Render2DStage.md)  
**Implements:** [`IScene3DStage`](IScene3DStage.md)

```csharp
public class Render3DStage : Render2DStage, IScene3DStage
```

A render stage that draws a 3D scene first and the 2D scene and UI over it. A game returns it from [`GameDefinition.CreateRenderStage()`](GameDefinition.md#m-createrenderstage-hostresources). Two ways to fill the scene:

- Pass a [`SceneView3D`](SceneView3D.md): the stage asks every [`IView3D`](IView3D.md) entity of the world being drawn what it looks like, so a replica shows what it holds. [`Render3DStage.UpdateScene()`](Render3DStage.md#m-updatescene-scene3d-frameinfo) then runs on the built scene, for the environment, post settings and extra lights.
- Pass none: upload models in [`Render3DStage.RendererCreated()`](Render3DStage.md#m-renderercreated-renderer3d) and move the camera, models and lights in [`Render3DStage.UpdateScene()`](Render3DStage.md#m-updatescene-scene3d-frameinfo).

## Constructors

| Member | Description |
| --- | --- |
| [`Render3DStage(HostResources, SceneView3D?)`](#m-ctor-hostresources-sceneview3d) | Creates the stage. |

### <a id="m-ctor-hostresources-sceneview3d"></a>Render3DStage(HostResources, SceneView3D?)

```csharp
public Render3DStage(HostResources resources, SceneView3D? view = null);
```

Creates the stage.

**Parameters**

- `resources` ([`HostResources`](HostResources.md)): What the host provides: fonts, shaders, art.
- `view` ([`SceneView3D`](SceneView3D.md)?): Builds the scene from entities, or null to fill the scene by hand.

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | A short name shown in harness output. |
| [`Renderer`](#m-renderer) | The 3D renderer, once the first frame has created it. |
| [`Scene`](#m-scene) | The scene drawn every frame. |
| [`View`](#m-view) | The view that builds the scene from the world's [`IView3D`](IView3D.md) entities; null for a stage whose game fills the scene by hand. |

### <a id="m-name"></a>Name

```csharp
public override string Name { get; }
```

A short name shown in harness output.

**Type:** `string`

### <a id="m-renderer"></a>Renderer

```csharp
public Renderer3D? Renderer { get; }
```

The 3D renderer, once the first frame has created it.

**Type:** [`Renderer3D`](../../OneGame.Render3D/OneGame.Render3D/Renderer3D.md)?

### <a id="m-scene"></a>Scene

```csharp
public Scene3D Scene { get; }
```

The scene drawn every frame. With a [`Render3DStage.View`](Render3DStage.md#m-view) its camera, models and lights are rebuilt from the world each frame.

**Type:** [`Scene3D`](../../OneGame.Render3D/OneGame.Render3D/Scene3D.md)

### <a id="m-view"></a>View

```csharp
public SceneView3D? View { get; }
```

The view that builds the scene from the world's [`IView3D`](IView3D.md) entities; null for a stage whose game fills the scene by hand.

**Type:** [`SceneView3D`](SceneView3D.md)?

## Methods

| Member | Description |
| --- | --- |
| [`AddScenePasses(GpuDevice, RenderGraph, GraphTexture, GpuTexture, in FrameInfo)`](#m-addscenepasses-gpudevice-rendergraph-graphtexture-gputexture-frameinfo) | Adds passes that draw into the target before the 2D renderer and the UI, for a stage that draws a 3D scene. |
| [`Dispose()`](#m-dispose) |  |
| [`RendererCreated(Renderer3D)`](#m-renderercreated-renderer3d) | Called once, on the first frame, with the new renderer: upload models and environments here. |
| [`UpdateScene(Scene3D, in FrameInfo)`](#m-updatescene-scene3d-frameinfo) | Called every frame before the scene is drawn: set the environment and post settings, or (with no view) the camera, models and lights. |

### <a id="m-addscenepasses-gpudevice-rendergraph-graphtexture-gputexture-frameinfo"></a>AddScenePasses(GpuDevice, RenderGraph, GraphTexture, GpuTexture, in FrameInfo)

```csharp
protected override bool AddScenePasses(GpuDevice device, RenderGraph graph, GraphTexture target, GpuTexture targetTexture, in FrameInfo frame);
```

Adds passes that draw into the target before the 2D renderer and the UI, for a stage that draws a 3D scene. Return true when it did; the 2D pass then keeps the target's contents instead of clearing it.

**Parameters**

- `device` ([`GpuDevice`](../../OneGame.Gpu/OneGame.Gpu/GpuDevice.md)): The graphics device.
- `graph` ([`RenderGraph`](../../OneGame.Gpu/OneGame.Gpu.Graph/RenderGraph.md)): The frame's render graph, already begun.
- `target` ([`GraphTexture`](../../OneGame.Gpu/OneGame.Gpu.Graph/GraphTexture.md)): The target as a graph resource.
- `targetTexture` ([`GpuTexture`](../../OneGame.Gpu/OneGame.Gpu/GpuTexture.md)): The target texture.
- `frame` ([`FrameInfo`](FrameInfo.md)): The frame being drawn.

**Returns:** `bool`

### <a id="m-dispose"></a>Dispose()

```csharp
public override void Dispose();
```

### <a id="m-renderercreated-renderer3d"></a>RendererCreated(Renderer3D)

```csharp
protected virtual void RendererCreated(Renderer3D renderer);
```

Called once, on the first frame, with the new renderer: upload models and environments here.

**Parameters**

- `renderer` ([`Renderer3D`](../../OneGame.Render3D/OneGame.Render3D/Renderer3D.md)): The renderer.

### <a id="m-updatescene-scene3d-frameinfo"></a>UpdateScene(Scene3D, in FrameInfo)

```csharp
protected virtual void UpdateScene(Scene3D scene, in FrameInfo frame);
```

Called every frame before the scene is drawn: set the environment and post settings, or (with no view) the camera, models and lights.

**Parameters**

- `scene` ([`Scene3D`](../../OneGame.Render3D/OneGame.Render3D/Scene3D.md)): The scene to update.
- `frame` ([`FrameInfo`](FrameInfo.md)): The frame being drawn.
