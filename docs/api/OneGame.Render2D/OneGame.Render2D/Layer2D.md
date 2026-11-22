[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# Layer2D class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)

```csharp
public sealed class Layer2D
```

A layer of a [`Scene2D`](Scene2D.md). Layers draw in the order they were added; each follows the camera by its own parallax factor.

## Properties

| Member | Description |
| --- | --- |
| [`Camera`](#m-camera) | A camera for this layer alone, or null (the default) to follow the camera the scene is drawn with. |
| [`Index`](#m-index) | Position in draw order; 0 is the back. |
| [`Name`](#m-name) | The layer's name, for debugging and captures. |
| [`Parallax`](#m-parallax) | How much the layer follows the camera position, per axis. |
| [`Post`](#m-post) | Effects run over this layer alone, after it is drawn and before the layers above it (a HUD on a later layer stays sharp), or null for none. |
| [`ScreenSpace`](#m-screenspace) | When true the layer ignores every camera and its coordinates are framebuffer pixels (origin top-left, one unit per pixel): the layer for a HUD, labels and menus drawn over a zoomed and panned world. |
| [`Target`](#m-target) | A render texture to draw this layer into instead of the frame, or null (the default). |
| [`TargetClear`](#m-targetclear) | The colour [`Layer2D.Target`](Layer2D.md#m-target) is cleared to (sRGB, straight alpha), transparent by default; null keeps what the texture holds, so drawing accumulates. |
| [`Viewport`](#m-viewport) | A framebuffer rectangle the layer is confined to, or null for the whole target. |
| [`Visible`](#m-visible) | Layers that are not visible are skipped. |

### <a id="m-camera"></a>Camera

```csharp
public Camera2D? Camera { get; set; }
```

A camera for this layer alone, or null (the default) to follow the camera the scene is drawn with. A layer with its own camera draws the world at another zoom or place than the layers beside it, which is how a minimap or a close-up shares a frame with the main view. [`Layer2D.Parallax`](Layer2D.md#m-parallax) still applies on top of it.

**Type:** [`Camera2D`](Camera2D.md)?

### <a id="m-index"></a>Index

```csharp
public int Index { get; }
```

Position in draw order; 0 is the back.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The layer's name, for debugging and captures.

**Type:** `string`

### <a id="m-parallax"></a>Parallax

```csharp
public Vector2 Parallax { get; set; }
```

How much the layer follows the camera position, per axis. 1 is the world itself; a distant backdrop uses something below 1.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-post"></a>Post

```csharp
public PostChain? Post { get; set; }
```

Effects run over this layer alone, after it is drawn and before the layers above it (a HUD on a later layer stays sharp), or null for none. The layer is drawn into an image of its own, the chain runs over it, and the result is blended onto the frame (or onto [`Layer2D.Target`](Layer2D.md#m-target)). An empty chain, like null, costs nothing and adds no pass.

**Type:** [`PostChain`](PostChain.md)?

### <a id="m-screenspace"></a>ScreenSpace

```csharp
public bool ScreenSpace { get; set; }
```

When true the layer ignores every camera and its coordinates are framebuffer pixels (origin top-left, one unit per pixel): the layer for a HUD, labels and menus drawn over a zoomed and panned world. Wins over [`Layer2D.Camera`](Layer2D.md#m-camera).

**Type:** `bool`

### <a id="m-target"></a>Target

```csharp
public RenderTexture2D? Target { get; set; }
```

A render texture to draw this layer into instead of the frame, or null (the default). The layer's camera, viewport and screen-space rules then refer to the texture's size. The texture is cleared to [`Layer2D.TargetClear`](Layer2D.md#m-targetclear) the first time a layer draws into it in a frame; a layer that has nothing to draw leaves it as it was, so a texture can be redrawn only when its contents change. Sprites that show the texture belong on a later layer; an earlier layer shows what the previous frame left.

**Type:** [`RenderTexture2D`](RenderTexture2D.md)?

### <a id="m-targetclear"></a>TargetClear

```csharp
public Color? TargetClear { get; set; }
```

The colour [`Layer2D.Target`](Layer2D.md#m-target) is cleared to (sRGB, straight alpha), transparent by default; null keeps what the texture holds, so drawing accumulates.

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)?

### <a id="m-viewport"></a>Viewport

```csharp
public Rect? Viewport { get; set; }
```

A framebuffer rectangle the layer is confined to, or null for the whole target. The layer's camera is centred on the rectangle (a [`Layer2D.ScreenSpace`](Layer2D.md#m-screenspace) layer puts its origin at the rectangle's top-left) and nothing outside it is drawn.

**Type:** [`Rect`](../../OneGame.UI/OneGame.UI/Rect.md)?

### <a id="m-visible"></a>Visible

```csharp
public bool Visible { get; set; }
```

Layers that are not visible are skipped.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`View(in Camera2D, Vector2)`](#m-view-camera2d-vector2) | The matrix taking this layer's coordinates to framebuffer pixels (row vectors), for the scene's camera and target size. |

### <a id="m-view-camera2d-vector2"></a>View(in Camera2D, Vector2)

```csharp
public Matrix3x2 View(in Camera2D sceneCamera, Vector2 screenSize);
```

The matrix taking this layer's coordinates to framebuffer pixels (row vectors), for the scene's camera and target size.

**Parameters**

- `sceneCamera` ([`Camera2D`](Camera2D.md)): The camera the scene is drawn with; used unless the layer has its own [`Layer2D.Camera`](Layer2D.md#m-camera).
- `screenSize` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): Target size in pixels.

**Returns:** [`Matrix3x2`](https://learn.microsoft.com/dotnet/api/system.numerics.matrix3x2) — The layer-to-screen matrix.
