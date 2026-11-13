[API reference](../index.md) › OneGame.Gpu

# OneGame.Gpu

Render graph over the GPU layer (RecursiveAI.OneGpu).

**Package:** `RecursiveAI.OneGame.Gpu`

```sh
dotnet add package RecursiveAI.OneGame.Gpu
```

## OneGame.Gpu.Graph

| Type | Kind | Description |
| --- | --- | --- |
| [`AccessReport`](OneGame.Gpu.Graph/AccessReport.md) | record | One thing a pass touches, as shown in a report. |
| [`BufferRead`](OneGame.Gpu.Graph/BufferRead.md) | enum | How a pass reads a buffer. |
| [`GraphBuffer`](OneGame.Gpu.Graph/GraphBuffer.md) | struct | A buffer inside one frame of a [`RenderGraph`](OneGame.Gpu.Graph/RenderGraph.md). |
| [`GraphStats`](OneGame.Gpu.Graph/GraphStats.md) | struct | Counts and sizes from one executed frame of a [`RenderGraph`](OneGame.Gpu.Graph/RenderGraph.md). |
| [`GraphTexture`](OneGame.Gpu.Graph/GraphTexture.md) | struct | A texture inside one frame of a [`RenderGraph`](OneGame.Gpu.Graph/RenderGraph.md): either created by the graph (transient) or imported. |
| [`IGraphPass`](OneGame.Gpu.Graph/IGraphPass.md) | interface | A pass of the render graph. |
| [`PassBuilder`](OneGame.Gpu.Graph/PassBuilder.md) | class | Declares what one pass touches. |
| [`PassContext`](OneGame.Gpu.Graph/PassContext.md) | class | What a pass may do while it records: get the physical textures and buffers it declared, and the render or compute pass the graph opened for it. |
| [`PassReport`](OneGame.Gpu.Graph/PassReport.md) | record | One pass of the last frame. |
| [`PassTime`](OneGame.Gpu.Graph/PassTime.md) | struct | The GPU time of one pass, from [`RenderGraph.ReadPassTimes()`](OneGame.Gpu.Graph/RenderGraph.md#m-readpasstimes-list-passtime). |
| [`PassType`](OneGame.Gpu.Graph/PassType.md) | enum | What kind of work a pass records, which decides how the graph runs it. |
| [`RenderGraph`](OneGame.Gpu.Graph/RenderGraph.md) | class | One frame structure for every renderer. |
| [`RenderGraphException`](OneGame.Gpu.Graph/RenderGraphException.md) | class | A problem in how a graph was declared: reading something no pass wrote, using a stale handle, touching a resource a pass did not declare. |
| [`RenderGraphOptions`](OneGame.Gpu.Graph/RenderGraphOptions.md) | class | Tuning for a [`RenderGraph`](OneGame.Gpu.Graph/RenderGraph.md). |
| [`RenderGraphReport`](OneGame.Gpu.Graph/RenderGraphReport.md) | record | What [`RenderGraph.Describe()`](OneGame.Gpu.Graph/RenderGraph.md#m-describe) returns: the structure of the last frame. |
| [`ResourceReport`](OneGame.Gpu.Graph/ResourceReport.md) | record | One texture or buffer of the last frame. |
