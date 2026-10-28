[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# IPumpedSink interface

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public interface IPumpedSink
```

Implemented by sinks of pumped backends, where the host calls [`AudioOutput.Pump()`](AudioOutput.md#m-pump) on its own schedule instead of a thread blocking in [`IAudioSink.Write()`](IAudioSink.md#m-write-readonlyspan-float).

## Properties

| Member | Description |
| --- | --- |
| [`WantedFrames`](#m-wantedframes) | How many frames to render now to keep the device fed, or 0. |

### <a id="m-wantedframes"></a>WantedFrames

```csharp
int WantedFrames { get; }
```

How many frames to render now to keep the device fed, or 0.

**Type:** `int`
