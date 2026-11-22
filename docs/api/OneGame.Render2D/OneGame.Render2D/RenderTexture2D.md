[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# RenderTexture2D class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class RenderTexture2D : IDisposable
```

An image the 2D renderer draws into and then samples like any texture: trails, a minimap, a reflection, or a layer drawn once and reused. Make one with [`Renderer2D.CreateRenderTexture()`](Renderer2D.md#m-createrendertexture-int-int-texturefilter). Draw into it between [`Renderer2D.BeginTarget()`](Renderer2D.md#m-begintarget-rendertexture2d-color) and [`Renderer2D.EndTarget()`](Renderer2D.md#m-endtarget), or point a [`Layer2D`](Layer2D.md) at it with [`Layer2D.Target`](Layer2D.md#m-target); show it with [`RenderTexture2D.Texture`](RenderTexture2D.md#m-texture). Its pixels are in the renderer's target format, premultiplied like everything the renderer draws, and persist between frames until cleared.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height in pixels. |
| [`Texture`](#m-texture) | The image as a texture, for [`Sprite.Texture`](Sprite.md#m-texture), tiles and the UI. |
| [`Width`](#m-width) | Width in pixels. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; }
```

Height in pixels.

**Type:** `int`

### <a id="m-texture"></a>Texture

```csharp
public Texture2D Texture { get; }
```

The image as a texture, for [`Sprite.Texture`](Sprite.md#m-texture), tiles and the UI. A render texture must not be drawn while it is the target being drawn into (the renderer throws [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)): use two and alternate for feedback.

**Type:** [`Texture2D`](Texture2D.md)

### <a id="m-width"></a>Width

```csharp
public int Width { get; }
```

Width in pixels.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) | Releases the GPU memory. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the GPU memory. Waits for the device, so it is for resizes and shutdown, not for every frame.
