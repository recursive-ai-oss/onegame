[API reference](../index.md) › OneGame.Gpu

# OneGame.Gpu

Graphics API abstraction and render graph.

**Package:** `RecursiveAI.OneGame.Gpu`

```sh
dotnet add package RecursiveAI.OneGame.Gpu
```

## OneGame.Gpu

| Type | Kind | Description |
| --- | --- | --- |
| [`AcquireStatus`](OneGame.Gpu/AcquireStatus.md) | enum | Result of acquiring the next swapchain image. |
| [`AdapterKind`](OneGame.Gpu/AdapterKind.md) | enum | Kind of graphics adapter. |
| [`AddressMode`](OneGame.Gpu/AddressMode.md) | enum | Texture coordinate handling outside 0..1. |
| [`BindGroupEntry`](OneGame.Gpu/BindGroupEntry.md) | struct | One resource bound at a binding index of a bind group. |
| [`BlendComponent`](OneGame.Gpu/BlendComponent.md) | struct | Blend settings for one of color or alpha. |
| [`BlendFactor`](OneGame.Gpu/BlendFactor.md) | enum | Factor applied to a blend operand. |
| [`BlendOperation`](OneGame.Gpu/BlendOperation.md) | enum | Operation combining the blended source and destination. |
| [`BlendState`](OneGame.Gpu/BlendState.md) | struct | Blending of one color target. |
| [`BufferDesc`](OneGame.Gpu/BufferDesc.md) | struct | Describes a buffer to create. |
| [`BufferUsage`](OneGame.Gpu/BufferUsage.md) | enum | How a buffer may be used. |
| [`ColorAttachment`](OneGame.Gpu/ColorAttachment.md) | struct | A color attachment of a render pass. |
| [`ColorTargetState`](OneGame.Gpu/ColorTargetState.md) | struct | A color target of a graphics pipeline. |
| [`ColorWriteMask`](OneGame.Gpu/ColorWriteMask.md) | enum | Color channels a pipeline writes. |
| [`CompareFunction`](OneGame.Gpu/CompareFunction.md) | enum | Comparison used by depth tests. |
| [`ComputePipelineDesc`](OneGame.Gpu/ComputePipelineDesc.md) | class | Describes a compute pipeline. |
| [`CullMode`](OneGame.Gpu/CullMode.md) | enum | Which triangle faces are discarded. |
| [`DepthAttachment`](OneGame.Gpu/DepthAttachment.md) | struct | The depth attachment of a render pass. |
| [`DepthStencilState`](OneGame.Gpu/DepthStencilState.md) | struct | Depth testing of a graphics pipeline. |
| [`DispatchIndirectArgs`](OneGame.Gpu/DispatchIndirectArgs.md) | struct | Arguments of one indirect dispatch, as stored in a buffer. |
| [`DrawIndexedIndirectArgs`](OneGame.Gpu/DrawIndexedIndirectArgs.md) | struct | Arguments of one indirect indexed draw, as stored in a buffer. |
| [`DrawIndirectArgs`](OneGame.Gpu/DrawIndirectArgs.md) | struct | Arguments of one indirect non-indexed draw, as stored in a buffer. |
| [`FilterMode`](OneGame.Gpu/FilterMode.md) | enum | Texture filtering. |
| [`FrontFace`](OneGame.Gpu/FrontFace.md) | enum | Winding order that makes a triangle front-facing, as seen with clip-space +Y up. |
| [`GpuAdapterInfo`](OneGame.Gpu/GpuAdapterInfo.md) | record | Information about a graphics adapter. |
| [`GpuBackend`](OneGame.Gpu/GpuBackend.md) | enum | The graphics API a [`GpuDevice`](OneGame.Gpu/GpuDevice.md) runs on. |
| [`GpuBindGroup`](OneGame.Gpu/GpuBindGroup.md) | class | A set of resources bound together to a pipeline. |
| [`GpuBuffer`](OneGame.Gpu/GpuBuffer.md) | class | A block of GPU memory. |
| [`GpuCapabilities`](OneGame.Gpu/GpuCapabilities.md) | struct | Optional features and limits of a device. |
| [`GpuCommandEncoder`](OneGame.Gpu/GpuCommandEncoder.md) | class | Records a sequence of copy, render and compute work, then is handed to [`GpuDevice.Submit()`](OneGame.Gpu/GpuDevice.md#m-submit-gpucommandencoder). |
| [`GpuComputePass`](OneGame.Gpu/GpuComputePass.md) | class | Records compute dispatches. |
| [`GpuComputePipeline`](OneGame.Gpu/GpuComputePipeline.md) | class | A compiled compute pipeline. |
| [`GpuDevice`](OneGame.Gpu/GpuDevice.md) | class | A graphics device: creates resources, records and submits work. |
| [`GpuDeviceLostException`](OneGame.Gpu/GpuDeviceLostException.md) | class | The device was lost (driver reset, GPU removed). |
| [`GpuDeviceOptions`](OneGame.Gpu/GpuDeviceOptions.md) | class | Options for creating a device. |
| [`GpuException`](OneGame.Gpu/GpuException.md) | class | An error from the graphics layer. |
| [`GpuFormat`](OneGame.Gpu/GpuFormat.md) | enum | Pixel formats of textures and render targets. |
| [`GpuGraphicsPipeline`](OneGame.Gpu/GpuGraphicsPipeline.md) | class | A compiled graphics pipeline. |
| [`GpuModule`](OneGame.Gpu/GpuModule.md) | static class | Identifies the Gpu assembly; a placeholder until its first feature lands. |
| [`GpuObject`](OneGame.Gpu/GpuObject.md) | class | Base class of every GPU object. |
| [`GpuOutOfMemoryException`](OneGame.Gpu/GpuOutOfMemoryException.md) | class | The device ran out of memory. |
| [`GpuPipelineLayout`](OneGame.Gpu/GpuPipelineLayout.md) | class | The resource interface shared by pipelines and bind groups. |
| [`GpuRenderPass`](OneGame.Gpu/GpuRenderPass.md) | class | Records draws inside a render pass. |
| [`GpuSampler`](OneGame.Gpu/GpuSampler.md) | class | Sampler state. |
| [`GpuShader`](OneGame.Gpu/GpuShader.md) | class | A compiled shader entry point. |
| [`GpuSurfaceLostException`](OneGame.Gpu/GpuSurfaceLostException.md) | class | The window's drawing surface is gone, as when an app is sent to the background or the system rebuilds its window. |
| [`GpuSwapchain`](OneGame.Gpu/GpuSwapchain.md) | class | Presents rendered images to a window. |
| [`GpuTexture`](OneGame.Gpu/GpuTexture.md) | class | A 2D image, 2D array, cube map, cube array or 3D volume. |
| [`GpuTimestampSet`](OneGame.Gpu/GpuTimestampSet.md) | class | A set of GPU timestamp queries. |
| [`GpuUnsupportedException`](OneGame.Gpu/GpuUnsupportedException.md) | class | The system has no usable graphics API, adapter or required feature. |
| [`GraphicsPipelineDesc`](OneGame.Gpu/GraphicsPipelineDesc.md) | class | Describes a graphics pipeline. |
| [`IndexFormat`](OneGame.Gpu/IndexFormat.md) | enum | Size of index values. |
| [`LoadOp`](OneGame.Gpu/LoadOp.md) | enum | What happens to an attachment's contents when a pass begins. |
| [`PassTiming`](OneGame.Gpu/PassTiming.md) | struct | Where a group of passes writes its GPU start and end times: pass `k` of the group uses queries `First + 2k` and `First + 2k + 1`. |
| [`PresentMode`](OneGame.Gpu/PresentMode.md) | enum | How a swapchain paces presentation. |
| [`PrimitiveTopology`](OneGame.Gpu/PrimitiveTopology.md) | enum | How vertices are assembled into primitives. |
| [`PushConstantRange`](OneGame.Gpu/PushConstantRange.md) | struct | Small constant data written straight into the command stream with `SetPushConstants`. |
| [`ResourceBinding`](OneGame.Gpu/ResourceBinding.md) | struct | Describes one shader resource binding. |
| [`ResourceKind`](OneGame.Gpu/ResourceKind.md) | enum | Kind of a shader resource binding. |
| [`SamplerDesc`](OneGame.Gpu/SamplerDesc.md) | struct | Describes a sampler to create. |
| [`ShaderLayouts`](OneGame.Gpu/ShaderLayouts.md) | static class | Turns the layouts the shader build generates (`Shader.Entry.Layout`) into this layer's own binding, push-constant and vertex types, so a pipeline layout is declared once, in the HLSL, and cannot disagree with the shaders that use it. |
| [`ShaderSource`](OneGame.Gpu/ShaderSource.md) | struct | One compiled shader entry point in every form a backend may need. |
| [`ShaderStage`](OneGame.Gpu/ShaderStage.md) | enum | Shader stage of a module or a resource's visibility. |
| [`StoreOp`](OneGame.Gpu/StoreOp.md) | enum | What happens to an attachment's contents when a pass ends. |
| [`SurfaceKind`](OneGame.Gpu/SurfaceKind.md) | enum | Native window system a [`SurfaceSource`](OneGame.Gpu/SurfaceSource.md) belongs to. |
| [`SurfaceSource`](OneGame.Gpu/SurfaceSource.md) | struct | Where a swapchain draws: the native handles of a window the platform layer created. |
| [`SwapchainDesc`](OneGame.Gpu/SwapchainDesc.md) | struct | Describes a swapchain to create. |
| [`TextureDesc`](OneGame.Gpu/TextureDesc.md) | struct | Describes a texture to create: 2D, 2D array, cube, cube array or 3D. |
| [`TextureDimension`](OneGame.Gpu/TextureDimension.md) | enum | The shape of a texture as shaders see it. |
| [`TextureRegion`](OneGame.Gpu/TextureRegion.md) | struct | Where a texture region is: a mip level, a layer, a box inside it. |
| [`TextureUsage`](OneGame.Gpu/TextureUsage.md) | enum | How a texture may be used. |
| [`TextureViewDesc`](OneGame.Gpu/TextureViewDesc.md) | struct | Selects part of a texture for a shader to see: some mip levels, some layers, and how they are presented. |
| [`TextureViewKind`](OneGame.Gpu/TextureViewKind.md) | enum | How a view of a texture presents it to a shader. |
| [`VertexBufferLayout`](OneGame.Gpu/VertexBufferLayout.md) | class | Layout of one vertex buffer binding. |
| [`VertexElement`](OneGame.Gpu/VertexElement.md) | struct | Describes one vertex attribute: where a shader input comes from inside a vertex. |
| [`VertexFormat`](OneGame.Gpu/VertexFormat.md) | enum | Format of one vertex attribute in memory. |
| [`VertexStepMode`](OneGame.Gpu/VertexStepMode.md) | enum | How often a vertex buffer advances. |

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
| [`PassType`](OneGame.Gpu.Graph/PassType.md) | enum | What kind of work a pass records, which decides how the graph runs it. |
| [`RenderGraph`](OneGame.Gpu.Graph/RenderGraph.md) | class | One frame structure for every renderer. |
| [`RenderGraphException`](OneGame.Gpu.Graph/RenderGraphException.md) | class | A problem in how a graph was declared: reading something no pass wrote, using a stale handle, touching a resource a pass did not declare. |
| [`RenderGraphOptions`](OneGame.Gpu.Graph/RenderGraphOptions.md) | class | Tuning for a [`RenderGraph`](OneGame.Gpu.Graph/RenderGraph.md). |
| [`RenderGraphReport`](OneGame.Gpu.Graph/RenderGraphReport.md) | record | What [`RenderGraph.Describe()`](OneGame.Gpu.Graph/RenderGraph.md#m-describe) returns: the structure of the last frame. |
| [`ResourceReport`](OneGame.Gpu.Graph/ResourceReport.md) | record | One texture or buffer of the last frame. |
