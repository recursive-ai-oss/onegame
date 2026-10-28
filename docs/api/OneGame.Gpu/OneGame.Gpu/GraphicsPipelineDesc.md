[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GraphicsPipelineDesc class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public sealed class GraphicsPipelineDesc
```

Describes a graphics pipeline.

## Constructors

| Member | Description |
| --- | --- |
| [`GraphicsPipelineDesc()`](#m-ctor) |  |

### <a id="m-ctor"></a>GraphicsPipelineDesc()

```csharp
public GraphicsPipelineDesc();
```

> **Obsolete.** Constructors of types with required members are not supported in this version of your compiler.

## Properties

| Member | Description |
| --- | --- |
| [`ColorTargets`](#m-colortargets) | Color targets, matching the pass's color attachments. |
| [`Cull`](#m-cull) | Face culling. |
| [`DepthClamp`](#m-depthclamp) | Clamps fragment depth to the viewport range instead of clipping the primitive, so shadow casters behind the light's near plane still write depth. |
| [`DepthStencil`](#m-depthstencil) | Depth testing, or null for none. |
| [`Front`](#m-front) | Front-face winding. |
| [`Label`](#m-label) | Optional name shown in debugging tools. |
| [`Layout`](#m-layout) | Resource layout shared with bind groups. |
| [`Pixel`](#m-pixel) | Pixel stage; null for depth-only pipelines. |
| [`SampleCount`](#m-samplecount) | Samples per pixel; must match the pass's attachments. |
| [`Topology`](#m-topology) | Primitive assembly. |
| [`Vertex`](#m-vertex) | Vertex stage. |
| [`VertexBuffers`](#m-vertexbuffers) | Vertex buffers, in binding order. |

### <a id="m-colortargets"></a>ColorTargets

```csharp
public IReadOnlyList<ColorTargetState> ColorTargets { get; init; }
```

Color targets, matching the pass's color attachments.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ColorTargetState`](ColorTargetState.md)&gt;

### <a id="m-cull"></a>Cull

```csharp
public CullMode Cull { get; init; }
```

Face culling.

**Type:** [`CullMode`](CullMode.md)

### <a id="m-depthclamp"></a>DepthClamp

```csharp
public bool DepthClamp { get; init; }
```

Clamps fragment depth to the viewport range instead of clipping the primitive, so shadow casters behind the light's near plane still write depth. Needs [`GpuCapabilities.DepthClamp`](GpuCapabilities.md#m-depthclamp) (a feature WebGPU only offers optionally).

**Type:** `bool`

### <a id="m-depthstencil"></a>DepthStencil

```csharp
public DepthStencilState? DepthStencil { get; init; }
```

Depth testing, or null for none.

**Type:** [`DepthStencilState`](DepthStencilState.md)?

### <a id="m-front"></a>Front

```csharp
public FrontFace Front { get; init; }
```

Front-face winding.

**Type:** [`FrontFace`](FrontFace.md)

### <a id="m-label"></a>Label

```csharp
public string? Label { get; init; }
```

Optional name shown in debugging tools.

**Type:** `string?`

### <a id="m-layout"></a>Layout

```csharp
public GpuPipelineLayout Layout { get; init; }
```

Resource layout shared with bind groups.

**Type:** [`GpuPipelineLayout`](GpuPipelineLayout.md)

### <a id="m-pixel"></a>Pixel

```csharp
public GpuShader? Pixel { get; init; }
```

Pixel stage; null for depth-only pipelines.

**Type:** [`GpuShader`](GpuShader.md)?

### <a id="m-samplecount"></a>SampleCount

```csharp
public uint SampleCount { get; init; }
```

Samples per pixel; must match the pass's attachments.

**Type:** `uint`

### <a id="m-topology"></a>Topology

```csharp
public PrimitiveTopology Topology { get; init; }
```

Primitive assembly.

**Type:** [`PrimitiveTopology`](PrimitiveTopology.md)

### <a id="m-vertex"></a>Vertex

```csharp
public GpuShader Vertex { get; init; }
```

Vertex stage.

**Type:** [`GpuShader`](GpuShader.md)

### <a id="m-vertexbuffers"></a>VertexBuffers

```csharp
public IReadOnlyList<VertexBufferLayout> VertexBuffers { get; init; }
```

Vertex buffers, in binding order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`VertexBufferLayout`](VertexBufferLayout.md)&gt;
