[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# MusicStatus record

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`MusicStatus`](MusicStatus.md)&gt;

```csharp
public sealed record MusicStatus : IEquatable<MusicStatus>
```

The playing music track.

## Constructors

| Member | Description |
| --- | --- |
| [`MusicStatus(double, double?)`](#m-ctor-double-double) | The playing music track. |

### <a id="m-ctor-double-double"></a>MusicStatus(double, double?)

```csharp
public MusicStatus(double PositionSeconds, double? LengthSeconds);
```

The playing music track.

**Parameters**

- `PositionSeconds` (`double`): Seconds into the track.
- `LengthSeconds` (`double?`): Its length, or null when unknown.

## Properties

| Member | Description |
| --- | --- |
| [`LengthSeconds`](#m-lengthseconds) | Its length, or null when unknown. |
| [`PositionSeconds`](#m-positionseconds) | Seconds into the track. |

### <a id="m-lengthseconds"></a>LengthSeconds

```csharp
public double? LengthSeconds { get; init; }
```

Its length, or null when unknown.

**Type:** `double?`

### <a id="m-positionseconds"></a>PositionSeconds

```csharp
public double PositionSeconds { get; init; }
```

Seconds into the track.

**Type:** `double`
