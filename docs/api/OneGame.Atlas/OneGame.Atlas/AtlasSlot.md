[API reference](../../index.md) › [OneGame.Atlas](../index.md) › OneGame.Atlas

# AtlasSlot struct

**Namespace:** `OneGame.Atlas`  
**Package:** [`RecursiveAI.OneGame.Atlas`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AtlasSlot`](AtlasSlot.md)&gt;

```csharp
public readonly struct AtlasSlot : IEquatable<AtlasSlot>
```

A rectangle reserved on a page.

## Constructors

| Member | Description |
| --- | --- |
| [`AtlasSlot(int, int, int)`](#m-ctor-int-int-int) | A rectangle reserved on a page. |

### <a id="m-ctor-int-int-int"></a>AtlasSlot(int, int, int)

```csharp
public AtlasSlot(int Page, int X, int Y);
```

A rectangle reserved on a page.

**Parameters**

- `Page` (`int`): Page index.
- `X` (`int`): Left edge of the usable rectangle, inside the padding.
- `Y` (`int`): Top edge of the usable rectangle.

## Properties

| Member | Description |
| --- | --- |
| [`Page`](#m-page) | Page index. |
| [`X`](#m-x) | Left edge of the usable rectangle, inside the padding. |
| [`Y`](#m-y) | Top edge of the usable rectangle. |

### <a id="m-page"></a>Page

```csharp
public int Page { get; init; }
```

Page index.

**Type:** `int`

### <a id="m-x"></a>X

```csharp
public int X { get; init; }
```

Left edge of the usable rectangle, inside the padding.

**Type:** `int`

### <a id="m-y"></a>Y

```csharp
public int Y { get; init; }
```

Top edge of the usable rectangle.

**Type:** `int`
