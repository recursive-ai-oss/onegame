[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# HapticEvent record

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`HapticEvent`](HapticEvent.md)&gt;

```csharp
public sealed record HapticEvent : IEquatable<HapticEvent>
```

One haptic request, as [`Haptics.GetLog()`](Haptics.md#m-getlog) keeps it.

## Constructors

| Member | Description |
| --- | --- |
| [`HapticEvent(long, long, HapticKind, bool)`](#m-ctor-long-long-haptickind-bool) | One haptic request, as [`Haptics.GetLog()`](Haptics.md#m-getlog) keeps it. |

### <a id="m-ctor-long-long-haptickind-bool"></a>HapticEvent(long, long, HapticKind, bool)

```csharp
public HapticEvent(long Sequence, long Frame, HapticKind Kind, bool Delivered);
```

One haptic request, as [`Haptics.GetLog()`](Haptics.md#m-getlog) keeps it.

**Parameters**

- `Sequence` (`long`): Counts up from 1 for the life of the process and never repeats, even after old entries were dropped.
- `Frame` (`long`): The game frame the call was made in (from [`Haptics.FrameClock`](Haptics.md#m-frameclock)), or -1 when no game clock is attached.
- `Kind` ([`HapticKind`](HapticKind.md)): What was asked for.
- `Delivered` (`bool`): True when the request was handed to the phone's system (which still drops it when the user turned touch feedback off); false on desktop and the web, on Android calls from a thread other than the game thread, and while [`Haptics.Enabled`](Haptics.md#m-enabled) is off.

## Properties

| Member | Description |
| --- | --- |
| [`Delivered`](#m-delivered) | True when the request was handed to the phone's system (which still drops it when the user turned touch feedback off); false on desktop and the web, on Android calls from a thread other than the game thread, and while [`Haptics.Enabled`](Haptics.md#m-enabled) is off. |
| [`Frame`](#m-frame) | The game frame the call was made in (from [`Haptics.FrameClock`](Haptics.md#m-frameclock)), or -1 when no game clock is attached. |
| [`Kind`](#m-kind) | What was asked for. |
| [`Sequence`](#m-sequence) | Counts up from 1 for the life of the process and never repeats, even after old entries were dropped. |

### <a id="m-delivered"></a>Delivered

```csharp
public bool Delivered { get; init; }
```

True when the request was handed to the phone's system (which still drops it when the user turned touch feedback off); false on desktop and the web, on Android calls from a thread other than the game thread, and while [`Haptics.Enabled`](Haptics.md#m-enabled) is off.

**Type:** `bool`

### <a id="m-frame"></a>Frame

```csharp
public long Frame { get; init; }
```

The game frame the call was made in (from [`Haptics.FrameClock`](Haptics.md#m-frameclock)), or -1 when no game clock is attached.

**Type:** `long`

### <a id="m-kind"></a>Kind

```csharp
public HapticKind Kind { get; init; }
```

What was asked for.

**Type:** [`HapticKind`](HapticKind.md)

### <a id="m-sequence"></a>Sequence

```csharp
public long Sequence { get; init; }
```

Counts up from 1 for the life of the process and never repeats, even after old entries were dropped.

**Type:** `long`
