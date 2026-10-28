[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuPipelineLayout class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Inherits:** [`GpuObject`](GpuObject.md)

```csharp
public abstract class GpuPipelineLayout : GpuObject
```

The resource interface shared by pipelines and bind groups.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuPipelineLayout()`](#m-ctor) |  |

### <a id="m-ctor"></a>GpuPipelineLayout()

```csharp
protected GpuPipelineLayout();
```

## Properties

| Member | Description |
| --- | --- |
| [`SetCount`](#m-setcount) | Number of bind group sets (highest set index + 1). |

### <a id="m-setcount"></a>SetCount

```csharp
public int SetCount { get; protected init; }
```

Number of bind group sets (highest set index + 1).

**Type:** `int`
