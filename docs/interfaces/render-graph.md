# render-graph — one frame structure for every renderer

`OneGame.Gpu.Graph` (in `src/OneGame.Gpu`, namespace `OneGame.Gpu.Graph`) builds a frame from passes that declare what they read and write. The 2D renderer runs as a pass on it; the 3D renderer (`render3d.md`) adds many. It is backend-neutral: it only uses `gpu.md`.

```csharp
using var graph = new RenderGraph(device);                 // one per device; keeps its memory pool between frames

graph.BeginFrame();
var depth  = graph.CreateTexture("depth", new TextureDesc(w, h, GpuFormat.Depth32Float, TextureUsage.None));   // usage is inferred
var color  = graph.CreateTexture("scene", new TextureDesc(w, h, GpuFormat.Rgba16Float, TextureUsage.None));
var screen = graph.ImportTexture("screen", backbuffer);    // not owned by the graph

graph.AddPass(scenePass);                                  // IGraphPass instances kept between frames: no allocation per frame
graph.AddPass("tonemap",
    b => { b.Read(color); b.ColorTarget(screen); },        // Setup: declare
    c => { c.Render.SetPipeline(p); c.Render.SetBindGroup(0, g); c.Render.Draw(3); });   // Execute: record
graph.Execute();                                           // validate, cull, allocate, record, submit
```

## Declaring a pass

`PassBuilder` (valid only inside `Setup`):

| Call | Meaning |
|---|---|
| `Read(texture)` / `Read(buffer, BufferRead.Uniform\|Storage\|Vertex\|Index\|Indirect\|CopySource)` | sampled / read as that kind |
| `ReadStorage`, `WriteStorage` (texture or buffer) | storage use; a write keeps what was there |
| `ColorTarget(texture, load, r, g, b, a, store, mip, layer, resolve)` | render target; up to 8; `LoadOp.Load` also reads it |
| `DepthTarget(texture, load, clearDepth, store, mip, layer)` | depth target; one |
| `CopySource`, `CopyDestination` | copy use |
| `Write(texture)` / `ReadWrite(texture or buffer)` | a pass that opens its own passes (custom) overwrites all of it / modifies it |
| `Compute()` | the graph opens a compute pass |
| `SideEffect()` | run even if nothing uses the results (readback, present, overlay) |

The pass's kind follows from that: with targets it is **raster** (the graph begins the render pass and ends it; `context.Render`), with `Compute()` it is **compute** (`context.Compute`), otherwise **custom** (`context.Encoder`, for copies, `GenerateMips`, or code that opens its own passes like `Renderer2D.EndFrame`). `context.Texture(handle)` and `context.Buffer(handle)` give the physical resource of anything the pass declared; asking for anything else is a `RenderGraphException`, because the graph could not have ordered or culled around it.

## What `Execute` does

1. **Validates.** A pass may only read a transient resource some earlier pass wrote; a pass may not use one resource twice if either use writes it (the backend keeps one state per texture). Violations throw `RenderGraphException` naming the pass and resource. Handles from another frame are rejected.
2. **Culls.** Walking backwards: the last writer of an imported resource is a root, so is any `SideEffect` pass. A pass survives if it writes something that is live. Reads of survivors make resources live; a pass that overwrites a whole resource (a clear, or `Write`) ends the liveness of what was written before it, so earlier writers drop out. Rendering into one mip or layer of a multi-mip or array texture is a partial write and does not.
3. **Orders.** Declaration order is dependency order: handles exist only after their creating call, and a read sees what the passes declared before it wrote. Survivors run in declaration order.
4. **Allocates.** A transient's lifetime runs from its first to its last surviving use. Transients with the same shape (size, format, mips, layers, samples, dimension) whose lifetimes do not overlap share one physical texture or buffer; usage flags are the union of what the sharers do, plus what the accesses imply. Physical resources live in a pool keyed by exact description and are handed out again next frame, so a steady graph creates nothing. Memory unused for `KeepUnusedFrames` (8) frames is released. Different shapes do not share memory; that needs allocator-level aliasing (heaps), which is a later backend feature.
5. **Barriers and layouts.** The backend tracks the last use of every resource and inserts the barriers and layout changes the recorded order needs, so the graph does not emit any. Reused memory gets a defined first write because the validator insists on it. Tests run with synchronization validation on.
6. **Records and submits** one encoder, one `Submit`.

## Time

With `Capabilities.Timestamps` every raster and compute pass gets a timestamp pair; a custom pass gets up to 8 (one per pass it opens). Results are read back two or three frames later without waiting; `Describe()` reports the newest finished frame's times by pass name (`timingFrame` says which frame). `RenderGraphOptions.Timing = false` turns it off.

## Inspecting

`graph.Describe()` returns a `RenderGraphReport`; `graph.Stats` holds the totals of the last frame. The live-control operation `render_graph` returns the report of the running game's graph as JSON (it draws one frame offscreen first if the game has not drawn yet; `no_graph` without a graphics device):

```json
{"frame":3,"timing":true,"timingFrame":2,
 "passes":[{"name":"render2d","type":"custom","culled":false,"order":0,"reads":[],"writes":[{"resource":"target","access":"Write","mip":0,"layer":0}],"gpuMicroseconds":41.2}],
 "resources":[{"name":"target","kind":"texture","imported":true,"used":true,"format":"Rgba8UnormSrgb","size":"1280x720","bytes":3686400,"slot":-1,"firstPass":"render2d","lastPass":"render2d","usage":"RenderTarget, CopySource"}],
 "stats":{"passes":1,"livePasses":1,"culledPasses":0,"transientResources":0,"physicalTextures":0,"physicalBuffers":0,"transientBytes":0,"unaliasedBytes":0,"created":0,"reused":0}}
```

Resources with the same `slot` share memory. `transientBytes` against `unaliasedBytes` shows what sharing saves.

## Writing renderers on it

- Keep pass objects between frames and set their handles each frame (see `Render2DStage`). A lambda that captures variables allocates every frame; a static lambda does not. A steady graph of ten passes allocates zero bytes (test).
- 2D: `Render2DStage` adds one custom pass, `render2d`, that `Write`s the host's target and calls `Renderer2D.EndFrame(context.Encoder)`. Its output is byte-identical to the stage before the graph.
- Shadow cascades, SSAO, bloom chains: create transients per frame; the graph shares memory between those whose lifetimes do not overlap and keeps the rest pooled.
- Mip chains and cascades inside one texture: render each mip or layer in its own pass with `mip`/`layer`; do not sample the texture you render into in the same pass.

## Backends

The graph is backend-neutral. On Metal every pass is its own encoder and hazards between passes are tracked by Metal, so the graph's dependency order is all that matters; pass timing is unavailable there (`GpuCapabilities.Timestamps` is false), and so on WebGPU.

## Not built yet

Reordering passes for overlap or memory (only declaration order), async compute, per-subresource state tracking, cross-frame (history) resources other than imports, heap-level aliasing of different shapes.
