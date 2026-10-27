# Interface: tweens, easing and style transitions

Namespace `OneGame.Tweens`, in the `OneGame.UI` assembly. User guide: `docs/tweens.md`. Version 1.

## Types

- `Easing` (struct, `IEquatable`): `Evaluate(float t)`; `Linear` (= `default`), CSS `Ease`/`EaseIn`/`EaseOut`/`EaseInOut`/`StepStart`/`StepEnd`, `CubicBezier(x1,y1,x2,y2)`, `Steps(n, StepJump)`, `Of(EasingFamily, EasingMode)` and a static for each named curve, `Parse`/`TryParse`/`ToString` in CSS spelling (plus `in-quad`, `out-cubic`, `in-out-back`...). Follows CSS Easing Functions Level 1: bezier x in 0..1, end-tangent extension outside 0..1, step jump rules (the `before` flag is not used). No `linear()` stops (Level 2).
- `TweenSet`: the clock and pool. `Advance(seconds)` is the only thing that moves time. Starts: `Number`, `Vector2/3/4`, `Color`, `Custom(ITweenTarget)`, `Delay`, `Call`, `Sequence(params ReadOnlySpan<Tween>)`, `Parallel(...)`. Also `Time`, `ActiveCount`, `TimeScale`, `Paused`, `CancelAll()`, `Snapshot(List<TweenInfo>)`.
- `Tween` (struct handle: set, node, generation): `Ease`, `Delay`, `Repeat(plays, yoyo)`, `RepeatForever(yoyo)`, `OnComplete`, `Named`, `Add` (groups), `Pause/Resume`, `Cancel`, `Complete`, `Seek`, `IsActive`, `Elapsed`, `TotalDuration`, `Progress`. A stale handle throws `TweenException`.
- `ITweenTarget { Apply(float eased); string? Label }`: what style transitions implement; open to games.
- `TweenInfo`: the harness's row for one running tween.

## Semantics

- A node's value is a pure function of its local time, so any step size gives the same value at the same time and `Seek` agrees with playing. Durations and delays are rounded to microseconds, so `0.2f` finishes on a `0.2` step.
- Nothing is applied during a delay, and a group child is not touched before its slot. The first `Advance` applies the start value; the last applies the end value exactly.
- A step that crosses into a later play first shows the end of the play it left (so a `Call` at the end of a repeated group runs); a step spanning several plays does this once.
- Completion callbacks (and `Call`) run after all values of an `Advance` are applied, in start order; tweens started from a callback begin on the next `Advance`. Value callbacks may start or cancel tweens; they may not call `Advance`, `Complete` or `Seek` (`TweenException`).
- Groups: children must be unstarted and finite; they belong to the group (cancel/pause the group); nesting is limited to 7 levels and 4096 children.
- Zero allocation per frame while tweens run, and when starting tweens with a reused delegate (nodes and lists are pooled).

## Style transitions

`transition` (CSS Transitions Level 1) is a text property parsed by `TransitionParser` (cached per distinct text on the `UiTree`, 256 entries). On restyle, a changed value of a listed property starts a tween in `UiTree.Tweens` from the element's current (possibly animated) value to the new one; `Computed` always holds the value in effect. Blendable: numbers, colors (premultiplied sRGB), lengths of one non-auto unit. First style never animates. A running transition whose target is unchanged keeps running; a changed target restarts from the current value with the full duration; dropping the property's transition snaps it. Layout properties reflow while moving. Each style transition is an `ITweenTarget` labelled `Kind#id property`.

## Host

`GameInstance.Tweens` is `Ui.Tweens`. `RunFrame` calls `Tweens.Advance` with the increase of view time `(World.Tick + FrameAlpha) / TickRate` since the previous frame (a lower tick, such as a restart, only moves the baseline). `GameInstance.AdvanceView(seconds)` runs `Ui.Update`, advances, runs `Ui.Update` and redraws, without ticks.

## Harness

- Control op `tweens` (view-only): `advance`, `settle`, `cancel_all`, `paused`, `time_scale`; returns `{time, paused, timeScale, active, tweens[]}`.
- Scenario step `tweens: <seconds>|settle`; expectation `tweens: {active: n}`.
- Sim safety: callbacks written inside `[View]` code are view code (OG0008 on synced writes); sim code calling the API gets OG0015.
