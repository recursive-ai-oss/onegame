# gpu — graphics layer interface

`OneGame.Gpu` is the backend-neutral API; `OneGame.Gpu.Vulkan` and `OneGame.Gpu.Metal` implement it. Renderers code against `OneGame.Gpu` only.
Everything in it maps onto Vulkan 1.3, Metal and WebGPU alike. Backend interop (`unsafe`, `LibraryImport`) lives only in the backend assemblies.

## Getting a device

```csharp
GpuDevice device = GpuBackends.CreateDevice(new GpuDeviceOptions { Validation = true });   // OneGame.Gpu.Backends
```

`GpuBackends` picks Metal on macOS and iOS and Vulkan elsewhere; `ONEGAME_GPU_BACKEND=vulkan|metal` overrides it. `VulkanGpu.CreateDevice` and `MetalGpu.CreateDevice` create a specific backend. `device.Backend` says which one you got. `GpuBackends.IsAvailable` / `UnavailableReason` tell whether the selected one can run.

- Adapter choice: discrete, then integrated, virtual, software. Override with `GpuDeviceOptions.Adapter` (name substring or kind: `software`, `discrete`, `integrated`) or `ONEGAME_GPU_ADAPTER`.
- `ONEGAME_GPU_VALIDATION=1` turns on the Khronos validation layer (plus synchronization validation). `RequireValidation` fails creation when the layer is missing. `device.TakeValidationMessages()` returns warnings and errors since the last call; tests assert it is empty.
- Needs Vulkan 1.3 (dynamic rendering, synchronization2, timeline semaphores). Otherwise `GpuUnsupportedException`.
- One device is used from one thread. Errors are `GpuException` (`GpuUnsupportedException`, `GpuOutOfMemoryException`, `GpuDeviceLostException`); misuse is `ArgumentException` / `InvalidOperationException`.

## Conventions (same on every backend)

- Clip space: +Y up, depth 0..1. Framebuffer origin top-left. `FrontFace.CounterClockwise` means counter-clockwise with +Y up.
- No explicit barriers or layouts: the backend tracks resource use and inserts them. Work executes in submission order; record and submit encoders in the same order.
- Texture data is tightly packed rows. Mip levels are read and written one at a time.
- Resources are disposed with `Dispose()`; the GPU may still be using them and the backend defers the release. Do not dispose a resource an unsubmitted encoder uses. Dispose resources before the device.
- A bind group does not keep its resources alive: keep them until the bind group is disposed.

## Resources

| Create | Notes |
|---|---|
| `CreateBuffer(new BufferDesc(size, usage))` | `BufferUsage`: Vertex, Index, Uniform, Storage, CopySource, CopyDestination. |
| `CreateTexture(new TextureDesc(w, h, format, usage) { MipLevels, ArrayLayers, SampleCount, Dimension, Depth })` | 2D, 2D array, cube, cube array or 3D (see "Textures"). Formats: R8, Rg8, Rgba8(+Srgb), Bgra8(+Srgb), R16F, Rg16F, Rgba16F, R32F, Rg32F, Rgba32F, R32Uint, Rg11B10F, Rgb10A2, Depth16, Depth32F, Depth32F+Stencil8. `GpuTexture.FullMipCount(w, h[, depth])`. |
| `CreateSampler(SamplerDesc.Linear / Nearest)` | Filters, address modes (U, V, W), anisotropy, LOD range, and `Compare` for shadow lookups. |
| `CreateShader(new ShaderSource(stage, entry) { ... })` | One entry point in every form: `SpirV` (Vulkan), `Msl` + `MslFunctionName` (Metal), `Wgsl` (browser), and the entry point's `Layout`. `CreateShader(stage, spirv, entryPoint)` takes bare SPIR-V and is Vulkan-only. |
| `CreatePipelineLayout(bindings[, new PushConstantRange(size, stages)])` | Groups bindings by set; duplicates from several shaders merge. `ResourceBinding.Dynamic` makes a constant or storage buffer binding take a byte offset at bind time. |
| `CreateBindGroup(layout, set, entries)` | One entry per binding of the set (`BindGroupEntry.ForBuffer/ForTexture/ForSampler`; `ForTextures/ForSamplers/ForBuffers` for a binding whose `Count` is above 1; `ForTexture(binding, texture, view)` for a mip or layer subset). |
| `CreateGraphicsPipeline(GraphicsPipelineDesc)` | Vertex layouts (per-vertex and per-instance), topology, cull, blend per target (`BlendState.AlphaBlend/Premultiplied/Additive`), optional depth with bias, `DepthClamp`, sample count. Viewport and scissor are always dynamic. |
| `CreateComputePipeline(ComputePipelineDesc)` | |
| `CreateTimestampSet(count)` | GPU timestamp queries (see "Timing"). |

### Plugging in generated shader bindings

The shader library's generated classes feed straight in. `ResourceBinding(Set, Binding, Count, Kind, Stages)` and `VertexElement(Location, Format, Offset)` mirror its support types.

```csharp
using var vs = device.CreateShader(new ShaderSource(ShaderStage.Vertex, Sprite.VSMain.EntryPoint)
{
    SpirV = Sprite.VSMain.SpirV,
    Msl = Sprite.VSMain.Msl, MslFunctionName = Sprite.VSMain.MslFunctionName,
    Layout = Sprite.VSMain.Layout,                       // Metal argument indices, push-constant slot and compute group size
});
using var layout = device.CreatePipelineLayout(ShaderLayouts.Bindings([Sprite.VSMain.Layout, Sprite.PSMain.Layout]));
var vertex = new VertexBufferLayout { Stride = (uint)Sprite.VSMain.Stride, Attributes = Sprite.VSMain.Inputs.ToArray() };
```

`Layout` also carries a compute shader's thread-group size and the slot of a `[[vk::push_constant]]` block on Metal and WebGPU, so nothing else is passed. `ShaderLayouts.Bindings(entries, dynamic, widenSets)`, `ShaderLayouts.PushConstants(entries)` and `ShaderLayouts.Vertex(entry, step, overrides)` derive the pipeline layout and vertex buffer layout from the generated layouts (OneShader's `TryCreateUnion` and `VertexLayoutOverrides`): entry points that run together agree on kind and count at a slot and their stages are united, `dynamic` names (set, binding) pairs that take a byte offset, `widenSets` adds stages to a whole set so one bind group fits pipelines that read it from different stages, and `overrides` says what the engine's vertex stores differently (a packed colour, a smaller stride). `UnionOptions` lets one slot be read as a 2D texture by one entry point and a cube by another. `ShaderLayouts.VertexStreams(entry, streams)` splits one vertex input into several buffers (a mesh and an instance stream). Build the project with `<OneShaderTargets>SpirV;Msl</OneShaderTargets>` (the repository default also adds Wgsl) so both forms exist.

To see the HLSL in RenderDoc, Nsight or Xcode, add `<OneShaderDebugInfo Condition="'$(Configuration)' == 'Debug'">true</OneShaderDebugInfo>` to the project that holds the shaders. It is off by default and must stay off in exports (Release), since the debug blobs are larger.

A pipeline is immutable. Replacing a shader means making a new pipeline and releasing the old one after `WaitIdle()`; `Renderer2D.ReplaceMaterialShader` does that for hot reload.

## Uploading and reading back

- `WriteBuffer` / `WriteTexture` copy through a pooled staging buffer and run before the next submitted commands. The destination needs `CopyDestination`.
- `ReadBuffer` / `ReadTexture` wait for the GPU and copy out. The source needs `CopySource`. This is the capture path: render into a `RenderTarget | CopySource` texture, then `ReadTexture`.
- `WriteTexture(texture, new TextureRegion(mip, layer, x, y, z, w, h, depth), data)` writes a box (a sub-rectangle for atlas updates, slices of a volume). `WriteTexture(texture, mip, layer, data)` is the whole-level shorthand (all slices of a volume).
- `encoder.GenerateMips(texture)` fills levels 1.. from level 0 (linear blit; 2D, array, cube and 3D). `encoder.CopyTexture(source, destination)` copies every mip and layer between textures of the same shape.

## Textures

- **Cube:** `Dimension = Cube`, square, `ArrayLayers` a multiple of 6 (6 = one cube, more = a cube array). Layer order +X, -X, +Y, -Y, +Z, -Z. Render into a face with `ColorAttachment.Layer`; shaders sample the whole thing as `TextureCube` / `TextureCubeArray`.
- **3D:** `Dimension = D3`, `Depth`, one layer. Sampled, storage and copy use; not a render target.
- **2D arrays and mip levels as render targets:** `ColorAttachment` and `DepthAttachment` both take `MipLevel` and `Layer` (shadow cascades are layers of a depth array).
- **Views:** `BindGroupEntry.ForTexture(binding, texture, new TextureViewDesc { BaseMip, MipCount, BaseLayer, LayerCount, Kind })`, with `TextureViewDesc.Mip(n)` and `.Layer(n)` shorthands. The default view of a cube is a cube, of a 3D texture a volume, of an array an array. A view that shows one layer is 2D.
- **Depth as a texture:** a depth texture created with `Sampled | RenderTarget` is sampled through a depth view; a sampler with `Compare` set gives shadow-map lookups (`SampleCmp`).
- **MSAA:** `SampleCount` 2, 4 or 8 (`Capabilities.MaxSampleCount`) on a 2D single-mip texture; `ColorAttachment.Resolve` names the single-sample target. Multisampled textures cannot be sampled.
- **Tracking granularity:** the backend tracks one state per texture, not per mip or layer. In one pass do not sample a texture you also render into, even different mips or layers of it; use two textures (or two passes).

## Pipelines, binding and draws

- **Depth bias:** `DepthStencilState.DepthBias` (integer steps of the smallest depth difference), `DepthBiasSlopeScale`, `DepthBiasClamp`. **Depth clamp:** `GraphicsPipelineDesc.DepthClamp`, only when `Capabilities.DepthClamp`.
- **Push constants:** declare a `PushConstantRange` (at most `GpuDevice.MaxPushConstantBytes` = 128, multiple of 4) on the layout and write with `pass.SetPushConstants(offset, bytes)` in render and compute passes. Metal has no push constants of its own: the block is sent with `setBytes` at the index the shader build reports, so declare it in HLSL with `[[vk::push_constant]]` and give the shader its `MslPushConstantIndex`.
- **Dynamic offsets:** `new ResourceBinding(...) { Dynamic = true }`; the bind group's entry gives the visible `Size` (required), and `SetBindGroup(set, group, offsets)` gives one offset per dynamic binding in binding order, each a multiple of `GpuDevice.DynamicOffsetAlignment` (256).
- **Resource arrays:** a binding with `Count` N takes N textures, samplers or buffers; indexing by a value that differs between invocations needs `Capabilities.NonUniformArrayIndexing`. At most `Capabilities.MaxResourceArrayCount` (≤ 512) elements.
- **Instancing:** a `VertexBufferLayout` with `StepMode = Instance`; `Draw(vertices, instances, ...)`.
- **Compute:** `BeginComputePass`, `SetPipeline`, `SetBindGroup`, `Dispatch`, `DispatchIndirect(buffer, offset)`. Storage textures need `TextureUsage.Storage`; a texture written by a dispatch and read by a draw (or the other way round) needs no barrier code, the backend adds it.
- **Indirect:** buffers created with `BufferUsage.Indirect` hold `DrawIndirectArgs` (16 bytes), `DrawIndexedIndirectArgs` (20) or `DispatchIndirectArgs` (12), packed. `DrawIndirect(buffer, offset, drawCount)` and `DrawIndexedIndirect` draw `drawCount` consecutive records (one GPU command with `Capabilities.MultiDrawIndirect`, a loop otherwise, same result). A non-zero `FirstInstance` needs `Capabilities.IndirectFirstInstance`.

## Timing and capabilities

`device.Capabilities` (`GpuCapabilities`) lists what is optional: `Timestamps`, `MultiDrawIndirect`, `IndirectFirstInstance`, `DepthClamp`, `DepthBiasClamp`, `NonUniformArrayIndexing`, `MaxSampleCount`, `MaxTextureSize`, `MaxTextureSize3D`, `MaxArrayLayers`, `MaxResourceArrayCount`.

Pass times: `device.CreateTimestampSet(n)`, then `encoder.BeginPassTiming(new PassTiming(set, firstQuery, maxPasses))` makes each following render or compute pass write a start and an end timestamp (queries `first + 2k`, `first + 2k + 1`); `EndPassTiming()` returns how many were timed. `device.TryReadTimestamps(set, first, span)` returns nanoseconds, or false while the GPU has not finished. Resetting is the backend's business. The render graph does all this per pass.

## Vulkan-only and backend notes

- Native push constants, multi-draw and timestamps exist on Vulkan; Metal has `setBytes`, indirect command buffers and counter sample buffers; WebGPU has no push constants (emulate), draws indirect one at a time, and timestamps are an optional feature. The API above is the common subset.
- A WebGPU backend must tell comparison from filtering samplers when it builds bind group layouts; the shader's resource kind `Sampler` covers both, so it reads that from the shader module or the `SamplerDesc.Compare` of the sampler at first use.
- Clamp-to-border is not offered (WebGPU has none): shadow code clamps to the edge and tests the light-space position itself.
- Not built: compute-written mip chains in one texture (per-mip state), render-to-3D-slice, 8-bit depth formats, compressed formats, stencil state.


## Frames

```csharp
var enc = device.BeginCommands();                       // pooled; reused after Submit
var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, 0, 0, 0, 1), depth: null);
pass.SetPipeline(p); pass.SetBindGroup(0, g); pass.SetVertexBuffer(0, vb); pass.SetIndexBuffer(ib, IndexFormat.Index16);
pass.DrawIndexed(6);
pass.EndPass();                                          // viewport and scissor default to the full target
device.Submit(enc);                                      // at most GpuDevice.MaxFramesInFlight submissions run ahead
```

Passes: `BeginRenderPass(colors, depth)` (multiple targets, mip/layer selection, MSAA resolve), `BeginComputePass()` + `Dispatch`. Copies (`CopyBuffer`, `CopyTextureToBuffer`, `CopyBufferToTexture`, `CopyTexture`) are recorded outside passes. Frame structure lives one level up: `render-graph.md`. Passes, encoders and the steady-state draw loop allocate nothing after warm-up (measured by a test).

## Window surfaces

The platform layer hands over native handles; this assembly takes them as plain values, so neither references the other:

```csharp
SurfaceSource.X11(display, window)        // Display*, Window id
SurfaceSource.Wayland(display, surface)   // wl_display*, wl_surface*
SurfaceSource.Win32(hinstance, hwnd)
using var sc = device.CreateSwapchain(surface, new SwapchainDesc(width, height) { Srgb = false, PresentMode = PresentMode.Vsync });
```

Per frame: `sc.TryAcquire(out var backbuffer)` (returns `Skipped` while the window has no area), render into `backbuffer` and submit, then `sc.Present()`. `sc.AcquireTimeout` (default 2 s) bounds the wait for a free image; on timeout `TryAcquire` returns `Skipped`, so a window system that stops answering cannot hang the frame loop. The swapchain rebuilds itself when the window size changes (polled from the surface on X11 and Win32; call `sc.Resize(w, h)` where the window system does not report a size, as on Wayland). Backbuffers are `RenderTarget | CopySource`, so a presented frame can be captured too.

Verified: X11 (Xlib window created by the test, under Xvfb), including resize. Wayland and Win32 surface creation is implemented but not exercised yet.

## Testing against the layer

- Run with `VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/lvp_icd.json` for the software rasterizer (golden images come from it); `nvidia_icd.json` / `radeon_icd.json` for real GPUs.
- The validation layer is found through `VK_ADD_LAYER_PATH` (and `LD_LIBRARY_PATH` for the layer library).
- Shader fixtures in the tests are HLSL in `tests/OneGame.Gpu.Vulkan.Tests/Shaders`, compiled during the build by the shader build package; the Vulkan binding code is generated by `tools/OneGame.VkBindGen` from `vk.xml` (`tools/fetch-vkxml.sh`).

## Metal backend (`OneGame.Gpu.Metal`)

- Runs on Apple arm64 (macOS, iOS) through the Objective-C runtime (`objc_msgSend` with typed function pointers, source-generated `LibraryImport` for the runtime and framework entry points); no Objective-C code, no reflection.
- `GpuDeviceOptions.Validation` turns on Metal API validation (the debug layer): it is read when the first Metal device of the process is created, so set `MTL_DEBUG_LAYER=1` in the environment of a test run. With it on a validation failure ends the process with Metal's message (`MTL_DEBUG_LAYER_ERROR_MODE=assert`, set by the backend unless already set). `RequireValidation` fails creation when the layer is not active. `TakeValidationMessages` returns failed command buffers.
- Resources are `Private` (GPU-only); writes and reads go through staging buffers, ordered like the other backends. Metal tracks hazards between encoders itself, so the backend needs no barriers.
- Clip space and the texture origin already match the API's conventions; nothing is flipped. `FrontFace.CounterClockwise` is `MTLWindingCounterClockwise`.
- Vertex buffer slot `n` is bound at Metal buffer index `30 - n`; resources use the shader build's Metal indices, counted per class (buffer, texture, sampler) over a shader file. A bind group is applied lazily at the next draw or dispatch, so one layout can serve shaders of different files (the 2D materials).
- Presenting: `SurfaceSource.MetalLayer(caMetalLayer)` (the layer a view owns, made by the window system layer). Backbuffers are readable. A layer that belongs to no view works too, for headless tests.
- Not built yet: timestamps (`Capabilities.Timestamps` is false, so the render graph reports no pass times), shader hot reload (the reloader recompiles SPIR-V only), x86-64 Macs (struct-returning message sends need `objc_msgSend_stret` there).

## WebGPU backend (`OneGame.Gpu.WebGpu`, browser only)

`GpuBackends.Selected` is `WebGpu` in a browser. A page creates the device asynchronously: `GpuBackends.CreateDeviceAsync` (or `WebGpu.CreateDeviceAsync`); the synchronous `CreateDevice` throws `GpuUnsupportedException` there. Shaders come from the same HLSL files, built with `OneShaderTargets` including `Wgsl`; a `ShaderSource` carries `Wgsl`, `WgslEntryPoint` and, for a shader with a `[[vk::push_constant]]` block, `WgslPushGroup`/`WgslPushBinding`.

- **A browser cannot block.** `WaitIdle` and the synchronous reads throw `GpuUnsupportedException`; use `ReadBufferAsync`/`ReadTextureAsync` (native backends complete them before returning) and `Offscreen3D.CaptureAsync`. `GameInstance.CaptureAsync` is the harness's path.
- **Command stream.** Calls are recorded into one byte stream per encoder and executed by `js/onegame-gpu.js` in a single JavaScript call per submit; resources are integer handles.
- **Layouts per pipeline.** WebGPU layouts name each texture's dimension, so the script builds a layout variant for each set of shaders from the declarations in their WGSL and makes bind groups against the variant of the pipeline they are used with. A slot a pipeline's shaders do not declare is left out of its layout. After changing to a pipeline made for a different `GpuPipelineLayout`, bind groups again.
- **Push constants** are a uniform buffer at the generated group and binding, bound with a dynamic offset into a ring: the block is at most 128 bytes, rounded up to 16, and must not share its group with the layout's own bindings.
- **Not available:** binding arrays (`Count` above 1; WGSL has none), timestamp queries (`TryReadTimestamps` returns false), `rg16f` storage images (the environment BRDF table is `Rgba16Float` on every backend for this reason), MSAA other than 1 or 4 samples (2 maps to 4).
- **Canvas.** `SurfaceSource.Canvas(id)`; the swapchain is `bgra8unorm` with an sRGB view. Headless Chrome on its software adapter cannot present a canvas (the GPU process loses the device); run such a page with `--no-present`.
- **Tests.** `tests/OneGame.Gpu.WebRunner` runs the shared graphics tests in a browser (`tools/web/run-page.mjs`), `tests/OneGame.Render3D.WebRunner` the Khronos reference cases. Test tooling only.
