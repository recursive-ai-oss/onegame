[API reference](../index.md) › OneGame.Navigation

# OneGame.Navigation

Pathfinding for deterministic simulations: grid searches on fixed-point positions, budgeted over several ticks, with state that is saved, restored and hashed with the world.

**Package:** `RecursiveAI.OneGame.Navigation`

```sh
dotnet add package RecursiveAI.OneGame.Navigation
```

## OneGame.Navigation

| Type | Kind | Description |
| --- | --- | --- |
| [`NavAlgorithm`](OneGame.Navigation/NavAlgorithm.md) | enum | Which search a one-shot [`NavGrid.FindPath()`](OneGame.Navigation/NavGrid.md#m-findpath-navcell-navcell-navoptions) runs. |
| [`NavCell`](OneGame.Navigation/NavCell.md) | struct | A grid cell. |
| [`NavConvert`](OneGame.Navigation/NavConvert.md) | static class | Converts between [`Fixed`](../OneGame.Core/OneGame.Core/Fixed.md) numbers and doubles, for the cases where a double is unavoidable. |
| [`NavDebugLine`](OneGame.Navigation/NavDebugLine.md) | struct | A line of debug geometry in world units. |
| [`NavDebugRect`](OneGame.Navigation/NavDebugRect.md) | struct | A filled rectangle of debug geometry in world units. |
| [`NavDebugShapes`](OneGame.Navigation/NavDebugShapes.md) | class | Debug geometry for navigation: blocked cells, costed areas, paths and searches, as plain lines and rectangles in world units. |
| [`NavGrid`](OneGame.Navigation/NavGrid.md) | class | The walkable map a unit searches: a grid of cells, each open, blocked or an area with its own movement cost, placed in the world by a [`NavSpace`](OneGame.Navigation/NavSpace.md). |
| [`NavLimits`](OneGame.Navigation/NavLimits.md) | static class | The limits of the fixed-point to grid conversion. |
| [`NavMoves`](OneGame.Navigation/NavMoves.md) | enum | How a unit may step between cells. |
| [`NavOptions`](OneGame.Navigation/NavOptions.md) | struct | What kind of unit is searching. |
| [`NavPath`](OneGame.Navigation/NavPath.md) | class | The result of a one-shot search: a status and, when there is one, the cells from start to goal. |
| [`NavRangeException`](OneGame.Navigation/NavRangeException.md) | class | A world position was outside what navigation can convert exactly (±2^21 world units). |
| [`NavSearcher`](OneGame.Navigation/NavSearcher.md) | class | Runs searches for [`NavState`](OneGame.Navigation/NavState.md)s, a few iterations per tick. |
| [`NavSpace`](OneGame.Navigation/NavSpace.md) | struct | Where a grid sits in the world: the top-left corner and the edge length of a cell, both in [`Fixed`](../OneGame.Core/OneGame.Core/Fixed.md) units. |
| [`NavState`](OneGame.Navigation/NavState.md) | struct | One unit's navigation, as a value you keep in a synced property: what it asked for, how far the search got, and the path it found. |
| [`NavStatus`](OneGame.Navigation/NavStatus.md) | enum | The outcome of a search. |
