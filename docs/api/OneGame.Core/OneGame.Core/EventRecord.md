[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# EventRecord struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public readonly ref struct EventRecord
```

> **Obsolete.** Types with embedded references are not supported in this version of your compiler.

One event as read from an [`EventLog`](EventLog.md). Valid until the reader's next call.

## Properties

| Member | Description |
| --- | --- |
| [`Entity`](#m-entity) | The emitting entity, or [`EntityId.None`](EntityId.md#m-none). |
| [`Info`](#m-info) | The position data without the payload, which can be kept after the reader moves on. |
| [`Payload`](#m-payload) | The encoded payload. |
| [`Sequence`](#m-sequence) | The event's position in its log. |
| [`Tick`](#m-tick) | The tick the event happened on. |
| [`TypeId`](#m-typeid) | The event type's id ([`ISimEvent<TSelf>.EventTypeId`](ISimEvent-1.md#m-eventtypeid)). |

### <a id="m-entity"></a>Entity

```csharp
public EntityId Entity { get; }
```

The emitting entity, or [`EntityId.None`](EntityId.md#m-none).

**Type:** [`EntityId`](EntityId.md)

### <a id="m-info"></a>Info

```csharp
public EventInfo Info { get; }
```

The position data without the payload, which can be kept after the reader moves on.

**Type:** [`EventInfo`](EventInfo.md)

### <a id="m-payload"></a>Payload

```csharp
public ReadOnlySpan<byte> Payload { get; }
```

The encoded payload.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`

### <a id="m-sequence"></a>Sequence

```csharp
public long Sequence { get; }
```

The event's position in its log.

**Type:** `long`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; }
```

The tick the event happened on.

**Type:** `long`

### <a id="m-typeid"></a>TypeId

```csharp
public uint TypeId { get; }
```

The event type's id ([`ISimEvent<TSelf>.EventTypeId`](ISimEvent-1.md#m-eventtypeid)).

**Type:** `uint`

## Methods

| Member | Description |
| --- | --- |
| [`Is<T>()`](#m-is-t) | True when the event is a `T`. |
| [`TryRead<T>(out T)`](#m-tryread-t-t) | Decodes the payload when the event is a `T`. |

### <a id="m-is-t"></a>Is&lt;T&gt;()

```csharp
public bool Is<T>() where T : ISimEvent<T>;
```

True when the event is a `T`.

**Returns:** `bool`

### <a id="m-tryread-t-t"></a>TryRead&lt;T&gt;(out T)

```csharp
public bool TryRead<T>(out T value) where T : ISimEvent<T>;
```

Decodes the payload when the event is a `T`.

**Parameters**

- `value` (`T`): The decoded event, or default.

**Returns:** `bool` — False when the event is of another type or the payload does not decode.
