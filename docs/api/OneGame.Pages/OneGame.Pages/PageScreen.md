[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages

# PageScreen class

**Namespace:** `OneGame.Pages`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)

```csharp
public sealed class PageScreen
```

One OneRender page shown as a screen: an HTML document ([`PageScreen.Interactive`](PageScreen.md#m-interactive)) or a builder tree ([`PageScreen.Live`](PageScreen.md#m-live)). It lays out at the size of the window in CSS px, keeps its display list up to date, takes input and time, and offers lookups by name for the harness.

**Example**

Show an HTML page from a screen builder, with a handler that sends a command:

```csharp
var fonts = new PageFonts(game.Fonts);
fonts.Add(File.ReadAllBytes("fonts/Body.ttf"), "Body");
game.Pages.Show("menu", PageScreen.FromHtml("<button id=\"start\">Start</button>", new PageOptions(fonts)
{
    Setup = s => s.Interactive!.Events.AddEventListener(
        EventTarget.Of(s.Interactive.FindElement("start")), "click", _ => Meter.Of(game.World).Send.Add(1)),
}));
```

## Properties

| Member | Description |
| --- | --- |
| [`Creations`](#m-creations) | How many times the page was created (the first frame, then once per size or scale change). |
| [`Interactive`](#m-interactive) | The interactive page of an HTML screen; null for a builder screen or before the first frame. |
| [`Live`](#m-live) | The live page of a builder screen; null for an HTML screen or before the first frame. |
| [`Name`](#m-name) | The name the screen was shown under. |
| [`Order`](#m-order) | Screens draw in ascending order; later ones are on top. |
| [`Viewport`](#m-viewport) | The viewport in CSS px. |
| [`Visible`](#m-visible) | Drawn when true (the default) and takes input. |

### <a id="m-creations"></a>Creations

```csharp
public int Creations { get; }
```

How many times the page was created (the first frame, then once per size or scale change).

**Type:** `int`

### <a id="m-interactive"></a>Interactive

```csharp
public InteractivePage? Interactive { get; }
```

The interactive page of an HTML screen; null for a builder screen or before the first frame.

**Type:** `InteractivePage?`

### <a id="m-live"></a>Live

```csharp
public LivePage? Live { get; }
```

The live page of a builder screen; null for an HTML screen or before the first frame.

**Type:** `LivePage?`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The name the screen was shown under.

**Type:** `string`

### <a id="m-order"></a>Order

```csharp
public int Order { get; set; }
```

Screens draw in ascending order; later ones are on top.

**Type:** `int`

### <a id="m-viewport"></a>Viewport

```csharp
public (double Width, double Height) Viewport { get; }
```

The viewport in CSS px.

**Type:** `(double Width, double Height)`

### <a id="m-visible"></a>Visible

```csharp
public bool Visible { get; set; }
```

Drawn when true (the default) and takes input.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`BoundsOf(string)`](#m-boundsof-string) | The box of a named element in device pixels, or null when there is none (builder pages report none). |
| [`Click(string)`](#m-click-string) | Clicks a named element the way `element.click()` does (events and activation, no hit test). |
| [`Covers(float, float)`](#m-covers-float-float) | Whether a pointer at this device position is over something of the page other than the empty background. |
| [`FromHtml(string, PageOptions)`](#m-fromhtml-string-pageoptions) | A screen from HTML and CSS (inline `<style>` and style attributes). |
| [`FromTree(StyledTree, PageOptions)`](#m-fromtree-styledtree-pageoptions) | A screen from a builder tree. |
| [`Has(string)`](#m-has-string) | Whether an element with this name exists (an `id` or a form control's `name` in HTML; `SetName` in a builder tree). |
| [`Refresh()`](#m-refresh) | Makes the page lay out again on the next frame, for an image whose size changed through [`PageImages.Replace()`](PageImages.md#m-replace-string-readonlymemory-byte). |
| [`StatesOf(string)`](#m-statesof-string) | The state flags of a named element: `hover active focus disabled` joined by spaces. |
| [`TextOf(string)`](#m-textof-string) | The text of a named element, or null when there is none. |

### <a id="m-boundsof-string"></a>BoundsOf(string)

```csharp
public (float X, float Y, float W, float H)? BoundsOf(string name);
```

The box of a named element in device pixels, or null when there is none (builder pages report none).

**Parameters**

- `name` (`string`): The name.

**Returns:** `(float X, float Y, float W, float H)?`

### <a id="m-click-string"></a>Click(string)

```csharp
public bool Click(string name);
```

Clicks a named element the way `element.click()` does (events and activation, no hit test).

**Parameters**

- `name` (`string`): The name.

**Returns:** `bool` — False when there is no such element.

### <a id="m-covers-float-float"></a>Covers(float, float)

```csharp
public bool Covers(float x, float y);
```

Whether a pointer at this device position is over something of the page other than the empty background.

**Parameters**

- `x` (`float`)
- `y` (`float`)

**Returns:** `bool`

### <a id="m-fromhtml-string-pageoptions"></a>FromHtml(string, PageOptions)

```csharp
public static PageScreen FromHtml(string html, PageOptions options);
```

A screen from HTML and CSS (inline `<style>` and style attributes).

**Parameters**

- `html` (`string`): The document.
- `options` ([`PageOptions`](PageOptions.md)): Fonts, images and setup.

**Returns:** [`PageScreen`](PageScreen.md)

### <a id="m-fromtree-styledtree-pageoptions"></a>FromTree(StyledTree, PageOptions)

```csharp
public static PageScreen FromTree(StyledTree tree, PageOptions options);
```

A screen from a builder tree. The tree stays the caller's to edit; changes show on the next frame.

**Parameters**

- `tree` (`StyledTree`): The tree.
- `options` ([`PageOptions`](PageOptions.md)): Fonts, images and setup.

**Returns:** [`PageScreen`](PageScreen.md)

### <a id="m-has-string"></a>Has(string)

```csharp
public bool Has(string name);
```

Whether an element with this name exists (an `id` or a form control's `name` in HTML; `SetName` in a builder tree).

**Parameters**

- `name` (`string`): The name.

**Returns:** `bool`

### <a id="m-refresh"></a>Refresh()

```csharp
public void Refresh();
```

Makes the page lay out again on the next frame, for an image whose size changed through [`PageImages.Replace()`](PageImages.md#m-replace-string-readonlymemory-byte). An HTML page is created anew (its focus and field values are lost); a builder page is repainted. Same-size replacements need nothing: pixels are read every frame.

### <a id="m-statesof-string"></a>StatesOf(string)

```csharp
public string StatesOf(string name);
```

The state flags of a named element: `hover active focus disabled` joined by spaces.

**Parameters**

- `name` (`string`): The name.

**Returns:** `string`

### <a id="m-textof-string"></a>TextOf(string)

```csharp
public string? TextOf(string name);
```

The text of a named element, or null when there is none.

**Parameters**

- `name` (`string`): The name.

**Returns:** `string?`
