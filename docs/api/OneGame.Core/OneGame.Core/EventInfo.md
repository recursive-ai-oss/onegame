[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# EventInfo struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`EventInfo`](EventInfo.md)&gt;

```csharp
public readonly struct EventInfo : IEquatable<EventInfo>
```

Where and when an event happened.

## Constructors

| Member | Description |
| --- | --- |
| [`EventInfo(long, EntityId, long)`](#m-ctor-long-entityid-long) | Where and when an event happened. |

### <a id="m-ctor-long-entityid-long"></a>EventInfo(long, EntityId, long)

```csharp
public EventInfo(long Tick, EntityId Entity, long Sequence);
```

Where and when an event happened.

**Parameters**

- `Tick` (`long`): The tick the sim was on when it emitted the event (the same number a command that ran then reports).
- `Entity` ([`EntityId`](EntityId.md)): The entity that emitted it, or [`EntityId.None`](EntityId.md#m-none) for a world event. A replica may not hold that entity.
- `Sequence` (`long`): The event's position in this log; rises by one per event and is only comparable within one log.

## Properties

| Member | Description |
| --- | --- |
| [`Entity`](#m-entity) | The entity that emitted it, or [`EntityId.None`](EntityId.md#m-none) for a world event. |
| [`Sequence`](#m-sequence) | The event's position in this log; rises by one per event and is only comparable within one log. |
| [`Tick`](#m-tick) | The tick the sim was on when it emitted the event (the same number a command that ran then reports). |

### <a id="m-entity"></a>Entity

```csharp
public EntityId Entity { get; init; }
```

The entity that emitted it, or [`EntityId.None`](EntityId.md#m-none) for a world event. A replica may not hold that entity.

**Type:** [`EntityId`](EntityId.md)

### <a id="m-sequence"></a>Sequence

```csharp
public long Sequence { get; init; }
```

The event's position in this log; rises by one per event and is only comparable within one log.

**Type:** `long`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The tick the sim was on when it emitted the event (the same number a command that ran then reports).

**Type:** `long`
