[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# GraphBuffer struct

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GraphBuffer`](GraphBuffer.md)&gt;

```csharp
public readonly struct GraphBuffer : IEquatable<GraphBuffer>
```

A buffer inside one frame of a [`RenderGraph`](RenderGraph.md). Valid only for the frame that made it.

## Properties

| Member | Description |
| --- | --- |
| [`IsValid`](#m-isvalid) | False for `default`. |

### <a id="m-isvalid"></a>IsValid

```csharp
public bool IsValid { get; }
```

False for `default`.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Equals(GraphBuffer)`](#m-equals-graphbuffer) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |

### <a id="m-equals-graphbuffer"></a>Equals(GraphBuffer)

```csharp
public bool Equals(GraphBuffer other);
```

**Parameters**

- `other` ([`GraphBuffer`](GraphBuffer.md))

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

**Returns:** `int`

## Operators

| Member | Description |
| --- | --- |
| [`operator !=(GraphBuffer, GraphBuffer)`](#m-operator-graphbuffer-graphbuffer) | Inequality. |
| [`operator ==(GraphBuffer, GraphBuffer)`](#m-operator-graphbuffer-graphbuffer-2) | Equality. |

### <a id="m-operator-graphbuffer-graphbuffer"></a>operator !=(GraphBuffer, GraphBuffer)

```csharp
public static bool operator !=(GraphBuffer left, GraphBuffer right);
```

Inequality.

**Parameters**

- `left` ([`GraphBuffer`](GraphBuffer.md))
- `right` ([`GraphBuffer`](GraphBuffer.md))

**Returns:** `bool`

### <a id="m-operator-graphbuffer-graphbuffer-2"></a>operator ==(GraphBuffer, GraphBuffer)

```csharp
public static bool operator ==(GraphBuffer left, GraphBuffer right);
```

Equality.

**Parameters**

- `left` ([`GraphBuffer`](GraphBuffer.md))
- `right` ([`GraphBuffer`](GraphBuffer.md))

**Returns:** `bool`
