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

- `Size` (`ulong`): Size in bytes.
- `Usage` ([`BufferUsage`](BufferUsage.md)): Allowed uses.

## Properties

| Member | Description |
| --- | --- |
| [`Label`](#m-label) | Optional name shown in debugging tools. |
| [`Size`](#m-size) | Size in bytes. |
| [`Usage`](#m-usage) | Allowed uses. |

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

Size in bytes.

**Type:** `ulong`

### <a id="m-usage"></a>Usage

```csharp
public BufferUsage Usage { get; init; }
```

Allowed uses.

**Type:** [`BufferUsage`](BufferUsage.md)
