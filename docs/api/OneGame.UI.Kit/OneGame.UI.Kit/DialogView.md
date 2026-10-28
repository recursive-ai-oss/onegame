[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# DialogView class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Inherits:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

```csharp
public sealed class DialogView : Element
```

A modal dialog (kind `Dialog`, role `dialog`): a full-screen scrim over everything with a panel in the middle. While it is open the pointer cannot reach what is behind it, and focus, Tab and the d-pad stay inside it. Opening it focuses the element marked [`FocusHelpers.AsDefaultFocus()`](FocusHelpers.md#m-asdefaultfocus-element) (else the first button); closing it gives focus back to what had it. Escape and the back button close it and call the cancel callback. Parts: `id/panel`, `id/title`, `id/buttons`. Open and close it from game code with [`DialogView.Open()`](DialogView.md#m-open) and [`DialogView.Close()`](DialogView.md#m-close), or follow a flag with [`DialogView.BindOpen()`](DialogView.md#m-bindopen-func-bool).

## Properties

| Member | Description |
| --- | --- |
| [`Buttons`](#m-buttons) | The row of buttons at the bottom of the panel. |
| [`IsOpen`](#m-isopen) | True while the dialog is shown. |
| [`Panel`](#m-panel) | The panel in the middle; add styling or extra content here. |

### <a id="m-buttons"></a>Buttons

```csharp
public Element Buttons { get; }
```

The row of buttons at the bottom of the panel.

**Type:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-isopen"></a>IsOpen

```csharp
public bool IsOpen { get; }
```

True while the dialog is shown.

**Type:** `bool`

### <a id="m-panel"></a>Panel

```csharp
public Element Panel { get; }
```

The panel in the middle; add styling or extra content here.

**Type:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

## Methods

| Member | Description |
| --- | --- |
| [`AddButton(string, Action?, ButtonKind, bool, string?)`](#m-addbutton-string-action-buttonkind-bool-string) | Adds a button to the bottom row. |
| [`BindOpen(Func<bool>)`](#m-bindopen-func-bool) | Makes the dialog follow a flag: open while `isOpen` returns true. |
| [`Close()`](#m-close) | Hides the dialog and gives focus back to the element that had it before it opened. |
| [`Open()`](#m-open) | Shows the dialog and moves focus into it. |

### <a id="m-addbutton-string-action-buttonkind-bool-string"></a>AddButton(string, Action?, ButtonKind, bool, string?)

```csharp
public Element AddButton(string text, Action? onClick, ButtonKind kind = 0, bool isDefault = false, string? id = null);
```

Adds a button to the bottom row. Clicking it closes the dialog and then calls `onClick`.

**Parameters**

- `text` (`string`): The label.
- `onClick` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action)?): Called after the dialog closes; may be null.
- `kind` ([`ButtonKind`](ButtonKind.md)): The look.
- `isDefault` (`bool`): True to make this the button focused when the dialog opens.
- `id` (`string?`): The id of the button, or null for `dialogId/buttons/n` when the dialog has an id.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md) — The button.

### <a id="m-bindopen-func-bool"></a>BindOpen(Func&lt;bool&gt;)

```csharp
public DialogView BindOpen(Func<bool> isOpen);
```

Makes the dialog follow a flag: open while `isOpen` returns true. Focus moves at the end of the update that changes it.

**Parameters**

- `isOpen` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<bool>`)

**Returns:** [`DialogView`](DialogView.md) — This dialog, for chaining.

### <a id="m-close"></a>Close()

```csharp
public void Close();
```

Hides the dialog and gives focus back to the element that had it before it opened.

### <a id="m-open"></a>Open()

```csharp
public void Open();
```

Shows the dialog and moves focus into it. Does nothing when it is already open.
