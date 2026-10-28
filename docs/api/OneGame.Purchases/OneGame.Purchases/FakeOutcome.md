[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# FakeOutcome enum

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)

```csharp
public enum FakeOutcome : int
```

What the fake store does with the next purchase of a product.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-succeed"></a>`Succeed` | `0` | The player pays: a [`TransactionState.Purchased`](TransactionState.md#m-purchased) transaction. |
| <a id="m-cancel"></a>`Cancel` | `1` | The player backs out: [`TransactionState.Cancelled`](TransactionState.md#m-cancelled). |
| <a id="m-pend"></a>`Pend` | `2` | The payment waits on approval: [`TransactionState.Pending`](TransactionState.md#m-pending), settled by [`FakeStore.ResolvePending()`](FakeStore.md#m-resolvepending-string-bool). |
| <a id="m-fail"></a>`Fail` | `3` | The store fails the purchase: [`TransactionState.Failed`](TransactionState.md#m-failed) with the scripted error. |
