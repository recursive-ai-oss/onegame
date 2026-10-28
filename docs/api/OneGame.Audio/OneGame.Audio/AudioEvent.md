[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioEvent struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudioEvent`](AudioEvent.md)&gt;

```csharp
public readonly struct AudioEvent : IEquatable<AudioEvent>
```

One thing that happened on the audio thread.

## Constructors

| Member | Description |
| --- | --- |
| [`AudioEvent(AudioEventKind, VoiceId, long)`](#m-ctor-audioeventkind-voiceid-long) | One thing that happened on the audio thread. |

### <a id="m-ctor-audioeventkind-voiceid-long"></a>AudioEvent(AudioEventKind, VoiceId, long)

```csharp
public AudioEvent(AudioEventKind Kind, VoiceId Voice, long Value);
```

One thing that happened on the audio thread.

**Parameters**

- `Kind` ([`AudioEventKind`](AudioEventKind.md)): What happened.
- `Voice` ([`VoiceId`](VoiceId.md)): The voice it happened to.
- `Value` (`long`): A kind-specific number, such as the frames lost to an underrun.

## Properties

| Member | Description |
| --- | --- |
| [`Kind`](#m-kind) | What happened. |
| [`Value`](#m-value) | A kind-specific number, such as the frames lost to an underrun. |
| [`Voice`](#m-voice) | The voice it happened to. |

### <a id="m-kind"></a>Kind

```csharp
public AudioEventKind Kind { get; init; }
```

What happened.

**Type:** [`AudioEventKind`](AudioEventKind.md)

### <a id="m-value"></a>Value

```csharp
public long Value { get; init; }
```

A kind-specific number, such as the frames lost to an underrun.

**Type:** `long`

### <a id="m-voice"></a>Voice

```csharp
public VoiceId Voice { get; init; }
```

The voice it happened to.

**Type:** [`VoiceId`](VoiceId.md)
