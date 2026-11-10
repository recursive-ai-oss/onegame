[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# InterestGrid2D class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class InterestGrid2D
```

A flat square grid for interest keys, and the helpers that keep them from flapping at cell edges. Keys pack the cell column and row into 16 bits each (offset so a key is never 0), which covers 65,534 cells a side: with 128-block cells that is far past ±100,000 blocks. Positions are whole blocks (or [`Fixed`](Fixed.md)); the grid does not wrap.

## Nested types

| Type | Description |
| --- | --- |
| [`InterestGrid2D.Follower`](InterestGrid2D.Follower.md) | Tracks the cells one connection should receive. |

## Constructors

| Member | Description |
| --- | --- |
| [`InterestGrid2D(int, int, int)`](#m-ctor-int-int-int) | Creates a grid. |

### <a id="m-ctor-int-int-int"></a>InterestGrid2D(int, int, int)

```csharp
public InterestGrid2D(int cellSize = 128, int enterCells = 2, int leaveCells = 3);
```

Creates a grid.

**Parameters**

- `cellSize` (`int`): Cell edge in blocks (64 to 128 suits players moving at a few metres a second).
- `enterCells` (`int`): A connection starts receiving a cell once its position is within this many cells (Chebyshev) of it.
- `leaveCells` (`int`): ...and stops only once it is farther than this many cells. Must be greater than `enterCells` so that standing on a boundary cannot flap.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): A size or radius is out of range, or leave is not greater than enter.

## Properties

| Member | Description |
| --- | --- |
| [`CellSize`](#m-cellsize) | Cell edge in blocks. |
| [`EnterCells`](#m-entercells) | Radius in cells at which a cell is added. |
| [`LeaveCells`](#m-leavecells) | Radius in cells past which a cell is dropped. |

### <a id="m-cellsize"></a>CellSize

```csharp
public int CellSize { get; }
```

Cell edge in blocks.

**Type:** `int`

### <a id="m-entercells"></a>EnterCells

```csharp
public int EnterCells { get; }
```

Radius in cells at which a cell is added.

**Type:** `int`

### <a id="m-leavecells"></a>LeaveCells

```csharp
public int LeaveCells { get; }
```

Radius in cells past which a cell is dropped.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Cell(int)`](#m-cell-int) | The cell column or row holding a coordinate in blocks. |
| [`Follow()`](#m-follow) | A connection's cursor: the cells around a position with enter/leave hysteresis. |
| [`Key(Fixed, Fixed)`](#m-key-fixed-fixed) | The key of the cell holding the point (never 0). |
| [`Key(int, int)`](#m-key-int-int) | The key of the cell holding the point (never 0). |
| [`Pack(int, int)`](#m-pack-int-int) | The key of cell (`cx`, `cy`). |
| [`Sticky(uint, int, int, int)`](#m-sticky-uint-int-int-int) | The key an entity should carry now: `current` while the point is still inside that cell widened by `marginBlocks`, otherwise the cell of the point. |
| [`Unpack(uint, out int, out int)`](#m-unpack-uint-int-int) | Splits a key made by [`InterestGrid2D.Pack()`](InterestGrid2D.md#m-pack-int-int). |

### <a id="m-cell-int"></a>Cell(int)

```csharp
public int Cell(int blocks);
```

The cell column or row holding a coordinate in blocks.

**Parameters**

- `blocks` (`int`): A coordinate in blocks.

**Returns:** `int`

### <a id="m-follow"></a>Follow()

```csharp
public InterestGrid2D.Follower Follow();
```

A connection's cursor: the cells around a position with enter/leave hysteresis. One per connection and scope.

**Returns:** `InterestGrid2D.`[`Follower`](InterestGrid2D.Follower.md)

### <a id="m-key-fixed-fixed"></a>Key(Fixed, Fixed)

```csharp
public uint Key(Fixed x, Fixed y);
```

The key of the cell holding the point (never 0).

**Parameters**

- `x` ([`Fixed`](Fixed.md)): Position.
- `y` ([`Fixed`](Fixed.md)): Position.

**Returns:** `uint`

### <a id="m-key-int-int"></a>Key(int, int)

```csharp
public uint Key(int x, int y);
```

The key of the cell holding the point (never 0).

**Parameters**

- `x` (`int`): Position in blocks.
- `y` (`int`): Position in blocks.

**Returns:** `uint`

### <a id="m-pack-int-int"></a>Pack(int, int)

```csharp
public static uint Pack(int cx, int cy);
```

The key of cell (`cx`, `cy`).

**Parameters**

- `cx` (`int`): Cell column.
- `cy` (`int`): Cell row.

**Returns:** `uint`

### <a id="m-sticky-uint-int-int-int"></a>Sticky(uint, int, int, int)

```csharp
public uint Sticky(uint current, int x, int y, int marginBlocks);
```

The key an entity should carry now: `current` while the point is still inside that cell widened by `marginBlocks`, otherwise the cell of the point. An entity walking along a cell edge keeps one key instead of switching on every step.

**Parameters**

- `current` (`uint`): The entity's present key (0 for none).
- `x` (`int`): Position in blocks.
- `y` (`int`): Position in blocks.
- `marginBlocks` (`int`): How far past the cell edge the entity may stray before it changes cell.

**Returns:** `uint`

### <a id="m-unpack-uint-int-int"></a>Unpack(uint, out int, out int)

```csharp
public static void Unpack(uint key, out int cx, out int cy);
```

Splits a key made by [`InterestGrid2D.Pack()`](InterestGrid2D.md#m-pack-int-int).

**Parameters**

- `key` (`uint`): A grid key.
- `cx` (`int`): Cell column.
- `cy` (`int`): Cell row.
