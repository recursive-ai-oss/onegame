[API reference](../../index.md) › [OneGame.Render3D](../index.md) › OneGame.Render3D

# BillboardTextureLoad3D class

**Namespace:** `OneGame.Render3D`  
**Package:** [`RecursiveAI.OneGame.Render3D`](../index.md)

```csharp
public sealed class BillboardTextureLoad3D
```

A billboard texture whose pixels are being premultiplied on pool threads (see [`Renderer3D.BeginCreateBillboardTexture()`](Renderer3D.md#m-begincreatebillboardtexture-int-int-readonlymemory-byte-string)). Poll [`BillboardTextureLoad3D.IsReady`](BillboardTextureLoad3D.md#m-isready) from the frame loop and call [`BillboardTextureLoad3D.Complete()`](BillboardTextureLoad3D.md#m-complete) on the renderer's thread.

## Properties

| Member | Description |
| --- | --- |
| [`IsReady`](#m-isready) | True once the pixels are ready and [`BillboardTextureLoad3D.Complete()`](BillboardTextureLoad3D.md#m-complete) will not wait. |

### <a id="m-isready"></a>IsReady

```csharp
public bool IsReady { get; }
```

True once the pixels are ready and [`BillboardTextureLoad3D.Complete()`](BillboardTextureLoad3D.md#m-complete) will not wait.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Complete()`](#m-complete) | Uploads the pixels and returns the texture, waiting for the premultiply if it has not finished. |

### <a id="m-complete"></a>Complete()

```csharp
public BillboardTexture3D Complete();
```

Uploads the pixels and returns the texture, waiting for the premultiply if it has not finished. Call it on the thread that owns the renderer; later calls return the same texture.

**Returns:** [`BillboardTexture3D`](BillboardTexture3D.md) — The texture. Dispose it before the renderer.
