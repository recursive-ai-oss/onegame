[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# ComputePipelineDesc class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public sealed class ComputePipelineDesc
```

Describes a compute pipeline.

## Constructors

| Member | Description |
| --- | --- |
| [`ComputePipelineDesc()`](#m-ctor) |  |

### <a id="m-ctor"></a>ComputePipelineDesc()

```csharp
public ComputePipelineDesc();
```

> **Obsolete.** Constructors of types with required members are not supported in this version of your compiler.

## Properties

| Member | Description |
| --- | --- |
| [`Compute`](#m-compute) | Compute shader. |
| [`Label`](#m-label) | Optional name shown in debugging tools. |
| [`Layout`](#m-layout) | Resource layout shared with bind groups. |

### <a id="m-compute"></a>Compute

```csharp
public GpuShader Compute { get; init; }
```

Compute shader.

**Type:** [`GpuShader`](GpuShader.md)

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
