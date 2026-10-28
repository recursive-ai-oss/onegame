[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# CaptureSink class

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IAudioSink`](IAudioSink.md), [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class CaptureSink : IAudioSink, IDisposable
```

The recording kept by [`CaptureAudioBackend`](CaptureAudioBackend.md): interleaved float samples after the limiter.

## Fields

| Member | Description |
| --- | --- |
| [`MaxFrames`](#m-maxframes) | The most frames a capture keeps (ten minutes at 48 kHz); further frames are counted in [`CaptureSink.DroppedFrames`](CaptureSink.md#m-droppedframes) but not stored. |

### <a id="m-maxframes"></a>MaxFrames

```csharp
public const long MaxFrames = 28800000;
```

The most frames a capture keeps (ten minutes at 48 kHz); further frames are counted in [`CaptureSink.DroppedFrames`](CaptureSink.md#m-droppedframes) but not stored.

**Type:** `long`

## Properties

| Member | Description |
| --- | --- |
| [`BufferFrames`](#m-bufferframes) | The device's total buffer in frames. |
| [`DroppedFrames`](#m-droppedframes) | Frames discarded because the capture reached [`CaptureSink.MaxFrames`](CaptureSink.md#m-maxframes). |
| [`Format`](#m-format) | The negotiated format. |
| [`FramesCaptured`](#m-framescaptured) | Frames captured so far. |
| [`Peak`](#m-peak) | The largest absolute sample captured. |
| [`PeriodFrames`](#m-periodframes) | The device's natural write size in frames. |
| [`QueuedFrames`](#m-queuedframes) | The newest measure of audio queued ahead of the speaker, in frames. |
| [`Samples`](#m-samples) | The captured interleaved samples. |

### <a id="m-bufferframes"></a>BufferFrames

```csharp
public int BufferFrames { get; }
```

The device's total buffer in frames.

**Type:** `int`

### <a id="m-droppedframes"></a>DroppedFrames

```csharp
public long DroppedFrames { get; }
```

Frames discarded because the capture reached [`CaptureSink.MaxFrames`](CaptureSink.md#m-maxframes).

**Type:** `long`

### <a id="m-format"></a>Format

```csharp
public AudioDeviceFormat Format { get; }
```

The negotiated format.

**Type:** [`AudioDeviceFormat`](AudioDeviceFormat.md)

### <a id="m-framescaptured"></a>FramesCaptured

```csharp
public int FramesCaptured { get; }
```

Frames captured so far.

**Type:** `int`

### <a id="m-peak"></a>Peak

```csharp
public float Peak { get; }
```

The largest absolute sample captured.

**Type:** `float`

### <a id="m-periodframes"></a>PeriodFrames

```csharp
public int PeriodFrames { get; }
```

The device's natural write size in frames.

**Type:** `int`

### <a id="m-queuedframes"></a>QueuedFrames

```csharp
public int QueuedFrames { get; }
```

The newest measure of audio queued ahead of the speaker, in frames.

**Type:** `int`

### <a id="m-samples"></a>Samples

```csharp
public ReadOnlySpan<float> Samples { get; }
```

The captured interleaved samples.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<float>`

## Methods

| Member | Description |
| --- | --- |
| [`Clear()`](#m-clear) | Discards everything captured so far. |
| [`Dispose()`](#m-dispose) |  |
| [`Write(ReadOnlySpan<float>)`](#m-write-readonlyspan-float) | Hands interleaved samples (a whole number of frames, already limited to -1..1) to the device and returns when they are queued. |
| [`WriteWav(Stream)`](#m-writewav-stream) | Writes the capture as a 32-bit float WAV file, so it can be opened or diffed with ordinary audio tools. |

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Discards everything captured so far.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

### <a id="m-write-readonlyspan-float"></a>Write(ReadOnlySpan&lt;float&gt;)

```csharp
public SinkWriteResult Write(ReadOnlySpan<float> interleaved);
```

Hands interleaved samples (a whole number of frames, already limited to -1..1) to the device and returns when they are queued. Blocks while the device buffer is full. Must not allocate and must not throw.

**Parameters**

- `interleaved` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<float>`)

**Returns:** [`SinkWriteResult`](SinkWriteResult.md)

### <a id="m-writewav-stream"></a>WriteWav(Stream)

```csharp
public void WriteWav(Stream destination);
```

Writes the capture as a 32-bit float WAV file, so it can be opened or diffed with ordinary audio tools.

**Parameters**

- `destination` ([`Stream`](https://learn.microsoft.com/dotnet/api/system.io.stream)): The stream to write to.
