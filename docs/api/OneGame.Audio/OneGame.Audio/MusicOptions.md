[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# MusicOptions struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`MusicOptions`](MusicOptions.md)&gt;

```csharp
public readonly struct MusicOptions : IEquatable<MusicOptions>
```

How to start a music track.

## Constructors

| Member | Description |
| --- | --- |
| [`MusicOptions()`](#m-ctor) | Creates options for a looping track at full volume that cuts in. |

### <a id="m-ctor"></a>MusicOptions()

```csharp
public MusicOptions();
```

Creates options for a looping track at full volume that cuts in.

## Properties

| Member | Description |
| --- | --- |
| [`CrossfadeSeconds`](#m-crossfadeseconds) | Seconds over which the previous track fades out and this one fades in. |
| [`Loop`](#m-loop) | Whether the track repeats. |
| [`Volume`](#m-volume) | Linear volume of the track. |

### <a id="m-crossfadeseconds"></a>CrossfadeSeconds

```csharp
public double CrossfadeSeconds { get; init; }
```

Seconds over which the previous track fades out and this one fades in. 0 cuts. Default 0.

**Type:** `double`

### <a id="m-loop"></a>Loop

```csharp
public bool Loop { get; init; }
```

Whether the track repeats. Default true.

**Type:** `bool`

### <a id="m-volume"></a>Volume

```csharp
public float Volume { get; init; }
```

Linear volume of the track. Default 1.

**Type:** `float`
