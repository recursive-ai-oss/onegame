[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# PassType enum

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public enum PassType : int
```

What kind of work a pass records, which decides how the graph runs it.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-raster"></a>`Raster` | `0` | The pass declared color or depth targets; the graph opens the render pass and the pass draws in it. |
| <a id="m-compute"></a>`Compute` | `1` | The pass called [`PassBuilder.Compute()`](PassBuilder.md#m-compute); the graph opens a compute pass. |
| <a id="m-custom"></a>`Custom` | `2` | The pass records on the encoder itself: copies, mip generation, or code that opens its own passes (such as the 2D renderer). |
