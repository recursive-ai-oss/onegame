[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# AudioDeviceFormat struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudioDeviceFormat`](AudioDeviceFormat.md)&gt;

```csharp
public readonly struct AudioDeviceFormat : IEquatable<AudioDeviceFormat>
```

The format an open device runs at.

## Constructors

| Member | Description |
| --- | --- |
| [`AudioDeviceFormat(int, int)`](#m-ctor-int-int) | The format an open device runs at. |

### <a id="m-ctor-int-int"></a>AudioDeviceFormat(int, int)

```csharp
public AudioDeviceFormat(int SampleRate, int Channels);
```

The format an open device runs at.

**Parameters**

- `SampleRate` (`int`): Frames per second.
- `Channels` (`int`): Samples per frame.

## Properties

| Member | Description |
| --- | --- |
| [`Channels`](#m-channels) | Samples per frame. |
| [`SampleRate`](#m-samplerate) | Frames per second. |

### <a id="m-channels"></a>Channels

```csharp
public int Channels { get; init; }
```

Samples per frame.

**Type:** `int`

### <a id="m-samplerate"></a>SampleRate

```csharp
public int SampleRate { get; init; }
```

Frames per second.

**Type:** `int`
