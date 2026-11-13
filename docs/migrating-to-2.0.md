# Moving a game from onegame 1.x to 2.0

onegame 2.0 takes its GPU layer from the standalone `RecursiveAI.OneGpu` packages (devices, buffers, textures, pipelines and the Vulkan, Metal and WebGPU backends). The render graph, the renderers, the UI and everything else stay in onegame. Games that only use `GameApp`, entities, views, UI, `Renderer2D` and `Renderer3D` need no source change at all. The changes below touch code that names GPU types directly, and a few settings.

`RecursiveAI.OneGpu*` 1.0 comes with the onegame packages as ordinary dependencies; there is nothing to add to a game project.

## Namespaces

The GPU types keep their names; their namespaces change.

| 1.x | 2.0 |
|---|---|
| `using OneGame.Gpu;` (`GpuDevice`, `GpuBuffer`, `GpuTexture`, `GpuFormat`, `ShaderSource`, `GpuException`, and the rest of the device layer) | `using RecursiveAI.OneGpu;` |
| `using OneGame.Gpu.Vulkan;` | `using RecursiveAI.OneGpu.Vulkan;` |
| `using OneGame.Gpu.Metal;` | `using RecursiveAI.OneGpu.Metal;` |
| `using OneGame.Gpu.WebGpu;` | `using RecursiveAI.OneGpu.WebGpu;` |
| `OneGame.Gpu.Backends`, `GpuBackends` | `GpuBackends` is in `RecursiveAI.OneGpu` (package `RecursiveAI.OneGpu.Backends`) |
| `using OneGame.Gpu.Graph;` (render graph) | unchanged |

Fully qualified names change the same way (`OneGame.Gpu.GpuBackends.CreateDevice` becomes `RecursiveAI.OneGpu.GpuBackends.CreateDevice`). The compiler points at every place.

Also changed in the GPU API:

- `GpuModule.Name` and `VulkanModule.Name` are gone. To touch an assembly, use `typeof(GpuDevice).Assembly`.
- `GpuDevice.CreateBuffer` and `GpuCommandEncoder.BeginRenderPass` have overloads where 1.x had optional parameters. Source code compiles unchanged; code compiled against 1.x must be rebuilt.
- `GpuDeviceOptions.ApplicationName` defaults to `"onegpu"` instead of `"onegame"`. `GameApp` still passes your game's name.

## Packages

These package ids are no longer produced; their contents are in `RecursiveAI.OneGpu` and its backends:

| Removed | Replaced by |
|---|---|
| `RecursiveAI.OneGame.Gpu.Backends` | `RecursiveAI.OneGpu.Backends` |
| `RecursiveAI.OneGame.Gpu.Vulkan` | `RecursiveAI.OneGpu.Vulkan` |
| `RecursiveAI.OneGame.Gpu.Metal` | `RecursiveAI.OneGpu.Metal` |
| `RecursiveAI.OneGame.Gpu.WebGpu` | `RecursiveAI.OneGpu.WebGpu` |

`RecursiveAI.OneGame.Gpu` stays and now holds only the render graph (`OneGame.Gpu.Graph`); it depends on `RecursiveAI.OneGpu`. A game that references `RecursiveAI.OneGame.Host` (or the SDK) gets all of them transitively, so the usual project file does not change. A project that listed the removed ids by hand should drop those lines.

## Environment variables

The GPU layer reads these names now. Setting one of the 1.x names still works in a game or in `onegame view`: onegame copies it to the new name before the first device is created, unless the new name is already set. Programs that create their own device with `GpuBackends` read only the new names.

| 1.x | 2.0 |
|---|---|
| `ONEGAME_GPU_VALIDATION` | `ONEGPU_VALIDATION` |
| `ONEGAME_GPU_ADAPTER` | `ONEGPU_ADAPTER` |
| `ONEGAME_GPU_BACKEND` | `ONEGPU_BACKEND` |
| `ONEGAME_VALIDATION_OPTIONAL` | `ONEGPU_VALIDATION_OPTIONAL` |
| `ONEGAME_VULKAN_LIBRARY` | `ONEGPU_VULKAN_LIBRARY` |

Scripts and CI files should move to the new names; the old ones are kept for 2.x and may go in a later major release. The test and tool variables that onegame itself reads (`ONEGAME_CAPTURE_DIR`, `ONEGAME_UPDATE_GOLDENS`, `ONEGAME_WINDOWED_ADAPTER` and the like) are unchanged.

## Web

`onegame export --target web` writes the page for you, so exported games need no change. The WebGPU script is no longer copied next to the page as `onegame-gpu.js`; the `RecursiveAI.OneGpu.WebGpu` package publishes it at `_content/RecursiveAI.OneGpu.WebGpu/onegpu.js`, and its module is named `onegpu`.

A page you wrote yourself (a custom `boot.mjs`) that imports the script and registers it must change two lines:

```js
import * as gpu from './_content/RecursiveAI.OneGpu.WebGpu/onegpu.js';
...
runtime.setModuleImports('onegpu', gpu);      // was setModuleImports('onegame-gpu', gpu)
```

A page that does neither works without edits: the WebGPU backend imports the script itself. The script's functions take the device's context number first, for example `gpu.takeMessages(0)` for the first device, since several devices can now share a page.

## Behaviour that changed without a code change

- Vulkan has one instance per process, shared by all devices, so creating several devices (tests, tools) no longer exhausts instances on some drivers.
- Adapters are listed in one fixed order, and a Vulkan swapchain failure throws `GpuUnsupportedException`.
- `ReadBufferAsync` is asynchronous on Vulkan and Metal.
- Validation messages are collected per device, and `TakeValidationMessages()` works after the device is disposed.

## Checklist

1. Replace the namespaces above; build.
2. Move scripts and CI settings to the `ONEGPU_*` names.
3. Custom web pages: update the two lines above.
4. Remove references to the removed package ids.
