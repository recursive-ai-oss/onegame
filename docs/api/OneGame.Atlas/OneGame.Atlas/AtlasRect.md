[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# AtlasRect struct

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AtlasRect`](AtlasRect.md)&gt;

```csharp
public readonly struct AtlasRect : IEquatable<AtlasRect>
```

An integer rectangle in page pixels, origin top-left.

## Constructors

| Member | Description |
| --- | --- |
| [`AtlasRect(int, int, int, int)`](#m-ctor-int-int-int-int) | An integer rectangle in page pixels, origin top-left. |

### <a id="m-ctor-int-int-int-int"></a>AtlasRect(int, int, int, int)

```csharp
public AtlasRect(int X, int Y, int Width, int Height);
```

An integer rectangle in page pixels, origin top-left.

**Parameters**

- `X` (`int`): Left edge.
- `Y` (`int`): Top edge.
- `Width` (`int`): Width in pixels.
- `Height` (`int`): Height in pixels.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height in pixels. |
| [`IsEmpty`](#m-isempty) | Gets whether the rectangle covers no pixels. |
| [`Width`](#m-width) | Width in pixels. |
| [`X`](#m-x) | Left edge. |
| [`Y`](#m-y) | Top edge. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Height in pixels.

**Type:** `int`

### <a id="m-isempty"></a>IsEmpty

```csharp
public bool IsEmpty { get; }
```

Gets whether the rectangle covers no pixels.

**Type:** `bool`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Width in pixels.

**Type:** `int`

### <a id="m-x"></a>X

```csharp
public int X { get; init; }
```

Left edge.

**Type:** `int`

### <a id="m-y"></a>Y

```csharp
public int Y { get; init; }
```

Top edge.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Union(AtlasRect)`](#m-union-atlasrect) | Gets the smallest rectangle holding both rectangles; an empty one is ignored. |

### <a id="m-union-atlasrect"></a>Union(AtlasRect)

```csharp
public AtlasRect Union(AtlasRect other);
```

Gets the smallest rectangle holding both rectangles; an empty one is ignored.

**Parameters**

- `other` ([`AtlasRect`](AtlasRect.md)): The other rectangle.

**Returns:** [`AtlasRect`](AtlasRect.md) — The union.
