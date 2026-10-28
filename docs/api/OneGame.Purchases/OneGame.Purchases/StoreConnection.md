[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# StoreConnection enum

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)

```csharp
public enum StoreConnection : int
```

Where the connection to the store stands.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-disconnected"></a>`Disconnected` | `0` | [`PurchaseService.Connect()`](PurchaseService.md#m-connect) has not been called, or the store went away. |
| <a id="m-connecting"></a>`Connecting` | `1` | Waiting for the store to answer. |
| <a id="m-connected"></a>`Connected` | `2` | Ready: queries and purchases can be made. |
| <a id="m-failed"></a>`Failed` | `3` | The last attempt failed; [`PurchaseService.ConnectError`](PurchaseService.md#m-connecterror) says why. |
