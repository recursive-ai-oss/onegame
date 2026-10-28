[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# PlayedSound record

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PlayedSound`](PlayedSound.md)&gt;

```csharp
public sealed record PlayedSound : IEquatable<PlayedSound>
```

One sound the game asked the service to start, as the event log keeps it.

## Constructors

| Member | Description |
| --- | --- |
| [`PlayedSound(long, long, string, int, float, bool, bool, bool)`](#m-ctor-long-long-string-int-float-bool-bool-bool) | One sound the game asked the service to start, as the event log keeps it. |

### <a id="m-ctor-long-long-string-int-float-bool-bool-bool"></a>PlayedSound(long, long, string, int, float, bool, bool, bool)

```csharp
public PlayedSound(long Sequence, long Frame, string Sound, int Bus, float Volume, bool Loop, bool Music, bool Started);
```

One sound the game asked the service to start, as the event log keeps it.

**Parameters**

- `Sequence` (`long`): Counts up from 1 for the life of the service and never repeats, even after old entries were dropped.
- `Frame` (`long`): The game frame the sound was requested in (from [`AudioService.FrameClock`](AudioService.md#m-frameclock)), or -1 when no game clock is attached.
- `Sound` (`string`): The name the clip was loaded under ([`AudioService.LoadSound()`](AudioService.md#m-loadsound-stream-string)), or the name given to [`AudioService.PlayMusic()`](AudioService.md#m-playmusic-stream-musicoptions-string).
- `Bus` (`int`): The bus index it plays on.
- `Volume` (`float`): The requested linear volume.
- `Loop` (`bool`): Whether it loops.
- `Music` (`bool`): True for a music track.
- `Started` (`bool`): False when the service dropped the request (command queue full).

## Properties

| Member | Description |
| --- | --- |
| [`Bus`](#m-bus) | The bus index it plays on. |
| [`Frame`](#m-frame) | The game frame the sound was requested in (from [`AudioService.FrameClock`](AudioService.md#m-frameclock)), or -1 when no game clock is attached. |
| [`Loop`](#m-loop) | Whether it loops. |
| [`Music`](#m-music) | True for a music track. |
| [`Sequence`](#m-sequence) | Counts up from 1 for the life of the service and never repeats, even after old entries were dropped. |
| [`Sound`](#m-sound) | The name the clip was loaded under ([`AudioService.LoadSound()`](AudioService.md#m-loadsound-stream-string)), or the name given to [`AudioService.PlayMusic()`](AudioService.md#m-playmusic-stream-musicoptions-string). |
| [`Started`](#m-started) | False when the service dropped the request (command queue full). |
| [`Volume`](#m-volume) | The requested linear volume. |

### <a id="m-bus"></a>Bus

```csharp
public int Bus { get; init; }
```

The bus index it plays on.

**Type:** `int`

### <a id="m-frame"></a>Frame

```csharp
public long Frame { get; init; }
```

The game frame the sound was requested in (from [`AudioService.FrameClock`](AudioService.md#m-frameclock)), or -1 when no game clock is attached.

**Type:** `long`

### <a id="m-loop"></a>Loop

```csharp
public bool Loop { get; init; }
```

Whether it loops.

**Type:** `bool`

### <a id="m-music"></a>Music

```csharp
public bool Music { get; init; }
```

True for a music track.

**Type:** `bool`

### <a id="m-sequence"></a>Sequence

```csharp
public long Sequence { get; init; }
```

Counts up from 1 for the life of the service and never repeats, even after old entries were dropped.

**Type:** `long`

### <a id="m-sound"></a>Sound

```csharp
public string Sound { get; init; }
```

The name the clip was loaded under ([`AudioService.LoadSound()`](AudioService.md#m-loadsound-stream-string)), or the name given to [`AudioService.PlayMusic()`](AudioService.md#m-playmusic-stream-musicoptions-string).

**Type:** `string`

### <a id="m-started"></a>Started

```csharp
public bool Started { get; init; }
```

False when the service dropped the request (command queue full).

**Type:** `bool`

### <a id="m-volume"></a>Volume

```csharp
public float Volume { get; init; }
```

The requested linear volume.

**Type:** `float`
