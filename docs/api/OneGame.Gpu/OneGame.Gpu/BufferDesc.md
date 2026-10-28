[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# BufferDesc struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`BufferDesc`](BufferDesc.md)&gt;

```csharp
public readonly struct BufferDesc : IEquatable<BufferDesc>
```

Describes a buffer to create.

## Constructors

| Member | Description |
| --- | --- |
| [`BufferDesc(ulong, BufferUsage)`](#m-ctor-ulong-bufferusage) | Describes a buffer to create. |

### <a id="m-ctor-ulong-bufferusage"></a>BufferDesc(ulong, BufferUsage)

```csharp
public BufferDesc(ulong Size, BufferUsage Usage);
```

Describes a buffer to create.

**Parameters**

- `Size` (`ulong`): Size in bytes. Uniform and storage buffers bound with a dynamic offset need their slices aligned to [`GpuDevice.DynamicOffsetAlignment`](GpuDevice.md#m-dynamicoffsetalignment).
- `Usage` ([`BufferUsage`](BufferUsage.md)): Every use the buffer will have, combined: a vertex buffer you upload to is `Vertex | CopyDestination`. A use not listed here is a validation error.

## Properties

| Member | Description |
| --- | --- |
| [`Label`](#m-label) | Optional name shown in debugging tools. |
| [`Size`](#m-size) | Size in bytes. |
| [`Usage`](#m-usage) | Every use the buffer will have, combined: a vertex buffer you upload to is `Vertex \| CopyDestination`. |

### <a id="m-label"></a>Label

```csharp
public string? Label { get; init; }
```

Optional name shown in debugging tools.

**Type:** `string?`

### <a id="m-size"></a>Size

```csharp
public ulong Size { get; init; }
```

Size in bytes. Uniform and storage buffers bound with a dynamic offset need their slices aligned to [`GpuDevice.DynamicOffsetAlignment`](GpuDevice.md#m-dynamicoffsetalignment).

**Type:** `ulong`

### <a id="m-usage"></a>Usage

```csharp
public BufferUsage Usage { get; init; }
```

Every use the buffer will have, combined: a vertex buffer you upload to is `Vertex | CopyDestination`. A use not listed here is a validation error.

**Type:** [`BufferUsage`](BufferUsage.md)
