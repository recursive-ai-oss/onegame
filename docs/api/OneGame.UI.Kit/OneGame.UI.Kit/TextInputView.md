[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# TextInputView class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Inherits:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

```csharp
public sealed class TextInputView : Element
```

A single-line text field (kind `TextInput`, role `textbox`). The caret and the selection are drawn as elements (`id/caret`, `id/selection` rectangles). Editing works on grapheme clusters: a letter with its accents, or an emoji sequence, is one step for the arrow keys, Backspace and Delete. Arrow keys move the caret through the text as it looks on screen, also where left-to-right and right-to-left text mix. Shift extends the selection; Ctrl (or Cmd) with A, C, X, V selects all, copies, cuts and pastes. Home and End go to the start and end of the text. Enter calls the submit callback. Up, Down, Tab and the gamepad d-pad leave the field. The text is the game's: the field reads it through `get` every frame and reports edits through `set`, so a game that refuses an edit by not storing it sees it undone. In password mode the field shows bullets, never copies, and keeps the text out of [`Element.Value`](../../OneGame.UI/OneGame.UI/Element.md#m-value).

## Properties

| Member | Description |
| --- | --- |
| [`CaretIndex`](#m-caretindex) | The caret as a UTF-16 index into [`TextInputView.FieldText`](TextInputView.md#m-fieldtext); it is always at the edge of a grapheme cluster. |
| [`CaretScreenRect`](#m-caretscreenrect) | The caret's box on screen, which the host hands to the platform layer to place an input method's candidate window. |
| [`CaretX`](#m-caretx) | The horizontal position of the caret in pixels from the start of the text, as of the last update. |
| [`Clipboard`](#m-clipboard) | Where cut and copied text goes, and where pasted text comes from. |
| [`FieldText`](#m-fieldtext) | The text in the field. |
| [`GraphemeCount`](#m-graphemecount) | The number of grapheme clusters in [`TextInputView.FieldText`](TextInputView.md#m-fieldtext). |
| [`Preedit`](#m-preedit) | The input method's uncommitted text, shown at the caret until the method commits it (as ordinary text input) or clears it; empty when none. |
| [`SelectedText`](#m-selectedtext) | The selected text, or an empty string. |
| [`SelectionEnd`](#m-selectionend) | Where the selection ends, as a UTF-16 index. |
| [`SelectionStart`](#m-selectionstart) | Where the selection starts, as a UTF-16 index; equal to [`TextInputView.SelectionEnd`](TextInputView.md#m-selectionend) when nothing is selected. |

### <a id="m-caretindex"></a>CaretIndex

```csharp
public int CaretIndex { get; }
```

The caret as a UTF-16 index into [`TextInputView.FieldText`](TextInputView.md#m-fieldtext); it is always at the edge of a grapheme cluster.

**Type:** `int`

### <a id="m-caretscreenrect"></a>CaretScreenRect

```csharp
public Rect CaretScreenRect { get; }
```

The caret's box on screen, which the host hands to the platform layer to place an input method's candidate window. While an input method has uncommitted text the box sits after it.

**Type:** [`Rect`](../../OneGame.UI/OneGame.UI/Rect.md)

### <a id="m-caretx"></a>CaretX

```csharp
public float CaretX { get; }
```

The horizontal position of the caret in pixels from the start of the text, as of the last update.

**Type:** `float`

### <a id="m-clipboard"></a>Clipboard

```csharp
public IClipboard Clipboard { get; set; }
```

Where cut and copied text goes, and where pasted text comes from. Defaults to [`UiKit.DefaultClipboard`](UiKit.md#m-defaultclipboard).

**Type:** [`IClipboard`](IClipboard.md)

### <a id="m-fieldtext"></a>FieldText

```csharp
public string FieldText { get; }
```

The text in the field.

**Type:** `string`

### <a id="m-graphemecount"></a>GraphemeCount

```csharp
public int GraphemeCount { get; }
```

The number of grapheme clusters in [`TextInputView.FieldText`](TextInputView.md#m-fieldtext).

**Type:** `int`

### <a id="m-preedit"></a>Preedit

```csharp
public string Preedit { get; }
```

The input method's uncommitted text, shown at the caret until the method commits it (as ordinary text input) or clears it; empty when none.

**Type:** `string`

### <a id="m-selectedtext"></a>SelectedText

```csharp
public string SelectedText { get; }
```

The selected text, or an empty string.

**Type:** `string`

### <a id="m-selectionend"></a>SelectionEnd

```csharp
public int SelectionEnd { get; }
```

Where the selection ends, as a UTF-16 index.

**Type:** `int`

### <a id="m-selectionstart"></a>SelectionStart

```csharp
public int SelectionStart { get; }
```

Where the selection starts, as a UTF-16 index; equal to [`TextInputView.SelectionEnd`](TextInputView.md#m-selectionend) when nothing is selected.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Copy()`](#m-copy) | Copies the selection to the clipboard; does nothing in password mode or without a selection. |
| [`Cut()`](#m-cut) | Copies the selection to the clipboard and deletes it. |
| [`Paste()`](#m-paste) | Inserts the clipboard's text at the caret, replacing the selection. |
| [`Select(int, int)`](#m-select-int-int) | Selects the text between two UTF-16 indexes (moved outward to cluster edges) and puts the caret at `end`. |
| [`SelectAll()`](#m-selectall) | Selects all the text. |
| [`SetText(string)`](#m-settext-string) | Replaces the text without calling the `set` callback, for a field that keeps its own text; the caret goes to the end. |

### <a id="m-copy"></a>Copy()

```csharp
public void Copy();
```

Copies the selection to the clipboard; does nothing in password mode or without a selection.

### <a id="m-cut"></a>Cut()

```csharp
public void Cut();
```

Copies the selection to the clipboard and deletes it.

### <a id="m-paste"></a>Paste()

```csharp
public void Paste();
```

Inserts the clipboard's text at the caret, replacing the selection.

### <a id="m-select-int-int"></a>Select(int, int)

```csharp
public void Select(int start, int end);
```

Selects the text between two UTF-16 indexes (moved outward to cluster edges) and puts the caret at `end`.

**Parameters**

- `start` (`int`)
- `end` (`int`)

### <a id="m-selectall"></a>SelectAll()

```csharp
public void SelectAll();
```

Selects all the text.

### <a id="m-settext-string"></a>SetText(string)

```csharp
public void SetText(string text);
```

Replaces the text without calling the `set` callback, for a field that keeps its own text; the caret goes to the end.

**Parameters**

- `text` (`string`)
