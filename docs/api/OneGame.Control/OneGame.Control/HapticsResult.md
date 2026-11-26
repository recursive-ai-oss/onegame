[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# HapticsResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`HapticsResult`](HapticsResult.md)&gt;

```csharp
public sealed record HapticsResult : IEquatable<HapticsResult>
```

Result of `haptics`.

## Constructors

| Member | Description |
| --- | --- |
| [`HapticsResult(long, long, bool, HapticEventInfo[])`](#m-ctor-long-long-bool-hapticeventinfo) | Result of `haptics`. |

### <a id="m-ctor-long-long-bool-hapticeventinfo"></a>HapticsResult(long, long, bool, HapticEventInfo\[\])

```csharp
public HapticsResult(long LastSequence, long Frame, bool Enabled, HapticEventInfo[] Events);
```

Result of `haptics`.

**Parameters**

- `LastSequence` (`long`): The sequence number of the newest request ever logged (0 before the first), also when none is returned.
- `Frame` (`long`): The game's frame counter now.
- `Enabled` (`bool`): Whether [`Haptics.Enabled`](../../OneGame.Platform/OneGame.Platform/Haptics.md#m-enabled) is on.
- `Events` ([`HapticEventInfo`](HapticEventInfo.md)\[\]): The requests after `since`, oldest first.

## Properties

| Member | Description |
| --- | --- |
| [`Enabled`](#m-enabled) | Whether [`Haptics.Enabled`](../../OneGame.Platform/OneGame.Platform/Haptics.md#m-enabled) is on. |
| [`Events`](#m-events) | The requests after `since`, oldest first. |
| [`Frame`](#m-frame) | The game's frame counter now. |
| [`LastSequence`](#m-lastsequence) | The sequence number of the newest request ever logged (0 before the first), also when none is returned. |

### <a id="m-enabled"></a>Enabled

```csharp
public bool Enabled { get; init; }
```

Whether [`Haptics.Enabled`](../../OneGame.Platform/OneGame.Platform/Haptics.md#m-enabled) is on.

**Type:** `bool`

### <a id="m-events"></a>Events

```csharp
public HapticEventInfo[] Events { get; init; }
```

The requests after `since`, oldest first.

**Type:** [`HapticEventInfo`](HapticEventInfo.md)\[\]

### <a id="m-frame"></a>Frame

```csharp
public long Frame { get; init; }
```

The game's frame counter now.

**Type:** `long`

### <a id="m-lastsequence"></a>LastSequence

```csharp
public long LastSequence { get; init; }
```

The sequence number of the newest request ever logged (0 before the first), also when none is returned.

**Type:** `long`
