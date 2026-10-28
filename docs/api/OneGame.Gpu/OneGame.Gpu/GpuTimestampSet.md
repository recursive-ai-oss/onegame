[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuTimestampSet class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Inherits:** [`GpuObject`](GpuObject.md)

```csharp
public abstract class GpuTimestampSet : GpuObject
```

A set of GPU timestamp queries. Passes write their start and end times into it; read them back once the work has finished.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuTimestampSet()`](#m-ctor) |  |

### <a id="m-ctor"></a>GpuTimestampSet()

```csharp
protected GpuTimestampSet();
```

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | Number of queries. |

### <a id="m-count"></a>Count

```csharp
public uint Count { get; protected init; }
```

Number of queries.

**Type:** `uint`
