[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# FocusHelpers static class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)

```csharp
public static class FocusHelpers
```

Focus helpers: a focus group that keeps arrow keys inside a set of controls, a default focus target, and a way to give a screen its first focus.

## Methods

| Member | Description |
| --- | --- |
| [`AsDefaultFocus(Element)`](#m-asdefaultfocus-element) | Marks an element as the one [`FocusHelpers.FocusDefault()`](FocusHelpers.md#m-focusdefault-uitree-element) focuses first on its screen. |
| [`FocusDefault(UiTree, Element)`](#m-focusdefault-uitree-element) | Focuses the first element below `screen` marked with [`FocusHelpers.AsDefaultFocus()`](FocusHelpers.md#m-asdefaultfocus-element) that can take focus, or else the first focusable element. |
| [`FocusGroup(Element, bool, bool)`](#m-focusgroup-element-bool-bool) | Makes `container` a focus group: the arrow keys and d-pad along `horizontal`'s axis move focus between the focusable descendants in document order. |

### <a id="m-asdefaultfocus-element"></a>AsDefaultFocus(Element)

```csharp
public static Element AsDefaultFocus(this Element element);
```

Marks an element as the one [`FocusHelpers.FocusDefault()`](FocusHelpers.md#m-focusdefault-uitree-element) focuses first on its screen.

**Parameters**

- `element` ([`Element`](../../OneGame.UI/OneGame.UI/Element.md))

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md) — `element`, for chaining.

### <a id="m-focusdefault-uitree-element"></a>FocusDefault(UiTree, Element)

```csharp
public static Element? FocusDefault(UiTree tree, Element screen);
```

Focuses the first element below `screen` marked with [`FocusHelpers.AsDefaultFocus()`](FocusHelpers.md#m-asdefaultfocus-element) that can take focus, or else the first focusable element. Call it when a screen opens, so a controller player is never left with nothing focused.

**Parameters**

- `tree` ([`UiTree`](../../OneGame.UI/OneGame.UI/UiTree.md))
- `screen` ([`Element`](../../OneGame.UI/OneGame.UI/Element.md))

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)? — The element that took focus, or null when the screen has nothing focusable.

### <a id="m-focusgroup-element-bool-bool"></a>FocusGroup(Element, bool, bool)

```csharp
public static Element FocusGroup(Element container, bool horizontal, bool wrap = false);
```

Makes `container` a focus group: the arrow keys and d-pad along `horizontal`'s axis move focus between the focusable descendants in document order. At the first or last member the event is not used, so focus leaves the group in that direction (unless `wrap` is set). The other axis and Tab behave as everywhere else.

**Parameters**

- `container` ([`Element`](../../OneGame.UI/OneGame.UI/Element.md)): The element holding the group's members.
- `horizontal` (`bool`): True for a row of controls (Left/Right), false for a column (Up/Down).
- `wrap` (`bool`): True to wrap from the last member to the first.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md) — `container`, for chaining.
