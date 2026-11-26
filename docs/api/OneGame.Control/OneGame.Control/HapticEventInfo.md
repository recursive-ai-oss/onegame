[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# HapticEventInfo record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`HapticEventInfo`](HapticEventInfo.md)&gt;

```csharp
public sealed record HapticEventInfo : IEquatable<HapticEventInfo>
```

One haptic request in [`HapticsResult`](HapticsResult.md).

## Constructors

| Member | Description |
| --- | --- |
| [`HapticEventInfo(long, long, string, bool)`](#m-ctor-long-long-string-bool) | One haptic request in [`HapticsResult`](HapticsResult.md). |

### <a id="m-ctor-long-long-string-bool"></a>HapticEventInfo(long, long, string, bool)

```csharp
public HapticEventInfo(long Sequence, long Frame, string Kind, bool Delivered);
```

One haptic request in [`HapticsResult`](HapticsResult.md).

**Parameters**

- `Sequence` (`long`): Counts up from 1 and never repeats.
- `Frame` (`long`): The game frame of the call.
- `Kind` (`string`): The name of the [`HapticKind`](../../OneGame.Platform/OneGame.Platform/HapticKind.md), such as `Tick`.
- `Delivered` (`bool`): Whether a device took the request (false on desktop and the web).

## Properties

| Member | Description |
| --- | --- |
| [`Delivered`](#m-delivered) | Whether a device took the request (false on desktop and the web). |
| [`Frame`](#m-frame) | The game frame of the call. |
| [`Kind`](#m-kind) | The name of the [`HapticKind`](../../OneGame.Platform/OneGame.Platform/HapticKind.md), such as `Tick`. |
| [`Sequence`](#m-sequence) | Counts up from 1 and never repeats. |

### <a id="m-delivered"></a>Delivered

```csharp
public bool Delivered { get; init; }
```

Whether a device took the request (false on desktop and the web).

**Type:** `bool`

### <a id="m-frame"></a>Frame

```csharp
public long Frame { get; init; }
```

The game frame of the call.

**Type:** `long`

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; init; }
```

The name of the [`HapticKind`](../../OneGame.Platform/OneGame.Platform/HapticKind.md), such as `Tick`.

**Type:** `string`

### <a id="m-sequence"></a>Sequence

```csharp
public long Sequence { get; init; }
```

Counts up from 1 and never repeats.

**Type:** `long`
