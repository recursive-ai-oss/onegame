[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# EscrowDelivery enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum EscrowDelivery : byte
```

Whether a leg's value has been handed on (to the counterparty after a commit, back to the giver after a cancel).

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | Nothing to deliver (yet, or never). |
| <a id="m-pending"></a>`Pending` | `1` | The coordinator wants it delivered; the credit has not been confirmed. |
| <a id="m-delivered"></a>`Delivered` | `2` | The credit was applied exactly once. |
