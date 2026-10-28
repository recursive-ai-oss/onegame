[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# KitHost class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)

```csharp
public sealed class KitHost
```

Installs the kit into a [`UiTree`](../../OneGame.UI/OneGame.UI/UiTree.md): adds the theme, owns the tooltip layer and the input glyph bindings, and keeps the kit's notion of time. Create one per tree, call [`KitHost.Advance()`](KitHost.md#m-advance-float) once per frame with the frame time (tooltips wait for a delay, and the UI never reads a clock itself), and send input through [`KitHost.Dispatch()`](KitHost.md#m-dispatch-uievent) or straight to the tree; either way glyphs learn which device the player is using.

**Example**

```csharp
private KitHost _kit = null!;

public override void Style(UiTree ui) => _kit = new KitHost(ui);      // default theme and tooltip layer

// In a screen builder: _kit.Tooltip(UiKit.Button("New game", Start), "Begins a fresh run").
// Once per frame, with the seconds since the last one (for example from OnEachFrame): _kit.Advance(seconds);
```

## Constructors

| Member | Description |
| --- | --- |
| [`KitHost(UiTree, StyleSheet?)`](#m-ctor-uitree-stylesheet) | Adds the theme and the tooltip layer to `tree`. |

### <a id="m-ctor-uitree-stylesheet"></a>KitHost(UiTree, StyleSheet?)

```csharp
public KitHost(UiTree tree, StyleSheet? theme = null);
```

Adds the theme and the tooltip layer to `tree`.

**Parameters**

- `tree` ([`UiTree`](../../OneGame.UI/OneGame.UI/UiTree.md)): The UI to install into.
- `theme` ([`StyleSheet`](../../OneGame.UI/OneGame.UI/StyleSheet.md)?): The style sheet to add; the default theme ([`KitTheme.Create()`](KitTheme.md#m-create-kittokens)) when null. Add game styles after it so they win ties.

## Properties

| Member | Description |
| --- | --- |
| [`Glyphs`](#m-glyphs) | Which device the player used last, and what each action looks like on it. |
| [`Theme`](#m-theme) | The kit's style sheet as installed (or last set with [`KitHost.SetTheme()`](KitHost.md#m-settheme-stylesheet)). |
| [`TooltipDelay`](#m-tooltipdelay) | Seconds a pointer or focus must rest on a target before its tooltip appears. |
| [`TooltipLayer`](#m-tooltiplayer) | The tooltip element (id `kit/tooltip`), visible while a tooltip shows. |
| [`Tree`](#m-tree) | The tree this host serves. |

### <a id="m-glyphs"></a>Glyphs

```csharp
public InputGlyphs Glyphs { get; }
```

Which device the player used last, and what each action looks like on it.

**Type:** [`InputGlyphs`](InputGlyphs.md)

### <a id="m-theme"></a>Theme

```csharp
public StyleSheet Theme { get; }
```

The kit's style sheet as installed (or last set with [`KitHost.SetTheme()`](KitHost.md#m-settheme-stylesheet)).

**Type:** [`StyleSheet`](../../OneGame.UI/OneGame.UI/StyleSheet.md)

### <a id="m-tooltipdelay"></a>TooltipDelay

```csharp
public float TooltipDelay { get; set; }
```

Seconds a pointer or focus must rest on a target before its tooltip appears.

**Type:** `float`

### <a id="m-tooltiplayer"></a>TooltipLayer

```csharp
public Element TooltipLayer { get; }
```

The tooltip element (id `kit/tooltip`), visible while a tooltip shows.

**Type:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md)

### <a id="m-tree"></a>Tree

```csharp
public UiTree Tree { get; }
```

The tree this host serves.

**Type:** [`UiTree`](../../OneGame.UI/OneGame.UI/UiTree.md)

## Methods

| Member | Description |
| --- | --- |
| [`Advance(float)`](#m-advance-float) | Moves the kit's clock on. |
| [`AttachTo<T>(T)`](#m-attachto-t-t) | Moves the tooltip layer into a screen. |
| [`Dispatch(in UiEvent)`](#m-dispatch-uievent) | Passes the event to the tree, which notes its device for the glyphs. |
| [`SetTheme(StyleSheet)`](#m-settheme-stylesheet) | Swaps the kit's style sheet for another, for example `KitTheme.Create(KitTokens.Light)`. |
| [`Tooltip<T>(T, Func<string>)`](#m-tooltip-t-t-func-string) | Gives an element a tooltip whose text follows a lambda (re-read every frame while it shows). |
| [`Tooltip<T>(T, string)`](#m-tooltip-t-t-string) | Gives an element a tooltip: after [`KitHost.TooltipDelay`](KitHost.md#m-tooltipdelay) of pointer hover or keyboard focus, a small box with the text appears under it (above it when there is no room below), clamped to the screen, and follows the element if it moves. |

### <a id="m-advance-float"></a>Advance(float)

```csharp
public void Advance(float seconds);
```

Moves the kit's clock on. Call once per frame, before the frame is built.

**Parameters**

- `seconds` (`float`): The time since the last call.

### <a id="m-attachto-t-t"></a>AttachTo&lt;T&gt;(T)

```csharp
public T AttachTo<T>(T screen) where T : Element;
```

Moves the tooltip layer into a screen. A host that replaces everything under the tree's root when it opens a screen removes the layer with the old screen; call this on each screen's root element while building it.

**Type parameters**

- `T`: The element type.

**Parameters**

- `screen` (`T`): The screen's root element.

**Returns:** `T` — `screen`, so the call fits into a builder chain.

### <a id="m-dispatch-uievent"></a>Dispatch(in UiEvent)

```csharp
public bool Dispatch(in UiEvent e);
```

Passes the event to the tree, which notes its device for the glyphs. Returns what [`UiTree.Dispatch()`](../../OneGame.UI/OneGame.UI/UiTree.md#m-dispatch-uievent) returns.

**Parameters**

- `e` ([`UiEvent`](../../OneGame.UI/OneGame.UI/UiEvent.md))

**Returns:** `bool`

### <a id="m-settheme-stylesheet"></a>SetTheme(StyleSheet)

```csharp
public void SetTheme(StyleSheet theme);
```

Swaps the kit's style sheet for another, for example `KitTheme.Create(KitTokens.Light)`. The tree keeps every element; styles are resolved again on the next frame. The new sheet takes the old one's place in the cascade, so game sheets added after the kit's still win ties.

**Parameters**

- `theme` ([`StyleSheet`](../../OneGame.UI/OneGame.UI/StyleSheet.md))

### <a id="m-tooltip-t-t-func-string"></a>Tooltip&lt;T&gt;(T, Func&lt;string&gt;)

```csharp
public T Tooltip<T>(T target, Func<string> text) where T : Element;
```

Gives an element a tooltip whose text follows a lambda (re-read every frame while it shows). See [`KitHost.Tooltip()`](KitHost.md#m-tooltip-t-t-string).

**Parameters**

- `target` (`T`)
- `text` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<string>`)

**Returns:** `T`

### <a id="m-tooltip-t-t-string"></a>Tooltip&lt;T&gt;(T, string)

```csharp
public T Tooltip<T>(T target, string text) where T : Element;
```

Gives an element a tooltip: after [`KitHost.TooltipDelay`](KitHost.md#m-tooltipdelay) of pointer hover or keyboard focus, a small box with the text appears under it (above it when there is no room below), clamped to the screen, and follows the element if it moves.

**Type parameters**

- `T`: The element type.

**Parameters**

- `target` (`T`): The element that gets the tooltip.
- `text` (`string`): The tooltip text.

**Returns:** `T` — `target`, so the call fits into a builder chain.
