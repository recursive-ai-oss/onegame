[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# PlayOptions struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PlayOptions`](PlayOptions.md)&gt;

```csharp
public readonly struct PlayOptions : IEquatable<PlayOptions>
```

How to play a [`Sound`](Sound.md).

## Constructors

| Member | Description |
| --- | --- |
| [`PlayOptions()`](#m-ctor) | Creates options for a plain one-shot on the sfx bus. |

### <a id="m-ctor"></a>PlayOptions()

```csharp
public PlayOptions();
```

Creates options for a plain one-shot on the sfx bus.

## Properties

| Member | Description |
| --- | --- |
| [`Bus`](#m-bus) | The bus to play on. |
| [`FadeInSeconds`](#m-fadeinseconds) | Seconds to fade in from silence. |
| [`Loop`](#m-loop) | Whether to restart at the end. |
| [`Pan`](#m-pan) | Stereo position, -1 (left) to 1 (right). |
| [`Pitch`](#m-pitch) | Speed and pitch multiplier, 0.0625 to 16 (a clip's rate times this must stay within the mixer's conversion range). |
| [`Priority`](#m-priority) | Higher priorities win when all voices are busy. |
| [`StartAtFrame`](#m-startatframe) | The audio-timeline frame to start at, for sounds that must land on a given frame; 0 plays as soon as possible. |
| [`VariablePitch`](#m-variablepitch) | Set to change the pitch later with [`AudioService.SetPitch()`](AudioService.md#m-setpitch-voiceid-float-double); a sound that starts at pitch 1 ignores later changes otherwise. |
| [`Volume`](#m-volume) | Linear volume, 0 and up. |

### <a id="m-bus"></a>Bus

```csharp
public AudioBus Bus { get; init; }
```

The bus to play on. Default [`AudioBus.Sfx`](AudioBus.md#m-sfx).

**Type:** [`AudioBus`](AudioBus.md)

### <a id="m-fadeinseconds"></a>FadeInSeconds

```csharp
public double FadeInSeconds { get; init; }
```

Seconds to fade in from silence. Default 0.

**Type:** `double`

### <a id="m-loop"></a>Loop

```csharp
public bool Loop { get; init; }
```

Whether to restart at the end. Default false.

**Type:** `bool`

### <a id="m-pan"></a>Pan

```csharp
public float Pan { get; init; }
```

Stereo position, -1 (left) to 1 (right). Default 0.

**Type:** `float`

### <a id="m-pitch"></a>Pitch

```csharp
public float Pitch { get; init; }
```

Speed and pitch multiplier, 0.0625 to 16 (a clip's rate times this must stay within the mixer's conversion range). Default 1.

**Type:** `float`

### <a id="m-priority"></a>Priority

```csharp
public int Priority { get; init; }
```

Higher priorities win when all voices are busy. Default 0.

**Type:** `int`

### <a id="m-startatframe"></a>StartAtFrame

```csharp
public long StartAtFrame { get; init; }
```

The audio-timeline frame to start at, for sounds that must land on a given frame; 0 plays as soon as possible. Default 0.

**Type:** `long`

### <a id="m-variablepitch"></a>VariablePitch

```csharp
public bool VariablePitch { get; init; }
```

Set to change the pitch later with [`AudioService.SetPitch()`](AudioService.md#m-setpitch-voiceid-float-double); a sound that starts at pitch 1 ignores later changes otherwise. Default false.

**Type:** `bool`

### <a id="m-volume"></a>Volume

```csharp
public float Volume { get; init; }
```

Linear volume, 0 and up. Default 1.

**Type:** `float`
