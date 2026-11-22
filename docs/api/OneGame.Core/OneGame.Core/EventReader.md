[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# EventReader class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class EventReader
```

A view's place in an [`EventLog`](EventLog.md). Create one per consumer (a view, the audio code, a test) and call [`EventReader.Next()`](EventReader.md#m-next-eventrecord) each frame: every event is delivered once, in emit order, with the tick it happened on. A reader never sees events that were already in the log when it was created, nor anything from before a snapshot restore. Create a new reader when the game's world is replaced.

## Properties

| Member | Description |
| --- | --- |
| [`Missed`](#m-missed) | Events lost because the reader fell more than [`EventLog.Capacity`](EventLog.md#m-capacity) behind. |
| [`Pending`](#m-pending) | How many events wait for this reader. |
| [`Restarts`](#m-restarts) | How many times the log was reset (a snapshot restore or resync) while this reader held a position; the events of the reset span are gone. |

### <a id="m-missed"></a>Missed

```csharp
public long Missed { get; }
```

Events lost because the reader fell more than [`EventLog.Capacity`](EventLog.md#m-capacity) behind.

**Type:** `long`

### <a id="m-pending"></a>Pending

```csharp
public long Pending { get; }
```

How many events wait for this reader.

**Type:** `long`

### <a id="m-restarts"></a>Restarts

```csharp
public int Restarts { get; }
```

How many times the log was reset (a snapshot restore or resync) while this reader held a position; the events of the reset span are gone.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Next(out EventRecord)`](#m-next-eventrecord) | Takes the next event of any type. |
| [`Next<T>(out T, out EventInfo)`](#m-next-t-t-eventinfo) | Takes the next event that is a `T`, passing over (and consuming) events of other types. |
| [`SkipToEnd()`](#m-skiptoend) | Moves to the end of the log, skipping whatever waits. |

### <a id="m-next-eventrecord"></a>Next(out EventRecord)

```csharp
public bool Next(out EventRecord record);
```

Takes the next event of any type.

**Parameters**

- `record` ([`EventRecord`](EventRecord.md)): The event; valid until the next call.

**Returns:** `bool` — False when none is waiting.

### <a id="m-next-t-t-eventinfo"></a>Next&lt;T&gt;(out T, out EventInfo)

```csharp
public bool Next<T>(out T value, out EventInfo info) where T : ISimEvent<T>;
```

Takes the next event that is a `T`, passing over (and consuming) events of other types. Use one reader per kind of consumer.

**Parameters**

- `value` (`T`): The decoded event.
- `info` ([`EventInfo`](EventInfo.md)): When and where it happened.

**Returns:** `bool` — False when no `T` is waiting.

### <a id="m-skiptoend"></a>SkipToEnd()

```csharp
public void SkipToEnd();
```

Moves to the end of the log, skipping whatever waits.
