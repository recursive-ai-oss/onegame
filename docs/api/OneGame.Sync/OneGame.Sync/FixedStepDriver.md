[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# FixedStepDriver class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class FixedStepDriver
```

Turns variable frame times into a fixed number of simulation ticks. It does exact integer arithmetic on the time it is given, so N ticks come out of N/rate seconds however the frames are cut, and it never reads a clock itself: the host passes the elapsed time.

## Constructors

| Member | Description |
| --- | --- |
| [`FixedStepDriver(int, int)`](#m-ctor-int-int) | Creates a driver. |

### <a id="m-ctor-int-int"></a>FixedStepDriver(int, int)

```csharp
public FixedStepDriver(int tickRate = 60, int maxStepsPerFrame = 5);
```

Creates a driver.

**Parameters**

- `tickRate` (`int`): Simulation ticks per second, 1 to 1000.
- `maxStepsPerFrame` (`int`): Most ticks one [`FixedStepDriver.Advance()`](FixedStepDriver.md#m-advance-timespan) returns; time beyond that is dropped so a stall cannot snowball.

## Properties

| Member | Description |
| --- | --- |
| [`Alpha`](#m-alpha) | How far between the last tick and the next the frame is, from 0 to just under 1. |
| [`DroppedTicks`](#m-droppedticks) | Ticks of time discarded by the catch-up cap. |
| [`MaxStepsPerFrame`](#m-maxstepsperframe) | The catch-up cap. |
| [`Paused`](#m-paused) | While true, [`FixedStepDriver.Advance()`](FixedStepDriver.md#m-advance-timespan) returns 0 (or 1 after [`FixedStepDriver.RequestStep()`](FixedStepDriver.md#m-requeststep)) and discards the time. |
| [`TickRate`](#m-tickrate) | Ticks per second. |
| [`TimeScale`](#m-timescale) | Speed multiplier, 0 to 100, in steps of 0.001. |
| [`TotalSteps`](#m-totalsteps) | Ticks returned so far. |

### <a id="m-alpha"></a>Alpha

```csharp
public double Alpha { get; }
```

How far between the last tick and the next the frame is, from 0 to just under 1. Views interpolate with it.

**Type:** `double`

### <a id="m-droppedticks"></a>DroppedTicks

```csharp
public long DroppedTicks { get; }
```

Ticks of time discarded by the catch-up cap.

**Type:** `long`

### <a id="m-maxstepsperframe"></a>MaxStepsPerFrame

```csharp
public int MaxStepsPerFrame { get; }
```

The catch-up cap.

**Type:** `int`

### <a id="m-paused"></a>Paused

```csharp
public bool Paused { get; set; }
```

While true, [`FixedStepDriver.Advance()`](FixedStepDriver.md#m-advance-timespan) returns 0 (or 1 after [`FixedStepDriver.RequestStep()`](FixedStepDriver.md#m-requeststep)) and discards the time.

**Type:** `bool`

### <a id="m-tickrate"></a>TickRate

```csharp
public int TickRate { get; }
```

Ticks per second.

**Type:** `int`

### <a id="m-timescale"></a>TimeScale

```csharp
public double TimeScale { get; set; }
```

Speed multiplier, 0 to 100, in steps of 0.001. 1 is real time.

**Type:** `double`

### <a id="m-totalsteps"></a>TotalSteps

```csharp
public long TotalSteps { get; }
```

Ticks returned so far.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`Advance(TimeSpan)`](#m-advance-timespan) | Takes the time since the last call and returns how many ticks to run now. |
| [`RequestStep()`](#m-requeststep) | While paused, lets the next [`FixedStepDriver.Advance()`](FixedStepDriver.md#m-advance-timespan) return one tick. |

### <a id="m-advance-timespan"></a>Advance(TimeSpan)

```csharp
public int Advance(TimeSpan frameTime);
```

Takes the time since the last call and returns how many ticks to run now.

**Parameters**

- `frameTime` ([`TimeSpan`](https://learn.microsoft.com/dotnet/api/system.timespan)): The elapsed time; negative values count as zero.

**Returns:** `int`

### <a id="m-requeststep"></a>RequestStep()

```csharp
public void RequestStep();
```

While paused, lets the next [`FixedStepDriver.Advance()`](FixedStepDriver.md#m-advance-timespan) return one tick.
