[API reference](../index.md) › OneGame.UI

# OneGame.UI

Retained UI tree with CSS-model styling and flex layout.

**Package:** `RecursiveAI.OneGame.UI`

```sh
dotnet add package RecursiveAI.OneGame.UI
```

## OneGame.Tweens

| Type | Kind | Description |
| --- | --- | --- |
| [`Easing`](OneGame.Tweens/Easing.md) | struct | An easing function: maps progress through a tween (0 at the start, 1 at the end) to how far along the value is. |
| [`EasingFamily`](OneGame.Tweens/EasingFamily.md) | enum | The shape family of a named easing curve (see [`Easing.Of()`](OneGame.Tweens/Easing.md#m-of-easingfamily-easingmode)). |
| [`EasingMode`](OneGame.Tweens/EasingMode.md) | enum | Which end of a named easing curve is eased. |
| [`ITweenTarget`](OneGame.Tweens/ITweenTarget.md) | interface | Something a tween drives that is not a plain value: it is told the eased progress each time the tween moves. |
| [`StepJump`](OneGame.Tweens/StepJump.md) | enum | Where a [`Easing.Steps()`](OneGame.Tweens/Easing.md#m-steps-int-stepjump) function jumps (CSS Easing Functions Level 1, §2.3). |
| [`Tween`](OneGame.Tweens/Tween.md) | struct | A handle to one tween in a [`TweenSet`](OneGame.Tweens/TweenSet.md). |
| [`TweenException`](OneGame.Tweens/TweenException.md) | class | Thrown when a tween handle is used wrongly: after the tween ended, on a tween that is part of a group, or with an unusable shape. |
| [`TweenInfo`](OneGame.Tweens/TweenInfo.md) | struct | A running tween as the harness lists it (see [`TweenSet.Snapshot()`](OneGame.Tweens/TweenSet.md#m-snapshot-list-tweeninfo)). |
| [`TweenSet`](OneGame.Tweens/TweenSet.md) | class | A set of tweens that all run on one clock. |

## OneGame.UI

| Type | Kind | Description |
| --- | --- | --- |
| [`Align`](OneGame.UI/Align.md) | enum | Values for `justify-content`, `align-items`, `align-self` and `align-content`. |
| [`AnchorPoint`](OneGame.UI/AnchorPoint.md) | enum | Where [`StyleBuilder.Anchor()`](OneGame.UI/StyleBuilder.md#m-anchor-anchorpoint-length-length-bool) pins an element. |
| [`BackgroundRepeat`](OneGame.UI/BackgroundRepeat.md) | enum | The `background-repeat` property (CSS Backgrounds 3 §3.3). |
| [`BackgroundSizing`](OneGame.UI/BackgroundSizing.md) | enum | The keyword forms of `background-size` (CSS Backgrounds 3 §3.9). |
| [`BoxSizing`](OneGame.UI/BoxSizing.md) | enum | The `box-sizing` property. |
| [`Color`](OneGame.UI/Color.md) | struct | An 8-bit-per-channel RGBA color (not premultiplied). |
| [`ComputedStyle`](OneGame.UI/ComputedStyle.md) | class | The resolved value of every style property of one element, after the cascade, inheritance and theme tokens. |
| [`CornerRadii`](OneGame.UI/CornerRadii.md) | struct | Four corner radii in pixels. |
| [`CursorShape`](OneGame.UI/CursorShape.md) | enum | The `cursor` property: a hint for the platform layer. |
| [`DisplayList`](OneGame.UI/DisplayList.md) | class | What the UI hands the renderer each frame: draw commands in paint order, plus the text they refer to. |
| [`DisplayMode`](OneGame.UI/DisplayMode.md) | enum | The `display` property. |
| [`DrawCommand`](OneGame.UI/DrawCommand.md) | struct | One drawing instruction. |
| [`DrawKind`](OneGame.UI/DrawKind.md) | enum | The kind of a [`DrawCommand`](OneGame.UI/DrawCommand.md). |
| [`Edges`](OneGame.UI/Edges.md) | struct | Four per-side values (border widths, nine-slice insets). |
| [`Element`](OneGame.UI/Element.md) | class | One node of the retained UI tree. |
| [`FlexDirection`](OneGame.UI/FlexDirection.md) | enum | The `flex-direction` property. |
| [`FlexWrap`](OneGame.UI/FlexWrap.md) | enum | The `flex-wrap` property. |
| [`FontStyle`](OneGame.UI/FontStyle.md) | enum | The `font-style` property. |
| [`IArtSizes`](OneGame.UI/IArtSizes.md) | interface | Tells the UI how big an image or SVG is, so it can size and tile backgrounds (`background-size`, `background-repeat`) without knowing how art is stored. |
| [`ITextMeasurer`](OneGame.UI/ITextMeasurer.md) | interface | Measures and breaks text for layout. |
| [`KeyModifiers`](OneGame.UI/KeyModifiers.md) | enum | Modifier keys held during a key event. |
| [`Length`](OneGame.UI/Length.md) | struct | A CSS-style length: a number and a unit. |
| [`LengthUnit`](OneGame.UI/LengthUnit.md) | enum | The unit of a [`Length`](OneGame.UI/Length.md). |
| [`MonospaceTextMeasurer`](OneGame.UI/MonospaceTextMeasurer.md) | class | A deterministic [`ITextMeasurer`](OneGame.UI/ITextMeasurer.md) where every character is `0.6 × size + letter-spacing` wide and a line is `1.2 × size` tall unless the style sets a line height. |
| [`NavDirection`](OneGame.UI/NavDirection.md) | enum | A direction for [`UiEventKind.Navigate`](OneGame.UI/UiEventKind.md#m-navigate). |
| [`OverflowMode`](OneGame.UI/OverflowMode.md) | enum | The `overflow` property. |
| [`PlacedLine`](OneGame.UI/PlacedLine.md) | struct | A line of text with its final position. |
| [`PointerEvents`](OneGame.UI/PointerEvents.md) | enum | The `pointer-events` property. |
| [`PositionMode`](OneGame.UI/PositionMode.md) | enum | The `position` property. |
| [`Rect`](OneGame.UI/Rect.md) | struct | An axis-aligned rectangle in pixels. |
| [`StyleBuilder`](OneGame.UI/StyleBuilder.md) | class | Collects style declarations for a rule or for one element's inline style. |
| [`StyleSheet`](OneGame.UI/StyleSheet.md) | class | An ordered list of style rules. |
| [`StyleValue`](OneGame.UI/StyleValue.md) | struct | A value for a style property or a theme token: a length, a number, a color, a keyword or a string. |
| [`TextAlignment`](OneGame.UI/TextAlignment.md) | enum | The `text-align` property (left to right text only for now). |
| [`TextLine`](OneGame.UI/TextLine.md) | struct | One line of laid-out text. |
| [`TextRun`](OneGame.UI/TextRun.md) | struct | A paragraph of text to draw: the string, its font, color and where each line goes. |
| [`TextSize`](OneGame.UI/TextSize.md) | struct | The size of laid-out text. |
| [`TextStyle`](OneGame.UI/TextStyle.md) | struct | The resolved font properties that decide how a piece of text is measured and wrapped. |
| [`TileAxes`](OneGame.UI/TileAxes.md) | enum | Which axes of a background image repeat, as carried by a display list command. |
| [`Ui`](OneGame.UI/Ui.md) | static class | Factory methods that read like a layout: `Ui.Column(Ui.Label("Hello"), Ui.Button("OK", Accept))`. |
| [`UiEvent`](OneGame.UI/UiEvent.md) | struct | One input or element event. |
| [`UiEventKind`](OneGame.UI/UiEventKind.md) | enum | What happened. |
| [`UiHandler`](OneGame.UI/UiHandler.md) | delegate | An event handler. |
| [`UiStyleException`](OneGame.UI/UiStyleException.md) | class | A style rule, selector or property was malformed. |
| [`UiTree`](OneGame.UI/UiTree.md) | class | A UI: a root element, the style sheets that apply to it, the screen size and scale, and the per-frame pipeline (bindings, then style, then layout, then the display list). |
| [`UiTreeException`](OneGame.UI/UiTreeException.md) | class | A tree operation was invalid (adding an element that already has a parent, or creating a cycle). |
| [`VisibilityMode`](OneGame.UI/VisibilityMode.md) | enum | The `visibility` property. |
| [`WhiteSpace`](OneGame.UI/WhiteSpace.md) | enum | The `white-space` property. |
