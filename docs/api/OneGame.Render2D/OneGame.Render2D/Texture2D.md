[API reference](../../index.md) › [OneGame.Render2D](../index.md) › OneGame.Render2D

# Texture2D class

**Namespace:** `OneGame.Render2D`  
**Package:** [`RecursiveAI.OneGame.Render2D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class Texture2D : IDisposable
```

An image on the GPU that sprites, tilesets and materials draw from. Created by [`Renderer2D.CreateTexture()`](Renderer2D.md#m-createtexture-int-int-readonlyspan-byte-texturefilter).

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height in pixels. |
| [`Width`](#m-width) | Width in pixels. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; }
```

Height in pixels.

**Type:** `int`

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

Releases the GPU memory. Do not draw the texture afterwards.
