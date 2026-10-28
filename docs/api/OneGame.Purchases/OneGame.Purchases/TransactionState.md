[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# TransactionState enum

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)

```csharp
public enum TransactionState : int
```

Where a transaction stands.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-purchased"></a>`Purchased` | `0` | The store took the payment. The game grants the item, then calls [`PurchaseService.Finish()`](PurchaseService.md#m-finish-purchasetransaction). |
| <a id="m-pending"></a>`Pending` | `1` | Payment is waiting on something outside the game (a parent's approval, a cash payment). Nothing is granted yet; a later update settles it. |
| <a id="m-cancelled"></a>`Cancelled` | `2` | The player backed out. |
| <a id="m-failed"></a>`Failed` | `3` | The purchase did not happen; [`PurchaseTransaction.Error`](PurchaseTransaction.md#m-error) says why. |
| <a id="m-revoked"></a>`Revoked` | `4` | The store took the purchase back (a refund or a revoked family share). The game removes what it granted. |
| <a id="m-restored"></a>`Restored` | `5` | A purchase that already exists, delivered again by a restore or on start. Handled like [`TransactionState.Purchased`](TransactionState.md#m-purchased). |
