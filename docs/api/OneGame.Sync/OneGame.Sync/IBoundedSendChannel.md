[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# IBoundedSendChannel interface

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IMessageChannel`](IMessageChannel.md)

```csharp
public interface IBoundedSendChannel : IMessageChannel
```

A channel whose transport keeps a bounded send queue and can say how full it is. The node uses it to pace what it sends: a snapshot larger than the queue is fed to the transport as it drains instead of being refused all at once.

## Properties

| Member | Description |
| --- | --- |
| [`QueueLimitBytes`](#m-queuelimitbytes) | The queue size at which the transport closes the connection with a send-queue-full reason. |
| [`QueuedBytes`](#m-queuedbytes) | Message bytes queued for sending and not yet acknowledged. |

### <a id="m-queuelimitbytes"></a>QueueLimitBytes

```csharp
long QueueLimitBytes { get; }
```

The queue size at which the transport closes the connection with a send-queue-full reason.

**Type:** `long`

### <a id="m-queuedbytes"></a>QueuedBytes

```csharp
long QueuedBytes { get; }
```

Message bytes queued for sending and not yet acknowledged.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`FailSendQueue()`](#m-failsendqueue) | Closes the connection with the transport's send-queue-full reason, which the peer receives and both ends report. |

### <a id="m-failsendqueue"></a>FailSendQueue()

```csharp
void FailSendQueue();
```

Closes the connection with the transport's send-queue-full reason, which the peer receives and both ends report.
