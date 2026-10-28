[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# StreamPosition struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`StreamPosition`](StreamPosition.md)&gt;

```csharp
public readonly struct StreamPosition : IEquatable<StreamPosition>
```

Where a streamed track is.

## Constructors

| Member | Description |
| --- | --- |
| [`StreamPosition(double, double)`](#m-ctor-double-double) | Where a streamed track is. |

### <a id="m-ctor-double-double"></a>StreamPosition(double, double)

```csharp
public StreamPosition(double PositionSeconds, double LengthSeconds);
```

Where a streamed track is.

**Parameters**

- `PositionSeconds` (`double`): Seconds into the track; wraps with a loop.
- `LengthSeconds` (`double`): The track's length, or a negative number when the format can't say.

## Properties

| Member | Description |
| --- | --- |
| [`LengthSeconds`](#m-lengthseconds) | The track's length, or a negative number when the format can't say. |
| [`PositionSeconds`](#m-positionseconds) | Seconds into the track; wraps with a loop. |

### <a id="m-lengthseconds"></a>LengthSeconds

```csharp
public double LengthSeconds { get; init; }
```

The track's length, or a negative number when the format can't say.

**Type:** `double`

### <a id="m-positionseconds"></a>PositionSeconds

```csharp
public double PositionSeconds { get; init; }
```

Seconds into the track; wraps with a loop.

**Type:** `double`
