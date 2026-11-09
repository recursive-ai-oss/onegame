[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# FrameTimings class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class FrameTimings
```

A short history of CPU frame times, filled by [`GameInstance`](GameInstance.md) only while [`FrameTimings.Enabled`](FrameTimings.md#m-enabled) is set; the debug panel sets it while it is shown. With it off a frame takes no timestamps and stores nothing. The history is a fixed ring: recording allocates nothing.

## Constructors

| Member | Description |
| --- | --- |
| [`FrameTimings()`](#m-ctor) |  |

### <a id="m-ctor"></a>FrameTimings()

```csharp
public FrameTimings();
```

## Fields

| Member | Description |
| --- | --- |
| [`Capacity`](#m-capacity) | How many frames are kept. |

### <a id="m-capacity"></a>Capacity

```csharp
public const int Capacity = 60;
```

How many frames are kept.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | Frames held, up to [`FrameTimings.Capacity`](FrameTimings.md#m-capacity). |
| [`Enabled`](#m-enabled) | Whether frames are being timed. |
| [`Latest`](#m-latest) | The newest frame, or a zeroed value when none was timed. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

Frames held, up to [`FrameTimings.Capacity`](FrameTimings.md#m-capacity).

**Type:** `int`

### <a id="m-enabled"></a>Enabled

```csharp
public bool Enabled { get; set; }
```

Whether frames are being timed. Off by default.

**Type:** `bool`

### <a id="m-latest"></a>Latest

```csharp
public FrameTiming Latest { get; }
```

The newest frame, or a zeroed value when none was timed.

**Type:** [`FrameTiming`](FrameTiming.md)

## Methods

| Member | Description |
| --- | --- |
| [`Clear()`](#m-clear) | Forgets the history. |
| [`CopyTo(Span<FrameTiming>)`](#m-copyto-span-frametiming) | Copies the history, oldest first. |

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Forgets the history.

### <a id="m-copyto-span-frametiming"></a>CopyTo(Span&lt;FrameTiming&gt;)

```csharp
public int CopyTo(Span<FrameTiming> destination);
```

Copies the history, oldest first.

**Parameters**

- `destination` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)&lt;[`FrameTiming`](FrameTiming.md)&gt;): At least [`FrameTimings.Count`](FrameTimings.md#m-count) long.

**Returns:** `int` — How many were written.
