[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# TweensResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TweensResult`](TweensResult.md)&gt;

```csharp
public sealed record TweensResult : IEquatable<TweensResult>
```

Result of the `tweens` operation.

## Constructors

| Member | Description |
| --- | --- |
| [`TweensResult(double, bool, float, int, TweenInfo[])`](#m-ctor-double-bool-float-int-tweeninfo) | Result of the `tweens` operation. |

### <a id="m-ctor-double-bool-float-int-tweeninfo"></a>TweensResult(double, bool, float, int, TweenInfo\[\])

```csharp
public TweensResult(double Time, bool Paused, float TimeScale, int Active, TweenInfo[] Tweens);
```

Result of the `tweens` operation.

**Parameters**

- `Time` (`double`): Seconds of view time the tween clock has advanced.
- `Paused` (`bool`): Whether the whole set is paused.
- `TimeScale` (`float`): The clock's speed factor.
- `Active` (`int`): How many top-level tweens are running or waiting.
- `Tweens` ([`TweenInfo`](../../OneGame.UI/OneGame.Tweens/TweenInfo.md)\[\]): The running tweens, oldest first.

## Properties

| Member | Description |
| --- | --- |
| [`Active`](#m-active) | How many top-level tweens are running or waiting. |
| [`Paused`](#m-paused) | Whether the whole set is paused. |
| [`Time`](#m-time) | Seconds of view time the tween clock has advanced. |
| [`TimeScale`](#m-timescale) | The clock's speed factor. |
| [`Tweens`](#m-tweens) | The running tweens, oldest first. |

### <a id="m-active"></a>Active

```csharp
public int Active { get; init; }
```

How many top-level tweens are running or waiting.

**Type:** `int`

### <a id="m-paused"></a>Paused

```csharp
public bool Paused { get; init; }
```

Whether the whole set is paused.

**Type:** `bool`

### <a id="m-time"></a>Time

```csharp
public double Time { get; init; }
```

Seconds of view time the tween clock has advanced.

**Type:** `double`

### <a id="m-timescale"></a>TimeScale

```csharp
public float TimeScale { get; init; }
```

The clock's speed factor.

**Type:** `float`

### <a id="m-tweens"></a>Tweens

```csharp
public TweenInfo[] Tweens { get; init; }
```

The running tweens, oldest first.

**Type:** [`TweenInfo`](../../OneGame.UI/OneGame.Tweens/TweenInfo.md)\[\]
