[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# RenderGraphOptions class

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public sealed class RenderGraphOptions
```

Tuning for a [`RenderGraph`](RenderGraph.md).

## Constructors

| Member | Description |
| --- | --- |
| [`RenderGraphOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>RenderGraphOptions()

```csharp
public RenderGraphOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`KeepUnusedFrames`](#m-keepunusedframes) | How many frames a pooled texture or buffer may sit unused before it is released. |
| [`Timing`](#m-timing) | Measure GPU time per pass when the device can ([`GpuCapabilities.Timestamps`](../OneGame.Gpu/GpuCapabilities.md#m-timestamps)). |

### <a id="m-keepunusedframes"></a>KeepUnusedFrames

```csharp
public int KeepUnusedFrames { get; init; }
```

How many frames a pooled texture or buffer may sit unused before it is released. Default 8.

**Type:** `int`

### <a id="m-timing"></a>Timing

```csharp
public bool Timing { get; init; }
```

Measure GPU time per pass when the device can ([`GpuCapabilities.Timestamps`](../OneGame.Gpu/GpuCapabilities.md#m-timestamps)). On by default.

**Type:** `bool`
