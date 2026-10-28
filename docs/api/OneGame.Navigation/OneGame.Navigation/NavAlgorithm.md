[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavAlgorithm enum

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)

```csharp
public enum NavAlgorithm : byte
```

Which search a one-shot [`NavGrid.FindPath()`](NavGrid.md#m-findpath-navcell-navcell-navoptions) runs. Budgeted searches always use the repairing one.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-astar"></a>`AStar` | `0` | A\*: the cheapest path on the grid. |
| <a id="m-jps"></a>`Jps` | `1` | Jump point search: the same cost as A\* on uniform grids, much faster on open ground. |
| <a id="m-thetastar"></a>`ThetaStar` | `2` | Theta\*: any-angle paths, shorter than grid paths where there is line of sight. |
