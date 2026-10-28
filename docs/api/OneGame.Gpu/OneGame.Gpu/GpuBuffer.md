[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# GpuBuffer class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Inherits:** [`GpuObject`](GpuObject.md)

```csharp
public abstract class GpuBuffer : GpuObject
```

A block of GPU memory.

## Constructors

| Member | Description |
| --- | --- |
| [`GpuBuffer()`](#m-ctor) |  |

### <a id="m-ctor"></a>GpuBuffer()

```csharp
protected GpuBuffer();
```

## Properties

| Member | Description |
| --- | --- |
| [`Size`](#m-size) | Size in bytes. |
| [`Usage`](#m-usage) | Allowed uses. |

### <a id="m-size"></a>Size

```csharp
public ulong Size { get; protected init; }
```

Size in bytes.

**Type:** `ulong`

### <a id="m-usage"></a>Usage

```csharp
public BufferUsage Usage { get; protected init; }
```

Allowed uses.

**Type:** [`BufferUsage`](BufferUsage.md)
