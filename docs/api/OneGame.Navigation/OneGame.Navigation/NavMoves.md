[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavMoves enum

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)

```csharp
public enum NavMoves : byte
```

How a unit may step between cells.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-eight"></a>`Eight` | `0` | Eight directions; a diagonal step needs both side cells free, so units never cut a wall corner. |
| <a id="m-four"></a>`Four` | `1` | Up, down, left and right only. |
| <a id="m-eightnosqueeze"></a>`EightNoSqueeze` | `2` | Eight directions; a diagonal step may not squeeze between two blocked cells, but may pass one. |
