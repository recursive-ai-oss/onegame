[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# UiKit static class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)

```csharp
public static class UiKit
```

The component kit: factory methods that return ready-made [`Element`](../../OneGame.UI/OneGame.UI/Element.md) trees for the controls a game screen needs. Every component is "controlled": it reads its value from a lambda you give it and reports changes through another, so the game's own state stays the single source of truth. Components are styled by classes (`kit-button`, `kit-slider` and so on) and theme tokens (`--kit-accent` and so on) from [`KitTheme`](KitTheme.md), which a game extends instead of forking. They set [`Element.Role`](../../OneGame.UI/OneGame.UI/Element.md#m-role) and [`Element.Value`](../../OneGame.UI/OneGame.UI/Element.md#m-value), which the JSON dump prints, and take an optional `id` that also names their parts (`id/thumb`).

## Fields

| Member | Description |
| --- | --- |
| [`DefaultFocusClass`](#m-defaultfocusclass) | The class that marks the element a screen should focus first; see [`FocusHelpers.FocusDefault()`](FocusHelpers.md#m-focusdefault-uitree-element). |
| [`FocusableClass`](#m-focusableclass) | The class every focusable component carries; the theme draws the focus ring on `.kit-focusable:focus`. |

### <a id="m-defaultfocusclass"></a>DefaultFocusClass

```csharp
public const string DefaultFocusClass = "kit-default-focus";
```

The class that marks the element a screen should focus first; see [`FocusHelpers.FocusDefault()`](FocusHelpers.md#m-focusdefault-uitree-element).

**Type:** `string`

### <a id="m-focusableclass"></a>FocusableClass

```csharp
public const string FocusableClass = "kit-focusable";
```

The class every focusable component carries; the theme draws the focus ring on `.kit-focusable:focus`.

**Type:** `string`

## Properties

| Member | Description |
| --- | --- |
| [`DefaultClipboard`](#m-defaultclipboard) | The clipboard text fields use unless they are given one. |

### <a id="m-defaultclipboard"></a>DefaultClipboard

```csharp
public static IClipboard DefaultClipboard { get; set; }
```

The clipboard text fields use unless they are given one. It keeps text inside the process until a windowed game instance replaces it with the system clipboard; a game can set its own.

**Type:** [`IClipboard`](IClipboard.md)

## Methods

| Member | Description |
| --- | --- |
| [`Button(Func<string>, Action, string?, ButtonKind)`](#m-button-func-string-action-string-buttonkind) | A button whose label follows a lambda, such as a price that changes. |
| [`Button(string, Action, string?, ButtonKind)`](#m-button-string-action-string-buttonkind) | A button (kind `Button`, role `button`) that runs `onClick` when clicked, or activated with Enter, Space or the confirm button. |
| [`Card(string?, Element[])`](#m-card-string-element) | A card (kind `Card`, role `card`): a raised panel with an optional title above its content. |
| [`ConfirmDialog(string, string, Action, Action?, string, string, string?, bool)`](#m-confirmdialog-string-string-action-action-string-string-string-bool) | A confirm dialog with a message and two buttons (`id/buttons/0` cancel, `id/buttons/1` confirm). |
| [`Dialog(string, Element, string?, Action?)`](#m-dialog-string-element-string-action) | A modal dialog with your content; add buttons with [`DialogView.AddButton()`](DialogView.md#m-addbutton-string-action-buttonkind-bool-string). |
| [`Dropdown<T>(IReadOnlyList<Choice<T>>, Func<T>, Action<T>, string?, string)`](#m-dropdown-t-ireadonlylist-choice-t-func-t-action-t-string-string) | A drop-down select (kind `Dropdown`, role `combobox`): a button showing the chosen option that opens a list of options below it (above it when there is no room). |
| [`Frame(string, float, Element[])`](#m-frame-string-float-element) | A panel framed with nine-slice art (kind `Frame`): the image `artId` is stretched so its corners keep their size, and the padding is the slice so content stays inside the border. |
| [`InputGlyph(InputGlyphs, string, string?)`](#m-inputglyph-inputglyphs-string-string) | A picture of the control an action uses on the device the player used last (kind `InputGlyph`, role `img`). |
| [`NumberStepper(Func<int>, Action<int>, int, int, int, string?, Func<int, string>?)`](#m-numberstepper-func-int-action-int-int-int-int-string-func-int-string) | A number stepper (kind `Stepper`, role `spinbutton`): minus and plus buttons around the value. |
| [`Progress(Func<float>, float, string?)`](#m-progress-func-float-float-string) | A progress bar (kind `Progress`, role `progressbar`). |
| [`RadioGroup<T>(IReadOnlyList<Choice<T>>, Func<T>, Action<T>, string?, bool)`](#m-radiogroup-t-ireadonlylist-choice-t-func-t-action-t-string-bool) | A group of mutually exclusive options (kind `RadioGroup`, role `radiogroup`; each option is kind `Radio`, role `radio`, ids `id/0`, `id/1`...). |
| [`ScrollView(Element, bool, string?)`](#m-scrollview-element-bool-string) | Wraps `content` in a scroll view with scroll bars. |
| [`Slider(Func<int>, Action<int>, int, int, int, string?, bool)`](#m-slider-func-int-action-int-int-int-int-string-bool) | A slider over whole numbers; see [`UiKit.Slider()`](UiKit.md#m-slider-func-float-action-float-float-float-float-string-func-float-string-bool). |
| [`Slider(Func<float>, Action<float>, float, float, float, string?, Func<float, string>?, bool)`](#m-slider-func-float-action-float-float-float-float-string-func-float-string-bool) | A slider (kind `Slider`, role `slider`) for a number between `min` and `max`, snapped to multiples of `step` from `min`. |
| [`Table<T>(IReadOnlyList<TableColumn<T>>, Func<IReadOnlyList<T>>, Length, Action<int, bool>?, Action<T, int>?, Action<T, int>?, string?, Func<int>?)`](#m-table-t-ireadonlylist-tablecolumn-t-func-ireadonlylist-t-length-action-int-bool-action-t-int-action-t-int-string-func-int) | A sortable, virtualized table: see [`TableView<T>`](TableView-1.md). |
| [`Tabs(IReadOnlyList<TabPage>, Func<int>, Action<int>, string?)`](#m-tabs-ireadonlylist-tabpage-func-int-action-int-string) | A tab view (kind `Tabs`): a row of tabs above the content of the selected page. |
| [`TextInput(Func<string>?, Action<string>?, string, bool, int, Func<string, bool>?, Action<string>?, IClipboard?, string?)`](#m-textinput-func-string-action-string-string-bool-int-func-string-bool-action-string-iclipboard-string) | A single-line text field. |
| [`TextList<T>(Func<IReadOnlyList<T>>, Func<T, string>, Length, Action<T, int>?, Action<T, int>?, string?)`](#m-textlist-t-func-ireadonlylist-t-func-t-string-length-action-t-int-action-t-int-string) | A virtualized list of text rows, one line per item: see [`UiKit.VirtualList()`](UiKit.md#m-virtuallist-t-func-ireadonlylist-t-func-element-action-element-t-int-length-action-t-int-action-t-int-string-func-int). |
| [`Toggle(string, Func<bool>, Action<bool>, string?)`](#m-toggle-string-func-bool-action-bool-string) | A checkbox with a label (kind `Toggle`, role `checkbox`). |
| [`ToggleButton(string, Func<bool>, Action<bool>, string?, ButtonKind)`](#m-togglebutton-string-func-bool-action-bool-string-buttonkind) | A toggle button (kind `Button`, role `button`) that is switched on or off, like a button with `aria-pressed`: a "FAST" speed switch, a bold-text toggle, a filter chip. |
| [`VirtualList<T>(Func<IReadOnlyList<T>>, Func<Element>, Action<Element, T, int>, Length, Action<T, int>?, Action<T, int>?, string?, Func<int>?)`](#m-virtuallist-t-func-ireadonlylist-t-func-element-action-element-t-int-length-action-t-int-action-t-int-string-func-int) | A virtualized list: see [`ListView<T>`](ListView-1.md). |

### <a id="m-button-func-string-action-string-buttonkind"></a>Button(Func&lt;string&gt;, Action, string?, ButtonKind)

```csharp
public static Element Button(Func<string> text, Action onClick, string? id = null, ButtonKind kind = 0);
```

A button whose label follows a lambda, such as a price that changes. See [`UiKit.Button()`](UiKit.md#m-button-string-action-string-buttonkind).

**Parameters**

- `text` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<string>`)
- `onClick` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action))
- `id` (`string?`)
- `kind` ([`ButtonKind`](ButtonKind.md))

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-button-string-action-string-buttonkind"></a>Button(string, Action, string?, ButtonKind)

```csharp
public static Element Button(string text, Action onClick, string? id = null, ButtonKind kind = 0);
```

A button (kind `Button`, role `button`) that runs `onClick` when clicked, or activated with Enter, Space or the confirm button.

**Parameters**

- `text` (`string`): The label.
- `onClick` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action)): Called on activation. A disabled button ([`Element.IsEnabled`](../../OneGame.UI/OneGame.UI/Element.md#m-isenabled) false) never calls it.
- `id` (`string?`): The id, or null.
- `kind` ([`ButtonKind`](ButtonKind.md)): The look.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-card-string-element"></a>Card(string?, Element\[\])

```csharp
public static Element Card(string? title, params Element[] content);
```

A card (kind `Card`, role `card`): a raised panel with an optional title above its content.

**Parameters**

- `title` (`string?`): The heading, or null for none.
- `content` ([`Element`](../../OneGame.UI/OneGame.UI/Element.md)\[\]): The content, laid out top to bottom.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-confirmdialog-string-string-action-action-string-string-string-bool"></a>ConfirmDialog(string, string, Action, Action?, string, string, string?, bool)

```csharp
public static DialogView ConfirmDialog(string title, string message, Action onConfirm, Action? onCancel = null, string confirmText = "OK", string cancelText = "Cancel", string? id = null, bool danger = false);
```

A confirm dialog with a message and two buttons (`id/buttons/0` cancel, `id/buttons/1` confirm). Both close the dialog; Escape cancels.

**Parameters**

- `title` (`string`): The heading.
- `message` (`string`): The question.
- `onConfirm` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action)): Called when the player confirms.
- `onCancel` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action)?): Called when the player cancels or presses Escape; may be null.
- `confirmText` (`string`): The confirm button's label.
- `cancelText` (`string`): The cancel button's label.
- `id` (`string?`): The id, or null.
- `danger` (`bool`): Style the confirm button as destructive and focus Cancel first, so an accidental Enter does not confirm.

**Returns:** [`DialogView`](DialogView.md)

### <a id="m-dialog-string-element-string-action"></a>Dialog(string, Element, string?, Action?)

```csharp
public static DialogView Dialog(string title, Element body, string? id = null, Action? onCancel = null);
```

A modal dialog with your content; add buttons with [`DialogView.AddButton()`](DialogView.md#m-addbutton-string-action-buttonkind-bool-string). See [`DialogView`](DialogView.md).

Add the dialog to the tree last (or give it its own screen layer) so it paints over everything; it is hidden until opened.

**Parameters**

- `title` (`string`): The heading.
- `body` ([`Element`](../../OneGame.UI/OneGame.UI/Element.md)): The content between the heading and the buttons.
- `id` (`string?`): The id, or null.
- `onCancel` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action)?): Called after Escape or the back button closes it.

**Returns:** [`DialogView`](DialogView.md)

### <a id="m-dropdown-t-ireadonlylist-choice-t-func-t-action-t-string-string"></a>Dropdown&lt;T&gt;(IReadOnlyList&lt;Choice&lt;T&gt;&gt;, Func&lt;T&gt;, Action&lt;T&gt;, string?, string)

```csharp
public static Element Dropdown<T>(IReadOnlyList<Choice<T>> choices, Func<T> get, Action<T> set, string? id = null, string placeholder = "");
```

A drop-down select (kind `Dropdown`, role `combobox`): a button showing the chosen option that opens a list of options below it (above it when there is no room). Enter, Space, the confirm button or a click opens it and focuses the chosen option; Up/Down (arrow keys or d-pad) move through the options, which trap focus while open; Enter or a click chooses, Escape or the back button closes without choosing, and focus returns to the button either way. While open the element has the class `open` and its [`Element.Value`](../../OneGame.UI/OneGame.UI/Element.md#m-value) is still the chosen label. Parts: `id/head` (the button), `id/popup`, `id/option/n`.

**Type parameters**

- `T`: The value type.

**Parameters**

- `choices` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Choice`](Choice-1.md)`<T>>`): The options, in order.
- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<T>`): Reads the chosen value, every frame.
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<T>`): Called with the value the player chose.
- `id` (`string?`): The id, or null.
- `placeholder` (`string`): Shown while the value matches no option.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-frame-string-float-element"></a>Frame(string, float, Element\[\])

```csharp
public static Element Frame(string artId, float slice, params Element[] content);
```

A panel framed with nine-slice art (kind `Frame`): the image `artId` is stretched so its corners keep their size, and the padding is the slice so content stays inside the border. Register the art with the atlas ([`KitArt.Frame`](KitArt.md#m-frame) is one to start with).

**Parameters**

- `artId` (`string`): The asset id registered in the art atlas.
- `slice` (`float`): The width of the unstretched border, in UI units (the insets the art was registered with, at scale 1).
- `content` ([`Element`](../../OneGame.UI/OneGame.UI/Element.md)\[\]): The content, laid out top to bottom.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-inputglyph-inputglyphs-string-string"></a>InputGlyph(InputGlyphs, string, string?)

```csharp
public static Element InputGlyph(InputGlyphs glyphs, string action, string? id = null);
```

A picture of the control an action uses on the device the player used last (kind `InputGlyph`, role `img`). [`Element.Value`](../../OneGame.UI/OneGame.UI/Element.md#m-value) is `device:name`, such as `gamepad:a` or `keyboard:Enter`. Keyboard glyphs are a key cap with the key's name as text; the others are pictures from [`KitArt`](KitArt.md), which must be registered in the art atlas. The element has the class `keyboard`, `mouse` or `gamepad`, and `unbound` when the action has no binding.

**Parameters**

- `glyphs` ([`InputGlyphs`](InputGlyphs.md)): The bindings and last-used device.
- `action` (`string`): The action to show.
- `id` (`string?`): The id, or null.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-numberstepper-func-int-action-int-int-int-int-string-func-int-string"></a>NumberStepper(Func&lt;int&gt;, Action&lt;int&gt;, int, int, int, string?, Func&lt;int, string&gt;?)

```csharp
public static Element NumberStepper(Func<int> get, Action<int> set, int min, int max, int step = 1, string? id = null, Func<int, string>? format = null);
```

A number stepper (kind `Stepper`, role `spinbutton`): minus and plus buttons around the value. With focus, Left/Right change the value by one step, PageUp/PageDown by ten, Home/End jump to the ends; holding a key repeats. Parts: `id/minus`, `id/value`, `id/plus`.

**Parameters**

- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<int>`): Reads the value, every frame.
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<int>`): Called with the new value, already clamped to the range.
- `min` (`int`): The smallest value.
- `max` (`int`): The largest value; must be at least `min`.
- `step` (`int`): The change per press; must be positive.
- `id` (`string?`): The id, or null.
- `format` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<int, string>?`): Formats the number; the default is the plain integer.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-progress-func-float-float-string"></a>Progress(Func&lt;float&gt;, float, string?)

```csharp
public static Element Progress(Func<float> value, float max = 1f, string? id = null);
```

A progress bar (kind `Progress`, role `progressbar`). [`Element.Value`](../../OneGame.UI/OneGame.UI/Element.md#m-value) is the fraction filled, such as `0.45`.

**Parameters**

- `value` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<float>`): Reads the current amount, every frame.
- `max` (`float`): The amount that means full.
- `id` (`string?`): The id, or null.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-radiogroup-t-ireadonlylist-choice-t-func-t-action-t-string-bool"></a>RadioGroup&lt;T&gt;(IReadOnlyList&lt;Choice&lt;T&gt;&gt;, Func&lt;T&gt;, Action&lt;T&gt;, string?, bool)

```csharp
public static Element RadioGroup<T>(IReadOnlyList<Choice<T>> choices, Func<T> get, Action<T> set, string? id = null, bool horizontal = false);
```

A group of mutually exclusive options (kind `RadioGroup`, role `radiogroup`; each option is kind `Radio`, role `radio`, ids `id/0`, `id/1`...). Only the selected option takes Tab focus; the arrow keys and the d-pad move to the neighbor and select it, and the group is left with the other directions.

**Type parameters**

- `T`: The value type.

**Parameters**

- `choices` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Choice`](Choice-1.md)`<T>>`): The options, in order.
- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<T>`): Reads the selected value, every frame.
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<T>`): Called with the value of the option the player picks.
- `id` (`string?`): The id, or null.
- `horizontal` (`bool`): Lay the options out in a row; the default is a column.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-scrollview-element-bool-string"></a>ScrollView(Element, bool, string?)

```csharp
public static ScrollView ScrollView(Element content, bool horizontal = false, string? id = null);
```

Wraps `content` in a scroll view with scroll bars. See [`UiKit.ScrollView()`](UiKit.md#m-scrollview-element-bool-string).

**Parameters**

- `content` ([`Element`](../../OneGame.UI/OneGame.UI/Element.md)): What scrolls.
- `horizontal` (`bool`): Also scroll sideways when the content is wider than the view; by default only vertically.
- `id` (`string?`): The id, or null.

**Returns:** [`ScrollView`](ScrollView.md)

### <a id="m-slider-func-int-action-int-int-int-int-string-bool"></a>Slider(Func&lt;int&gt;, Action&lt;int&gt;, int, int, int, string?, bool)

```csharp
public static Element Slider(Func<int> get, Action<int> set, int min, int max, int step = 1, string? id = null, bool showValue = true);
```

A slider over whole numbers; see [`UiKit.Slider()`](UiKit.md#m-slider-func-float-action-float-float-float-float-string-func-float-string-bool).

**Parameters**

- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<int>`)
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<int>`)
- `min` (`int`)
- `max` (`int`)
- `step` (`int`)
- `id` (`string?`)
- `showValue` (`bool`)

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-slider-func-float-action-float-float-float-float-string-func-float-string-bool"></a>Slider(Func&lt;float&gt;, Action&lt;float&gt;, float, float, float, string?, Func&lt;float, string&gt;?, bool)

```csharp
public static Element Slider(Func<float> get, Action<float> set, float min, float max, float step = 1f, string? id = null, Func<float, string>? format = null, bool showValue = true);
```

A slider (kind `Slider`, role `slider`) for a number between `min` and `max`, snapped to multiples of `step` from `min`. Drag the thumb or click the track; with focus, Left/Right (arrow keys or d-pad) change the value by one step, PageUp/PageDown by ten, Home/End jump to the ends. Up and Down move focus away. Parts: `id/track`, `id/fill`, `id/thumb`, `id/value`. [`Element.Value`](../../OneGame.UI/OneGame.UI/Element.md#m-value) is the formatted number.

**Parameters**

- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<float>`): Reads the value, every frame.
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<float>`): Called with the new snapped value, only when it differs from the current one.
- `min` (`float`): The smallest value.
- `max` (`float`): The largest value; must be greater than `min`.
- `step` (`float`): The snapping step; must be positive.
- `id` (`string?`): The id, or null.
- `format` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<float, string>?`): Formats the value for the label and [`Element.Value`](../../OneGame.UI/OneGame.UI/Element.md#m-value); the default prints whole numbers for whole steps and up to two decimals otherwise.
- `showValue` (`bool`): Show the number beside the track.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-table-t-ireadonlylist-tablecolumn-t-func-ireadonlylist-t-length-action-int-bool-action-t-int-action-t-int-string-func-int"></a>Table&lt;T&gt;(IReadOnlyList&lt;TableColumn&lt;T&gt;&gt;, Func&lt;IReadOnlyList&lt;T&gt;&gt;, Length, Action&lt;int, bool&gt;?, Action&lt;T, int&gt;?, Action&lt;T, int&gt;?, string?, Func&lt;int&gt;?)

```csharp
public static TableView<T> Table<T>(IReadOnlyList<TableColumn<T>> columns, Func<IReadOnlyList<T>> rows, Length rowHeight, Action<int, bool>? onSort = null, Action<T, int>? onSelect = null, Action<T, int>? onActivate = null, string? id = null, Func<int>? version = null);
```

A sortable, virtualized table: see [`TableView<T>`](TableView-1.md).

**Type parameters**

- `T`: The row item type.

**Parameters**

- `columns` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`TableColumn`](TableColumn-1.md)`<T>>`): The columns, left to right.
- `rows` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<T>>`): Returns the rows, every frame. Return the same list instance while it is unchanged; a new instance (for example after filtering), a different count or a different `version` refreshes the rows.
- `rowHeight` ([`Length`](../../OneGame.UI/OneGame.UI/Length.md)): The height of every row, in UI units or pixels.
- `onSort` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<int, bool>?`): Called when a sortable header is clicked: the column index and whether the order is descending. Sort the list you return from `rows`.
- `onSelect` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<T, int>?`): Called with the item and its index when the selection changes.
- `onActivate` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<T, int>?`): Called when Enter, Space or the confirm button is pressed on the selected row.
- `id` (`string?`): The id, or null.
- `version` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<int>?`): Optional counter the game bumps when rows change in place.

**Returns:** [`TableView`](TableView-1.md)`<T>`

### <a id="m-tabs-ireadonlylist-tabpage-func-int-action-int-string"></a>Tabs(IReadOnlyList&lt;TabPage&gt;, Func&lt;int&gt;, Action&lt;int&gt;, string?)

```csharp
public static Element Tabs(IReadOnlyList<TabPage> pages, Func<int> selected, Action<int> select, string? id = null);
```

A tab view (kind `Tabs`): a row of tabs above the content of the selected page. Pages stay in the tree while hidden, so their state (scroll position, text typed) survives. Tabs are kind `Tab`, role `tab`, ids `id/tab/0`...; the strip is role `tablist` (`id/list`); each panel is role `tabpanel` (`id/panel/0`...). Only the selected tab takes Tab focus. With focus on a tab, Left/Right (arrow keys or d-pad) select and focus the neighbor; Ctrl+PageUp and Ctrl+PageDown switch tabs from anywhere inside.

**Parameters**

- `pages` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`TabPage`](TabPage.md)&gt;): The pages, in tab order.
- `selected` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<int>`): Reads the selected page index, every frame.
- `select` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<int>`): Called with the index of the tab the player picked.
- `id` (`string?`): The id, or null.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-textinput-func-string-action-string-string-bool-int-func-string-bool-action-string-iclipboard-string"></a>TextInput(Func&lt;string&gt;?, Action&lt;string&gt;?, string, bool, int, Func&lt;string, bool&gt;?, Action&lt;string&gt;?, IClipboard?, string?)

```csharp
public static TextInputView TextInput(Func<string>? get = null, Action<string>? set = null, string placeholder = "", bool password = false, int maxLength = 2147483647, Func<string, bool>? validate = null, Action<string>? onSubmit = null, IClipboard? clipboard = null, string? id = null);
```

A single-line text field. See [`TextInputView`](TextInputView.md).

**Parameters**

- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<string>?`): Reads the text, every frame. Null makes the field keep its own text (read it from [`TextInputView.FieldText`](TextInputView.md#m-fieldtext)).
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<string>?`): Called with the whole new text after each edit.
- `placeholder` (`string`): Dimmed text shown while the field is empty.
- `password` (`bool`): Show bullets instead of the text and never copy it.
- `maxLength` (`int`): The most grapheme clusters the field takes; longer input is cut.
- `validate` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<string, bool>?`): Called with the text an edit would produce; return false to reject the edit (digits only, for example).
- `onSubmit` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<string>?`): Called with the text when Enter is pressed.
- `clipboard` ([`IClipboard`](IClipboard.md)?): Where copy and paste go; the default is [`UiKit.DefaultClipboard`](UiKit.md#m-defaultclipboard).
- `id` (`string?`): The id, or null.

**Returns:** [`TextInputView`](TextInputView.md)

### <a id="m-textlist-t-func-ireadonlylist-t-func-t-string-length-action-t-int-action-t-int-string"></a>TextList&lt;T&gt;(Func&lt;IReadOnlyList&lt;T&gt;&gt;, Func&lt;T, string&gt;, Length, Action&lt;T, int&gt;?, Action&lt;T, int&gt;?, string?)

```csharp
public static ListView<T> TextList<T>(Func<IReadOnlyList<T>> items, Func<T, string> text, Length rowHeight, Action<T, int>? onSelect = null, Action<T, int>? onActivate = null, string? id = null);
```

A virtualized list of text rows, one line per item: see [`UiKit.VirtualList()`](UiKit.md#m-virtuallist-t-func-ireadonlylist-t-func-element-action-element-t-int-length-action-t-int-action-t-int-string-func-int).

**Type parameters**

- `T`: The item type.

**Parameters**

- `items` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<T>>`): Returns the items, every frame.
- `text` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<T, string>`): Gives the line of text for an item; it runs when a row is bound, not every frame.
- `rowHeight` ([`Length`](../../OneGame.UI/OneGame.UI/Length.md)): The height of every row.
- `onSelect` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<T, int>?`): Called when the selection changes.
- `onActivate` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<T, int>?`): Called when the selected item is activated.
- `id` (`string?`): The id, or null.

**Returns:** [`ListView`](ListView-1.md)`<T>`

### <a id="m-toggle-string-func-bool-action-bool-string"></a>Toggle(string, Func&lt;bool&gt;, Action&lt;bool&gt;, string?)

```csharp
public static Element Toggle(string label, Func<bool> get, Action<bool> set, string? id = null);
```

A checkbox with a label (kind `Toggle`, role `checkbox`). While checked it has the class `checked` and [`Element.Value`](../../OneGame.UI/OneGame.UI/Element.md#m-value) is `true`.

**Parameters**

- `label` (`string`): The text beside the box.
- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<bool>`): Reads the state, every frame.
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<bool>`): Called with the new state when the player toggles it.
- `id` (`string?`): The id, or null.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-togglebutton-string-func-bool-action-bool-string-buttonkind"></a>ToggleButton(string, Func&lt;bool&gt;, Action&lt;bool&gt;, string?, ButtonKind)

```csharp
public static Element ToggleButton(string text, Func<bool> get, Action<bool> set, string? id = null, ButtonKind kind = 0);
```

A toggle button (kind `Button`, role `button`) that is switched on or off, like a button with `aria-pressed`: a "FAST" speed switch, a bold-text toggle, a filter chip. While on it has the class `pressed` (the kit draws an accent-tinted fill and outline), [`Element.Pressed`](../../OneGame.UI/OneGame.UI/Element.md#m-pressed) is `true`, and the JSON dump shows `"pressed": true`.

**Parameters**

- `text` (`string`): The label.
- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<bool>`): Reads the state, every frame.
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<bool>`): Called with the new state when the player activates the button.
- `id` (`string?`): The id, or null.
- `kind` ([`ButtonKind`](ButtonKind.md)): The look; the switched-on tint replaces the variant's fill.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-virtuallist-t-func-ireadonlylist-t-func-element-action-element-t-int-length-action-t-int-action-t-int-string-func-int"></a>VirtualList&lt;T&gt;(Func&lt;IReadOnlyList&lt;T&gt;&gt;, Func&lt;Element&gt;, Action&lt;Element, T, int&gt;, Length, Action&lt;T, int&gt;?, Action&lt;T, int&gt;?, string?, Func&lt;int&gt;?)

```csharp
public static ListView<T> VirtualList<T>(Func<IReadOnlyList<T>> items, Func<Element> createRow, Action<Element, T, int> bindRow, Length rowHeight, Action<T, int>? onSelect = null, Action<T, int>? onActivate = null, string? id = null, Func<int>? version = null);
```

A virtualized list: see [`ListView<T>`](ListView-1.md). `createRow` builds one row element and runs once per recycled row; `bindRow` fills a row for an item and runs only when the row is assigned a different item (scrolling, a changed source), never on a frame where nothing changed.

**Type parameters**

- `T`: The item type.

**Parameters**

- `items` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<T>>`): Returns the items, every frame. Return the same list instance while it is unchanged; a new instance, a different count or a different `version` refreshes the rows.
- `createRow` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`Element`](../../OneGame.UI/OneGame.UI/Element.md)&gt;): Builds an empty row.
- `bindRow` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-3)&lt;[`Element`](../../OneGame.UI/OneGame.UI/Element.md)`, T, int>`): Shows an item in a row: the row, the item and its index.
- `rowHeight` ([`Length`](../../OneGame.UI/OneGame.UI/Length.md)): The height of every row, in UI units or pixels.
- `onSelect` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<T, int>?`): Called with the item and index when the selection changes by the player or by [`ListView<T>.Select()`](ListView-1.md#m-select-int).
- `onActivate` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<T, int>?`): Called when Enter, Space or the confirm button is pressed on the selected item.
- `id` (`string?`): The id, or null.
- `version` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<int>?`): Optional counter the game bumps when items change in place; a change refreshes the rows.

**Returns:** [`ListView`](ListView-1.md)`<T>`
