[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# AudioEventsResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudioEventsResult`](AudioEventsResult.md)&gt;

```csharp
public sealed record AudioEventsResult : IEquatable<AudioEventsResult>
```

Result of `audio_events`.

## Constructors

| Member | Description |
| --- | --- |
| [`AudioEventsResult(long, long, PlayedSound[])`](#m-ctor-long-long-playedsound) | Result of `audio_events`. |

### <a id="m-ctor-long-long-playedsound"></a>AudioEventsResult(long, long, PlayedSound\[\])

```csharp
public AudioEventsResult(long LastSequence, long Frame, PlayedSound[] Events);
```

Result of `audio_events`.

**Parameters**

- `LastSequence` (`long`): The sequence number of the newest event ever logged (0 before the first), also when none is returned.
- `Frame` (`long`): The game's frame counter now.
- `Events` ([`PlayedSound`](../../OneGame.Audio/OneGame.Audio/PlayedSound.md)\[\]): The sounds requested after `since`, oldest first.

## Properties

| Member | Description |
| --- | --- |
| [`Events`](#m-events) | The sounds requested after `since`, oldest first. |
| [`Frame`](#m-frame) | The game's frame counter now. |
| [`LastSequence`](#m-lastsequence) | The sequence number of the newest event ever logged (0 before the first), also when none is returned. |

### <a id="m-events"></a>Events

```csharp
public PlayedSound[] Events { get; init; }
```

The sounds requested after `since`, oldest first.

**Type:** [`PlayedSound`](../../OneGame.Audio/OneGame.Audio/PlayedSound.md)\[\]

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

The sequence number of the newest event ever logged (0 before the first), also when none is returned.

**Type:** `long`
