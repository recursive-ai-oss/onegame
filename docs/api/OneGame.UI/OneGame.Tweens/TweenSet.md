[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.Tweens

# TweenSet class

**Namespace:** `OneGame.Tweens`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public sealed class TweenSet
```

A set of tweens that all run on one clock. Games and the UI share one: `game.Tweens` is the set the UI's style transitions run in, and the host advances it once per frame by the view time that passed. A tween changes values and calls back; it never touches the simulation, so replays and state hashes are the same with or without tweens.

Tweens are pooled: starting and running them allocates nothing once the pool is warm (the delegate you pass is yours to keep alive). Every tween starts on the next [`TweenSet.Advance()`](TweenSet.md#m-advance-double). A tween's handle is a small struct that stops working when the tween ends or is cancelled.

## Constructors

| Member | Description |
| --- | --- |
| [`TweenSet()`](#m-ctor) |  |

### <a id="m-ctor"></a>TweenSet()

```csharp
public TweenSet();
```

## Properties

| Member | Description |
| --- | --- |
| [`ActiveCount`](#m-activecount) | How many top-level tweens are running or waiting to start. |
| [`Paused`](#m-paused) | True stops the whole set: [`TweenSet.Advance()`](TweenSet.md#m-advance-double) does nothing until it is false again. |
| [`Time`](#m-time) | Seconds of view time this set has been advanced by (scaled by [`TweenSet.TimeScale`](TweenSet.md#m-timescale), not counting paused time). |
| [`TimeScale`](#m-timescale) | Multiplies every [`TweenSet.Advance()`](TweenSet.md#m-advance-double): 0.5 is slow motion. |

### <a id="m-activecount"></a>ActiveCount

```csharp
public int ActiveCount { get; }
```

How many top-level tweens are running or waiting to start.

**Type:** `int`

### <a id="m-paused"></a>Paused

```csharp
public bool Paused { get; set; }
```

True stops the whole set: [`TweenSet.Advance()`](TweenSet.md#m-advance-double) does nothing until it is false again.

**Type:** `bool`

### <a id="m-time"></a>Time

```csharp
public double Time { get; }
```

Seconds of view time this set has been advanced by (scaled by [`TweenSet.TimeScale`](TweenSet.md#m-timescale), not counting paused time).

**Type:** `double`

### <a id="m-timescale"></a>TimeScale

```csharp
public float TimeScale { get; set; }
```

Multiplies every [`TweenSet.Advance()`](TweenSet.md#m-advance-double): 0.5 is slow motion. Not negative.

**Type:** `float`

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The value is negative or not finite.

## Methods

| Member | Description |
| --- | --- |
| [`Advance(double)`](#m-advance-double) | Moves every tween forward by `seconds` (times [`TweenSet.TimeScale`](TweenSet.md#m-timescale)), applies their values, then runs the completion callbacks of the tweens that finished. |
| [`Call(Action)`](#m-call-action) | Calls `action` when its turn comes: at once on the next advance, or in the middle of a [`TweenSet.Sequence()`](TweenSet.md#m-sequence-readonlyspan-tween). |
| [`CancelAll()`](#m-cancelall) | Stops every tween where it is, without callbacks. |
| [`Color(Color, Color, float, Action<Color>)`](#m-color-color-color-float-action-color) | Moves a color between two values, blending the way CSS does (premultiplied alpha, in sRGB). |
| [`Custom(ITweenTarget, float)`](#m-custom-itweentarget-float) | Drives a [`ITweenTarget`](ITweenTarget.md) with eased progress from 0 to 1. |
| [`Delay(float)`](#m-delay-float) | Does nothing for `seconds`: a pause inside a [`TweenSet.Sequence()`](TweenSet.md#m-sequence-readonlyspan-tween). |
| [`Number(float, float, float, Action<float>)`](#m-number-float-float-float-action-float) | Moves a number from `from` to `to`, calling `apply` with each value. |
| [`Parallel(ReadOnlySpan<Tween>)`](#m-parallel-readonlyspan-tween) | Plays tweens at the same time. |
| [`Sequence(ReadOnlySpan<Tween>)`](#m-sequence-readonlyspan-tween) | Plays tweens one after another. |
| [`Snapshot(List<TweenInfo>)`](#m-snapshot-list-tweeninfo) | Lists the running top-level tweens, oldest first, into `into` (which is cleared first). |
| [`Vector2(Vector2, Vector2, float, Action<Vector2>)`](#m-vector2-vector2-vector2-float-action-vector2) | Moves a 2D vector (a position, a size) between two values. |
| [`Vector3(Vector3, Vector3, float, Action<Vector3>)`](#m-vector3-vector3-vector3-float-action-vector3) | Moves a 3D vector between two values. |
| [`Vector4(Vector4, Vector4, float, Action<Vector4>)`](#m-vector4-vector4-vector4-float-action-vector4) | Moves a 4D vector between two values. |

### <a id="m-advance-double"></a>Advance(double)

```csharp
public void Advance(double seconds);
```

Moves every tween forward by `seconds` (times [`TweenSet.TimeScale`](TweenSet.md#m-timescale)), applies their values, then runs the completion callbacks of the tweens that finished. The host calls this once per frame; a test or the harness may call it with exact steps.

**Parameters**

- `seconds` (`double`): View time that passed; zero or more.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The value is negative or not finite.
- [`TweenException`](TweenException.md): Called from inside a value callback of this set.

### <a id="m-call-action"></a>Call(Action)

```csharp
public Tween Call(Action action);
```

Calls `action` when its turn comes: at once on the next advance, or in the middle of a [`TweenSet.Sequence()`](TweenSet.md#m-sequence-readonlyspan-tween).

**Parameters**

- `action` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action)): What to run. It runs after the frame's values are applied, so it may start or cancel tweens.

**Returns:** [`Tween`](Tween.md)

### <a id="m-cancelall"></a>CancelAll()

```csharp
public void CancelAll();
```

Stops every tween where it is, without callbacks. Handles stop working.

### <a id="m-color-color-color-float-action-color"></a>Color(Color, Color, float, Action&lt;Color&gt;)

```csharp
public Tween Color(Color from, Color to, float seconds, Action<Color> apply);
```

Moves a color between two values, blending the way CSS does (premultiplied alpha, in sRGB).

**Parameters**

- `from` ([`Color`](../OneGame.UI/Color.md)): The value at the start.
- `to` ([`Color`](../OneGame.UI/Color.md)): The value at the end.
- `seconds` (`float`): How long it takes.
- `apply` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`Color`](../OneGame.UI/Color.md)&gt;): Receives the color.

**Returns:** [`Tween`](Tween.md)

### <a id="m-custom-itweentarget-float"></a>Custom(ITweenTarget, float)

```csharp
public Tween Custom(ITweenTarget target, float seconds);
```

Drives a [`ITweenTarget`](ITweenTarget.md) with eased progress from 0 to 1.

**Parameters**

- `target` ([`ITweenTarget`](ITweenTarget.md)): Told the progress.
- `seconds` (`float`): How long it takes.

**Returns:** [`Tween`](Tween.md)

### <a id="m-delay-float"></a>Delay(float)

```csharp
public Tween Delay(float seconds);
```

Does nothing for `seconds`: a pause inside a [`TweenSet.Sequence()`](TweenSet.md#m-sequence-readonlyspan-tween).

**Parameters**

- `seconds` (`float`): How long to wait.

**Returns:** [`Tween`](Tween.md)

### <a id="m-number-float-float-float-action-float"></a>Number(float, float, float, Action&lt;float&gt;)

```csharp
public Tween Number(float from, float to, float seconds, Action<float> apply);
```

Moves a number from `from` to `to`, calling `apply` with each value.

**Parameters**

- `from` (`float`): The value at the start.
- `to` (`float`): The value at the end.
- `seconds` (`float`): How long it takes; zero jumps to the end on the first advance.
- `apply` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<float>`): Receives the value. Also called once with `from` when the tween starts, and with exactly `to` at the end.

**Returns:** [`Tween`](Tween.md)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The duration is negative or not finite.

### <a id="m-parallel-readonlyspan-tween"></a>Parallel(ReadOnlySpan&lt;Tween&gt;)

```csharp
public Tween Parallel(ReadOnlySpan<Tween> children);
```

Plays tweens at the same time. It ends when the longest child ends.

**Parameters**

- `children` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`Tween`](Tween.md)&gt;): Tweens made but not yet advanced. They become part of the group.

**Returns:** [`Tween`](Tween.md)

**Exceptions**

- [`TweenException`](TweenException.md): A child already started, belongs to a group or another set, or repeats forever.

### <a id="m-sequence-readonlyspan-tween"></a>Sequence(ReadOnlySpan&lt;Tween&gt;)

```csharp
public Tween Sequence(ReadOnlySpan<Tween> children);
```

Plays tweens one after another. Each child starts when the one before it has finished (its delay and repeats included).

**Parameters**

- `children` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`Tween`](Tween.md)&gt;): Tweens made but not yet advanced, such as `tweens.Number(...)` results. They become part of the group.

**Returns:** [`Tween`](Tween.md)

**Exceptions**

- [`TweenException`](TweenException.md): A child already started, belongs to a group or another set, or repeats forever.

### <a id="m-snapshot-list-tweeninfo"></a>Snapshot(List&lt;TweenInfo&gt;)

```csharp
public void Snapshot(List<TweenInfo> into);
```

Lists the running top-level tweens, oldest first, into `into` (which is cleared first).

**Parameters**

- `into` ([`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`TweenInfo`](TweenInfo.md)&gt;): The list to fill; reuse one to avoid allocating.

### <a id="m-vector2-vector2-vector2-float-action-vector2"></a>Vector2(Vector2, Vector2, float, Action&lt;Vector2&gt;)

```csharp
public Tween Vector2(Vector2 from, Vector2 to, float seconds, Action<Vector2> apply);
```

Moves a 2D vector (a position, a size) between two values.

**Parameters**

- `from` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): The value at the start.
- `to` ([`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)): The value at the end.
- `seconds` (`float`): How long it takes.
- `apply` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)&gt;): Receives the value.

**Returns:** [`Tween`](Tween.md)

### <a id="m-vector3-vector3-vector3-float-action-vector3"></a>Vector3(Vector3, Vector3, float, Action&lt;Vector3&gt;)

```csharp
public Tween Vector3(Vector3 from, Vector3 to, float seconds, Action<Vector3> apply);
```

Moves a 3D vector between two values.

**Parameters**

- `from` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The value at the start.
- `to` ([`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)): The value at the end.
- `seconds` (`float`): How long it takes.
- `apply` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`Vector3`](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)&gt;): Receives the value.

**Returns:** [`Tween`](Tween.md)

### <a id="m-vector4-vector4-vector4-float-action-vector4"></a>Vector4(Vector4, Vector4, float, Action&lt;Vector4&gt;)

```csharp
public Tween Vector4(Vector4 from, Vector4 to, float seconds, Action<Vector4> apply);
```

Moves a 4D vector between two values.

**Parameters**

- `from` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)): The value at the start.
- `to` ([`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)): The value at the end.
- `seconds` (`float`): How long it takes.
- `apply` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`Vector4`](https://learn.microsoft.com/dotnet/api/system.numerics.vector4)&gt;): Receives the value.

**Returns:** [`Tween`](Tween.md)
