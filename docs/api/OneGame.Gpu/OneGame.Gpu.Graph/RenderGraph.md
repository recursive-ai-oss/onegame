[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# RenderGraph class

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class RenderGraph : IDisposable
```

One frame structure for every renderer. Each frame: [`RenderGraph.BeginFrame()`](RenderGraph.md#m-beginframe), create or import resources, add passes that declare what they read and write, then [`RenderGraph.Execute()`](RenderGraph.md#m-execute).

**What the graph does with the declarations.** It checks them (a pass may not read what no earlier pass wrote), culls passes whose results nobody uses (a pass survives if it is marked [`PassBuilder.SideEffect()`](PassBuilder.md#m-sideeffect), or writes an imported resource that nothing later overwrites, or writes something a surviving pass reads), allocates the transient textures and buffers, and records the survivors into one encoder in declaration order. Declaration order is dependency order: a pass can only name resources that exist, and reading a resource means reading what the passes declared before it wrote.

**Aliasing.** Transient resources whose lifetimes do not overlap and whose shapes (size, format, mips, layers, samples) match share one physical texture or buffer. Physical resources stay in a pool between frames, so a steady graph creates nothing after its first frames.

**Barriers and layouts** are the backend's job: it tracks every resource's last use and inserts what the order of the recorded passes needs, so the graph only has to put passes in a valid order and give reused memory a defined first write.

**Allocation.** Pass objects that implement [`IGraphPass`](IGraphPass.md) and are kept between frames make a graph that allocates nothing per frame once its pools have warmed up. Lambdas that capture variables allocate every frame.

## Constructors

| Member | Description |
| --- | --- |
| [`RenderGraph(GpuDevice, RenderGraphOptions?)`](#m-ctor-gpudevice-rendergraphoptions) | Creates a graph that records on `device`. |

### <a id="m-ctor-gpudevice-rendergraphoptions"></a>RenderGraph(GpuDevice, RenderGraphOptions?)

```csharp
public RenderGraph(GpuDevice device, RenderGraphOptions? options = null);
```

Creates a graph that records on `device`.

**Parameters**

- `device` ([`GpuDevice`](../OneGame.Gpu/GpuDevice.md))
- `options` ([`RenderGraphOptions`](RenderGraphOptions.md)?)

## Properties

| Member | Description |
| --- | --- |
| [`Device`](#m-device) | The device the graph records on. |
| [`Frame`](#m-frame) | Number of the current (or last) frame, counting from 1. |
| [`Stats`](#m-stats) | Counts from the last executed frame. |
| [`TimingEnabled`](#m-timingenabled) | Whether per-pass GPU time is being measured. |

### <a id="m-device"></a>Device

```csharp
public GpuDevice Device { get; }
```

The device the graph records on.

**Type:** [`GpuDevice`](../OneGame.Gpu/GpuDevice.md)

### <a id="m-frame"></a>Frame

```csharp
public int Frame { get; }
```

Number of the current (or last) frame, counting from 1.

**Type:** `int`

### <a id="m-stats"></a>Stats

```csharp
public GraphStats Stats { get; }
```

Counts from the last executed frame.

**Type:** [`GraphStats`](GraphStats.md)

### <a id="m-timingenabled"></a>TimingEnabled

```csharp
public bool TimingEnabled { get; }
```

Whether per-pass GPU time is being measured.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`AddPass(IGraphPass)`](#m-addpass-igraphpass) | Adds a pass: calls its [`IGraphPass.Setup()`](IGraphPass.md#m-setup-passbuilder) now to learn what it touches, and its [`IGraphPass.Execute()`](IGraphPass.md#m-execute-passcontext) later if it survives culling. |
| [`AddPass(string, Action<PassBuilder>, Action<PassContext>)`](#m-addpass-string-action-passbuilder-action-passcontext) | Adds a pass from two delegates. |
| [`BeginFrame()`](#m-beginframe) | Starts a frame. |
| [`Cancel()`](#m-cancel) | Drops the frame being built without recording anything. |
| [`CreateBuffer(string, ulong)`](#m-createbuffer-string-ulong) | Declares a buffer the graph allocates for this frame. |
| [`CreateTexture(string, in TextureDesc)`](#m-createtexture-string-texturedesc) | Declares a texture the graph allocates for this frame. |
| [`Describe()`](#m-describe) | Describes the last frame: passes (in order, with what they read and write, whether they were culled, GPU time), resources (lifetime, which physical memory they share) and totals. |
| [`Dispose()`](#m-dispose) | Releases the pooled textures and buffers and the timestamp queries. |
| [`Execute()`](#m-execute) | Checks, culls and allocates, then records every surviving pass into one encoder and submits it. |
| [`ImportBuffer(string, GpuBuffer)`](#m-importbuffer-string-gpubuffer) | Brings a buffer the graph does not own into this frame. |
| [`ImportTexture(string, GpuTexture)`](#m-importtexture-string-gputexture) | Brings a texture the graph does not own into this frame: a swapchain image, a capture target, a persistent shadow atlas. |
| [`ResolveTimings()`](#m-resolvetimings) | Collects the GPU times of frames whose work has finished. |

### <a id="m-addpass-igraphpass"></a>AddPass(IGraphPass)

```csharp
public void AddPass(IGraphPass pass);
```

Adds a pass: calls its [`IGraphPass.Setup()`](IGraphPass.md#m-setup-passbuilder) now to learn what it touches, and its [`IGraphPass.Execute()`](IGraphPass.md#m-execute-passcontext) later if it survives culling.

**Parameters**

- `pass` ([`IGraphPass`](IGraphPass.md))

### <a id="m-addpass-string-action-passbuilder-action-passcontext"></a>AddPass(string, Action&lt;PassBuilder&gt;, Action&lt;PassContext&gt;)

```csharp
public void AddPass(string name, Action<PassBuilder> setup, Action<PassContext> execute);
```

Adds a pass from two delegates. Cache them (static lambdas) to avoid a per-frame allocation.

**Parameters**

- `name` (`string`)
- `setup` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`PassBuilder`](PassBuilder.md)&gt;)
- `execute` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`PassContext`](PassContext.md)&gt;)

### <a id="m-beginframe"></a>BeginFrame()

```csharp
public void BeginFrame();
```

Starts a frame. Handles from earlier frames stop being valid.

### <a id="m-cancel"></a>Cancel()

```csharp
public void Cancel();
```

Drops the frame being built without recording anything.

### <a id="m-createbuffer-string-ulong"></a>CreateBuffer(string, ulong)

```csharp
public GraphBuffer CreateBuffer(string name, ulong size);
```

Declares a buffer the graph allocates for this frame. See [`RenderGraph.CreateTexture()`](RenderGraph.md#m-createtexture-string-texturedesc).

**Parameters**

- `name` (`string`)
- `size` (`ulong`)

**Returns:** [`GraphBuffer`](GraphBuffer.md)

### <a id="m-createtexture-string-texturedesc"></a>CreateTexture(string, in TextureDesc)

```csharp
public GraphTexture CreateTexture(string name, in TextureDesc desc);
```

Declares a texture the graph allocates for this frame. It may share memory with other transients whose use does not overlap. Its contents start undefined: a pass must write it before any pass reads it.

**Parameters**

- `name` (`string`)
- `desc` ([`TextureDesc`](../OneGame.Gpu/TextureDesc.md))

**Returns:** [`GraphTexture`](GraphTexture.md)

### <a id="m-describe"></a>Describe()

```csharp
public RenderGraphReport Describe();
```

Describes the last frame: passes (in order, with what they read and write, whether they were culled, GPU time), resources (lifetime, which physical memory they share) and totals. Allocates; meant for tools, not for the frame loop.

**Returns:** [`RenderGraphReport`](RenderGraphReport.md)

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Releases the pooled textures and buffers and the timestamp queries.

### <a id="m-execute"></a>Execute()

```csharp
public void Execute();
```

Checks, culls and allocates, then records every surviving pass into one encoder and submits it.

**Exceptions**

- [`RenderGraphException`](RenderGraphException.md): A pass reads something no earlier pass wrote, or uses a resource twice.

### <a id="m-importbuffer-string-gpubuffer"></a>ImportBuffer(string, GpuBuffer)

```csharp
public GraphBuffer ImportBuffer(string name, GpuBuffer buffer);
```

Brings a buffer the graph does not own into this frame.

**Parameters**

- `name` (`string`)
- `buffer` ([`GpuBuffer`](../OneGame.Gpu/GpuBuffer.md))

**Returns:** [`GraphBuffer`](GraphBuffer.md)

### <a id="m-importtexture-string-gputexture"></a>ImportTexture(string, GpuTexture)

```csharp
public GraphTexture ImportTexture(string name, GpuTexture texture);
```

Brings a texture the graph does not own into this frame: a swapchain image, a capture target, a persistent shadow atlas. Its contents are defined; the graph never frees it.

**Parameters**

- `name` (`string`)
- `texture` ([`GpuTexture`](../OneGame.Gpu/GpuTexture.md))

**Returns:** [`GraphTexture`](GraphTexture.md)

### <a id="m-resolvetimings"></a>ResolveTimings()

```csharp
public void ResolveTimings();
```

Collects the GPU times of frames whose work has finished. Called by [`RenderGraph.Execute()`](RenderGraph.md#m-execute) and by reports; cheap when nothing is pending.
