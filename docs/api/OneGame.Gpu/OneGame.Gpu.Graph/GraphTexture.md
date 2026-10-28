[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# GraphTexture struct

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`GraphTexture`](GraphTexture.md)&gt;

```csharp
public readonly struct GraphTexture : IEquatable<GraphTexture>
```

A texture inside one frame of a [`RenderGraph`](RenderGraph.md): either created by the graph (transient) or imported. Valid only for the frame that made it.

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
| [`Equals(GraphTexture)`](#m-equals-graphtexture) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |

### <a id="m-equals-graphtexture"></a>Equals(GraphTexture)

```csharp
public bool Equals(GraphTexture other);
```

**Parameters**

- `other` ([`GraphTexture`](GraphTexture.md))

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
| [`operator !=(GraphTexture, GraphTexture)`](#m-operator-graphtexture-graphtexture) | Inequality. |
| [`operator ==(GraphTexture, GraphTexture)`](#m-operator-graphtexture-graphtexture-2) | Equality. |

### <a id="m-operator-graphtexture-graphtexture"></a>operator !=(GraphTexture, GraphTexture)

```csharp
public static bool operator !=(GraphTexture left, GraphTexture right);
```

Inequality.

**Parameters**

- `left` ([`GraphTexture`](GraphTexture.md))
- `right` ([`GraphTexture`](GraphTexture.md))

**Returns:** `bool`

### <a id="m-operator-graphtexture-graphtexture-2"></a>operator ==(GraphTexture, GraphTexture)

```csharp
public static bool operator ==(GraphTexture left, GraphTexture right);
```

Equality.

**Parameters**

- `left` ([`GraphTexture`](GraphTexture.md))
- `right` ([`GraphTexture`](GraphTexture.md))

**Returns:** `bool`
