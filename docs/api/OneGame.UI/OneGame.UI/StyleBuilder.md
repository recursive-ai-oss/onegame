[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# StyleBuilder class

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public sealed class StyleBuilder
```

Collects style declarations for a rule or for one element's inline style. Every method returns the builder, so calls chain.

## Constructors

| Member | Description |
| --- | --- |
| [`StyleBuilder()`](#m-ctor) | Creates an empty builder for use in a style sheet rule. |

### <a id="m-ctor"></a>StyleBuilder()

```csharp
public StyleBuilder();
```

Creates an empty builder for use in a style sheet rule.

## Methods

| Member | Description |
| --- | --- |
| [`AlignContent(Align)`](#m-aligncontent-align) | Sets `align-content`. |
| [`AlignItems(Align)`](#m-alignitems-align) | Sets `align-items`. |
| [`AlignSelf(Align)`](#m-alignself-align) | Sets `align-self`. |
| [`Anchor(AnchorPoint, Length, Length, bool)`](#m-anchor-anchorpoint-length-length-bool) | Pins an element at `point` of its containing box (the parent, or the screen when `toScreen`). |
| [`Background(Color)`](#m-background-color) | Sets `background-color`. |
| [`BackgroundImage(string)`](#m-backgroundimage-string) | Sets the background image by asset id (raster or SVG); an empty string clears it. |
| [`BackgroundPosition(Length, Length)`](#m-backgroundposition-length-length) | Sets `background-position-x` and `-y`. |
| [`BackgroundRepeat(BackgroundRepeat)`](#m-backgroundrepeat-backgroundrepeat) | Sets `background-repeat`: whether the background image tiles across, down, both or neither. |
| [`BackgroundSize(BackgroundSizing)`](#m-backgroundsize-backgroundsizing) | Sets `background-size` to a keyword ([`BackgroundSizing.Fill`](BackgroundSizing.md#m-fill) stretches to the box, [`BackgroundSizing.Auto`](BackgroundSizing.md#m-auto) uses the natural size times the UI scale) and clears `background-width`/`-height`. |
| [`BackgroundSize(Length, Length)`](#m-backgroundsize-length-length) | Sets an explicit tile or image size. |
| [`BackgroundSlice(Length)`](#m-backgroundslice-length) | Sets the nine-slice inset of the background image, in pixels on every side; zero draws the image stretched. |
| [`BackgroundTint(Color)`](#m-backgroundtint-color) | Sets the color the background image is multiplied by (white by default, which leaves it as drawn). |
| [`Basis(Length)`](#m-basis-length) | Sets `flex-basis`. |
| [`Border(Length, Color)`](#m-border-length-color) | Sets all four border widths and the border color. |
| [`BorderColor(Color)`](#m-bordercolor-color) | Sets the border color (one color for all sides). |
| [`BorderWidth(Length)`](#m-borderwidth-length) | Sets all four border widths. |
| [`BorderWidth(Length, Length, Length, Length)`](#m-borderwidth-length-length-length-length) | Sets the border widths top, right, bottom, left. |
| [`Bottom(Length)`](#m-bottom-length) | Sets `bottom`. |
| [`BoxSizing(BoxSizing)`](#m-boxsizing-boxsizing) | Sets `box-sizing`. |
| [`ColumnGap(Length)`](#m-columngap-length) | Sets the gap between items in a row (`column-gap`). |
| [`Cursor(CursorShape)`](#m-cursor-cursorshape) | Sets `cursor`. |
| [`Direction(FlexDirection)`](#m-direction-flexdirection) | Sets `flex-direction`. |
| [`Display(DisplayMode)`](#m-display-displaymode) | Sets `display`. |
| [`Flex(float, float, Length)`](#m-flex-float-float-length) | Sets `flex-grow`, `flex-shrink` and `flex-basis` (the CSS `flex` shorthand). |
| [`FontFamily(string)`](#m-fontfamily-string) | Sets `font-family` (a family name or generic family the text system understands). |
| [`FontSize(Length)`](#m-fontsize-length) | Sets `font-size`. |
| [`FontStyle(FontStyle)`](#m-fontstyle-fontstyle) | Sets `font-style`. |
| [`FontWeight(int)`](#m-fontweight-int) | Sets `font-weight` (100..900). |
| [`Gap(Length)`](#m-gap-length) | Sets both gaps. |
| [`Grow(float)`](#m-grow-float) | Sets `flex-grow`. |
| [`Height(Length)`](#m-height-length) | Sets `height`. |
| [`Inset(Length)`](#m-inset-length) | Sets all four insets (`left/top/right/bottom`). |
| [`JustifyContent(Align)`](#m-justifycontent-align) | Sets `justify-content`. |
| [`Left(Length)`](#m-left-length) | Sets `left`. |
| [`LetterSpacing(Length)`](#m-letterspacing-length) | Sets `letter-spacing`. |
| [`LineHeight(Length)`](#m-lineheight-length) | Sets `line-height`; a percentage is of the font size, auto uses the font's own. |
| [`Margin(Length)`](#m-margin-length) | Sets all four margins. |
| [`Margin(Length, Length)`](#m-margin-length-length) | Sets vertical then horizontal margins. |
| [`Margin(Length, Length, Length, Length)`](#m-margin-length-length-length-length) | Sets the margins top, right, bottom, left (the CSS shorthand order). |
| [`MarginBottom(Length)`](#m-marginbottom-length) | Sets `margin-bottom`. |
| [`MarginLeft(Length)`](#m-marginleft-length) | Sets `margin-left`. |
| [`MarginRight(Length)`](#m-marginright-length) | Sets `margin-right`. |
| [`MarginTop(Length)`](#m-margintop-length) | Sets `margin-top`. |
| [`MaxHeight(Length)`](#m-maxheight-length) | Sets `max-height`; auto means none. |
| [`MaxWidth(Length)`](#m-maxwidth-length) | Sets `max-width`; auto means none. |
| [`MinHeight(Length)`](#m-minheight-length) | Sets `min-height`. |
| [`MinWidth(Length)`](#m-minwidth-length) | Sets `min-width`. |
| [`Opacity(float)`](#m-opacity-float) | Sets `opacity` (0..1); the element and its children fade as a group. |
| [`Order(int)`](#m-order-int) | Sets `order`. |
| [`Outline(Length, Color, Length)`](#m-outline-length-color-length) | Sets an outline drawn outside the border box (a focus ring does not take layout space). |
| [`Overflow(OverflowMode)`](#m-overflow-overflowmode) | Sets `overflow` on both axes. |
| [`Overflow(OverflowMode, OverflowMode)`](#m-overflow-overflowmode-overflowmode) | Sets `overflow-x` and `overflow-y`. |
| [`Padding(Length)`](#m-padding-length) | Sets all four paddings. |
| [`Padding(Length, Length)`](#m-padding-length-length) | Sets vertical then horizontal paddings. |
| [`Padding(Length, Length, Length, Length)`](#m-padding-length-length-length-length) | Sets the paddings top, right, bottom, left. |
| [`PointerEvents(PointerEvents)`](#m-pointerevents-pointerevents) | Sets `pointer-events`. |
| [`Position(PositionMode)`](#m-position-positionmode) | Sets `position`. |
| [`Radius(Length)`](#m-radius-length) | Sets all four corner radii. |
| [`Radius(Length, Length, Length, Length)`](#m-radius-length-length-length-length) | Sets the corner radii top-left, top-right, bottom-right, bottom-left. |
| [`Right(Length)`](#m-right-length) | Sets `right`. |
| [`Rotate(float)`](#m-rotate-float) | Rotates the painted box about its center, in degrees clockwise. |
| [`RowGap(Length)`](#m-rowgap-length) | Sets the gap between lines (`row-gap`). |
| [`Scale(float)`](#m-scale-float) | Scales the painted box about its center. |
| [`Set(string, StyleValue)`](#m-set-string-stylevalue) | Sets a property by its CSS name. |
| [`Shadow(Length, Length, Length, Length, Color)`](#m-shadow-length-length-length-length-color) | Sets a drop shadow under the box. |
| [`Shrink(float)`](#m-shrink-float) | Sets `flex-shrink`. |
| [`Size(Length, Length)`](#m-size-length-length) | Sets `width` and `height`. |
| [`TextAlign(TextAlignment)`](#m-textalign-textalignment) | Sets `text-align`. |
| [`TextColor(Color)`](#m-textcolor-color) | Sets the inherited text `color`. |
| [`Token(string, StyleValue)`](#m-token-string-stylevalue) | Defines a theme token on the matched elements; they and their descendants can read it with [`StyleBuilder.Var()`](StyleBuilder.md#m-var-string-string-stylevalue). |
| [`TokenFrom(string, string, StyleValue)`](#m-tokenfrom-string-string-stylevalue) | Defines a theme token whose value is another token, looked up on the parent chain. |
| [`Top(Length)`](#m-top-length) | Sets `top`. |
| [`Transition(string)`](#m-transition-string) | Sets `transition`: when a listed property's value changes (a class switches, a state such as `:hover` starts to match), the element moves to the new value over the time given instead of jumping. |
| [`Translate(Length, Length)`](#m-translate-length-length) | Moves the painted box and its content without affecting layout; percentages are of the element's own size. |
| [`Var(string, string, StyleValue)`](#m-var-string-string-stylevalue) | Sets a property to the value of a theme token, such as `--accent`, falling back to `fallback` when no ancestor defines it. |
| [`Visibility(VisibilityMode)`](#m-visibility-visibilitymode) | Sets `visibility`. |
| [`WhiteSpace(WhiteSpace)`](#m-whitespace-whitespace) | Sets `white-space`. |
| [`Width(Length)`](#m-width-length) | Sets `width`. |
| [`Wrap(FlexWrap)`](#m-wrap-flexwrap) | Sets `flex-wrap`. |
| [`ZIndex(int)`](#m-zindex-int) | Sets `z-index`: it orders an element among its siblings; higher paints later and is hit first. |

### <a id="m-aligncontent-align"></a>AlignContent(Align)

```csharp
public StyleBuilder AlignContent(Align v);
```

Sets `align-content`.

**Parameters**

- `v` ([`Align`](Align.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-alignitems-align"></a>AlignItems(Align)

```csharp
public StyleBuilder AlignItems(Align v);
```

Sets `align-items`.

**Parameters**

- `v` ([`Align`](Align.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-alignself-align"></a>AlignSelf(Align)

```csharp
public StyleBuilder AlignSelf(Align v);
```

Sets `align-self`.

**Parameters**

- `v` ([`Align`](Align.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-anchor-anchorpoint-length-length-bool"></a>Anchor(AnchorPoint, Length, Length, bool)

```csharp
public StyleBuilder Anchor(AnchorPoint point, Length x = default, Length y = default, bool toScreen = false);
```

Pins an element at `point` of its containing box (the parent, or the screen when `toScreen`). `x` and `y` push the element inward from the anchored edges, or move it when centered.

**Parameters**

- `point` ([`AnchorPoint`](AnchorPoint.md))
- `x` ([`Length`](Length.md))
- `y` ([`Length`](Length.md))
- `toScreen` (`bool`)

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-background-color"></a>Background(Color)

```csharp
public StyleBuilder Background(Color v);
```

Sets `background-color`.

**Parameters**

- `v` ([`Color`](Color.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-backgroundimage-string"></a>BackgroundImage(string)

```csharp
public StyleBuilder BackgroundImage(string id);
```

Sets the background image by asset id (raster or SVG); an empty string clears it.

**Parameters**

- `id` (`string`)

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-backgroundposition-length-length"></a>BackgroundPosition(Length, Length)

```csharp
public StyleBuilder BackgroundPosition(Length x, Length y);
```

Sets `background-position-x` and `-y`. A percentage aligns that fraction of the image with the same fraction of the box, as in CSS; other lengths move the image from the box's top-left corner.

**Parameters**

- `x` ([`Length`](Length.md))
- `y` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-backgroundrepeat-backgroundrepeat"></a>BackgroundRepeat(BackgroundRepeat)

```csharp
public StyleBuilder BackgroundRepeat(BackgroundRepeat v);
```

Sets `background-repeat`: whether the background image tiles across, down, both or neither.

**Parameters**

- `v` ([`BackgroundRepeat`](BackgroundRepeat.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-backgroundsize-backgroundsizing"></a>BackgroundSize(BackgroundSizing)

```csharp
public StyleBuilder BackgroundSize(BackgroundSizing v);
```

Sets `background-size` to a keyword ([`BackgroundSizing.Fill`](BackgroundSizing.md#m-fill) stretches to the box, [`BackgroundSizing.Auto`](BackgroundSizing.md#m-auto) uses the natural size times the UI scale) and clears `background-width`/`-height`.

**Parameters**

- `v` ([`BackgroundSizing`](BackgroundSizing.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-backgroundsize-length-length"></a>BackgroundSize(Length, Length)

```csharp
public StyleBuilder BackgroundSize(Length width, Length height);
```

Sets an explicit tile or image size. Percentages are of the box; an `auto` side keeps the image's aspect ratio.

**Parameters**

- `width` ([`Length`](Length.md))
- `height` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-backgroundslice-length"></a>BackgroundSlice(Length)

```csharp
public StyleBuilder BackgroundSlice(Length v);
```

Sets the nine-slice inset of the background image, in pixels on every side; zero draws the image stretched.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-backgroundtint-color"></a>BackgroundTint(Color)

```csharp
public StyleBuilder BackgroundTint(Color v);
```

Sets the color the background image is multiplied by (white by default, which leaves it as drawn). White single-color art tinted with a theme token follows the theme.

**Parameters**

- `v` ([`Color`](Color.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-basis-length"></a>Basis(Length)

```csharp
public StyleBuilder Basis(Length v);
```

Sets `flex-basis`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-border-length-color"></a>Border(Length, Color)

```csharp
public StyleBuilder Border(Length width, Color color);
```

Sets all four border widths and the border color.

**Parameters**

- `width` ([`Length`](Length.md))
- `color` ([`Color`](Color.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-bordercolor-color"></a>BorderColor(Color)

```csharp
public StyleBuilder BorderColor(Color v);
```

Sets the border color (one color for all sides).

**Parameters**

- `v` ([`Color`](Color.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-borderwidth-length"></a>BorderWidth(Length)

```csharp
public StyleBuilder BorderWidth(Length all);
```

Sets all four border widths.

**Parameters**

- `all` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-borderwidth-length-length-length-length"></a>BorderWidth(Length, Length, Length, Length)

```csharp
public StyleBuilder BorderWidth(Length top, Length right, Length bottom, Length left);
```

Sets the border widths top, right, bottom, left.

**Parameters**

- `top` ([`Length`](Length.md))
- `right` ([`Length`](Length.md))
- `bottom` ([`Length`](Length.md))
- `left` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-bottom-length"></a>Bottom(Length)

```csharp
public StyleBuilder Bottom(Length v);
```

Sets `bottom`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-boxsizing-boxsizing"></a>BoxSizing(BoxSizing)

```csharp
public StyleBuilder BoxSizing(BoxSizing v);
```

Sets `box-sizing`.

**Parameters**

- `v` ([`BoxSizing`](BoxSizing.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-columngap-length"></a>ColumnGap(Length)

```csharp
public StyleBuilder ColumnGap(Length v);
```

Sets the gap between items in a row (`column-gap`).

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-cursor-cursorshape"></a>Cursor(CursorShape)

```csharp
public StyleBuilder Cursor(CursorShape v);
```

Sets `cursor`.

**Parameters**

- `v` ([`CursorShape`](CursorShape.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-direction-flexdirection"></a>Direction(FlexDirection)

```csharp
public StyleBuilder Direction(FlexDirection v);
```

Sets `flex-direction`.

**Parameters**

- `v` ([`FlexDirection`](FlexDirection.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-display-displaymode"></a>Display(DisplayMode)

```csharp
public StyleBuilder Display(DisplayMode v);
```

Sets `display`.

**Parameters**

- `v` ([`DisplayMode`](DisplayMode.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-flex-float-float-length"></a>Flex(float, float, Length)

```csharp
public StyleBuilder Flex(float grow, float shrink, Length basis);
```

Sets `flex-grow`, `flex-shrink` and `flex-basis` (the CSS `flex` shorthand).

**Parameters**

- `grow` (`float`)
- `shrink` (`float`)
- `basis` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-fontfamily-string"></a>FontFamily(string)

```csharp
public StyleBuilder FontFamily(string v);
```

Sets `font-family` (a family name or generic family the text system understands).

**Parameters**

- `v` (`string`)

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-fontsize-length"></a>FontSize(Length)

```csharp
public StyleBuilder FontSize(Length v);
```

Sets `font-size`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-fontstyle-fontstyle"></a>FontStyle(FontStyle)

```csharp
public StyleBuilder FontStyle(FontStyle v);
```

Sets `font-style`.

**Parameters**

- `v` ([`FontStyle`](FontStyle.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-fontweight-int"></a>FontWeight(int)

```csharp
public StyleBuilder FontWeight(int v);
```

Sets `font-weight` (100..900).

**Parameters**

- `v` (`int`)

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-gap-length"></a>Gap(Length)

```csharp
public StyleBuilder Gap(Length v);
```

Sets both gaps.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-grow-float"></a>Grow(float)

```csharp
public StyleBuilder Grow(float v);
```

Sets `flex-grow`.

**Parameters**

- `v` (`float`)

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-height-length"></a>Height(Length)

```csharp
public StyleBuilder Height(Length v);
```

Sets `height`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-inset-length"></a>Inset(Length)

```csharp
public StyleBuilder Inset(Length v);
```

Sets all four insets (`left/top/right/bottom`).

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-justifycontent-align"></a>JustifyContent(Align)

```csharp
public StyleBuilder JustifyContent(Align v);
```

Sets `justify-content`.

**Parameters**

- `v` ([`Align`](Align.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-left-length"></a>Left(Length)

```csharp
public StyleBuilder Left(Length v);
```

Sets `left`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-letterspacing-length"></a>LetterSpacing(Length)

```csharp
public StyleBuilder LetterSpacing(Length v);
```

Sets `letter-spacing`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-lineheight-length"></a>LineHeight(Length)

```csharp
public StyleBuilder LineHeight(Length v);
```

Sets `line-height`; a percentage is of the font size, auto uses the font's own.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-margin-length"></a>Margin(Length)

```csharp
public StyleBuilder Margin(Length all);
```

Sets all four margins.

**Parameters**

- `all` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-margin-length-length"></a>Margin(Length, Length)

```csharp
public StyleBuilder Margin(Length vertical, Length horizontal);
```

Sets vertical then horizontal margins.

**Parameters**

- `vertical` ([`Length`](Length.md))
- `horizontal` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-margin-length-length-length-length"></a>Margin(Length, Length, Length, Length)

```csharp
public StyleBuilder Margin(Length top, Length right, Length bottom, Length left);
```

Sets the margins top, right, bottom, left (the CSS shorthand order).

**Parameters**

- `top` ([`Length`](Length.md))
- `right` ([`Length`](Length.md))
- `bottom` ([`Length`](Length.md))
- `left` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-marginbottom-length"></a>MarginBottom(Length)

```csharp
public StyleBuilder MarginBottom(Length v);
```

Sets `margin-bottom`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-marginleft-length"></a>MarginLeft(Length)

```csharp
public StyleBuilder MarginLeft(Length v);
```

Sets `margin-left`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-marginright-length"></a>MarginRight(Length)

```csharp
public StyleBuilder MarginRight(Length v);
```

Sets `margin-right`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-margintop-length"></a>MarginTop(Length)

```csharp
public StyleBuilder MarginTop(Length v);
```

Sets `margin-top`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-maxheight-length"></a>MaxHeight(Length)

```csharp
public StyleBuilder MaxHeight(Length v);
```

Sets `max-height`; auto means none.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-maxwidth-length"></a>MaxWidth(Length)

```csharp
public StyleBuilder MaxWidth(Length v);
```

Sets `max-width`; auto means none.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-minheight-length"></a>MinHeight(Length)

```csharp
public StyleBuilder MinHeight(Length v);
```

Sets `min-height`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-minwidth-length"></a>MinWidth(Length)

```csharp
public StyleBuilder MinWidth(Length v);
```

Sets `min-width`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-opacity-float"></a>Opacity(float)

```csharp
public StyleBuilder Opacity(float v);
```

Sets `opacity` (0..1); the element and its children fade as a group.

**Parameters**

- `v` (`float`)

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-order-int"></a>Order(int)

```csharp
public StyleBuilder Order(int v);
```

Sets `order`.

**Parameters**

- `v` (`int`)

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-outline-length-color-length"></a>Outline(Length, Color, Length)

```csharp
public StyleBuilder Outline(Length width, Color color, Length offset = default);
```

Sets an outline drawn outside the border box (a focus ring does not take layout space).

**Parameters**

- `width` ([`Length`](Length.md))
- `color` ([`Color`](Color.md))
- `offset` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-overflow-overflowmode"></a>Overflow(OverflowMode)

```csharp
public StyleBuilder Overflow(OverflowMode v);
```

Sets `overflow` on both axes.

**Parameters**

- `v` ([`OverflowMode`](OverflowMode.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-overflow-overflowmode-overflowmode"></a>Overflow(OverflowMode, OverflowMode)

```csharp
public StyleBuilder Overflow(OverflowMode x, OverflowMode y);
```

Sets `overflow-x` and `overflow-y`.

**Parameters**

- `x` ([`OverflowMode`](OverflowMode.md))
- `y` ([`OverflowMode`](OverflowMode.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-padding-length"></a>Padding(Length)

```csharp
public StyleBuilder Padding(Length all);
```

Sets all four paddings.

**Parameters**

- `all` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-padding-length-length"></a>Padding(Length, Length)

```csharp
public StyleBuilder Padding(Length vertical, Length horizontal);
```

Sets vertical then horizontal paddings.

**Parameters**

- `vertical` ([`Length`](Length.md))
- `horizontal` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-padding-length-length-length-length"></a>Padding(Length, Length, Length, Length)

```csharp
public StyleBuilder Padding(Length top, Length right, Length bottom, Length left);
```

Sets the paddings top, right, bottom, left.

**Parameters**

- `top` ([`Length`](Length.md))
- `right` ([`Length`](Length.md))
- `bottom` ([`Length`](Length.md))
- `left` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-pointerevents-pointerevents"></a>PointerEvents(PointerEvents)

```csharp
public StyleBuilder PointerEvents(PointerEvents v);
```

Sets `pointer-events`.

**Parameters**

- `v` ([`PointerEvents`](PointerEvents.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-position-positionmode"></a>Position(PositionMode)

```csharp
public StyleBuilder Position(PositionMode v);
```

Sets `position`.

**Parameters**

- `v` ([`PositionMode`](PositionMode.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-radius-length"></a>Radius(Length)

```csharp
public StyleBuilder Radius(Length all);
```

Sets all four corner radii.

**Parameters**

- `all` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-radius-length-length-length-length"></a>Radius(Length, Length, Length, Length)

```csharp
public StyleBuilder Radius(Length topLeft, Length topRight, Length bottomRight, Length bottomLeft);
```

Sets the corner radii top-left, top-right, bottom-right, bottom-left.

**Parameters**

- `topLeft` ([`Length`](Length.md))
- `topRight` ([`Length`](Length.md))
- `bottomRight` ([`Length`](Length.md))
- `bottomLeft` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-right-length"></a>Right(Length)

```csharp
public StyleBuilder Right(Length v);
```

Sets `right`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-rotate-float"></a>Rotate(float)

```csharp
public StyleBuilder Rotate(float degrees);
```

Rotates the painted box about its center, in degrees clockwise.

**Parameters**

- `degrees` (`float`)

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-rowgap-length"></a>RowGap(Length)

```csharp
public StyleBuilder RowGap(Length v);
```

Sets the gap between lines (`row-gap`).

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-scale-float"></a>Scale(float)

```csharp
public StyleBuilder Scale(float v);
```

Scales the painted box about its center.

**Parameters**

- `v` (`float`)

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-set-string-stylevalue"></a>Set(string, StyleValue)

```csharp
public StyleBuilder Set(string cssName, StyleValue value);
```

Sets a property by its CSS name.

**Parameters**

- `cssName` (`string`)
- `value` ([`StyleValue`](StyleValue.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

**Exceptions**

- [`UiStyleException`](UiStyleException.md): The name is unknown or the value has the wrong type for it.

### <a id="m-shadow-length-length-length-length-color"></a>Shadow(Length, Length, Length, Length, Color)

```csharp
public StyleBuilder Shadow(Length x, Length y, Length blur, Length spread, Color color);
```

Sets a drop shadow under the box.

**Parameters**

- `x` ([`Length`](Length.md))
- `y` ([`Length`](Length.md))
- `blur` ([`Length`](Length.md))
- `spread` ([`Length`](Length.md))
- `color` ([`Color`](Color.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-shrink-float"></a>Shrink(float)

```csharp
public StyleBuilder Shrink(float v);
```

Sets `flex-shrink`.

**Parameters**

- `v` (`float`)

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-size-length-length"></a>Size(Length, Length)

```csharp
public StyleBuilder Size(Length w, Length h);
```

Sets `width` and `height`.

**Parameters**

- `w` ([`Length`](Length.md))
- `h` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-textalign-textalignment"></a>TextAlign(TextAlignment)

```csharp
public StyleBuilder TextAlign(TextAlignment v);
```

Sets `text-align`.

**Parameters**

- `v` ([`TextAlignment`](TextAlignment.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-textcolor-color"></a>TextColor(Color)

```csharp
public StyleBuilder TextColor(Color v);
```

Sets the inherited text `color`.

**Parameters**

- `v` ([`Color`](Color.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-token-string-stylevalue"></a>Token(string, StyleValue)

```csharp
public StyleBuilder Token(string name, StyleValue value);
```

Defines a theme token on the matched elements; they and their descendants can read it with [`StyleBuilder.Var()`](StyleBuilder.md#m-var-string-string-stylevalue).

**Parameters**

- `name` (`string`)
- `value` ([`StyleValue`](StyleValue.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

**Exceptions**

- [`UiStyleException`](UiStyleException.md): The token name does not start with `--`.

### <a id="m-tokenfrom-string-string-stylevalue"></a>TokenFrom(string, string, StyleValue)

```csharp
public StyleBuilder TokenFrom(string name, string otherToken, StyleValue fallback = default);
```

Defines a theme token whose value is another token, looked up on the parent chain.

**Parameters**

- `name` (`string`)
- `otherToken` (`string`)
- `fallback` ([`StyleValue`](StyleValue.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

**Exceptions**

- [`UiStyleException`](UiStyleException.md): A token name does not start with `--`.

### <a id="m-top-length"></a>Top(Length)

```csharp
public StyleBuilder Top(Length v);
```

Sets `top`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-transition-string"></a>Transition(string)

```csharp
public StyleBuilder Transition(string v);
```

Sets `transition`: when a listed property's value changes (a class switches, a state such as `:hover` starts to match), the element moves to the new value over the time given instead of jumping. A comma-separated list of `property duration easing delay`, such as `"opacity 150ms ease-out, width 0.3s 50ms"`; the property can be `all`, and any CSS timing function or [`Easing`](../OneGame.Tweens/Easing.md) name works. Numbers, colors and lengths in one unit animate; the first style an element gets never does. The transitions run in [`UiTree.Tweens`](UiTree.md#m-tweens).

**Parameters**

- `v` (`string`)

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-translate-length-length"></a>Translate(Length, Length)

```csharp
public StyleBuilder Translate(Length x, Length y);
```

Moves the painted box and its content without affecting layout; percentages are of the element's own size.

**Parameters**

- `x` ([`Length`](Length.md))
- `y` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-var-string-string-stylevalue"></a>Var(string, string, StyleValue)

```csharp
public StyleBuilder Var(string cssName, string token, StyleValue fallback = default);
```

Sets a property to the value of a theme token, such as `--accent`, falling back to `fallback` when no ancestor defines it.

**Parameters**

- `cssName` (`string`)
- `token` (`string`)
- `fallback` ([`StyleValue`](StyleValue.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

**Exceptions**

- [`UiStyleException`](UiStyleException.md): The property name is unknown or the token name does not start with `--`.

### <a id="m-visibility-visibilitymode"></a>Visibility(VisibilityMode)

```csharp
public StyleBuilder Visibility(VisibilityMode v);
```

Sets `visibility`.

**Parameters**

- `v` ([`VisibilityMode`](VisibilityMode.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-whitespace-whitespace"></a>WhiteSpace(WhiteSpace)

```csharp
public StyleBuilder WhiteSpace(WhiteSpace v);
```

Sets `white-space`.

**Parameters**

- `v` ([`WhiteSpace`](WhiteSpace.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-width-length"></a>Width(Length)

```csharp
public StyleBuilder Width(Length v);
```

Sets `width`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-wrap-flexwrap"></a>Wrap(FlexWrap)

```csharp
public StyleBuilder Wrap(FlexWrap v);
```

Sets `flex-wrap`.

**Parameters**

- `v` ([`FlexWrap`](FlexWrap.md))

**Returns:** [`StyleBuilder`](StyleBuilder.md)

### <a id="m-zindex-int"></a>ZIndex(int)

```csharp
public StyleBuilder ZIndex(int v);
```

Sets `z-index`: it orders an element among its siblings; higher paints later and is hit first.

**Parameters**

- `v` (`int`)

**Returns:** [`StyleBuilder`](StyleBuilder.md)
