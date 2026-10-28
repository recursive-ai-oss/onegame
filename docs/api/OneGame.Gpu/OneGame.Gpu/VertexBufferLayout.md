[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# VertexBufferLayout class

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public sealed class VertexBufferLayout
```

Layout of one vertex buffer binding.

## Constructors

| Member | Description |
| --- | --- |
| [`VertexBufferLayout()`](#m-ctor) |  |

### <a id="m-ctor"></a>VertexBufferLayout()

```csharp
public VertexBufferLayout();
```

> **Obsolete.** Constructors of types with required members are not supported in this version of your compiler.

## Properties

| Member | Description |
| --- | --- |
| [`Attributes`](#m-attributes) | Attributes read from this buffer. |
| [`StepMode`](#m-stepmode) | Whether the buffer advances per vertex or per instance. |
| [`Stride`](#m-stride) | Bytes from one element to the next. |

### <a id="m-attributes"></a>Attributes

```csharp
public ReadOnlyMemory<VertexElement> Attributes { get; init; }
```

Attributes read from this buffer.

**Type:** [`ReadOnlyMemory`](https://learn.microsoft.com/dotnet/api/system.readonlymemory-1)&lt;[`VertexElement`](VertexElement.md)&gt;

### <a id="m-stepmode"></a>StepMode

```csharp
public VertexStepMode StepMode { get; init; }
```

Whether the buffer advances per vertex or per instance.

**Type:** [`VertexStepMode`](VertexStepMode.md)

### <a id="m-stride"></a>Stride

```csharp
public uint Stride { get; init; }
```

Bytes from one element to the next.

**Type:** `uint`
