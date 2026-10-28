[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# IAudioRenderSource interface

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public interface IAudioRenderSource
```

Anything that produces interleaved float frames on demand; the shape of a mixer's render call.

## Properties

| Member | Description |
| --- | --- |
| [`Channels`](#m-channels) | Samples per frame. |
| [`SampleRate`](#m-samplerate) | Frames per second the source renders at. |

### <a id="m-channels"></a>Channels

```csharp
int Channels { get; }
```

Samples per frame.

**Type:** `int`

### <a id="m-samplerate"></a>SampleRate

```csharp
int SampleRate { get; }
```

Frames per second the source renders at.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Render(Span<float>)`](#m-render-span-float) | Fills `interleaved` (a whole number of frames) on the audio thread. |

### <a id="m-render-span-float"></a>Render(Span&lt;float&gt;)

```csharp
void Render(Span<float> interleaved);
```

Fills `interleaved` (a whole number of frames) on the audio thread. Overwrites the span; may exceed -1..1.

**Parameters**

- `interleaved` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<float>`)
