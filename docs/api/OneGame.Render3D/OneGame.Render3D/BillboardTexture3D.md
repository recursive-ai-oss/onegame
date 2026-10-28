[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# BillboardTexture3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class BillboardTexture3D : IDisposable
```

An image for billboards, uploaded with its colours premultiplied by alpha in linear light. Create it with [`Renderer3D.CreateBillboardTexture()`](Renderer3D.md#m-createbillboardtexture-int-int-readonlyspan-byte-string) and dispose it before the renderer.

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
| [`Dispose()`](#m-dispose) | Releases the GPU image. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the GPU image.
