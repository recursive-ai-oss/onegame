[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# Element class

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public class Element
```

One node of the retained UI tree. An element has a kind (its type selector), an optional id, classes, children, inline style, event handlers and bindings. Changing anything marks only what it affects as dirty; [`UiTree.Frame()`](UiTree.md#m-frame) restyles, lays out and repaints what changed.

## Constructors

| Member | Description |
| --- | --- |
| [`Element(string)`](#m-ctor-string) | Creates an element. |

### <a id="m-ctor-string"></a>Element(string)

```csharp
public Element(string kind = "Box");
```

Creates an element. `kind` is what type selectors match; layout and painting do not depend on it.

**Parameters**

- `kind` (`string`)

## Properties

| Member | Description |
| --- | --- |
| [`AbsoluteBounds`](#m-absolutebounds) | The border box in screen pixels as of the last layout. |
| [`AcceptsText`](#m-acceptstext) | True for elements that take typed text (a text field). |
| [`Bounds`](#m-bounds) | The border box relative to the parent's border box, as of the last layout. |
| [`CaretArea`](#m-caretarea) | Where an input method should place its candidate window: the caret's rectangle in screen pixels. |
| [`ChildCount`](#m-childcount) | The number of children. |
| [`Children`](#m-children) | The children in document order. |
| [`Classes`](#m-classes) | The classes on the element, in the order added. |
| [`Computed`](#m-computed) | The element's own computed style after the last [`UiTree.Frame()`](UiTree.md#m-frame). |
| [`Id`](#m-id) | The id selectors and the harness find the element by; null for none. |
| [`IntrinsicSize`](#m-intrinsicsize) | Natural size of the content for leaf elements such as images; layout uses it when no width or height is set. |
| [`IsEffectivelyEnabled`](#m-iseffectivelyenabled) | True when the element and all its ancestors are enabled. |
| [`IsEnabled`](#m-isenabled) | False disables the element and its descendants: no pointer or focus input, and `:disabled` matches the element and its descendants, as it matches the controls inside a disabled fieldset in CSS. |
| [`IsFocusable`](#m-isfocusable) | True when the element can take keyboard or gamepad focus (while enabled and shown). |
| [`IsFocused`](#m-isfocused) | True when the element has keyboard focus. |
| [`IsHovered`](#m-ishovered) | True when the pointer is over the element or one of its descendants. |
| [`IsPressed`](#m-ispressed) | True while a pointer button pressed on the element or one of its descendants is still down. |
| [`IsVisible`](#m-isvisible) | False hides the element and its subtree like `display: none`, without touching its style. |
| [`Kind`](#m-kind) | The element's type name, such as `Box`, `Label` or `Button`. |
| [`MaxScroll`](#m-maxscroll) | The largest valid scroll offsets. |
| [`Parent`](#m-parent) | The parent, or null for the root and for elements not yet added. |
| [`Pressed`](#m-pressed) | Whether a toggle control is switched on, like WAI-ARIA `aria-pressed`: `null` for an element that is not a toggle, otherwise `true` or `false`. |
| [`Role`](#m-role) | What the element is for, such as `button`, `slider` or `row`. |
| [`ScrollSize`](#m-scrollsize) | The size of the scrollable content, which is at least the padding box. |
| [`ScrollX`](#m-scrollx) | Horizontal scroll offset in pixels, clamped to the scrollable range. |
| [`ScrollY`](#m-scrolly) | Vertical scroll offset in pixels, clamped to the scrollable range. |
| [`TabIndex`](#m-tabindex) | Lower values come first in Tab order among focusable elements; zero (the default) follows document order after all positive values, as in HTML. |
| [`Tag`](#m-tag) | Free-form data for the game's own use; the UI never reads it. |
| [`Text`](#m-text) | The text of a text element; null for a container. |
| [`TrapsFocus`](#m-trapsfocus) | When true, directional and Tab navigation stays inside this element while it is shown (a modal dialog). |
| [`Tree`](#m-tree) | The tree this element belongs to, or null when it is not attached. |
| [`Value`](#m-value) | The current value a control shows or holds, as text (a slider's number, a tab's title, a toggle's `true`). |

### <a id="m-absolutebounds"></a>AbsoluteBounds

```csharp
public Rect AbsoluteBounds { get; }
```

The border box in screen pixels as of the last layout. Scroll offsets are applied; transforms are not.

**Type:** [`Rect`](Rect.md)

### <a id="m-acceptstext"></a>AcceptsText

```csharp
public bool AcceptsText { get; set; }
```

True for elements that take typed text (a text field). While one has focus the host tells the platform that text input is wanted, so an on-screen keyboard or input method can appear, and it sends [`UiEventKind.Composition`](UiEventKind.md#m-composition) events to the element.

**Type:** `bool`

### <a id="m-bounds"></a>Bounds

```csharp
public Rect Bounds { get; }
```

The border box relative to the parent's border box, as of the last layout.

**Type:** [`Rect`](Rect.md)

### <a id="m-caretarea"></a>CaretArea

```csharp
public Rect? CaretArea { get; set; }
```

Where an input method should place its candidate window: the caret's rectangle in screen pixels. When null, the element's whole bounds are used. A text field updates it as the caret moves.

**Type:** [`Rect`](Rect.md)?

### <a id="m-childcount"></a>ChildCount

```csharp
public int ChildCount { get; }
```

The number of children.

**Type:** `int`

### <a id="m-children"></a>Children

```csharp
public IReadOnlyList<Element> Children { get; }
```

The children in document order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Element`](Element.md)&gt;

### <a id="m-classes"></a>Classes

```csharp
public IReadOnlyList<string> Classes { get; }
```

The classes on the element, in the order added.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-computed"></a>Computed

```csharp
public ComputedStyle Computed { get; }
```

The element's own computed style after the last [`UiTree.Frame()`](UiTree.md#m-frame).

**Type:** [`ComputedStyle`](ComputedStyle.md)

### <a id="m-id"></a>Id

```csharp
public string? Id { get; set; }
```

The id selectors and the harness find the element by; null for none.

**Type:** `string?`

### <a id="m-intrinsicsize"></a>IntrinsicSize

```csharp
public Vector2 IntrinsicSize { get; set; }
```

Natural size of the content for leaf elements such as images; layout uses it when no width or height is set. Zero means none.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-iseffectivelyenabled"></a>IsEffectivelyEnabled

```csharp
public bool IsEffectivelyEnabled { get; }
```

True when the element and all its ancestors are enabled.

**Type:** `bool`

### <a id="m-isenabled"></a>IsEnabled

```csharp
public bool IsEnabled { get; set; }
```

False disables the element and its descendants: no pointer or focus input, and `:disabled` matches the element and its descendants, as it matches the controls inside a disabled fieldset in CSS.

**Type:** `bool`

### <a id="m-isfocusable"></a>IsFocusable

```csharp
public bool IsFocusable { get; set; }
```

True when the element can take keyboard or gamepad focus (while enabled and shown).

**Type:** `bool`

### <a id="m-isfocused"></a>IsFocused

```csharp
public bool IsFocused { get; }
```

True when the element has keyboard focus.

**Type:** `bool`

### <a id="m-ishovered"></a>IsHovered

```csharp
public bool IsHovered { get; }
```

True when the pointer is over the element or one of its descendants.

**Type:** `bool`

### <a id="m-ispressed"></a>IsPressed

```csharp
public bool IsPressed { get; }
```

True while a pointer button pressed on the element or one of its descendants is still down.

**Type:** `bool`

### <a id="m-isvisible"></a>IsVisible

```csharp
public bool IsVisible { get; set; }
```

False hides the element and its subtree like `display: none`, without touching its style.

**Type:** `bool`

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; }
```

The element's type name, such as `Box`, `Label` or `Button`.

**Type:** `string`

### <a id="m-maxscroll"></a>MaxScroll

```csharp
public Vector2 MaxScroll { get; }
```

The largest valid scroll offsets.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-parent"></a>Parent

```csharp
public Element? Parent { get; }
```

The parent, or null for the root and for elements not yet added.

**Type:** [`Element`](Element.md)?

### <a id="m-pressed"></a>Pressed

```csharp
public bool? Pressed { get; set; }
```

Whether a toggle control is switched on, like WAI-ARIA `aria-pressed`: `null` for an element that is not a toggle, otherwise `true` or `false`. Components keep it current; the JSON dump prints it as `pressed` and scenarios check it with `pressed:`. It has no effect on painting by itself: the kit also sets the class `pressed`.

**Type:** `bool?`

### <a id="m-role"></a>Role

```csharp
public string? Role { get; set; }
```

What the element is for, such as `button`, `slider` or `row`. Components set it; the JSON dump prints it, so scenarios can find controls by role. It has no effect on layout or painting.

**Type:** `string?`

### <a id="m-scrollsize"></a>ScrollSize

```csharp
public Vector2 ScrollSize { get; }
```

The size of the scrollable content, which is at least the padding box.

**Type:** [`Vector2`](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="m-scrollx"></a>ScrollX

```csharp
public float ScrollX { get; set; }
```

Horizontal scroll offset in pixels, clamped to the scrollable range.

**Type:** `float`

### <a id="m-scrolly"></a>ScrollY

```csharp
public float ScrollY { get; set; }
```

Vertical scroll offset in pixels, clamped to the scrollable range.

**Type:** `float`

### <a id="m-tabindex"></a>TabIndex

```csharp
public int TabIndex { get; set; }
```

Lower values come first in Tab order among focusable elements; zero (the default) follows document order after all positive values, as in HTML.

**Type:** `int`

### <a id="m-tag"></a>Tag

```csharp
public object? Tag { get; set; }
```

Free-form data for the game's own use; the UI never reads it.

**Type:** `object?`

### <a id="m-text"></a>Text

```csharp
public string? Text { get; set; }
```

The text of a text element; null for a container. A text element is a leaf sized by the [`ITextMeasurer`](ITextMeasurer.md).

**Type:** `string?`

### <a id="m-trapsfocus"></a>TrapsFocus

```csharp
public bool TrapsFocus { get; set; }
```

When true, directional and Tab navigation stays inside this element while it is shown (a modal dialog).

**Type:** `bool`

### <a id="m-tree"></a>Tree

```csharp
public UiTree? Tree { get; }
```

The tree this element belongs to, or null when it is not attached.

**Type:** [`UiTree`](UiTree.md)?

### <a id="m-value"></a>Value

```csharp
public string? Value { get; set; }
```

The current value a control shows or holds, as text (a slider's number, a tab's title, a toggle's `true`). Components keep it current; the JSON dump prints it.

**Type:** `string?`

## Methods

| Member | Description |
| --- | --- |
| [`Add(Element)`](#m-add-element) | Appends a child. |
| [`Align(Align)`](#m-align-align) | Sets `align-items`. |
| [`AlignSelf(Align)`](#m-alignself-align) | Sets `align-self`. |
| [`Anchor(AnchorPoint, Length, Length, bool)`](#m-anchor-anchorpoint-length-length-bool) | Pins the element at a point of its parent (or the screen); see [`StyleBuilder.Anchor()`](StyleBuilder.md#m-anchor-anchorpoint-length-length-bool). |
| [`Background(Color)`](#m-background-color) | Sets `background-color`. |
| [`Basis(Length)`](#m-basis-length) | Sets `flex-basis`. |
| [`BindChildren<T>(Func<IReadOnlyList<T>>, Func<T, Element>)`](#m-bindchildren-t-func-ireadonlylist-t-func-t-element) | Keeps the children in step with a list: `build` runs once per distinct item, and an item keeps its element while it stays in the list. |
| [`BindClass(string, Func<bool>)`](#m-bindclass-string-func-bool) | Turns a class on or off from a lambda. |
| [`BindEnabled(Func<bool>)`](#m-bindenabled-func-bool) | Enables or disables the element from a lambda. |
| [`BindStyle(Action<StyleBuilder>)`](#m-bindstyle-action-stylebuilder) | Re-applies inline style every frame, for values that follow game state; unchanged values cost nothing. |
| [`BindText(Func<string>)`](#m-bindtext-func-string) | Sets [`Element.Text`](Element.md#m-text) from a lambda, re-evaluated every frame. |
| [`BindText<T>(Func<T>, Func<T, string>)`](#m-bindtext-t-func-t-func-t-string) | Sets [`Element.Text`](Element.md#m-text) from `format` applied to a value that is re-read every frame; the text is rebuilt only when the value changes. |
| [`BindVisible(Func<bool>)`](#m-bindvisible-func-bool) | Shows or hides the element from a lambda. |
| [`Border(Length, Color)`](#m-border-length-color) | Draws a border on all sides. |
| [`ChildAt(int)`](#m-childat-int) | The child at `index`. |
| [`Class(string)`](#m-class-string) | Adds one or more classes, separated by spaces. |
| [`Clear()`](#m-clear) | Removes every child. |
| [`Column()`](#m-column) | Lays children out top to bottom. |
| [`Contains(Element?)`](#m-contains-element) | True when `other` is this element or lies below it. |
| [`Enabled(bool)`](#m-enabled-bool) | Enables or disables the element (and its descendants). |
| [`Focusable(bool)`](#m-focusable-bool) | Makes the element take focus from Tab, gamepad and pointer input. |
| [`FontSize(Length)`](#m-fontsize-length) | Sets `font-size`. |
| [`Gap(Length)`](#m-gap-length) | Sets both gaps. |
| [`Grow(float)`](#m-grow-float) | Sets `flex-grow`. |
| [`HasClass(string)`](#m-hasclass-string) | True when the element has the class. |
| [`Height(Length)`](#m-height-length) | Sets `height`. |
| [`Insert(int, Element)`](#m-insert-int-element) | Inserts a child at `index`. |
| [`Justify(Align)`](#m-justify-align) | Sets `justify-content`. |
| [`Margin(Length)`](#m-margin-length) | Sets `margin` on all sides. |
| [`Margin(Length, Length)`](#m-margin-length-length) | Sets vertical and horizontal `margin`. |
| [`MaxWidth(Length)`](#m-maxwidth-length) | Sets `max-width`. |
| [`MinWidth(Length)`](#m-minwidth-length) | Sets `min-width`. |
| [`Named(string)`](#m-named-string) | Sets the id and returns the element, for chaining. |
| [`On(UiEventKind, UiHandler)`](#m-on-uieventkind-uihandler) | Attaches a handler for an event kind. |
| [`OnBlur(Action)`](#m-onblur-action) | Runs `action` when the element loses focus. |
| [`OnCancel(Action)`](#m-oncancel-action) | Runs `action` when the back button is pressed while focus is in this element. |
| [`OnClick(Action)`](#m-onclick-action) | Runs `action` on click or activation. |
| [`OnClick(Action<Element>)`](#m-onclick-action-element) | Runs `action` with this element on click or activation. |
| [`OnFocus(Action)`](#m-onfocus-action) | Runs `action` when the element gains focus. |
| [`OnPointerEnter(Action)`](#m-onpointerenter-action) | Runs `action` when the pointer enters the element. |
| [`OnPointerLeave(Action)`](#m-onpointerleave-action) | Runs `action` when the pointer leaves the element. |
| [`OnUpdate(Action<Element>)`](#m-onupdate-action-element) | Runs a lambda against the element every frame, before styles are resolved. |
| [`Overflow(OverflowMode)`](#m-overflow-overflowmode) | Sets `overflow` on both axes. |
| [`Padding(Length)`](#m-padding-length) | Sets `padding` on all sides. |
| [`Padding(Length, Length)`](#m-padding-length-length) | Sets vertical and horizontal `padding`. |
| [`Radius(Length)`](#m-radius-length) | Rounds all corners. |
| [`Remove(Element)`](#m-remove-element) | Removes a child; returns false when it is not a child of this element. |
| [`RemoveFromParent()`](#m-removefromparent) | Removes this element from its parent, if it has one. |
| [`Row()`](#m-row) | Lays children out left to right. |
| [`ScrollTo(float, float)`](#m-scrollto-float-float) | Sets the scroll offsets, clamped to the scrollable range. |
| [`SetChildren(IReadOnlyList<Element>)`](#m-setchildren-ireadonlylist-element) | Makes the children exactly `wanted`, in order, reusing the ones already present and touching only the difference. |
| [`SetClass(string, bool)`](#m-setclass-string-bool) | Adds or removes a class. |
| [`Shrink(float)`](#m-shrink-float) | Sets `flex-shrink`. |
| [`Size(Length, Length)`](#m-size-length-length) | Sets `width` and `height`. |
| [`Style(Action<StyleBuilder>)`](#m-style-action-stylebuilder) | Applies inline style declarations. |
| [`Tab(int)`](#m-tab-int) | Sets the tab order. |
| [`TextColor(Color)`](#m-textcolor-color) | Sets the inherited text color. |
| [`ToString()`](#m-tostring) |  |
| [`TrapFocus(bool)`](#m-trapfocus-bool) | Makes focus navigation stay within this element while it is shown. |
| [`Width(Length)`](#m-width-length) | Sets `width`. |
| [`With(Element[])`](#m-with-element) | Appends children and returns this element. |
| [`Wrap()`](#m-wrap) | Lets children wrap onto further lines. |

### <a id="m-add-element"></a>Add(Element)

```csharp
public Element Add(Element child);
```

Appends a child.

**Parameters**

- `child` ([`Element`](Element.md))

**Returns:** [`Element`](Element.md)

**Exceptions**

- [`UiTreeException`](UiTreeException.md): The child already has a parent, or is this element or one of its ancestors.

### <a id="m-align-align"></a>Align(Align)

```csharp
public Element Align(Align v);
```

Sets `align-items`.

**Parameters**

- `v` ([`Align`](Align.md))

**Returns:** [`Element`](Element.md)

### <a id="m-alignself-align"></a>AlignSelf(Align)

```csharp
public Element AlignSelf(Align v);
```

Sets `align-self`.

**Parameters**

- `v` ([`Align`](Align.md))

**Returns:** [`Element`](Element.md)

### <a id="m-anchor-anchorpoint-length-length-bool"></a>Anchor(AnchorPoint, Length, Length, bool)

```csharp
public Element Anchor(AnchorPoint point, Length x = default, Length y = default, bool toScreen = false);
```

Pins the element at a point of its parent (or the screen); see [`StyleBuilder.Anchor()`](StyleBuilder.md#m-anchor-anchorpoint-length-length-bool).

**Parameters**

- `point` ([`AnchorPoint`](AnchorPoint.md))
- `x` ([`Length`](Length.md))
- `y` ([`Length`](Length.md))
- `toScreen` (`bool`)

**Returns:** [`Element`](Element.md)

### <a id="m-background-color"></a>Background(Color)

```csharp
public Element Background(Color v);
```

Sets `background-color`.

**Parameters**

- `v` ([`Color`](Color.md))

**Returns:** [`Element`](Element.md)

### <a id="m-basis-length"></a>Basis(Length)

```csharp
public Element Basis(Length v);
```

Sets `flex-basis`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-bindchildren-t-func-ireadonlylist-t-func-t-element"></a>BindChildren&lt;T&gt;(Func&lt;IReadOnlyList&lt;T&gt;&gt;, Func&lt;T, Element&gt;)

```csharp
public Element BindChildren<T>(Func<IReadOnlyList<T>> items, Func<T, Element> build);
```

Keeps the children in step with a list: `build` runs once per distinct item, and an item keeps its element while it stays in the list.

**Parameters**

- `items` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<T>>`)
- `build` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<T,` [`Element`](Element.md)&gt;)

**Returns:** [`Element`](Element.md)

### <a id="m-bindclass-string-func-bool"></a>BindClass(string, Func&lt;bool&gt;)

```csharp
public Element BindClass(string name, Func<bool> on);
```

Turns a class on or off from a lambda.

**Parameters**

- `name` (`string`)
- `on` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<bool>`)

**Returns:** [`Element`](Element.md)

### <a id="m-bindenabled-func-bool"></a>BindEnabled(Func&lt;bool&gt;)

```csharp
public Element BindEnabled(Func<bool> enabled);
```

Enables or disables the element from a lambda.

**Parameters**

- `enabled` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<bool>`)

**Returns:** [`Element`](Element.md)

### <a id="m-bindstyle-action-stylebuilder"></a>BindStyle(Action&lt;StyleBuilder&gt;)

```csharp
public Element BindStyle(Action<StyleBuilder> apply);
```

Re-applies inline style every frame, for values that follow game state; unchanged values cost nothing.

**Parameters**

- `apply` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`StyleBuilder`](StyleBuilder.md)&gt;)

**Returns:** [`Element`](Element.md)

### <a id="m-bindtext-func-string"></a>BindText(Func&lt;string&gt;)

```csharp
public Element BindText(Func<string> source);
```

Sets [`Element.Text`](Element.md#m-text) from a lambda, re-evaluated every frame. Returning a cached string keeps frames allocation-free.

**Parameters**

- `source` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<string>`)

**Returns:** [`Element`](Element.md)

### <a id="m-bindtext-t-func-t-func-t-string"></a>BindText&lt;T&gt;(Func&lt;T&gt;, Func&lt;T, string&gt;)

```csharp
public Element BindText<T>(Func<T> source, Func<T, string> format) where T : IEquatable<T>;
```

Sets [`Element.Text`](Element.md#m-text) from `format` applied to a value that is re-read every frame; the text is rebuilt only when the value changes.

**Parameters**

- `source` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<T>`)
- `format` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<T, string>`)

**Returns:** [`Element`](Element.md)

### <a id="m-bindvisible-func-bool"></a>BindVisible(Func&lt;bool&gt;)

```csharp
public Element BindVisible(Func<bool> visible);
```

Shows or hides the element from a lambda.

**Parameters**

- `visible` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<bool>`)

**Returns:** [`Element`](Element.md)

### <a id="m-border-length-color"></a>Border(Length, Color)

```csharp
public Element Border(Length width, Color color);
```

Draws a border on all sides.

**Parameters**

- `width` ([`Length`](Length.md))
- `color` ([`Color`](Color.md))

**Returns:** [`Element`](Element.md)

### <a id="m-childat-int"></a>ChildAt(int)

```csharp
public Element ChildAt(int index);
```

The child at `index`.

**Parameters**

- `index` (`int`)

**Returns:** [`Element`](Element.md)

### <a id="m-class-string"></a>Class(string)

```csharp
public Element Class(string names);
```

Adds one or more classes, separated by spaces.

**Parameters**

- `names` (`string`)

**Returns:** [`Element`](Element.md)

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Removes every child.

### <a id="m-column"></a>Column()

```csharp
public Element Column();
```

Lays children out top to bottom.

**Returns:** [`Element`](Element.md)

### <a id="m-contains-element"></a>Contains(Element?)

```csharp
public bool Contains(Element? other);
```

True when `other` is this element or lies below it.

**Parameters**

- `other` ([`Element`](Element.md)?)

**Returns:** `bool`

### <a id="m-enabled-bool"></a>Enabled(bool)

```csharp
public Element Enabled(bool on);
```

Enables or disables the element (and its descendants).

**Parameters**

- `on` (`bool`)

**Returns:** [`Element`](Element.md)

### <a id="m-focusable-bool"></a>Focusable(bool)

```csharp
public Element Focusable(bool on = true);
```

Makes the element take focus from Tab, gamepad and pointer input.

**Parameters**

- `on` (`bool`)

**Returns:** [`Element`](Element.md)

### <a id="m-fontsize-length"></a>FontSize(Length)

```csharp
public Element FontSize(Length v);
```

Sets `font-size`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-gap-length"></a>Gap(Length)

```csharp
public Element Gap(Length v);
```

Sets both gaps.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-grow-float"></a>Grow(float)

```csharp
public Element Grow(float v = 1f);
```

Sets `flex-grow`.

**Parameters**

- `v` (`float`)

**Returns:** [`Element`](Element.md)

### <a id="m-hasclass-string"></a>HasClass(string)

```csharp
public bool HasClass(string name);
```

True when the element has the class.

**Parameters**

- `name` (`string`)

**Returns:** `bool`

### <a id="m-height-length"></a>Height(Length)

```csharp
public Element Height(Length v);
```

Sets `height`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-insert-int-element"></a>Insert(int, Element)

```csharp
public Element Insert(int index, Element child);
```

Inserts a child at `index`.

**Parameters**

- `index` (`int`)
- `child` ([`Element`](Element.md))

**Returns:** [`Element`](Element.md)

**Exceptions**

- [`UiTreeException`](UiTreeException.md): The child already has a parent, or is this element or one of its ancestors.

### <a id="m-justify-align"></a>Justify(Align)

```csharp
public Element Justify(Align v);
```

Sets `justify-content`.

**Parameters**

- `v` ([`Align`](Align.md))

**Returns:** [`Element`](Element.md)

### <a id="m-margin-length"></a>Margin(Length)

```csharp
public Element Margin(Length v);
```

Sets `margin` on all sides.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-margin-length-length"></a>Margin(Length, Length)

```csharp
public Element Margin(Length vertical, Length horizontal);
```

Sets vertical and horizontal `margin`.

**Parameters**

- `vertical` ([`Length`](Length.md))
- `horizontal` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-maxwidth-length"></a>MaxWidth(Length)

```csharp
public Element MaxWidth(Length v);
```

Sets `max-width`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-minwidth-length"></a>MinWidth(Length)

```csharp
public Element MinWidth(Length v);
```

Sets `min-width`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-named-string"></a>Named(string)

```csharp
public Element Named(string id);
```

Sets the id and returns the element, for chaining.

**Parameters**

- `id` (`string`)

**Returns:** [`Element`](Element.md)

### <a id="m-on-uieventkind-uihandler"></a>On(UiEventKind, UiHandler)

```csharp
public Element On(UiEventKind kind, UiHandler handler);
```

Attaches a handler for an event kind. Returning true marks the event handled.

**Parameters**

- `kind` ([`UiEventKind`](UiEventKind.md))
- `handler` ([`UiHandler`](UiHandler.md))

**Returns:** [`Element`](Element.md)

### <a id="m-onblur-action"></a>OnBlur(Action)

```csharp
public Element OnBlur(Action action);
```

Runs `action` when the element loses focus.

**Parameters**

- `action` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action))

**Returns:** [`Element`](Element.md)

### <a id="m-oncancel-action"></a>OnCancel(Action)

```csharp
public Element OnCancel(Action action);
```

Runs `action` when the back button is pressed while focus is in this element.

**Parameters**

- `action` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action))

**Returns:** [`Element`](Element.md)

### <a id="m-onclick-action"></a>OnClick(Action)

```csharp
public Element OnClick(Action action);
```

Runs `action` on click or activation.

**Parameters**

- `action` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action))

**Returns:** [`Element`](Element.md)

### <a id="m-onclick-action-element"></a>OnClick(Action&lt;Element&gt;)

```csharp
public Element OnClick(Action<Element> action);
```

Runs `action` with this element on click or activation.

**Parameters**

- `action` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`Element`](Element.md)&gt;)

**Returns:** [`Element`](Element.md)

### <a id="m-onfocus-action"></a>OnFocus(Action)

```csharp
public Element OnFocus(Action action);
```

Runs `action` when the element gains focus.

**Parameters**

- `action` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action))

**Returns:** [`Element`](Element.md)

### <a id="m-onpointerenter-action"></a>OnPointerEnter(Action)

```csharp
public Element OnPointerEnter(Action action);
```

Runs `action` when the pointer enters the element.

**Parameters**

- `action` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action))

**Returns:** [`Element`](Element.md)

### <a id="m-onpointerleave-action"></a>OnPointerLeave(Action)

```csharp
public Element OnPointerLeave(Action action);
```

Runs `action` when the pointer leaves the element.

**Parameters**

- `action` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action))

**Returns:** [`Element`](Element.md)

### <a id="m-onupdate-action-element"></a>OnUpdate(Action&lt;Element&gt;)

```csharp
public Element OnUpdate(Action<Element> update);
```

Runs a lambda against the element every frame, before styles are resolved.

**Parameters**

- `update` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`Element`](Element.md)&gt;)

**Returns:** [`Element`](Element.md)

### <a id="m-overflow-overflowmode"></a>Overflow(OverflowMode)

```csharp
public Element Overflow(OverflowMode v);
```

Sets `overflow` on both axes.

**Parameters**

- `v` ([`OverflowMode`](OverflowMode.md))

**Returns:** [`Element`](Element.md)

### <a id="m-padding-length"></a>Padding(Length)

```csharp
public Element Padding(Length v);
```

Sets `padding` on all sides.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-padding-length-length"></a>Padding(Length, Length)

```csharp
public Element Padding(Length vertical, Length horizontal);
```

Sets vertical and horizontal `padding`.

**Parameters**

- `vertical` ([`Length`](Length.md))
- `horizontal` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-radius-length"></a>Radius(Length)

```csharp
public Element Radius(Length v);
```

Rounds all corners.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-remove-element"></a>Remove(Element)

```csharp
public bool Remove(Element child);
```

Removes a child; returns false when it is not a child of this element.

**Parameters**

- `child` ([`Element`](Element.md))

**Returns:** `bool`

### <a id="m-removefromparent"></a>RemoveFromParent()

```csharp
public void RemoveFromParent();
```

Removes this element from its parent, if it has one.

### <a id="m-row"></a>Row()

```csharp
public Element Row();
```

Lays children out left to right.

**Returns:** [`Element`](Element.md)

### <a id="m-scrollto-float-float"></a>ScrollTo(float, float)

```csharp
public void ScrollTo(float x, float y);
```

Sets the scroll offsets, clamped to the scrollable range. Only elements with a scrolling `overflow` scroll.

**Parameters**

- `x` (`float`)
- `y` (`float`)

### <a id="m-setchildren-ireadonlylist-element"></a>SetChildren(IReadOnlyList&lt;Element&gt;)

```csharp
public void SetChildren(IReadOnlyList<Element> wanted);
```

Makes the children exactly `wanted`, in order, reusing the ones already present and touching only the difference. Elements that are no longer wanted are removed.

**Parameters**

- `wanted` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Element`](Element.md)&gt;)

### <a id="m-setclass-string-bool"></a>SetClass(string, bool)

```csharp
public Element SetClass(string name, bool on);
```

Adds or removes a class. Does nothing when the element already is in that state.

**Parameters**

- `name` (`string`)
- `on` (`bool`)

**Returns:** [`Element`](Element.md)

### <a id="m-shrink-float"></a>Shrink(float)

```csharp
public Element Shrink(float v);
```

Sets `flex-shrink`.

**Parameters**

- `v` (`float`)

**Returns:** [`Element`](Element.md)

### <a id="m-size-length-length"></a>Size(Length, Length)

```csharp
public Element Size(Length w, Length h);
```

Sets `width` and `height`.

**Parameters**

- `w` ([`Length`](Length.md))
- `h` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-style-action-stylebuilder"></a>Style(Action&lt;StyleBuilder&gt;)

```csharp
public Element Style(Action<StyleBuilder> declare);
```

Applies inline style declarations. Inline style beats every style sheet rule.

**Parameters**

- `declare` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`StyleBuilder`](StyleBuilder.md)&gt;)

**Returns:** [`Element`](Element.md)

### <a id="m-tab-int"></a>Tab(int)

```csharp
public Element Tab(int tabIndex);
```

Sets the tab order.

**Parameters**

- `tabIndex` (`int`)

**Returns:** [`Element`](Element.md)

### <a id="m-textcolor-color"></a>TextColor(Color)

```csharp
public Element TextColor(Color v);
```

Sets the inherited text color.

**Parameters**

- `v` ([`Color`](Color.md))

**Returns:** [`Element`](Element.md)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

Overrides [`Object.ToString()`](https://learn.microsoft.com/dotnet/api/system.object.tostring).

**Returns:** `string`

### <a id="m-trapfocus-bool"></a>TrapFocus(bool)

```csharp
public Element TrapFocus(bool on = true);
```

Makes focus navigation stay within this element while it is shown.

**Parameters**

- `on` (`bool`)

**Returns:** [`Element`](Element.md)

### <a id="m-width-length"></a>Width(Length)

```csharp
public Element Width(Length v);
```

Sets `width`.

**Parameters**

- `v` ([`Length`](Length.md))

**Returns:** [`Element`](Element.md)

### <a id="m-with-element"></a>With(Element\[\])

```csharp
public Element With(params Element[] children);
```

Appends children and returns this element.

**Parameters**

- `children` ([`Element`](Element.md)\[\])

**Returns:** [`Element`](Element.md)

### <a id="m-wrap"></a>Wrap()

```csharp
public Element Wrap();
```

Lets children wrap onto further lines.

**Returns:** [`Element`](Element.md)
