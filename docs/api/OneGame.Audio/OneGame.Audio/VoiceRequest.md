[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# VoiceRequest struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`VoiceRequest`](VoiceRequest.md)&gt;

```csharp
public readonly struct VoiceRequest : IEquatable<VoiceRequest>
```

A request to start one voice.

## Constructors

| Member | Description |
| --- | --- |
| [`VoiceRequest()`](#m-ctor) | Creates a request with unit gain, centred, on bus 0. |

### <a id="m-ctor"></a>VoiceRequest()

```csharp
public VoiceRequest();
```

Creates a request with unit gain, centred, on bus 0.

## Properties

| Member | Description |
| --- | --- |
| [`Bus`](#m-bus) | The bus index the voice is summed into. |
| [`FadeInFrames`](#m-fadeinframes) | Frames over which the voice fades in from silence. |
| [`Gain`](#m-gain) | Linear gain. |
| [`Loop`](#m-loop) | Whether to restart at the end. |
| [`Pan`](#m-pan) | Stereo position, -1 (left) to 1 (right). |
| [`Pitch`](#m-pitch) | Playback speed and pitch multiplier; 1 is the source's own. |
| [`Priority`](#m-priority) | Higher priorities steal voices from lower ones when all voices are busy. |
| [`Source`](#m-source) | The source object obtained from the core's [`IAudioCore.LoadClip()`](IAudioCore.md#m-loadclip-stream) or [`IAudioCore.OpenStream()`](IAudioCore.md#m-openstream-stream-bool). |
| [`StartAtFrame`](#m-startatframe) | The frame on the core's timeline to start at (0 = as soon as possible). |
| [`VariablePitch`](#m-variablepitch) | Reserve what the core needs to change the pitch later with [`IAudioCore.SetPitch()`](IAudioCore.md#m-setpitch-voiceid-float-int). |

### <a id="m-bus"></a>Bus

```csharp
public int Bus { get; init; }
```

The bus index the voice is summed into.

**Type:** `int`

### <a id="m-fadeinframes"></a>FadeInFrames

```csharp
public int FadeInFrames { get; init; }
```

Frames over which the voice fades in from silence.

**Type:** `int`

### <a id="m-gain"></a>Gain

```csharp
public float Gain { get; init; }
```

Linear gain.

**Type:** `float`

### <a id="m-loop"></a>Loop

```csharp
public bool Loop { get; init; }
```

Whether to restart at the end.

**Type:** `bool`

### <a id="m-pan"></a>Pan

```csharp
public float Pan { get; init; }
```

Stereo position, -1 (left) to 1 (right).

**Type:** `float`

### <a id="m-pitch"></a>Pitch

```csharp
public float Pitch { get; init; }
```

Playback speed and pitch multiplier; 1 is the source's own.

**Type:** `float`

### <a id="m-priority"></a>Priority

```csharp
public int Priority { get; init; }
```

Higher priorities steal voices from lower ones when all voices are busy.

**Type:** `int`

### <a id="m-source"></a>Source

```csharp
public object? Source { get; init; }
```

The source object obtained from the core's [`IAudioCore.LoadClip()`](IAudioCore.md#m-loadclip-stream) or [`IAudioCore.OpenStream()`](IAudioCore.md#m-openstream-stream-bool).

**Type:** `object?`

### <a id="m-startatframe"></a>StartAtFrame

```csharp
public long StartAtFrame { get; init; }
```

The frame on the core's timeline to start at (0 = as soon as possible). Frame-exact.

**Type:** `long`

### <a id="m-variablepitch"></a>VariablePitch

```csharp
public bool VariablePitch { get; init; }
```

Reserve what the core needs to change the pitch later with [`IAudioCore.SetPitch()`](IAudioCore.md#m-setpitch-voiceid-float-int).

**Type:** `bool`
