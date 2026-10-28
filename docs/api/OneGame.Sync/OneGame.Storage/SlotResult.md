[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Storage

# SlotResult record

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SlotResult`](SlotResult.md)&gt;

```csharp
public sealed record SlotResult : IEquatable<SlotResult>
```

The outcome of a slot operation.

## Constructors

| Member | Description |
| --- | --- |
| [`SlotResult(SlotStatus, SlotInfo?, LoadResult?, string)`](#m-ctor-slotstatus-slotinfo-loadresult-string) | The outcome of a slot operation. |

### <a id="m-ctor-slotstatus-slotinfo-loadresult-string"></a>SlotResult(SlotStatus, SlotInfo?, LoadResult?, string)

```csharp
public SlotResult(SlotStatus Status, SlotInfo? Info, LoadResult? Load, string Detail);
```

The outcome of a slot operation.

**Parameters**

- `Status` ([`SlotStatus`](SlotStatus.md)): How it ended.
- `Info` ([`SlotInfo`](SlotInfo.md)?): The slot's description, when it could be read or was written.
- `Load` ([`LoadResult`](../OneGame.Sync/LoadResult.md)?): What the save loader found, for loads that reached it.
- `Detail` (`string`): A sentence for logs when something went wrong.

## Properties

| Member | Description |
| --- | --- |
| [`Detail`](#m-detail) | A sentence for logs when something went wrong. |
| [`Info`](#m-info) | The slot's description, when it could be read or was written. |
| [`Load`](#m-load) | What the save loader found, for loads that reached it. |
| [`Ok`](#m-ok) | True when [`SlotResult.Status`](SlotResult.md#m-status) is [`SlotStatus.Ok`](SlotStatus.md#m-ok). |
| [`Status`](#m-status) | How it ended. |

### <a id="m-detail"></a>Detail

```csharp
public string Detail { get; init; }
```

A sentence for logs when something went wrong.

**Type:** `string`

### <a id="m-info"></a>Info

```csharp
public SlotInfo? Info { get; init; }
```

The slot's description, when it could be read or was written.

**Type:** [`SlotInfo`](SlotInfo.md)?

### <a id="m-load"></a>Load

```csharp
public LoadResult? Load { get; init; }
```

What the save loader found, for loads that reached it.

**Type:** [`LoadResult`](../OneGame.Sync/LoadResult.md)?

### <a id="m-ok"></a>Ok

```csharp
public bool Ok { get; }
```

True when [`SlotResult.Status`](SlotResult.md#m-status) is [`SlotStatus.Ok`](SlotStatus.md#m-ok).

**Type:** `bool`

### <a id="m-status"></a>Status

```csharp
public SlotStatus Status { get; init; }
```

How it ended.

**Type:** [`SlotStatus`](SlotStatus.md)
