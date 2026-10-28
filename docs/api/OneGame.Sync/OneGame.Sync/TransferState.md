[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# TransferState enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum TransferState : int
```

Where an authority transfer stands. See "Transfer, relaying and escrow" in the sync interface document.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-pending"></a>`Pending` | `0` | Requested; starts after the next tick. |
| <a id="m-offered"></a>`Offered` | `1` | The entities left the world and were offered to the other node. |
| <a id="m-committed"></a>`Committed` | `2` | The other node accepted and this node decided to commit; waiting for it to confirm the install. |
| <a id="m-complete"></a>`Complete` | `3` | The other node installed the entities. They are its authority now. |
| <a id="m-aborted"></a>`Aborted` | `4` | The transfer did not happen; the entities are back here, unchanged. |
