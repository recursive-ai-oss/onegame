[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.Tweens

# Tween struct

**Namespace:** `OneGame.Tweens`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Tween`](Tween.md)&gt;

```csharp
public readonly struct Tween : IEquatable<Tween>
```

A handle to one tween in a [`TweenSet`](TweenSet.md). Configure it right after you start it (`tweens.Number(...).Ease(Easing.OutCubic).Delay(0.1f)`), then keep it only if you may want to cancel or inspect it. A handle stops working when its tween ends or is cancelled: [`Tween.IsActive`](Tween.md#m-isactive) turns false, and members that need a live tween throw [`TweenException`](TweenException.md).

## Properties

| Member | Description |
| --- | --- |
| [`Elapsed`](#m-elapsed) | Seconds since the tween started (paused time does not count), or 0 when it is not active. |
| [`IsActive`](#m-isactive) | True while the tween is running or waiting to start. |
| [`Progress`](#m-progress) | How far along the tween is, 0 to 1 of its [`Tween.TotalDuration`](Tween.md#m-totalduration). |
| [`TotalDuration`](#m-totalduration) | Seconds from start to finish including delay and repeats; infinity when it repeats forever; 0 when not active. |

### <a id="m-elapsed"></a>Elapsed

```csharp
public double Elapsed { get; }
```

Seconds since the tween started (paused time does not count), or 0 when it is not active.

**Type:** `double`

### <a id="m-isactive"></a>IsActive

```csharp
public bool IsActive { get; }
```

True while the tween is running or waiting to start. False for `default(Tween)`, and after it ended or was cancelled.

**Type:** `bool`

### <a id="m-progress"></a>Progress

```csharp
public double Progress { get; }
```

How far along the tween is, 0 to 1 of its [`Tween.TotalDuration`](Tween.md#m-totalduration). 0 when it is not active or never ends.

**Type:** `double`

### <a id="m-totalduration"></a>TotalDuration

```csharp
public double TotalDuration { get; }
```

Seconds from start to finish including delay and repeats; infinity when it repeats forever; 0 when not active.

**Type:** `double`

## Methods

| Member | Description |
| --- | --- |
| [`Add(Tween)`](#m-add-tween) | Appends a tween to a sequence, or adds it to a parallel group. |
| [`Cancel()`](#m-cancel) | Stops the tween where it is. |
| [`Complete()`](#m-complete) | Jumps to the end now: applies the final value and runs the completion callback, then the tween is over. |
| [`Delay(float)`](#m-delay-float) | Waits before starting. |
| [`Ease(Easing)`](#m-ease-easing) | Sets the easing curve. |
| [`Ease(string)`](#m-ease-string) | Sets the easing curve from CSS spelling, such as `"ease-out"` or `"cubic-bezier(0.2, 0, 0, 1)"`. |
| [`Equals(Tween)`](#m-equals-tween) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Named(string?)`](#m-named-string) | Gives the tween a name that shows in the harness's tween listing. |
| [`OnComplete(Action?)`](#m-oncomplete-action) | Runs `action` when the tween finishes (after its last repeat). |
| [`Pause()`](#m-pause) | Holds the tween where it is until [`Tween.Resume()`](Tween.md#m-resume). |
| [`Repeat(int, bool)`](#m-repeat-int-bool) | Plays the tween this many times in total (1 is the default: no repeat). |
| [`RepeatForever(bool)`](#m-repeatforever-bool) | Plays until cancelled. |
| [`Resume()`](#m-resume) | Lets a paused tween run again. |
| [`Seek(double)`](#m-seek-double) | Moves the tween to a time (seconds since its start, delay included) and applies the values there. |

### <a id="m-add-tween"></a>Add(Tween)

```csharp
public Tween Add(Tween child);
```

Appends a tween to a sequence, or adds it to a parallel group. The child must be fresh: made in the same frame and not yet advanced.

**Parameters**

- `child` ([`Tween`](Tween.md)): The tween to add.

**Returns:** [`Tween`](Tween.md)

**Exceptions**

- [`TweenException`](TweenException.md): This is not a group, or the child started, is in a group, or repeats forever.

### <a id="m-cancel"></a>Cancel()

```csharp
public void Cancel();
```

Stops the tween where it is. Its completion callback does not run. The handle stops working.

**Exceptions**

- [`TweenException`](TweenException.md): The tween is inside a group; cancel the group.

### <a id="m-complete"></a>Complete()

```csharp
public void Complete();
```

Jumps to the end now: applies the final value and runs the completion callback, then the tween is over.

**Exceptions**

- [`TweenException`](TweenException.md): The tween repeats forever, is inside a group, or this was called from inside a tween callback.

### <a id="m-delay-float"></a>Delay(float)

```csharp
public Tween Delay(float seconds);
```

Waits before starting. Nothing is changed during the wait. Inside a sequence the delay is part of this tween's slot.

**Parameters**

- `seconds` (`float`): Seconds to wait; zero or more.

**Returns:** [`Tween`](Tween.md)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The value is negative or not finite.

### <a id="m-ease-easing"></a>Ease(Easing)

```csharp
public Tween Ease(Easing easing);
```

Sets the easing curve. The default is [`Easing.Linear`](Easing.md#m-linear).

**Parameters**

- `easing` ([`Easing`](Easing.md)): The curve.

**Returns:** [`Tween`](Tween.md)

### <a id="m-ease-string"></a>Ease(string)

```csharp
public Tween Ease(string easing);
```

Sets the easing curve from CSS spelling, such as `"ease-out"` or `"cubic-bezier(0.2, 0, 0, 1)"`.

**Parameters**

- `easing` (`string`): The text; see [`Easing.TryParse()`](Easing.md#m-tryparse-readonlyspan-char-easing).

**Returns:** [`Tween`](Tween.md)

**Exceptions**

- [`FormatException`](https://learn.microsoft.com/dotnet/api/system.formatexception): The text is not an easing function.

### <a id="m-equals-tween"></a>Equals(Tween)

```csharp
public bool Equals(Tween other);
```

Implements [`IEquatable<Tween>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`Tween`](Tween.md))

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

Overrides [`ValueType.Equals()`](https://learn.microsoft.com/dotnet/api/system.valuetype.equals).

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

Overrides [`ValueType.GetHashCode()`](https://learn.microsoft.com/dotnet/api/system.valuetype.gethashcode).

**Returns:** `int`

### <a id="m-named-string"></a>Named(string?)

```csharp
public Tween Named(string? name);
```

Gives the tween a name that shows in the harness's tween listing.

**Parameters**

- `name` (`string?`): A short name.

**Returns:** [`Tween`](Tween.md)

### <a id="m-oncomplete-action"></a>OnComplete(Action?)

```csharp
public Tween OnComplete(Action? action);
```

Runs `action` when the tween finishes (after its last repeat). It runs after that frame's values are applied.

**Parameters**

- `action` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action)?): The callback; replaces an earlier one. It may start or cancel tweens. A tween that is cancelled never calls it.

**Returns:** [`Tween`](Tween.md)

### <a id="m-pause"></a>Pause()

```csharp
public Tween Pause();
```

Holds the tween where it is until [`Tween.Resume()`](Tween.md#m-resume).

**Returns:** [`Tween`](Tween.md)

**Exceptions**

- [`TweenException`](TweenException.md): The tween is inside a group.

### <a id="m-repeat-int-bool"></a>Repeat(int, bool)

```csharp
public Tween Repeat(int plays, bool yoyo = false);
```

Plays the tween this many times in total (1 is the default: no repeat).

**Parameters**

- `plays` (`int`): How many times to play; at least 1.
- `yoyo` (`bool`): True plays every second repeat backwards, so a pulse goes there and back.

**Returns:** [`Tween`](Tween.md)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The count is below 1.

### <a id="m-repeatforever-bool"></a>RepeatForever(bool)

```csharp
public Tween RepeatForever(bool yoyo = false);
```

Plays until cancelled.

**Parameters**

- `yoyo` (`bool`): True plays every second repeat backwards.

**Returns:** [`Tween`](Tween.md)

**Exceptions**

- [`TweenException`](TweenException.md): The tween is inside a group.

### <a id="m-resume"></a>Resume()

```csharp
public Tween Resume();
```

Lets a paused tween run again.

**Returns:** [`Tween`](Tween.md)

### <a id="m-seek-double"></a>Seek(double)

```csharp
public void Seek(double seconds);
```

Moves the tween to a time (seconds since its start, delay included) and applies the values there. Scrubbing back replays callbacks.

**Parameters**

- `seconds` (`double`): The time; zero or more.

**Exceptions**

- [`TweenException`](TweenException.md): The tween is inside a group, or this was called from inside a tween callback.

## Operators

| Member | Description |
| --- | --- |
| [`operator !=(Tween, Tween)`](#m-operator-tween-tween) | Inequality. |
| [`operator ==(Tween, Tween)`](#m-operator-tween-tween-2) | Equality: the same tween. |

### <a id="m-operator-tween-tween"></a>operator !=(Tween, Tween)

```csharp
public static bool operator !=(Tween left, Tween right);
```

Inequality.

**Parameters**

- `left` ([`Tween`](Tween.md))
- `right` ([`Tween`](Tween.md))

**Returns:** `bool`

### <a id="m-operator-tween-tween-2"></a>operator ==(Tween, Tween)

```csharp
public static bool operator ==(Tween left, Tween right);
```

Equality: the same tween.

**Parameters**

- `left` ([`Tween`](Tween.md))
- `right` ([`Tween`](Tween.md))

**Returns:** `bool`
