[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# StoreEventBuffer class

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)

```csharp
public sealed class StoreEventBuffer
```

A thread-safe, bounded queue that carries events from a store's threads to the game thread.

## Constructors

| Member | Description |
| --- | --- |
| [`StoreEventBuffer()`](#m-ctor) |  |

### <a id="m-ctor"></a>StoreEventBuffer()

```csharp
public StoreEventBuffer();
```

## Methods

| Member | Description |
| --- | --- |
| [`Post(StoreEvent)`](#m-post-storeevent) | Adds an event. |
| [`TryTake(out StoreEvent)`](#m-trytake-storeevent) | Takes the oldest event. |

### <a id="m-post-storeevent"></a>Post(StoreEvent)

```csharp
public void Post(StoreEvent storeEvent);
```

Adds an event. When the game stops pumping and the queue is full, the oldest event is dropped, so a stalled game cannot grow memory.

**Parameters**

- `storeEvent` ([`StoreEvent`](StoreEvent.md))

### <a id="m-trytake-storeevent"></a>TryTake(out StoreEvent)

```csharp
public bool TryTake(out StoreEvent storeEvent);
```

Takes the oldest event.

**Parameters**

- `storeEvent` ([`StoreEvent`](StoreEvent.md))

**Returns:** `bool`
