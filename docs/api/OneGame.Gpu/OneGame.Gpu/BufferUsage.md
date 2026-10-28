[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# BufferUsage enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
[Flags]
public enum BufferUsage : int
```

How a buffer may be used.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | No usage. |
| <a id="m-vertex"></a>`Vertex` | `1` | Vertex data. |
| <a id="m-index"></a>`Index` | `2` | Index data. |
| <a id="m-uniform"></a>`Uniform` | `4` | Uniform (constant) buffer. |
| <a id="m-storage"></a>`Storage` | `8` | Storage buffer, readable and writable by shaders. |
| <a id="m-copysource"></a>`CopySource` | `16` | Source of copies and of [`GpuDevice.ReadBuffer()`](GpuDevice.md#m-readbuffer-gpubuffer-ulong-span-byte). |
| <a id="m-copydestination"></a>`CopyDestination` | `32` | Destination of copies and of [`GpuDevice.WriteBuffer()`](GpuDevice.md#m-writebuffer-gpubuffer-ulong-readonlyspan-byte) uploads. |
| <a id="m-indirect"></a>`Indirect` | `64` | Holds indirect draw or dispatch arguments. |
