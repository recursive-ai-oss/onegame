# Tweens and transitions

A tween moves a value from one number to another over some seconds and hands you each value on the way. Use it for view state and UI: fade a panel, slide a card in, pulse a score, smooth a camera. Tweens run on the view clock, never the simulation: they cannot change synced state, and a replay or state hash is the same with or without them.

`game.Tweens` is the set to start them in. The host advances it once per frame by the view time that passed (the ticks shown plus how far past the last tick the frame is). A stepped run or a capture moves it exactly one tick (1/60 s) per tick.

## Examples

Fade a panel in:

```csharp
panel.Style(s => s.Opacity(0));
tweens.Number(0, 1, 0.25f, v => panel.Style(s => s.Opacity(v)));
```

Slide a card in from the left, easing out, and run something when it lands:

```csharp
tweens.Number(-300, 0, 0.4f, x => card.Style(s => s.Translate(x, 0)))
    .Ease(Easing.OutCubic)
    .OnComplete(() => card.Class("landed"));
```

Pulse a score when it changes (a little overshoot, then settle):

```csharp
tweens.Number(1.4f, 1f, 0.3f, v => score.Style(s => s.Scale(v)))
    .Ease(Easing.OutBack)
    .Named("score-pulse");
```

Smooth a camera: start a new tween toward the target each time it changes, and cancel the one still running.

```csharp
var zoom = 1f;
var move = default(Tween);
void ZoomTo(float target)
{
    if (move.IsActive)
    {
        move.Cancel();
    }

    move = tweens.Number(zoom, target, 0.5f, v => zoom = v).Ease("ease-in-out");
}

ZoomTo(2);
```

Blink a highlight forever, there and back, until you cancel it:

```csharp
var blink = tweens.Color(Color.White, Color.Rgb(0xf2c14e), 0.6f, c => panel.Style(s => s.Background(c)))
    .RepeatForever(yoyo: true);
// later
blink.Cancel();
```

A toast that slides in, waits, and slides out, as one sequence:

```csharp
tweens.Sequence(
    tweens.Number(0, 1, 0.2f, v => card.Style(s => s.Opacity(v))),
    tweens.Delay(1.5f),
    tweens.Number(1, 0, 0.3f, v => card.Style(s => s.Opacity(v))).Ease(Easing.InQuad),
    tweens.Call(() => card.RemoveFromParent()));
```

Two things at once:

```csharp
tweens.Parallel(
    tweens.Number(0, 1, 0.3f, v => panel.Style(s => s.Opacity(v))),
    tweens.Number(20, 0, 0.3f, y => panel.Style(s => s.Translate(0, y))).Ease(Easing.OutCubic));
```

## Style transitions: no code at all

The `transition` style property animates a style change for you, with the same easing and the same clock:

```csharp
ui.AddStyleSheet(new StyleSheet()
    .Rule(".card", s => s.Opacity(1).Transition("opacity 150ms ease-out, background-color 0.2s"))
    .Rule(".card:hover", s => s.Opacity(0.8f).Background(Color.Rgb(0x2a3b4c))));
```

When a property listed in `transition` changes (a class switches, `:hover` starts to match, a binding changes the inline style), it moves to the new value over the time given instead of jumping. The value is a comma-separated list of `property duration easing delay`:

- the property is a CSS name (`opacity`, `width`, `background-color`, `translate-x`), `all`, or a shorthand (`margin`, `padding`, `border-width`, `border-radius`, `gap`, `translate`). Later list items win over earlier ones, so `all 1s, opacity 100ms` fades faster than everything else;
- the first time is the duration, the second the delay; easing is any [CSS timing function](https://www.w3.org/TR/css-easing-1/) (`ease` is the default) or one of our names such as `out-cubic`;
- numbers, colors and lengths in one unit blend. Anything else (`display`, `px` to `%`, `auto`) switches at once;
- the first style an element gets never animates, and `none` or a zero duration turns it off;
- a change in the middle of a transition starts again from the value reached, with the full duration;
- colors blend premultiplied in sRGB, as CSS does.

Layout properties (`width`, `padding`) reflow every frame while they move. Style transitions are tweens in `ui.Tweens`, so `game.Tweens` lists them too.

## Starting tweens

| Call | Moves |
|---|---|
| `tweens.Number(from, to, seconds, v => ...)` | a `float` |
| `tweens.Vector2`, `Vector3`, `Vector4` | `System.Numerics` vectors, component by component |
| `tweens.Color(from, to, seconds, c => ...)` | a `Color` (premultiplied blend) |
| `tweens.Custom(target, seconds)` | anything: `target.Apply(progress)` gets the eased progress |
| `tweens.Delay(seconds)` | nothing; a gap inside a sequence |
| `tweens.Call(action)` | nothing; runs `action` in turn |
| `tweens.Sequence(a, b, ...)` | children one after another |
| `tweens.Parallel(a, b, ...)` | children together; ends with the longest |

The call returns a `Tween` handle. Chain what you need on it: `.Ease(...)` (an `Easing` or CSS text), `.Delay(seconds)`, `.Repeat(plays, yoyo)`, `.RepeatForever(yoyo)`, `.OnComplete(action)` and `.Named(name)`.

- The setter receives the start value on the first advance and exactly the end value at the end. During a delay nothing is touched.
- `Repeat(3)` plays three times in total; `yoyo: true` plays every second play backwards.
- Make a group's children in the same call (or the same frame) and do not keep their handles: they belong to the group. Groups nest. A child cannot repeat forever.
- A handle works until its tween ends or is cancelled. `IsActive` says; other members then throw `TweenException`, whose message says what to do.
- `Cancel()` stops where it is and never calls `OnComplete`. `Complete()` jumps to the end and calls it. `Pause()`/`Resume()`, `Seek(seconds)` and `Progress` do what they say.
- Callbacks run after the frame's values are applied, so they can start or cancel tweens. A tween they start begins on the next frame.
- `tweens.TimeScale` slows or speeds all of them; `tweens.Paused` freezes them.

Starting and running tweens allocates nothing once the pool is warm. The lambda you pass is yours: keep one around (a field) instead of writing a new lambda per start if you start thousands per second.

## Easing

`Easing` is a small struct; evaluating it never allocates. `default(Easing)` is linear.

```csharp
var e = Easing.OutCubic;                                  // named curves: In/Out/InOut + Sine, Quad, Cubic, Quart, Quint, Expo, Circ, Back, Elastic, Bounce
var css = Easing.CubicBezier(0.2f, 0, 0, 1);              // CSS cubic-bezier(); x values must be 0..1, y may overshoot
var stairs = Easing.Steps(4, StepJump.Start);              // CSS steps(4, jump-start)
var fromText = Easing.Parse("ease-in-out");               // also "steps(3)", "cubic-bezier(...)", "out-back", "linear"
var halfway = e.Evaluate(0.5f);                           // 0.875
```

`Ease`, `EaseIn`, `EaseOut`, `EaseInOut`, `StepStart` and `StepEnd` are the CSS keywords. Values follow CSS Easing Functions Level 1: cubic-bezier continues along its end tangents outside 0..1, and `steps()` follows the spec's jump rules.

## Looking at tweens from the harness

The `tweens` control op lists what is running and moves view time, so a capture can be taken at exact points of an animation:

```text
onegame run --live ...      # then, over MCP or the CLI:
tweens {"advance": 0.1}     # {"time":0.1, "active":2, "tweens":[{"id":1,"kind":"value","label":"Box#panel opacity","elapsed":0.1,"duration":0.2,"progress":0.5,...}]}
tweens {"settle": true}     # let everything finite finish
tweens {"cancel_all": true}
```

`advance` first lets a style change made since the last frame start its transition, then moves time; no ticks run. Scenarios have the same as a step and an expectation:

```yaml
steps:
  - click: {id: toggle}
  - tweens: 0.1                       # seconds of view time; or `tweens: settle`
  - expect: {capture: {reference: refs/half-way.png, tolerance: 4}}
  - tweens: settle
  - expect: {tweens: {active: 0}}
```
