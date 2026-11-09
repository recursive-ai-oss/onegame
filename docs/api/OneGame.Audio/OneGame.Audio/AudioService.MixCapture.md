[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioService.MixCapture class

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Declared in:** [`AudioService`](AudioService.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class AudioService.MixCapture : IDisposable
```

An exclusive, manually stepped recording of an audio service. Dispose to return the timeline to its device.

## Properties

| Member | Description |
| --- | --- |
| [`Recording`](#m-recording) | The samples rendered so far. |

### <a id="m-recording"></a>Recording

```csharp
public CaptureSink Recording { get; }
```

The samples rendered so far. Silence is retained too.

**Type:** [`CaptureSink`](CaptureSink.md)

## Methods

| Member | Description |
| --- | --- |
| [`Advance(int)`](#m-advance-int) | Renders exactly this many sample frames, feeding streams synchronously before each slice. |
| [`Dispose()`](#m-dispose) | Returns timeline ownership to the service's device. |

### <a id="m-advance-int"></a>Advance(int)

```csharp
public void Advance(int frames);
```

Renders exactly this many sample frames, feeding streams synchronously before each slice.

**Parameters**

- `frames` (`int`): Nonnegative sample frame count, within the recording's remaining capacity.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Returns timeline ownership to the service's device. The recording remains readable.
