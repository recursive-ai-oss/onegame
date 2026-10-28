[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuBackend enum

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum GpuBackend : int
```

The graphics API a [`GpuDevice`](GpuDevice.md) runs on.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-vulkan"></a>`Vulkan` | `0` | Vulkan 1.3. |
| <a id="m-metal"></a>`Metal` | `1` | Metal. |
| <a id="m-webgpu"></a>`WebGpu` | `2` | Browser WebGPU. |
