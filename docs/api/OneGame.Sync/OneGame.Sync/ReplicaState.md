[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# ReplicaState enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum ReplicaState : int
```

Where a replica is in its life.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-subscribing"></a>`Subscribing` | `0` | The subscription was sent; no snapshot has arrived yet. |
| <a id="m-live"></a>`Live` | `1` | Snapshot applied, frames arriving in order. |
| <a id="m-resyncing"></a>`Resyncing` | `2` | A frame, snapshot or drift made the replica ask for a fresh snapshot; frames are ignored until it arrives. |
| <a id="m-refused"></a>`Refused` | `3` | The authority refused the subscription (see [`Node.Refusals`](Node.md#m-refusals)). |
| <a id="m-failed"></a>`Failed` | `4` | The authority kept sending data this replica could not apply, three times in a row. It stopped asking. |
| <a id="m-revoked"></a>`Revoked` | `5` | The authority took the read right away. The replica's world was emptied; [`Node.Resubscribe()`](Node.md#m-resubscribe-replicascope) asks again. |
| <a id="m-unsubscribed"></a>`Unsubscribed` | `6` | This node stopped following the scope ([`Node.Unsubscribe()`](Node.md#m-unsubscribe-replicascope)). The world keeps its last state and is no longer updated. |
