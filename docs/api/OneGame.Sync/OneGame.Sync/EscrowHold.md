[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# EscrowHold enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum EscrowHold : byte
```

Whether a leg's value has been taken from its giver.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-pending"></a>`Pending` | `0` | Not known yet. |
| <a id="m-held"></a>`Held` | `1` | Taken from the giver and held (the participant keeps a receipt). |
| <a id="m-failed"></a>`Failed` | `2` | Never taken, and never will be. |
