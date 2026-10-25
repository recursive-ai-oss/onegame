# gpu — graphics layer interface

`OneGame.Gpu` is the backend-neutral API; `OneGame.Gpu.Vulkan` implements it. Renderers code against `OneGame.Gpu` only.
Everything in it maps onto Vulkan 1.3, Metal and WebGPU alike. Backend interop (`unsafe`, `LibraryImport`) lives only in the backend assemblies.

## Getting a device

```csharp
GpuDevice device = VulkanGpu.CreateDevice(new GpuDeviceOptions { Validation = true });
```

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
| `CreateTexture(new TextureDesc(w, h, format, usage) { MipLevels, ArrayLayers, SampleCount })` | 2D or 2D array. Formats: R8, Rg8, Rgba8(+Srgb), Bgra8(+Srgb), R16F, Rgba16F, R32F, Rg32F, Rgba32F, R32Uint, Depth32F, Depth32F+Stencil8. `GpuTexture.FullMipCount(w, h)`. |
| `CreateSampler(SamplerDesc.Linear / Nearest)` | Filters, address modes, anisotropy. |
| `CreateShader(stage, spirv, entryPoint)` | SPIR-V bytes. |
| `CreatePipelineLayout(ReadOnlySpan<ResourceBinding>)` | Groups bindings by set; duplicates from several shaders merge. |
| `CreateBindGroup(layout, set, entries)` | One entry per binding of the set (`BindGroupEntry.ForBuffer/ForTexture/ForSampler`). No resource arrays or dynamic offsets yet. |
| `CreateGraphicsPipeline(GraphicsPipelineDesc)` | Vertex layouts, topology, cull, blend per target, optional depth, sample count. Viewport and scissor are always dynamic. |
| `CreateComputePipeline(ComputePipelineDesc)` | |

### Plugging in generated shader bindings

The shader library's generated classes feed straight in. `ResourceBinding(Set, Binding, Count, Kind, Stages)`, `VertexAttribute`-shaped `VertexElement(Location, Format, Offset)`, `ShaderStage` and `ResourceKind` mirror its support types and are replaced by them when the package ships.

```csharp
using var vs = device.CreateShader(Sprite.VSMain.Stage, Sprite.VSMain.SpirV, Sprite.VSMain.EntryPoint);
using var layout = device.CreatePipelineLayout(Sprite.Bindings.All);
var vertex = new VertexBufferLayout { Stride = (uint)Sprite.VSMain.Stride, Attributes = Sprite.VSMain.Inputs.ToArray() };
```

A pipeline is immutable. Replacing a shader means making a new pipeline and releasing the old one after `WaitIdle()`; `Renderer2D.ReplaceMaterialShader` does that for hot reload.

## Uploading and reading back

- `WriteBuffer` / `WriteTexture` copy through a pooled staging buffer and run before the next submitted commands. The destination needs `CopyDestination`.
- `ReadBuffer` / `ReadTexture` wait for the GPU and copy out. The source needs `CopySource`. This is the capture path: render into a `RenderTarget | CopySource` texture, then `ReadTexture`.
- `encoder.GenerateMips(texture)` fills levels 1.. from level 0 (linear blit).

## Frames

```csharp
var enc = device.BeginCommands();                       // pooled; reused after Submit
var pass = enc.BeginRenderPass(ColorAttachment.Clear(target, 0, 0, 0, 1), depth: null);
pass.SetPipeline(p); pass.SetBindGroup(0, g); pass.SetVertexBuffer(0, vb); pass.SetIndexBuffer(ib, IndexFormat.Index16);
pass.DrawIndexed(6);
pass.EndPass();                                          // viewport and scissor default to the full target
device.Submit(enc);                                      // at most GpuDevice.MaxFramesInFlight submissions run ahead
```

Passes: `BeginRenderPass(colors, depth)` (multiple targets, mip/layer selection, MSAA resolve), `BeginComputePass()` + `Dispatch`. Copies (`CopyBuffer`, `CopyTextureToBuffer`, `CopyBufferToTexture`) are recorded outside passes. Passes, encoders and the steady-state draw loop allocate nothing after warm-up (measured by a test).

## Window surfaces

The platform layer hands over native handles; this assembly takes them as plain values, so neither references the other:

```csharp
SurfaceSource.X11(display, window)        // Display*, Window id
SurfaceSource.Wayland(display, surface)   // wl_display*, wl_surface*
SurfaceSource.Win32(hinstance, hwnd)
using var sc = device.CreateSwapchain(surface, new SwapchainDesc(width, height) { Srgb = false, PresentMode = PresentMode.Vsync });
```

Per frame: `sc.TryAcquire(out var backbuffer)` (returns `Skipped` while the window has no area), render into `backbuffer` and submit, then `sc.Present()`. The swapchain rebuilds itself when the window size changes (polled from the surface on X11 and Win32; call `sc.Resize(w, h)` where the window system does not report a size, as on Wayland). Backbuffers are `RenderTarget | CopySource`, so a presented frame can be captured too.

Verified: X11 (Xlib window created by the test, under Xvfb), including resize. Wayland and Win32 surface creation is implemented but not exercised yet.

## Testing against the layer

- Run with `VK_ICD_FILENAMES=/usr/share/vulkan/icd.d/lvp_icd.json` for the software rasterizer (golden images come from it); `nvidia_icd.json` / `radeon_icd.json` for real GPUs.
- The validation layer is found through `VK_ADD_LAYER_PATH` (and `LD_LIBRARY_PATH` for the layer library).
- Shader fixtures in the tests are HLSL in `tests/OneGame.Gpu.Vulkan.Tests/Shaders`, compiled during the build by the shader build package; the Vulkan binding code is generated by `tools/OneGame.VkBindGen` from `vk.xml` (`tools/fetch-vkxml.sh`).
