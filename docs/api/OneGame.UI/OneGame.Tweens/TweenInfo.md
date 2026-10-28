[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.Tweens

# TweenInfo struct

**Namespace:** `OneGame.Tweens`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TweenInfo`](TweenInfo.md)&gt;

```csharp
public readonly struct TweenInfo : IEquatable<TweenInfo>
```

A running tween as the harness lists it (see [`TweenSet.Snapshot()`](TweenSet.md#m-snapshot-list-tweeninfo)).

## Constructors

| Member | Description |
| --- | --- |
| [`TweenInfo(long, string, string?, string?, double, double, double, bool, int)`](#m-ctor-long-string-string-string-double-double-double-bool-int) | A running tween as the harness lists it (see [`TweenSet.Snapshot()`](TweenSet.md#m-snapshot-list-tweeninfo)). |

### <a id="m-ctor-long-string-string-string-double-double-double-bool-int"></a>TweenInfo(long, string, string?, string?, double, double, double, bool, int)

```csharp
public TweenInfo(long Id, string Kind, string? Name, string? Label, double Elapsed, double Duration, double Progress, bool Paused, int Children);
```

A running tween as the harness lists it (see [`TweenSet.Snapshot()`](TweenSet.md#m-snapshot-list-tweeninfo)).

**Parameters**

- `Id` (`long`): A number that counts up from 1 in creation order.
- `Kind` (`string`): `value`, `call`, `delay`, `sequence` or `parallel`.
- `Name` (`string?`): The name given with [`Tween.Named()`](Tween.md#m-named-string), if any.
- `Label` (`string?`): What the tween drives, when it says (a style transition names its element and property).
- `Elapsed` (`double`): Seconds since it started, delay included.
- `Duration` (`double`): Seconds from start to finish, delay and repeats included; -1 when it repeats forever.
- `Progress` (`double`): 0 to 1 through `Duration`; through the current repeat when it runs forever.
- `Paused` (`bool`): True while [`Tween.Pause()`](Tween.md#m-pause) holds it.
- `Children` (`int`): How many tweens are inside it (groups only).

## Properties

| Member | Description |
| --- | --- |
| [`Children`](#m-children) | How many tweens are inside it (groups only). |
| [`Duration`](#m-duration) | Seconds from start to finish, delay and repeats included; -1 when it repeats forever. |
| [`Elapsed`](#m-elapsed) | Seconds since it started, delay included. |
| [`Id`](#m-id) | A number that counts up from 1 in creation order. |
| [`Kind`](#m-kind) | `value`, `call`, `delay`, `sequence` or `parallel`. |
| [`Label`](#m-label) | What the tween drives, when it says (a style transition names its element and property). |
| [`Name`](#m-name) | The name given with [`Tween.Named()`](Tween.md#m-named-string), if any. |
| [`Paused`](#m-paused) | True while [`Tween.Pause()`](Tween.md#m-pause) holds it. |
| [`Progress`](#m-progress) | 0 to 1 through `Duration`; through the current repeat when it runs forever. |

### <a id="m-children"></a>Children

```csharp
public int Children { get; init; }
```

How many tweens are inside it (groups only).

**Type:** `int`

### <a id="m-duration"></a>Duration

```csharp
public double Duration { get; init; }
```

Seconds from start to finish, delay and repeats included; -1 when it repeats forever.

**Type:** `double`

### <a id="m-elapsed"></a>Elapsed

```csharp
public double Elapsed { get; init; }
```

Seconds since it started, delay included.

**Type:** `double`

### <a id="m-id"></a>Id

```csharp
public long Id { get; init; }
```

A number that counts up from 1 in creation order.

**Type:** `long`

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; init; }
```

`value`, `call`, `delay`, `sequence` or `parallel`.

**Type:** `string`

### <a id="m-label"></a>Label

```csharp
public string? Label { get; init; }
```

What the tween drives, when it says (a style transition names its element and property).

**Type:** `string?`

### <a id="m-name"></a>Name

```csharp
public string? Name { get; init; }
```

The name given with [`Tween.Named()`](Tween.md#m-named-string), if any.

**Type:** `string?`

### <a id="m-paused"></a>Paused

```csharp
public bool Paused { get; init; }
```

True while [`Tween.Pause()`](Tween.md#m-pause) holds it.

**Type:** `bool`

### <a id="m-progress"></a>Progress

```csharp
public double Progress { get; init; }
```

0 to 1 through `Duration`; through the current repeat when it runs forever.

**Type:** `double`
