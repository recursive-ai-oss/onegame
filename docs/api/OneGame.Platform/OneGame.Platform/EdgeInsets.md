[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# EdgeInsets struct

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`EdgeInsets`](EdgeInsets.md)&gt;

```csharp
public readonly struct EdgeInsets : IEquatable<EdgeInsets>
```

Distances in pixels from each edge of a window.

## Constructors

| Member | Description |
| --- | --- |
| [`EdgeInsets(int, int, int, int)`](#m-ctor-int-int-int-int) | Distances in pixels from each edge of a window. |

### <a id="m-ctor-int-int-int-int"></a>EdgeInsets(int, int, int, int)

```csharp
public EdgeInsets(int Left, int Top, int Right, int Bottom);
```

Distances in pixels from each edge of a window.

**Parameters**

- `Left` (`int`): From the left edge.
- `Top` (`int`): From the top edge.
- `Right` (`int`): From the right edge.
- `Bottom` (`int`): From the bottom edge.

## Properties

| Member | Description |
| --- | --- |
| [`Bottom`](#m-bottom) | From the bottom edge. |
| [`Left`](#m-left) | From the left edge. |
| [`Right`](#m-right) | From the right edge. |
| [`Top`](#m-top) | From the top edge. |

### <a id="m-bottom"></a>Bottom

```csharp
public int Bottom { get; init; }
```

From the bottom edge.

**Type:** `int`

### <a id="m-left"></a>Left

```csharp
public int Left { get; init; }
```

From the left edge.

**Type:** `int`

### <a id="m-right"></a>Right

```csharp
public int Right { get; init; }
```

From the right edge.

**Type:** `int`

### <a id="m-top"></a>Top

```csharp
public int Top { get; init; }
```

From the top edge.

**Type:** `int`
