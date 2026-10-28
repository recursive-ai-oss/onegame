[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.Tweens

# Easing struct

**Namespace:** `OneGame.Tweens`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Easing`](Easing.md)&gt;

```csharp
public readonly struct Easing : IEquatable<Easing>
```

An easing function: maps progress through a tween (0 at the start, 1 at the end) to how far along the value is. Linear, the CSS timing functions (`ease`, `cubic-bezier()`, `steps()`; CSS Easing Functions Level 1) and the common named in/out/in-out families are all values of this one small struct, so evaluating one never allocates. `default(Easing)` is [`Easing.Linear`](Easing.md#m-linear).

## Properties

| Member | Description |
| --- | --- |
| [`Ease`](#m-ease) | CSS `ease`: `cubic-bezier(0.25, 0.1, 0.25, 1)`, the default of CSS transitions. |
| [`EaseIn`](#m-easein) | CSS `ease-in`: `cubic-bezier(0.42, 0, 1, 1)`. |
| [`EaseInOut`](#m-easeinout) | CSS `ease-in-out`: `cubic-bezier(0.42, 0, 0.58, 1)`. |
| [`EaseOut`](#m-easeout) | CSS `ease-out`: `cubic-bezier(0, 0, 0.58, 1)`. |
| [`InBack`](#m-inback) | Pulls back before starting. |
| [`InBounce`](#m-inbounce) | Bounces away from the start. |
| [`InCirc`](#m-incirc) | Circular, eased at the start. |
| [`InCubic`](#m-incubic) | Cubic, eased at the start. |
| [`InElastic`](#m-inelastic) | A spring winding up at the start. |
| [`InExpo`](#m-inexpo) | Exponential, eased at the start. |
| [`InOutBack`](#m-inoutback) | Pulls back at the start and overshoots at the end. |
| [`InOutBounce`](#m-inoutbounce) | Bounces at both ends. |
| [`InOutCirc`](#m-inoutcirc) | Circular, eased at both ends. |
| [`InOutCubic`](#m-inoutcubic) | Cubic, eased at both ends. |
| [`InOutElastic`](#m-inoutelastic) | A spring at both ends. |
| [`InOutExpo`](#m-inoutexpo) | Exponential, eased at both ends. |
| [`InOutQuad`](#m-inoutquad) | Quadratic, eased at both ends. |
| [`InOutQuart`](#m-inoutquart) | Quartic, eased at both ends. |
| [`InOutQuint`](#m-inoutquint) | Quintic, eased at both ends. |
| [`InOutSine`](#m-inoutsine) | Sine, eased at both ends. |
| [`InQuad`](#m-inquad) | Quadratic, eased at the start. |
| [`InQuart`](#m-inquart) | Quartic, eased at the start. |
| [`InQuint`](#m-inquint) | Quintic, eased at the start. |
| [`InSine`](#m-insine) | Sine, eased at the start. |
| [`Linear`](#m-linear) | Constant speed (CSS `linear`). |
| [`OutBack`](#m-outback) | Overshoots the end and settles back: a good pop for a card landing. |
| [`OutBounce`](#m-outbounce) | Bounces on the end like a dropped ball. |
| [`OutCirc`](#m-outcirc) | Circular, eased at the end. |
| [`OutCubic`](#m-outcubic) | Cubic, eased at the end: the usual choice for things that slide into place. |
| [`OutElastic`](#m-outelastic) | A spring that rings around the end value. |
| [`OutExpo`](#m-outexpo) | Exponential, eased at the end. |
| [`OutQuad`](#m-outquad) | Quadratic, eased at the end. |
| [`OutQuart`](#m-outquart) | Quartic, eased at the end. |
| [`OutQuint`](#m-outquint) | Quintic, eased at the end. |
| [`OutSine`](#m-outsine) | Sine, eased at the end. |
| [`StepEnd`](#m-stepend) | CSS `step-end`: `steps(1, jump-end)`. |
| [`StepStart`](#m-stepstart) | CSS `step-start`: `steps(1, jump-start)`. |

### <a id="m-ease"></a>Ease

```csharp
public static Easing Ease { get; }
```

CSS `ease`: `cubic-bezier(0.25, 0.1, 0.25, 1)`, the default of CSS transitions.

**Type:** [`Easing`](Easing.md)

### <a id="m-easein"></a>EaseIn

```csharp
public static Easing EaseIn { get; }
```

CSS `ease-in`: `cubic-bezier(0.42, 0, 1, 1)`.

**Type:** [`Easing`](Easing.md)

### <a id="m-easeinout"></a>EaseInOut

```csharp
public static Easing EaseInOut { get; }
```

CSS `ease-in-out`: `cubic-bezier(0.42, 0, 0.58, 1)`.

**Type:** [`Easing`](Easing.md)

### <a id="m-easeout"></a>EaseOut

```csharp
public static Easing EaseOut { get; }
```

CSS `ease-out`: `cubic-bezier(0, 0, 0.58, 1)`.

**Type:** [`Easing`](Easing.md)

### <a id="m-inback"></a>InBack

```csharp
public static Easing InBack { get; }
```

Pulls back before starting.

**Type:** [`Easing`](Easing.md)

### <a id="m-inbounce"></a>InBounce

```csharp
public static Easing InBounce { get; }
```

Bounces away from the start.

**Type:** [`Easing`](Easing.md)

### <a id="m-incirc"></a>InCirc

```csharp
public static Easing InCirc { get; }
```

Circular, eased at the start.

**Type:** [`Easing`](Easing.md)

### <a id="m-incubic"></a>InCubic

```csharp
public static Easing InCubic { get; }
```

Cubic, eased at the start.

**Type:** [`Easing`](Easing.md)

### <a id="m-inelastic"></a>InElastic

```csharp
public static Easing InElastic { get; }
```

A spring winding up at the start.

**Type:** [`Easing`](Easing.md)

### <a id="m-inexpo"></a>InExpo

```csharp
public static Easing InExpo { get; }
```

Exponential, eased at the start.

**Type:** [`Easing`](Easing.md)

### <a id="m-inoutback"></a>InOutBack

```csharp
public static Easing InOutBack { get; }
```

Pulls back at the start and overshoots at the end.

**Type:** [`Easing`](Easing.md)

### <a id="m-inoutbounce"></a>InOutBounce

```csharp
public static Easing InOutBounce { get; }
```

Bounces at both ends.

**Type:** [`Easing`](Easing.md)

### <a id="m-inoutcirc"></a>InOutCirc

```csharp
public static Easing InOutCirc { get; }
```

Circular, eased at both ends.

**Type:** [`Easing`](Easing.md)

### <a id="m-inoutcubic"></a>InOutCubic

```csharp
public static Easing InOutCubic { get; }
```

Cubic, eased at both ends.

**Type:** [`Easing`](Easing.md)

### <a id="m-inoutelastic"></a>InOutElastic

```csharp
public static Easing InOutElastic { get; }
```

A spring at both ends.

**Type:** [`Easing`](Easing.md)

### <a id="m-inoutexpo"></a>InOutExpo

```csharp
public static Easing InOutExpo { get; }
```

Exponential, eased at both ends.

**Type:** [`Easing`](Easing.md)

### <a id="m-inoutquad"></a>InOutQuad

```csharp
public static Easing InOutQuad { get; }
```

Quadratic, eased at both ends.

**Type:** [`Easing`](Easing.md)

### <a id="m-inoutquart"></a>InOutQuart

```csharp
public static Easing InOutQuart { get; }
```

Quartic, eased at both ends.

**Type:** [`Easing`](Easing.md)

### <a id="m-inoutquint"></a>InOutQuint

```csharp
public static Easing InOutQuint { get; }
```

Quintic, eased at both ends.

**Type:** [`Easing`](Easing.md)

### <a id="m-inoutsine"></a>InOutSine

```csharp
public static Easing InOutSine { get; }
```

Sine, eased at both ends.

**Type:** [`Easing`](Easing.md)

### <a id="m-inquad"></a>InQuad

```csharp
public static Easing InQuad { get; }
```

Quadratic, eased at the start.

**Type:** [`Easing`](Easing.md)

### <a id="m-inquart"></a>InQuart

```csharp
public static Easing InQuart { get; }
```

Quartic, eased at the start.

**Type:** [`Easing`](Easing.md)

### <a id="m-inquint"></a>InQuint

```csharp
public static Easing InQuint { get; }
```

Quintic, eased at the start.

**Type:** [`Easing`](Easing.md)

### <a id="m-insine"></a>InSine

```csharp
public static Easing InSine { get; }
```

Sine, eased at the start.

**Type:** [`Easing`](Easing.md)

### <a id="m-linear"></a>Linear

```csharp
public static Easing Linear { get; }
```

Constant speed (CSS `linear`).

**Type:** [`Easing`](Easing.md)

### <a id="m-outback"></a>OutBack

```csharp
public static Easing OutBack { get; }
```

Overshoots the end and settles back: a good pop for a card landing.

**Type:** [`Easing`](Easing.md)

### <a id="m-outbounce"></a>OutBounce

```csharp
public static Easing OutBounce { get; }
```

Bounces on the end like a dropped ball.

**Type:** [`Easing`](Easing.md)

### <a id="m-outcirc"></a>OutCirc

```csharp
public static Easing OutCirc { get; }
```

Circular, eased at the end.

**Type:** [`Easing`](Easing.md)

### <a id="m-outcubic"></a>OutCubic

```csharp
public static Easing OutCubic { get; }
```

Cubic, eased at the end: the usual choice for things that slide into place.

**Type:** [`Easing`](Easing.md)

### <a id="m-outelastic"></a>OutElastic

```csharp
public static Easing OutElastic { get; }
```

A spring that rings around the end value.

**Type:** [`Easing`](Easing.md)

### <a id="m-outexpo"></a>OutExpo

```csharp
public static Easing OutExpo { get; }
```

Exponential, eased at the end.

**Type:** [`Easing`](Easing.md)

### <a id="m-outquad"></a>OutQuad

```csharp
public static Easing OutQuad { get; }
```

Quadratic, eased at the end.

**Type:** [`Easing`](Easing.md)

### <a id="m-outquart"></a>OutQuart

```csharp
public static Easing OutQuart { get; }
```

Quartic, eased at the end.

**Type:** [`Easing`](Easing.md)

### <a id="m-outquint"></a>OutQuint

```csharp
public static Easing OutQuint { get; }
```

Quintic, eased at the end.

**Type:** [`Easing`](Easing.md)

### <a id="m-outsine"></a>OutSine

```csharp
public static Easing OutSine { get; }
```

Sine, eased at the end.

**Type:** [`Easing`](Easing.md)

### <a id="m-stepend"></a>StepEnd

```csharp
public static Easing StepEnd { get; }
```

CSS `step-end`: `steps(1, jump-end)`.

**Type:** [`Easing`](Easing.md)

### <a id="m-stepstart"></a>StepStart

```csharp
public static Easing StepStart { get; }
```

CSS `step-start`: `steps(1, jump-start)`.

**Type:** [`Easing`](Easing.md)

## Methods

| Member | Description |
| --- | --- |
| [`CubicBezier(float, float, float, float)`](#m-cubicbezier-float-float-float-float) | A cubic Bézier curve through (0,0) and (1,1) with control points (x1,y1) and (x2,y2), as CSS `cubic-bezier()`. |
| [`Equals(Easing)`](#m-equals-easing) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`Evaluate(float)`](#m-evaluate-float) | Maps progress to eased progress. |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Of(EasingFamily, EasingMode)`](#m-of-easingfamily-easingmode) | A named curve, such as `Easing.Of(EasingFamily.Cubic, EasingMode.Out)`. |
| [`Parse(string)`](#m-parse-string) | Reads an easing from CSS spelling; see [`Easing.TryParse()`](Easing.md#m-tryparse-readonlyspan-char-easing). |
| [`Steps(int, StepJump)`](#m-steps-int-stepjump) | A staircase of `count` equal steps, as CSS `steps()`. |
| [`ToString()`](#m-tostring) | The CSS spelling, which [`Easing.TryParse()`](Easing.md#m-tryparse-readonlyspan-char-easing) reads back. |
| [`TryParse(ReadOnlySpan<char>, out Easing)`](#m-tryparse-readonlyspan-char-easing) | Reads an easing from CSS spelling: `linear`, `ease`, `ease-in`, `ease-out`, `ease-in-out`, `step-start`, `step-end`, `cubic-bezier(x1, y1, x2, y2)`, `steps(n[, jump-start\|jump-end\|jump-none\|jump-both\|start\|end])`, plus the named curves as `in-quad`, `out-cubic`, `in-out-back` and so on. |

### <a id="m-cubicbezier-float-float-float-float"></a>CubicBezier(float, float, float, float)

```csharp
public static Easing CubicBezier(float x1, float y1, float x2, float y2);
```

A cubic Bézier curve through (0,0) and (1,1) with control points (x1,y1) and (x2,y2), as CSS `cubic-bezier()`.

**Parameters**

- `x1` (`float`): First control point x, from 0 to 1.
- `y1` (`float`): First control point y; may leave 0..1 for overshoot.
- `x2` (`float`): Second control point x, from 0 to 1.
- `y2` (`float`): Second control point y; may leave 0..1 for overshoot.

**Returns:** [`Easing`](Easing.md)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): An x value is outside 0..1 (CSS makes such a curve invalid) or a value is not finite.

### <a id="m-equals-easing"></a>Equals(Easing)

```csharp
public bool Equals(Easing other);
```

**Parameters**

- `other` ([`Easing`](Easing.md))

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-evaluate-float"></a>Evaluate(float)

```csharp
public float Evaluate(float t);
```

Maps progress to eased progress. Input outside 0..1 is allowed: Bézier curves continue along their end tangents (CSS Easing §2.2.1), named curves and steps hold their end values. Output may leave 0..1 for curves that overshoot.

**Parameters**

- `t` (`float`): Progress through the tween, normally 0 to 1.

**Returns:** `float`

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

**Returns:** `int`

### <a id="m-of-easingfamily-easingmode"></a>Of(EasingFamily, EasingMode)

```csharp
public static Easing Of(EasingFamily family, EasingMode mode);
```

A named curve, such as `Easing.Of(EasingFamily.Cubic, EasingMode.Out)`.

**Parameters**

- `family` ([`EasingFamily`](EasingFamily.md)): The curve's shape.
- `mode` ([`EasingMode`](EasingMode.md)): Which end is eased.

**Returns:** [`Easing`](Easing.md)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The family or mode is not a defined value.

### <a id="m-parse-string"></a>Parse(string)

```csharp
public static Easing Parse(string text);
```

Reads an easing from CSS spelling; see [`Easing.TryParse()`](Easing.md#m-tryparse-readonlyspan-char-easing).

**Parameters**

- `text` (`string`): The text.

**Returns:** [`Easing`](Easing.md)

**Exceptions**

- [`FormatException`](https://learn.microsoft.com/dotnet/api/system.formatexception): The text is not an easing function.

### <a id="m-steps-int-stepjump"></a>Steps(int, StepJump)

```csharp
public static Easing Steps(int count, StepJump jump = 0);
```

A staircase of `count` equal steps, as CSS `steps()`.

**Parameters**

- `count` (`int`): The number of intervals; at least 1, or 2 with [`StepJump.None`](StepJump.md#m-none).
- `jump` ([`StepJump`](StepJump.md)): Where the jumps fall.

**Returns:** [`Easing`](Easing.md)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The count is too small or too large (above 4096), or the jump is not defined.

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

The CSS spelling, which [`Easing.TryParse()`](Easing.md#m-tryparse-readonlyspan-char-easing) reads back.

**Returns:** `string`

### <a id="m-tryparse-readonlyspan-char-easing"></a>TryParse(ReadOnlySpan&lt;char&gt;, out Easing)

```csharp
public static bool TryParse(ReadOnlySpan<char> text, out Easing easing);
```

Reads an easing from CSS spelling: `linear`, `ease`, `ease-in`, `ease-out`, `ease-in-out`, `step-start`, `step-end`, `cubic-bezier(x1, y1, x2, y2)`, `steps(n[, jump-start|jump-end|jump-none|jump-both|start|end])`, plus the named curves as `in-quad`, `out-cubic`, `in-out-back` and so on. Keywords are not case sensitive.

**Parameters**

- `text` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<char>`): The text.
- `easing` ([`Easing`](Easing.md)): The easing, when the text is valid.

**Returns:** `bool` — False when the text is not an easing function or its arguments are out of range.

## Operators

| Member | Description |
| --- | --- |
| [`operator !=(Easing, Easing)`](#m-operator-easing-easing) | Inequality. |
| [`operator ==(Easing, Easing)`](#m-operator-easing-easing-2) | Equality. |

### <a id="m-operator-easing-easing"></a>operator !=(Easing, Easing)

```csharp
public static bool operator !=(Easing left, Easing right);
```

Inequality.

**Parameters**

- `left` ([`Easing`](Easing.md))
- `right` ([`Easing`](Easing.md))

**Returns:** `bool`

### <a id="m-operator-easing-easing-2"></a>operator ==(Easing, Easing)

```csharp
public static bool operator ==(Easing left, Easing right);
```

Equality.

**Parameters**

- `left` ([`Easing`](Easing.md))
- `right` ([`Easing`](Easing.md))

**Returns:** `bool`
