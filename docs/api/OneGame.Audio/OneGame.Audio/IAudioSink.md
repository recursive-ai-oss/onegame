[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# IAudioSink interface

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public interface IAudioSink : IDisposable
```

Where the output stage's final float samples go. Implemented per operating system, by the capture sink, and by fakes in tests.

## Properties

| Member | Description |
| --- | --- |
| [`BufferFrames`](#m-bufferframes) | The device's total buffer in frames. |
| [`Format`](#m-format) | The negotiated format. |
| [`PeriodFrames`](#m-periodframes) | The device's natural write size in frames. |
| [`QueuedFrames`](#m-queuedframes) | The newest measure of audio queued ahead of the speaker, in frames. |

### <a id="m-bufferframes"></a>BufferFrames

```csharp
int BufferFrames { get; }
```

The device's total buffer in frames.

**Type:** `int`

### <a id="m-format"></a>Format

```csharp
AudioDeviceFormat Format { get; }
```

The negotiated format.

**Type:** [`AudioDeviceFormat`](AudioDeviceFormat.md)

### <a id="m-periodframes"></a>PeriodFrames

```csharp
int PeriodFrames { get; }
```

The device's natural write size in frames.

**Type:** `int`

### <a id="m-queuedframes"></a>QueuedFrames

```csharp
int QueuedFrames { get; }
```

The newest measure of audio queued ahead of the speaker, in frames.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Write(ReadOnlySpan<float>)`](#m-write-readonlyspan-float) | Hands interleaved samples (a whole number of frames, already limited to -1..1) to the device and returns when they are queued. |

### <a id="m-write-readonlyspan-float"></a>Write(ReadOnlySpan&lt;float&gt;)

```csharp
SinkWriteResult Write(ReadOnlySpan<float> interleaved);
```

Hands interleaved samples (a whole number of frames, already limited to -1..1) to the device and returns when they are queued. Blocks while the device buffer is full. Must not allocate and must not throw.

**Parameters**

- `interleaved` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<float>`)

**Returns:** [`SinkWriteResult`](SinkWriteResult.md)
