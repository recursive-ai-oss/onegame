[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavSearcher class

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)

```csharp
public sealed class NavSearcher
```

Runs searches for [`NavState`](NavState.md)s, a few iterations per tick. It is a cache, not state: it holds the search engine for the state it last produced, and builds it again from the state alone when handed a state it does not recognise (after a restore, a snapshot load or a different grid). Nothing here needs saving.

```csharp
protected override void OnTick()
{
    var nav = Nav;
    if (_searcher.Advance(ref nav, Map, budget: 200)) { Nav = nav; }   // assign only when something changed
}
```

One searcher serves one unit (it keeps that unit's search warm); keep it in a non-synced field. It searches a private copy of the map taken when the search starts (a byte per cell), so it holds nothing that needs releasing and edits made mid-search cannot give a path through a half-changed map.

## Constructors

| Member | Description |
| --- | --- |
| [`NavSearcher()`](#m-ctor) |  |

### <a id="m-ctor"></a>NavSearcher()

```csharp
public NavSearcher();
```

## Properties

| Member | Description |
| --- | --- |
| [`Rebuilds`](#m-rebuilds) | Number of times the search engine was rebuilt from a state instead of continued (after a restore, for instance). |

### <a id="m-rebuilds"></a>Rebuilds

```csharp
public int Rebuilds { get; }
```

Number of times the search engine was rebuilt from a state instead of continued (after a restore, for instance). Diagnostics.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Advance(ref NavState, NavGrid, int)`](#m-advance-navstate-navgrid-int) | Spends up to `budget` search iterations on the state. |
| [`Begin(NavGrid, FVec2, FVec2, NavOptions)`](#m-begin-navgrid-fvec2-fvec2-navoptions) | Starts a search between two world positions. |
| [`Begin(NavGrid, NavCell, NavCell, NavOptions)`](#m-begin-navgrid-navcell-navcell-navoptions) | Starts a search between two cells. |

### <a id="m-advance-navstate-navgrid-int"></a>Advance(ref NavState, NavGrid, int)

```csharp
public bool Advance(ref NavState state, NavGrid grid, int budget);
```

Spends up to `budget` search iterations on the state. A finished search becomes [`NavStatus.Found`](NavStatus.md#m-found) or [`NavStatus.NoPath`](NavStatus.md#m-nopath); an unfinished one stays [`NavStatus.Searching`](NavStatus.md#m-searching) with its progress recorded. A map edited since the search or path was made restarts it from [`NavState.Start`](NavState.md#m-start). Does nothing for the other statuses.

**Parameters**

- `state` ([`NavState`](NavState.md)): The state to advance; replaced when it changed.
- `grid` ([`NavGrid`](NavGrid.md)): The map.
- `budget` (`int`): Iterations (queue pops) to spend this call, at least 1. A few hundred per tick keeps the cost flat.

**Returns:** `bool` — True when `state` changed, and so should be stored.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The budget is below 1.

### <a id="m-begin-navgrid-fvec2-fvec2-navoptions"></a>Begin(NavGrid, FVec2, FVec2, NavOptions)

```csharp
public NavState Begin(NavGrid grid, FVec2 from, FVec2 to, NavOptions options = default);
```

Starts a search between two world positions.

**Parameters**

- `grid` ([`NavGrid`](NavGrid.md)): The map.
- `from` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where the unit is.
- `to` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): Where it wants to go.
- `options` ([`NavOptions`](NavOptions.md)): The unit's size and moves.

**Returns:** [`NavState`](NavState.md) — A state to store: [`NavStatus.Searching`](NavStatus.md#m-searching), or [`NavStatus.StartInvalid`](NavStatus.md#m-startinvalid) / [`NavStatus.GoalInvalid`](NavStatus.md#m-goalinvalid) when a position is off the grid or blocked.

**Exceptions**

- [`NavRangeException`](NavRangeException.md): A position is beyond ±2^21 units.

### <a id="m-begin-navgrid-navcell-navcell-navoptions"></a>Begin(NavGrid, NavCell, NavCell, NavOptions)

```csharp
public NavState Begin(NavGrid grid, NavCell start, NavCell goal, NavOptions options = default);
```

Starts a search between two cells.

**Parameters**

- `grid` ([`NavGrid`](NavGrid.md)): The map.
- `start` ([`NavCell`](NavCell.md)): The first cell.
- `goal` ([`NavCell`](NavCell.md)): The last cell.
- `options` ([`NavOptions`](NavOptions.md)): The unit's size and moves.

**Returns:** [`NavState`](NavState.md) — A state to store; see [`NavSearcher.Begin()`](NavSearcher.md#m-begin-navgrid-fvec2-fvec2-navoptions).
