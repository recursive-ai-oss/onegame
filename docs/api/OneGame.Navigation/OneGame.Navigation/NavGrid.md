[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavGrid class

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)

```csharp
public sealed class NavGrid
```

The walkable map a unit searches: a grid of cells, each open, blocked or an area with its own movement cost, placed in the world by a [`NavSpace`](NavSpace.md). It is level data: build it the same way on every node (from the level, or from synced cells), and edit it from sim code only. Searches read it through [`NavSearcher`](NavSearcher.md) or [`NavGrid.FindPath()`](NavGrid.md#m-findpath-navcell-navcell-navoptions).

## Constructors

| Member | Description |
| --- | --- |
| [`NavGrid(int, int, NavSpace)`](#m-ctor-int-int-navspace) | Creates an all-open grid. |

### <a id="m-ctor-int-int-navspace"></a>NavGrid(int, int, NavSpace)

```csharp
public NavGrid(int width, int height, NavSpace space = default);
```

Creates an all-open grid.

**Parameters**

- `width` (`int`): Cells across.
- `height` (`int`): Cells down.
- `space` ([`NavSpace`](NavSpace.md)): Where the grid sits in the world; unit cells at the origin by default.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): A size is below 1, or the grid has more than [`NavLimits.MaxCells`](NavLimits.md#m-maxcells) cells.
- [`NavRangeException`](NavRangeException.md): The grid reaches beyond ±2^21 units.

## Properties

| Member | Description |
| --- | --- |
| [`Grid`](#m-grid) | The search library's grid, for what this class does not cover. |
| [`Height`](#m-height) | Cells down. |
| [`Space`](#m-space) | Where the grid sits in the world. |
| [`Stamp`](#m-stamp) | A hash of everything a search depends on (size, every cell, every area cost). |
| [`Width`](#m-width) | Cells across. |

### <a id="m-grid"></a>Grid

```csharp
public Grid Grid { get; }
```

The search library's grid, for what this class does not cover. Edits made on it are seen by searches, but not through [`NavSpace`](NavSpace.md) checks.

**Type:** `Grid`

### <a id="m-height"></a>Height

```csharp
public int Height { get; }
```

Cells down.

**Type:** `int`

### <a id="m-space"></a>Space

```csharp
public NavSpace Space { get; }
```

Where the grid sits in the world.

**Type:** [`NavSpace`](NavSpace.md)

### <a id="m-stamp"></a>Stamp

```csharp
public ulong Stamp { get; }
```

A hash of everything a search depends on (size, every cell, every area cost). A [`NavState`](NavState.md) remembers it, so a path or a running search noticed to be out of date after the map changed, including a change that happened before a restore. Recomputed only when the map was edited.

**Type:** `ulong`

### <a id="m-width"></a>Width

```csharp
public int Width { get; }
```

Cells across.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`CellAt(int)`](#m-cellat-int) | The cell at an index. |
| [`CellCenter(NavCell)`](#m-cellcenter-navcell) | The world position of a cell's centre. |
| [`Contains(NavCell)`](#m-contains-navcell) | Whether a cell is inside the grid. |
| [`FindPath(FVec2, FVec2, NavOptions)`](#m-findpath-fvec2-fvec2-navoptions) | Finds a path between two world positions, running the search to the end in this call. |
| [`FindPath(NavCell, NavCell, NavOptions)`](#m-findpath-navcell-navcell-navoptions) | Finds a path between two cells, running the search to the end in this call. |
| [`FromText(string, NavSpace)`](#m-fromtext-string-navspace) | Creates a grid from text: `.` is open, `#` a wall and `1` to `9` are areas; rows must be equally long. |
| [`GetArea(int, int)`](#m-getarea-int-int) | The area of a cell: 0 is open, 1 to 254 are costed areas, 255 is a wall. |
| [`IndexOf(NavCell)`](#m-indexof-navcell) | The cell's index, `y * Width + x`, which is how paths are stored. |
| [`IsBlocked(int, int)`](#m-isblocked-int-int) | True if the cell is a wall. |
| [`SetArea(int, int, byte)`](#m-setarea-int-int-byte) | Sets the area of one cell. |
| [`SetAreaCost(byte, Fixed)`](#m-setareacost-byte-fixed) | Sets what moving through an area costs, relative to an open cell (1). |
| [`SetBlocked(int, int, bool)`](#m-setblocked-int-int-bool) | Opens or blocks one cell. |
| [`SetRect(int, int, int, int, byte)`](#m-setrect-int-int-int-int-byte) | Sets the area of a rectangle of cells. |
| [`TryGetCell(FVec2, out NavCell)`](#m-trygetcell-fvec2-navcell) | The cell holding a world position. |

### <a id="m-cellat-int"></a>CellAt(int)

```csharp
public NavCell CellAt(int index);
```

The cell at an index.

**Parameters**

- `index` (`int`): 0 to `Width * Height - 1`.

**Returns:** [`NavCell`](NavCell.md) — The cell.

### <a id="m-cellcenter-navcell"></a>CellCenter(NavCell)

```csharp
public FVec2 CellCenter(NavCell cell);
```

The world position of a cell's centre. Exact: half a cell is a shift of the raw value, rounded down.

**Parameters**

- `cell` ([`NavCell`](NavCell.md)): A cell inside the grid.

**Returns:** [`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md) — The centre.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The cell is outside the grid.

### <a id="m-contains-navcell"></a>Contains(NavCell)

```csharp
public bool Contains(NavCell cell);
```

Whether a cell is inside the grid.

**Parameters**

- `cell` ([`NavCell`](NavCell.md)): The cell.

**Returns:** `bool` — True if it is.

### <a id="m-findpath-fvec2-fvec2-navoptions"></a>FindPath(FVec2, FVec2, NavOptions)

```csharp
public NavPath FindPath(FVec2 from, FVec2 to, NavOptions options = default);
```

Finds a path between two world positions, running the search to the end in this call.

**Parameters**

- `from` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where the unit is.
- `to` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where it wants to go.
- `options` ([`NavOptions`](NavOptions.md)): The unit's size and moves; [`NavOptions.Default`](NavOptions.md#m-default) when omitted.

**Returns:** [`NavPath`](NavPath.md) — The path; its status says [`NavStatus.StartInvalid`](NavStatus.md#m-startinvalid) or [`NavStatus.GoalInvalid`](NavStatus.md#m-goalinvalid) when a position is outside the grid.

**Exceptions**

- [`NavRangeException`](NavRangeException.md): A position is beyond ±2^21 units.

### <a id="m-findpath-navcell-navcell-navoptions"></a>FindPath(NavCell, NavCell, NavOptions)

```csharp
public NavPath FindPath(NavCell start, NavCell goal, NavOptions options = default);
```

Finds a path between two cells, running the search to the end in this call. Use [`NavSearcher`](NavSearcher.md) to spread a search over ticks.

**Parameters**

- `start` ([`NavCell`](NavCell.md)): The first cell.
- `goal` ([`NavCell`](NavCell.md)): The last cell.
- `options` ([`NavOptions`](NavOptions.md)): The unit's size and moves; [`NavOptions.Default`](NavOptions.md#m-default) when omitted.

**Returns:** [`NavPath`](NavPath.md) — The path, or why there is none.

### <a id="m-fromtext-string-navspace"></a>FromText(string, NavSpace)

```csharp
public static NavGrid FromText(string text, NavSpace space = default);
```

Creates a grid from text: `.` is open, `#` a wall and `1` to `9` are areas; rows must be equally long.

**Parameters**

- `text` (`string`): The rows, one per line.
- `space` ([`NavSpace`](NavSpace.md)): Where the grid sits in the world.

**Returns:** [`NavGrid`](NavGrid.md) — The grid.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The text is not a rectangle of known characters.

### <a id="m-getarea-int-int"></a>GetArea(int, int)

```csharp
public byte GetArea(int x, int y);
```

The area of a cell: 0 is open, 1 to 254 are costed areas, 255 is a wall.

**Parameters**

- `x` (`int`): Column.
- `y` (`int`): Row.

**Returns:** `byte` — The area id.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The cell is outside the grid.

### <a id="m-indexof-navcell"></a>IndexOf(NavCell)

```csharp
public int IndexOf(NavCell cell);
```

The cell's index, `y * Width + x`, which is how paths are stored.

**Parameters**

- `cell` ([`NavCell`](NavCell.md)): A cell inside the grid.

**Returns:** `int` — The index.

### <a id="m-isblocked-int-int"></a>IsBlocked(int, int)

```csharp
public bool IsBlocked(int x, int y);
```

True if the cell is a wall.

**Parameters**

- `x` (`int`): Column.
- `y` (`int`): Row.

**Returns:** `bool` — True for a wall.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The cell is outside the grid.

### <a id="m-setarea-int-int-byte"></a>SetArea(int, int, byte)

```csharp
public void SetArea(int x, int y, byte area);
```

Sets the area of one cell.

**Parameters**

- `x` (`int`): Column.
- `y` (`int`): Row.
- `area` (`byte`): 0 open, 1 to 254 an area, 255 a wall.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The cell is outside the grid.

### <a id="m-setareacost-byte-fixed"></a>SetAreaCost(byte, Fixed)

```csharp
public void SetAreaCost(byte area, Fixed multiplier);
```

Sets what moving through an area costs, relative to an open cell (1).

**Parameters**

- `area` (`byte`): 1 to 254.
- `multiplier` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): Greater than zero; 3 means three times the effort of open ground.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The area is 0 or 255, or the multiplier is not positive or is out of range.

### <a id="m-setblocked-int-int-bool"></a>SetBlocked(int, int, bool)

```csharp
public void SetBlocked(int x, int y, bool blocked);
```

Opens or blocks one cell.

**Parameters**

- `x` (`int`): Column.
- `y` (`int`): Row.
- `blocked` (`bool`): True for a wall.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The cell is outside the grid.

### <a id="m-setrect-int-int-int-int-byte"></a>SetRect(int, int, int, int, byte)

```csharp
public void SetRect(int x, int y, int width, int height, byte area);
```

Sets the area of a rectangle of cells.

**Parameters**

- `x` (`int`): Left column.
- `y` (`int`): Top row.
- `width` (`int`): Columns.
- `height` (`int`): Rows.
- `area` (`byte`): 0 open, 1 to 254 an area, 255 a wall.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The rectangle is not inside the grid.

### <a id="m-trygetcell-fvec2-navcell"></a>TryGetCell(FVec2, out NavCell)

```csharp
public bool TryGetCell(FVec2 position, out NavCell cell);
```

The cell holding a world position.

**Parameters**

- `position` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): A world position.
- `cell` ([`NavCell`](NavCell.md)): The cell, when the position is inside the grid.

**Returns:** `bool` — False when the position is outside the grid.

**Exceptions**

- [`NavRangeException`](NavRangeException.md): The position is beyond ±2^21 units.
