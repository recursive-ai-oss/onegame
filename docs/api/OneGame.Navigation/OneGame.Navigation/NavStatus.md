[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavStatus enum

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)

```csharp
public enum NavStatus : byte
```

The outcome of a search.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-idle"></a>`Idle` | `0` | No search has been asked for. |
| <a id="m-searching"></a>`Searching` | `1` | A search is running over several ticks. |
| <a id="m-found"></a>`Found` | `2` | A path is ready. |
| <a id="m-nopath"></a>`NoPath` | `3` | The goal cannot be reached from the start. |
| <a id="m-startinvalid"></a>`StartInvalid` | `4` | The start is blocked, outside the grid, or too narrow for the unit. |
| <a id="m-goalinvalid"></a>`GoalInvalid` | `5` | The goal is blocked, outside the grid, or too narrow for the unit. |
| <a id="m-arrived"></a>`Arrived` | `6` | The unit walked the whole path. |
