[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# TextureUsage enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
[Flags]
public enum TextureUsage : int
```

How a texture may be used. Combine flags; a texture can only be used in the ways it was created for.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | No usage. |
| <a id="m-sampled"></a>`Sampled` | `1` | Read by shaders through a sampler (`Texture2D.Sample`). |
| <a id="m-storage"></a>`Storage` | `2` | Written by shaders as a storage image. |
| <a id="m-rendertarget"></a>`RenderTarget` | `4` | Drawn into as a color or depth attachment of a render pass. |
| <a id="m-copysource"></a>`CopySource` | `8` | Source of copies and of [`GpuDevice.ReadTexture()`](GpuDevice.md#m-readtexture-gputexture-uint-uint-span-byte). |
| <a id="m-copydestination"></a>`CopyDestination` | `16` | Destination of copies and of `WriteTexture` uploads. |
