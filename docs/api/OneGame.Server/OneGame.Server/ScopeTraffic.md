[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# ScopeTraffic record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ScopeTraffic`](ScopeTraffic.md)&gt;

```csharp
public sealed record ScopeTraffic : IEquatable<ScopeTraffic>
```

A scope's outgoing totals.

## Constructors

| Member | Description |
| --- | --- |
| [`ScopeTraffic(long, long, long, long)`](#m-ctor-long-long-long-long) | A scope's outgoing totals. |

### <a id="m-ctor-long-long-long-long"></a>ScopeTraffic(long, long, long, long)

```csharp
public ScopeTraffic(long FrameBytes, long SnapshotBytes, long EntitiesSent, long EntitiesRemoved);
```

A scope's outgoing totals.

**Parameters**

- `FrameBytes` (`long`): Bytes of frame messages, every connection's copy counted.
- `SnapshotBytes` (`long`): Bytes of snapshot messages.
- `EntitiesSent` (`long`): Entities sent whole.
- `EntitiesRemoved` (`long`): Entity removals sent.

## Properties

| Member | Description |
| --- | --- |
| [`EntitiesRemoved`](#m-entitiesremoved) | Entity removals sent. |
| [`EntitiesSent`](#m-entitiessent) | Entities sent whole. |
| [`FrameBytes`](#m-framebytes) | Bytes of frame messages, every connection's copy counted. |
| [`SnapshotBytes`](#m-snapshotbytes) | Bytes of snapshot messages. |

### <a id="m-entitiesremoved"></a>EntitiesRemoved

```csharp
public long EntitiesRemoved { get; init; }
```

Entity removals sent.

**Type:** `long`

### <a id="m-entitiessent"></a>EntitiesSent

```csharp
public long EntitiesSent { get; init; }
```

Entities sent whole.

**Type:** `long`

### <a id="m-framebytes"></a>FrameBytes

```csharp
public long FrameBytes { get; init; }
```

Bytes of frame messages, every connection's copy counted.

**Type:** `long`

### <a id="m-snapshotbytes"></a>SnapshotBytes

```csharp
public long SnapshotBytes { get; init; }
```

Bytes of snapshot messages.

**Type:** `long`
