[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# EscrowState enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum EscrowState : byte
```

Where one trade stands on the coordinator.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-collecting"></a>`Collecting` | `1` | Waiting for both givers' values to be taken and held. |
| <a id="m-committed"></a>`Committed` | `2` | Both values are held; the coordinator decided to deliver them to the counterparties. |
| <a id="m-cancelled"></a>`Cancelled` | `3` | The trade failed or timed out; whatever was taken is being returned to its giver. |
| <a id="m-settled"></a>`Settled` | `4` | Both values were delivered. Final. |
| <a id="m-refunded"></a>`Refunded` | `5` | Everything taken was returned. Final. |
