[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# UiTree class

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public sealed class UiTree
```

A UI: a root element, the style sheets that apply to it, the screen size and scale, and the per-frame pipeline (bindings, then style, then layout, then the display list). Nothing runs until [`UiTree.Frame()`](UiTree.md#m-frame) or [`UiTree.Update()`](UiTree.md#m-update) is called, and a frame in which nothing changed does no work and allocates nothing.

## Constructors

| Member | Description |
| --- | --- |
| [`UiTree(ITextMeasurer?)`](#m-ctor-itextmeasurer) | Creates a UI with a 1280×720 screen at scale 1. |

### <a id="m-ctor-itextmeasurer"></a>UiTree(ITextMeasurer?)

```csharp
public UiTree(ITextMeasurer? measurer = null);
```

Creates a UI with a 1280×720 screen at scale 1.

**Parameters**

- `measurer` ([`ITextMeasurer`](ITextMeasurer.md)?): Measures text for layout; defaults to [`MonospaceTextMeasurer`](MonospaceTextMeasurer.md) for headless use.

## Properties

| Member | Description |
| --- | --- |
| [`ArrowKeysNavigate`](#m-arrowkeysnavigate) | When true (the default), unhandled arrow keys move focus like the d-pad. |
| [`ArtSizes`](#m-artsizes) | Reports the natural size of images and SVGs by asset id. |
| [`Cursor`](#m-cursor) | The cursor the platform layer should show for the hovered element. |
| [`DisplayList`](#m-displaylist) | The display list produced by the last [`UiTree.Frame()`](UiTree.md#m-frame). |
| [`Focused`](#m-focused) | The element with keyboard focus, or null. |
| [`Height`](#m-height) | Screen height in pixels. |
| [`Hovered`](#m-hovered) | The deepest element under the pointer as of the last pointer event, or null. |
| [`InputObserver`](#m-inputobserver) | Called with every event passed to [`UiTree.Dispatch()`](UiTree.md#m-dispatch-uievent) before it is handled, whoever sends it. |
| [`Measurer`](#m-measurer) | The text measurer. |
| [`PointerCaptor`](#m-pointercaptor) | The element that receives pointer moves and the release while a drag is in progress, or null. |
| [`Root`](#m-root) | The root element. |
| [`Scale`](#m-scale) | Pixels per UI unit (the `ui` length unit), for example 2 on a 4K display with a 1920×1080 design. |
| [`TextInputArea`](#m-textinputarea) | The rectangle (screen pixels) an input method should avoid covering for [`UiTree.TextInputTarget`](UiTree.md#m-textinputtarget): its caret, or its bounds. |
| [`TextInputTarget`](#m-textinputtarget) | The focused element when it takes typed text ([`Element.AcceptsText`](Element.md#m-acceptstext)), otherwise null. |
| [`Tweens`](#m-tweens) | The tweens of this UI, on the UI's clock: style transitions (`transition: opacity 150ms`) run here, and so do the ones a game starts. |
| [`Width`](#m-width) | Screen width in pixels. |

### <a id="m-arrowkeysnavigate"></a>ArrowKeysNavigate

```csharp
public bool ArrowKeysNavigate { get; set; }
```

When true (the default), unhandled arrow keys move focus like the d-pad.

**Type:** `bool`

### <a id="m-artsizes"></a>ArtSizes

```csharp
public IArtSizes? ArtSizes { get; set; }
```

Reports the natural size of images and SVGs by asset id. Background sizing keywords (`auto`, `cover`, `contain`) and tiling need it; without it every background image is stretched to its box.

**Type:** [`IArtSizes`](IArtSizes.md)?

### <a id="m-cursor"></a>Cursor

```csharp
public CursorShape Cursor { get; }
```

The cursor the platform layer should show for the hovered element.

**Type:** [`CursorShape`](CursorShape.md)

### <a id="m-displaylist"></a>DisplayList

```csharp
public DisplayList DisplayList { get; }
```

The display list produced by the last [`UiTree.Frame()`](UiTree.md#m-frame).

**Type:** [`DisplayList`](DisplayList.md)

### <a id="m-focused"></a>Focused

```csharp
public Element? Focused { get; }
```

The element with keyboard focus, or null.

**Type:** [`Element`](Element.md)?

### <a id="m-height"></a>Height

```csharp
public float Height { get; }
```

Screen height in pixels.

**Type:** `float`

### <a id="m-hovered"></a>Hovered

```csharp
public Element? Hovered { get; }
```

The deepest element under the pointer as of the last pointer event, or null.

**Type:** [`Element`](Element.md)?

### <a id="m-inputobserver"></a>InputObserver

```csharp
public Action<UiEvent>? InputObserver { get; set; }
```

Called with every event passed to [`UiTree.Dispatch()`](UiTree.md#m-dispatch-uievent) before it is handled, whoever sends it. The component kit uses it to learn which input device is in use.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`UiEvent`](UiEvent.md)&gt;?

### <a id="m-measurer"></a>Measurer

```csharp
public ITextMeasurer Measurer { get; set; }
```

The text measurer. Replace it when the text system becomes available; all layout is redone.

**Type:** [`ITextMeasurer`](ITextMeasurer.md)

### <a id="m-pointercaptor"></a>PointerCaptor

```csharp
public Element? PointerCaptor { get; }
```

The element that receives pointer moves and the release while a drag is in progress, or null.

**Type:** [`Element`](Element.md)?

### <a id="m-root"></a>Root

```csharp
public Element Root { get; }
```

The root element. Its size is the screen size; add the screens as its children.

**Type:** [`Element`](Element.md)

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; }
```

Pixels per UI unit (the `ui` length unit), for example 2 on a 4K display with a 1920×1080 design.

**Type:** `float`

### <a id="m-textinputarea"></a>TextInputArea

```csharp
public Rect? TextInputArea { get; }
```

The rectangle (screen pixels) an input method should avoid covering for [`UiTree.TextInputTarget`](UiTree.md#m-textinputtarget): its caret, or its bounds.

**Type:** [`Rect`](Rect.md)?

### <a id="m-textinputtarget"></a>TextInputTarget

```csharp
public Element? TextInputTarget { get; }
```

The focused element when it takes typed text ([`Element.AcceptsText`](Element.md#m-acceptstext)), otherwise null.

**Type:** [`Element`](Element.md)?

### <a id="m-tweens"></a>Tweens

```csharp
public TweenSet Tweens { get; }
```

The tweens of this UI, on the UI's clock: style transitions (`transition: opacity 150ms`) run here, and so do the ones a game starts. Whoever runs the frame loop calls [`TweenSet.Advance()`](../OneGame.Tweens/TweenSet.md#m-advance-double) on it once per frame; the game host does that with the view time that passed.

**Type:** [`TweenSet`](../OneGame.Tweens/TweenSet.md)

### <a id="m-width"></a>Width

```csharp
public float Width { get; }
```

Screen width in pixels.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`AddStyleSheet(StyleSheet)`](#m-addstylesheet-stylesheet) | Adds a style sheet after the existing ones; its rules win ties in specificity. |
| [`CapturePointer(Element)`](#m-capturepointer-element) | Sends pointer moves and the next primary-button release to `element` even when the pointer leaves it, so a slider thumb or a scroll bar can be dragged. |
| [`Dispatch(in UiEvent)`](#m-dispatch-uievent) | Feeds one input event to the UI. |
| [`DumpJson(Element?, bool, bool)`](#m-dumpjson-element-bool-bool) | The tree as JSON for the harness: for each element its kind, id, classes, states, text, local and screen boxes, scroll extents and computed style. |
| [`Find(string)`](#m-find-string) | Finds the first element (in document order) with this id, or null. |
| [`Focus(Element?)`](#m-focus-element) | Moves keyboard focus to `element`, or clears it with null. |
| [`Frame()`](#m-frame) | Runs [`UiTree.Update()`](UiTree.md#m-update) and returns the display list, rebuilt only if something changed since the last call. |
| [`HitTest(float, float)`](#m-hittest-float-float) | The topmost element at a screen position, honoring clipping, scroll offsets, transforms, z-index, `visibility` and `pointer-events`; null when nothing is there. |
| [`IdSelector(string)`](#m-idselector-string) | Builds the selector that matches exactly the elements with this id, escaping characters a selector cannot hold bare. |
| [`Invoke(Element)`](#m-invoke-element) | Clicks an element as a pointer or the confirm button would, without hit testing. |
| [`MoveFocus(NavDirection)`](#m-movefocus-navdirection) | Moves focus in a direction. |
| [`Query(string)`](#m-query-string) | Finds every element matching a selector (the same syntax as style rules), in document order. |
| [`ReleasePointer()`](#m-releasepointer) | Ends a pointer capture early. |
| [`RemoveStyleSheet(StyleSheet)`](#m-removestylesheet-stylesheet) | Removes a style sheet; returns false when it was not added. |
| [`ReplaceStyleSheet(StyleSheet, StyleSheet)`](#m-replacestylesheet-stylesheet-stylesheet) | Puts `replacement` where `existing` is in the cascade, so a theme can be swapped without changing which sheets win ties. |
| [`RequestFocus(Element?)`](#m-requestfocus-element) | Asks for `element` to take focus at the end of the next [`UiTree.Update()`](UiTree.md#m-update), once layout is current. |
| [`ScrollIntoView(Element)`](#m-scrollintoview-element) | Scrolls the ancestors of `element` that scroll so that it is inside their padding boxes, as far as it fits. |
| [`SetViewport(float, float, float)`](#m-setviewport-float-float-float) | Sets the screen size in pixels and the UI scale. |
| [`TextStyleOf(Element)`](#m-textstyleof-element) | The font properties text in `element` is measured with, in pixels, as of the last update. |
| [`TryGetColorToken(string, out Color)`](#m-trygetcolortoken-string-color) | Reads a color token (such as `--kit-accent`) as the cascade resolved it on the root, game overrides included. |
| [`Update()`](#m-update) | Evaluates bindings, resolves styles and lays out whatever changed, without producing a display list. |

### <a id="m-addstylesheet-stylesheet"></a>AddStyleSheet(StyleSheet)

```csharp
public void AddStyleSheet(StyleSheet sheet);
```

Adds a style sheet after the existing ones; its rules win ties in specificity.

**Parameters**

- `sheet` ([`StyleSheet`](StyleSheet.md))

### <a id="m-capturepointer-element"></a>CapturePointer(Element)

```csharp
public void CapturePointer(Element element);
```

Sends pointer moves and the next primary-button release to `element` even when the pointer leaves it, so a slider thumb or a scroll bar can be dragged. Call it from a [`UiEventKind.PointerDown`](UiEventKind.md#m-pointerdown) handler. The capture ends on the release, or when the element leaves the tree.

**Parameters**

- `element` ([`Element`](Element.md))

### <a id="m-dispatch-uievent"></a>Dispatch(in UiEvent)

```csharp
public bool Dispatch(in UiEvent e);
```

Feeds one input event to the UI. Pointer events are hit-tested against the current layout, key and text events go to the focused element, and what no handler takes gets the default action: click on release, Tab and arrow focus moves, Enter or Space to click, wheel to scroll. Returns true when a handler or a default action consumed the event.

**Parameters**

- `e` ([`UiEvent`](UiEvent.md))

**Returns:** `bool`

### <a id="m-dumpjson-element-bool-bool"></a>DumpJson(Element?, bool, bool)

```csharp
public string DumpJson(Element? from = null, bool allStyles = false, bool children = true);
```

The tree as JSON for the harness: for each element its kind, id, classes, states, text, local and screen boxes, scroll extents and computed style. Styles that equal their initial value are left out unless `allStyles` is set.

**Parameters**

- `from` ([`Element`](Element.md)?): The element to start at; the root by default.
- `allStyles` (`bool`): Include every computed property.
- `children` (`bool`): Include descendants.

**Returns:** `string`

### <a id="m-find-string"></a>Find(string)

```csharp
public Element? Find(string id);
```

Finds the first element (in document order) with this id, or null.

**Parameters**

- `id` (`string`)

**Returns:** [`Element`](Element.md)?

### <a id="m-focus-element"></a>Focus(Element?)

```csharp
public bool Focus(Element? element);
```

Moves keyboard focus to `element`, or clears it with null. Returns false when the element cannot take focus (not focusable, disabled, hidden or not in this tree).

**Parameters**

- `element` ([`Element`](Element.md)?)

**Returns:** `bool`

### <a id="m-frame"></a>Frame()

```csharp
public DisplayList Frame();
```

Runs [`UiTree.Update()`](UiTree.md#m-update) and returns the display list, rebuilt only if something changed since the last call.

**Returns:** [`DisplayList`](DisplayList.md)

### <a id="m-hittest-float-float"></a>HitTest(float, float)

```csharp
public Element? HitTest(float x, float y);
```

The topmost element at a screen position, honoring clipping, scroll offsets, transforms, z-index, `visibility` and `pointer-events`; null when nothing is there.

**Parameters**

- `x` (`float`)
- `y` (`float`)

**Returns:** [`Element`](Element.md)?

### <a id="m-idselector-string"></a>IdSelector(string)

```csharp
public static string IdSelector(string id);
```

Builds the selector that matches exactly the elements with this id, escaping characters a selector cannot hold bare. Ids containing a comma cannot be selected this way; use [`UiTree.Find()`](UiTree.md#m-find-string).

**Parameters**

- `id` (`string`)

**Returns:** `string`

### <a id="m-invoke-element"></a>Invoke(Element)

```csharp
public bool Invoke(Element element);
```

Clicks an element as a pointer or the confirm button would, without hit testing. The click carries [`UiEvent.Button`](UiEvent.md#m-button) -1, which tells a handler it came from activation (Enter, Space, the confirm button) and not from the pointer. Returns false when it is disabled or nothing handled the click.

**Parameters**

- `element` ([`Element`](Element.md))

**Returns:** `bool`

### <a id="m-movefocus-navdirection"></a>MoveFocus(NavDirection)

```csharp
public bool MoveFocus(NavDirection direction);
```

Moves focus in a direction. Next and Previous follow tab order (positive [`Element.TabIndex`](Element.md#m-tabindex) first, then document order, wrapping). Up, Down, Left and Right pick the focusable element nearest in that direction by edge distance, preferring ones that overlap the current element's row or column. With nothing focused, the first focusable element takes focus. Focus stays inside a shown [`Element.TrapsFocus`](Element.md#m-trapsfocus) element. Returns true when focus moved.

**Parameters**

- `direction` ([`NavDirection`](NavDirection.md))

**Returns:** `bool`

### <a id="m-query-string"></a>Query(string)

```csharp
public List<Element> Query(string selector);
```

Finds every element matching a selector (the same syntax as style rules), in document order.

**Parameters**

- `selector` (`string`)

**Returns:** [`List`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1)&lt;[`Element`](Element.md)&gt;

**Exceptions**

- [`UiStyleException`](UiStyleException.md): The selector does not parse.

### <a id="m-releasepointer"></a>ReleasePointer()

```csharp
public void ReleasePointer();
```

Ends a pointer capture early.

### <a id="m-removestylesheet-stylesheet"></a>RemoveStyleSheet(StyleSheet)

```csharp
public bool RemoveStyleSheet(StyleSheet sheet);
```

Removes a style sheet; returns false when it was not added.

**Parameters**

- `sheet` ([`StyleSheet`](StyleSheet.md))

**Returns:** `bool`

### <a id="m-replacestylesheet-stylesheet-stylesheet"></a>ReplaceStyleSheet(StyleSheet, StyleSheet)

```csharp
public bool ReplaceStyleSheet(StyleSheet existing, StyleSheet replacement);
```

Puts `replacement` where `existing` is in the cascade, so a theme can be swapped without changing which sheets win ties. Styles are resolved again on the next frame.

**Parameters**

- `existing` ([`StyleSheet`](StyleSheet.md))
- `replacement` ([`StyleSheet`](StyleSheet.md))

**Returns:** `bool` — False when `existing` was not added; nothing changes then.

### <a id="m-requestfocus-element"></a>RequestFocus(Element?)

```csharp
public void RequestFocus(Element? element);
```

Asks for `element` to take focus at the end of the next [`UiTree.Update()`](UiTree.md#m-update), once layout is current. Components use it from bindings, where calling [`UiTree.Focus()`](UiTree.md#m-focus-element) directly would re-enter the update. A request that cannot be honored (hidden or disabled element) is dropped.

**Parameters**

- `element` ([`Element`](Element.md)?)

### <a id="m-scrollintoview-element"></a>ScrollIntoView(Element)

```csharp
public static void ScrollIntoView(Element element);
```

Scrolls the ancestors of `element` that scroll so that it is inside their padding boxes, as far as it fits.

**Parameters**

- `element` ([`Element`](Element.md))

### <a id="m-setviewport-float-float-float"></a>SetViewport(float, float, float)

```csharp
public void SetViewport(float width, float height, float scale = 1f);
```

Sets the screen size in pixels and the UI scale.

**Parameters**

- `width` (`float`)
- `height` (`float`)
- `scale` (`float`)

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): A size or the scale is not positive.

### <a id="m-textstyleof-element"></a>TextStyleOf(Element)

```csharp
public TextStyle TextStyleOf(Element element);
```

The font properties text in `element` is measured with, in pixels, as of the last update. Components use it to place carets.

**Parameters**

- `element` ([`Element`](Element.md))

**Returns:** [`TextStyle`](TextStyle.md)

### <a id="m-trygetcolortoken-string-color"></a>TryGetColorToken(string, out Color)

```csharp
public bool TryGetColorToken(string name, out Color color);
```

Reads a color token (such as `--kit-accent`) as the cascade resolved it on the root, game overrides included. False when the token is not defined, or is not a color. Styles resolve on the first frame, so call it after one.

**Parameters**

- `name` (`string`): The token name, with its leading `--`.
- `color` ([`Color`](Color.md)): The color, when found.

**Returns:** `bool`

### <a id="m-update"></a>Update()

```csharp
public void Update();
```

Evaluates bindings, resolves styles and lays out whatever changed, without producing a display list. Input dispatch calls this first so hit tests see current boxes.
