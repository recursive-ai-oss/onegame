[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu

# VertexElement struct

**Namespace:** `OneGame.Gpu`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`VertexElement`](VertexElement.md)&gt;

```csharp
public readonly struct VertexElement : IEquatable<VertexElement>
```

Describes one vertex attribute: where a shader input comes from inside a vertex.

## Constructors

| Member | Description |
| --- | --- |
| [`VertexElement(uint, VertexFormat, uint)`](#m-ctor-uint-vertexformat-uint) | Describes one vertex attribute: where a shader input comes from inside a vertex. |

### <a id="m-ctor-uint-vertexformat-uint"></a>VertexElement(uint, VertexFormat, uint)

```csharp
public VertexElement(uint Location, VertexFormat Format, uint Offset);
```

Describes one vertex attribute: where a shader input comes from inside a vertex.

**Parameters**

- `Location` (`uint`): Shader input location.
- `Format` ([`VertexFormat`](VertexFormat.md)): Memory format.
- `Offset` (`uint`): Byte offset inside the vertex.

## Properties

| Member | Description |
| --- | --- |
| [`Format`](#m-format) | Memory format. |
| [`Location`](#m-location) | Shader input location. |
| [`Offset`](#m-offset) | Byte offset inside the vertex. |

### <a id="m-format"></a>Format

```csharp
public VertexFormat Format { get; init; }
```

Memory format.

**Type:** [`VertexFormat`](VertexFormat.md)

### <a id="m-location"></a>Location

```csharp
public uint Location { get; init; }
```

Shader input location.

**Type:** `uint`

### <a id="m-offset"></a>Offset

```csharp
public uint Offset { get; init; }
```

Byte offset inside the vertex.

**Type:** `uint`
