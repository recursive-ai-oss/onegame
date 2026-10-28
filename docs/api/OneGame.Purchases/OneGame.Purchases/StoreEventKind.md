[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# StoreEventKind enum

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)

```csharp
public enum StoreEventKind : int
```

What kind of thing happened in the store.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-connected"></a>`Connected` | `0` | The connection attempt ended; [`StoreEvent.Error`](StoreEvent.md#m-error) is set when it failed. |
| <a id="m-disconnected"></a>`Disconnected` | `1` | The store went away after being connected (Play's service disconnected, for example). |
| <a id="m-productsloaded"></a>`ProductsLoaded` | `2` | A product query finished: [`StoreEvent.Products`](StoreEvent.md#m-products) and [`StoreEvent.UnknownProductIds`](StoreEvent.md#m-unknownproductids). |
| <a id="m-transaction"></a>`Transaction` | `3` | A transaction changed: [`StoreEvent.Transaction`](StoreEvent.md#m-transaction). Also used for failed purchases that have no store transaction. |
| <a id="m-restorefinished"></a>`RestoreFinished` | `4` | A restore finished. [`StoreEvent.Error`](StoreEvent.md#m-error) is set when it failed. Every restored transaction arrived as a [`StoreEventKind.Transaction`](StoreEventKind.md#m-transaction) event before this one. |
| <a id="m-finished"></a>`Finished` | `5` | A [`PurchaseService.Finish()`](PurchaseService.md#m-finish-purchasetransaction) call ended: [`StoreEvent.Transaction`](StoreEvent.md#m-transaction) is the one finished; [`StoreEvent.Error`](StoreEvent.md#m-error) is set when the store refused. |
