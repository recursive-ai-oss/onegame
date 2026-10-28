[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# WorldRole enum

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public enum WorldRole : int
```

Whether a world owns its state or mirrors another node's.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-authority"></a>`Authority` | `0` | The world simulates and is the source of truth. |
| <a id="m-replica"></a>`Replica` | `1` | The world only applies state received from an authority. Setters and ticking throw [`ReplicaWriteException`](ReplicaWriteException.md). |
| <a id="m-predicted"></a>`Predicted` | `2` | A writable copy a replica simulates ahead of its authority (client prediction). It runs ticks and predicted commands like an authority, but commands sent to its entities from outside the sim are forwarded through [`World.Observer`](World.md#m-observer) as on a replica. |
