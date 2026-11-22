[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# EventLog class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class EventLog
```

The events a world's sim emitted, as a bounded ring of the most recent ones. Authority worlds record what their sim emits; a replica world records the events its authority sent it (only those its audience may see). Events are output of a tick, not state: they are not saved, not snapshotted, not part of any state hash, and a restore or a late join starts with an empty log. Read them with an [`EventReader`](EventReader.md).

## Fields

| Member | Description |
| --- | --- |
| [`Capacity`](#m-capacity) | How many events a log keeps; a reader that falls further behind loses the oldest ([`EventReader.Missed`](EventReader.md#m-missed)). |
| [`MaxPayloadBytes`](#m-maxpayloadbytes) | The largest encoded payload of one event, in bytes. |

### <a id="m-capacity"></a>Capacity

```csharp
public const int Capacity = 512;
```

How many events a log keeps; a reader that falls further behind loses the oldest ([`EventReader.Missed`](EventReader.md#m-missed)).

**Type:** `int`

### <a id="m-maxpayloadbytes"></a>MaxPayloadBytes

```csharp
public const int MaxPayloadBytes = 512;
```

The largest encoded payload of one event, in bytes. Emitting a bigger one throws.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | Events appended since the log was created. |
| [`Dropped`](#m-dropped) | Events dropped because a tick emitted more than [`EventLog.EventsPerTickLimit`](EventLog.md#m-eventsperticklimit). |
| [`Epoch`](#m-epoch) | Counts every reset (snapshot restore, load): readers see it and start from the end. |
| [`EventsPerTickLimit`](#m-eventsperticklimit) | How many events one tick may emit before the rest are dropped and counted in [`EventLog.Dropped`](EventLog.md#m-dropped) (1 to [`EventLog.Capacity`](EventLog.md#m-capacity), default 128). |
| [`FirstRetained`](#m-firstretained) | The sequence of the oldest event still held. |
| [`StreamHash`](#m-streamhash) | A hash of every event appended since the log was created or last reset (tick, type, entity and payload, in order). |

### <a id="m-count"></a>Count

```csharp
public long Count { get; }
```

Events appended since the log was created. The next event gets this as its [`EventInfo.Sequence`](EventInfo.md#m-sequence).

**Type:** `long`

### <a id="m-dropped"></a>Dropped

```csharp
public long Dropped { get; }
```

Events dropped because a tick emitted more than [`EventLog.EventsPerTickLimit`](EventLog.md#m-eventsperticklimit).

**Type:** `long`

### <a id="m-epoch"></a>Epoch

```csharp
public int Epoch { get; }
```

Counts every reset (snapshot restore, load): readers see it and start from the end.

**Type:** `int`

### <a id="m-eventsperticklimit"></a>EventsPerTickLimit

```csharp
public int EventsPerTickLimit { get; set; }
```

How many events one tick may emit before the rest are dropped and counted in [`EventLog.Dropped`](EventLog.md#m-dropped) (1 to [`EventLog.Capacity`](EventLog.md#m-capacity), default 128). Deterministic: the same tick drops the same events everywhere.

**Type:** `int`

### <a id="m-firstretained"></a>FirstRetained

```csharp
public long FirstRetained { get; }
```

The sequence of the oldest event still held.

**Type:** `long`

### <a id="m-streamhash"></a>StreamHash

```csharp
public ulong StreamHash { get; }
```

A hash of every event appended since the log was created or last reset (tick, type, entity and payload, in order). Not part of the world's state hash; tests use it to compare two runs' event streams cheaply.

**Type:** `ulong`

## Methods

| Member | Description |
| --- | --- |
| [`NewReader()`](#m-newreader) | Starts a reader at the end of the log: it sees only events emitted from now on. |
| [`NewReaderFromStart()`](#m-newreaderfromstart) | Starts a reader at the oldest event still held, so it sees the history first. |
| [`TryGet(long, out EventRecord)`](#m-tryget-long-eventrecord) | Reads one retained event by sequence. |
| [`WriteJson(JsonOut, long, int)`](#m-writejson-jsonout-long-int) | Writes the retained events with a sequence of at least `since` (at most the newest `max`) as a JSON array: sequence, tick, entity (`Type#id` is not known here, so the raw id), type name and the payload fields. |

### <a id="m-newreader"></a>NewReader()

```csharp
public EventReader NewReader();
```

Starts a reader at the end of the log: it sees only events emitted from now on.

**Returns:** [`EventReader`](EventReader.md)

### <a id="m-newreaderfromstart"></a>NewReaderFromStart()

```csharp
public EventReader NewReaderFromStart();
```

Starts a reader at the oldest event still held, so it sees the history first.

**Returns:** [`EventReader`](EventReader.md)

### <a id="m-tryget-long-eventrecord"></a>TryGet(long, out EventRecord)

```csharp
public bool TryGet(long sequence, out EventRecord record);
```

Reads one retained event by sequence.

**Parameters**

- `sequence` (`long`): The event's [`EventInfo.Sequence`](EventInfo.md#m-sequence).
- `record` ([`EventRecord`](EventRecord.md)): The event; valid until the log changes.

**Returns:** `bool` — False when the event is older than the ring holds or not emitted yet.

### <a id="m-writejson-jsonout-long-int"></a>WriteJson(JsonOut, long, int)

```csharp
public void WriteJson(JsonOut json, long since = 0, int max = 256);
```

Writes the retained events with a sequence of at least `since` (at most the newest `max`) as a JSON array: sequence, tick, entity (`Type#id` is not known here, so the raw id), type name and the payload fields.

**Parameters**

- `json` ([`JsonOut`](JsonOut.md))
- `since` (`long`)
- `max` (`int`)
