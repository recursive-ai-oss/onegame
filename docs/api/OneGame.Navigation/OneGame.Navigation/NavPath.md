[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavPath class

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)

```csharp
public sealed class NavPath
```

The result of a one-shot search: a status and, when there is one, the cells from start to goal.

## Properties

| Member | Description |
| --- | --- |
| [`Cells`](#m-cells) | The cells from the start to the goal, both included. |
| [`Cost`](#m-cost) | The cost of the path in open-ground steps (a straight step is 1, a diagonal one about 1.41, scaled by area costs), rounded to the nearest [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) and capped at 2^21. |
| [`Count`](#m-count) | Number of cells. |
| [`Found`](#m-found) | True when [`NavPath.Cells`](NavPath.md#m-cells) holds a path. |
| [`Status`](#m-status) | Found, NoPath, StartInvalid or GoalInvalid. |

### <a id="m-cells"></a>Cells

```csharp
public ReadOnlySpan<NavCell> Cells { get; }
```

The cells from the start to the goal, both included. Empty when there is no path.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`NavCell`](NavCell.md)&gt;

### <a id="m-cost"></a>Cost

```csharp
public Fixed Cost { get; }
```

The cost of the path in open-ground steps (a straight step is 1, a diagonal one about 1.41, scaled by area costs), rounded to the nearest [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md) and capped at 2^21. Zero when there is no path.

**Type:** [`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

Number of cells.

**Type:** `int`

### <a id="m-found"></a>Found

```csharp
public bool Found { get; }
```

True when [`NavPath.Cells`](NavPath.md#m-cells) holds a path.

**Type:** `bool`

### <a id="m-status"></a>Status

```csharp
public NavStatus Status { get; }
```

Found, NoPath, StartInvalid or GoalInvalid.

**Type:** [`NavStatus`](NavStatus.md)

## Methods

| Member | Description |
| --- | --- |
| [`CopyPoints(NavGrid, Span<FVec2>)`](#m-copypoints-navgrid-span-fvec2) | Writes the world positions of the path's cell centres into `destination`. |

### <a id="m-copypoints-navgrid-span-fvec2"></a>CopyPoints(NavGrid, Span&lt;FVec2&gt;)

```csharp
public int CopyPoints(NavGrid grid, Span<FVec2> destination);
```

Writes the world positions of the path's cell centres into `destination`.

**Parameters**

- `grid` ([`NavGrid`](NavGrid.md)): The grid the path was found on.
- `destination` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)&lt;[`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)&gt;): At least [`NavPath.Count`](NavPath.md#m-count) elements.

**Returns:** `int` — The number written.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The destination is too short.
