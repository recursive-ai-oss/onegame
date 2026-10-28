[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# ShaderLayouts static class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public static class ShaderLayouts
```

Turns the layouts the shader build generates (`Shader.Entry.Layout`) into this layer's own binding, push-constant and vertex types, so a pipeline layout is declared once, in the HLSL, and cannot disagree with the shaders that use it.

Several entry points that share one pipeline layout (a vertex shader and some pixel variants, or a family of compute passes) are combined with `PipelineLayout.TryCreateUnion`: a resource at the same set and binding must agree on kind and count, and its stages are the union of the entry points' stages.

## Methods

| Member | Description |
| --- | --- |
| [`Bindings(ReadOnlySpan<EntryLayout>, ReadOnlySpan<(uint Set, uint Binding)>, ReadOnlySpan<(uint Set, ShaderStage Stages)>, UnionOptions?)`](#m-bindings-readonlyspan-entrylayout-readonlyspan-uint-set-uint-binding-readonlyspan-uint-set-shaderstage-stages-unionoptions) | The resource bindings that cover every entry point in `entries`, ordered by set then binding. |
| [`PushConstants(ReadOnlySpan<EntryLayout>)`](#m-pushconstants-readonlyspan-entrylayout) | The push-constant range that covers the blocks of `entries`, or an empty range when none has one. |
| [`Vertex(EntryLayout, VertexStepMode, VertexLayoutOverrides?)`](#m-vertex-entrylayout-vertexstepmode-vertexlayoutoverrides) | The vertex buffer layout of a vertex entry point. |
| [`VertexStreams(EntryLayout, VertexStream[])`](#m-vertexstreams-entrylayout-vertexstream) | The vertex buffer layouts of a vertex entry point that reads several buffers, for example a per-vertex mesh and a per-instance stream. |

### <a id="m-bindings-readonlyspan-entrylayout-readonlyspan-uint-set-uint-binding-readonlyspan-uint-set-shaderstage-stages-unionoptions"></a>Bindings(ReadOnlySpan&lt;EntryLayout&gt;, ReadOnlySpan&lt;(uint Set, uint Binding)&gt;, ReadOnlySpan&lt;(uint Set, ShaderStage Stages)&gt;, UnionOptions?)

```csharp
public static ResourceBinding[] Bindings(ReadOnlySpan<EntryLayout> entries, ReadOnlySpan<(uint Set, uint Binding)> dynamic = default, ReadOnlySpan<(uint Set, ShaderStage Stages)> widenSets = default, UnionOptions? options = null);
```

The resource bindings that cover every entry point in `entries`, ordered by set then binding.

**Parameters**

- `entries` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<EntryLayout>`): The entry points that run with the pipeline layout.
- `dynamic` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<(uint Set, uint Binding)>`): (set, binding) pairs of buffers to declare with a dynamic offset.
- `widenSets` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<(uint Set,` [`ShaderStage`](ShaderStage.md) `Stages)>`): Stages to add to every binding of a set, so a bind group made once fits pipelines whose shaders use it from different stages.
- `options` (`UnionOptions?`): Leniency for slots one descriptor covers: a texture read as 2D by one entry point and as a cube by another, or a buffer read by one and written by another.

**Returns:** [`ResourceBinding`](ResourceBinding.md)\[\] — Bindings for [`GpuDevice.CreatePipelineLayout()`](GpuDevice.md#m-createpipelinelayout-readonlyspan-resourcebinding).

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): Two entry points declare different resources at the same slot, or one uses a resource kind this layer has no binding for.

### <a id="m-pushconstants-readonlyspan-entrylayout"></a>PushConstants(ReadOnlySpan&lt;EntryLayout&gt;)

```csharp
public static PushConstantRange PushConstants(ReadOnlySpan<EntryLayout> entries);
```

The push-constant range that covers the blocks of `entries`, or an empty range when none has one.

**Parameters**

- `entries` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<EntryLayout>`): The entry points that run with the pipeline layout.

**Returns:** [`PushConstantRange`](PushConstantRange.md) — The largest block, visible to every stage that declares one.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): Two entry points declare incompatible push-constant blocks.

### <a id="m-vertex-entrylayout-vertexstepmode-vertexlayoutoverrides"></a>Vertex(EntryLayout, VertexStepMode, VertexLayoutOverrides?)

```csharp
public static VertexBufferLayout Vertex(EntryLayout entry, VertexStepMode step = 0, VertexLayoutOverrides? overrides = null);
```

The vertex buffer layout of a vertex entry point.

**Parameters**

- `entry` (`EntryLayout`): The vertex shader's layout.
- `step` ([`VertexStepMode`](VertexStepMode.md)): Whether the buffer advances per vertex or per instance.
- `overrides` (`VertexLayoutOverrides?`): What the engine's own vertex stores differently from the shader's declared inputs (a packed colour, a narrower integer, a smaller stride).

**Returns:** [`VertexBufferLayout`](VertexBufferLayout.md) — The layout, with the engine's vertex formats.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The entry point has no vertex input, the overrides don't fit it, or an input uses a format this layer has no vertex format for.

### <a id="m-vertexstreams-entrylayout-vertexstream"></a>VertexStreams(EntryLayout, VertexStream\[\])

```csharp
public static VertexBufferLayout[] VertexStreams(EntryLayout entry, params VertexStream[] streams);
```

The vertex buffer layouts of a vertex entry point that reads several buffers, for example a per-vertex mesh and a per-instance stream.

**Parameters**

- `entry` (`EntryLayout`): The vertex shader's layout.
- `streams` (`VertexStream[]`): The buffers: each takes the shader inputs from its first location up to the next stream's.

**Returns:** [`VertexBufferLayout`](VertexBufferLayout.md)\[\] — One layout per stream, in order, with offsets relative to the stream.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The entry point has no vertex input, the streams don't fit it, or an input uses a format this layer has no vertex format for.
