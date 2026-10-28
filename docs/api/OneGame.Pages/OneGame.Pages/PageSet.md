[API reference](../../index.md) › [OneGame.Pages](../index.md) › OneGame.Pages

# PageSet class

**Namespace:** `OneGame.Pages`  
**Package:** [`RecursiveAI.OneGame.Pages`](../index.md)

```csharp
public sealed class PageSet
```

The page screens of a game (`game.Pages`): which are shown, in what order, the clock they run on and the input routed to them. Pages are view-only; nothing here is simulation state.

## Constructors

| Member | Description |
| --- | --- |
| [`PageSet()`](#m-ctor) |  |

### <a id="m-ctor"></a>PageSet()

```csharp
public PageSet();
```

## Properties

| Member | Description |
| --- | --- |
| [`All`](#m-all) | Every screen, shown or not. |
| [`Any`](#m-any) | True when some screen is shown. |
| [`HasFocus`](#m-hasfocus) | True when an element of a shown HTML page has keyboard focus. |
| [`NowMs`](#m-nowms) | The page clock in milliseconds of view time. |
| [`Scopes`](#m-scopes) | What the page host scopes mean. |
| [`Screens`](#m-screens) | The shown screens, back to front. |

### <a id="m-all"></a>All

```csharp
public IReadOnlyList<PageScreen> All { get; }
```

Every screen, shown or not.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`PageScreen`](PageScreen.md)&gt;

### <a id="m-any"></a>Any

```csharp
public bool Any { get; }
```

True when some screen is shown.

**Type:** `bool`

### <a id="m-hasfocus"></a>HasFocus

```csharp
public bool HasFocus { get; }
```

True when an element of a shown HTML page has keyboard focus.

**Type:** `bool`

### <a id="m-nowms"></a>NowMs

```csharp
public double NowMs { get; }
```

The page clock in milliseconds of view time.

**Type:** `double`

### <a id="m-scopes"></a>Scopes

```csharp
public PageScopes Scopes { get; }
```

What the page host scopes mean.

**Type:** [`PageScopes`](PageScopes.md)

### <a id="m-screens"></a>Screens

```csharp
public IReadOnlyList<PageScreen> Screens { get; }
```

The shown screens, back to front.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`PageScreen`](PageScreen.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`Advance(double)`](#m-advance-double) | Moves the page clock forward and updates what animates. |
| [`Get(string)`](#m-get-string) | The screen shown under a name, or null. |
| [`Key(string, string?, bool, KeyModifiers)`](#m-key-string-string-bool-keymodifiers) | Offers a key to the shown screens, topmost first. |
| [`Prepare(int, int, float)`](#m-prepare-int-int-float) | Creates or resizes the shown screens for a window of this size. |
| [`Refresh()`](#m-refresh) | Lays every screen out again; see [`PageScreen.Refresh()`](PageScreen.md#m-refresh). |
| [`Remove(string)`](#m-remove-string) | Stops showing a screen and forgets it. |
| [`ScreenWith(string)`](#m-screenwith-string) | Finds a named element in the shown screens, topmost first. |
| [`SendPointer(PointerAction, float, float, int)`](#m-sendpointer-pointeraction-float-float-int) | Offers a pointer event to the shown screens, topmost first. |
| [`SetVisible(string, bool)`](#m-setvisible-string-bool) | Hides or shows a screen without forgetting it. |
| [`Show(string, PageScreen)`](#m-show-string-pagescreen) | Shows a screen under a name; a screen already shown under that name is replaced. |
| [`Text(string)`](#m-text-string) | Offers typed text to the shown screens, topmost first. |
| [`Wheel(float, float, float, float)`](#m-wheel-float-float-float-float) | Offers a wheel step to the shown screens, topmost first. |

### <a id="m-advance-double"></a>Advance(double)

```csharp
public void Advance(double viewSeconds);
```

Moves the page clock forward and updates what animates.

**Parameters**

- `viewSeconds` (`double`): View time that passed; the same value the tweens get.

### <a id="m-get-string"></a>Get(string)

```csharp
public PageScreen? Get(string name);
```

The screen shown under a name, or null.

**Parameters**

- `name` (`string`): The name.

**Returns:** [`PageScreen`](PageScreen.md)?

### <a id="m-key-string-string-bool-keymodifiers"></a>Key(string, string?, bool, KeyModifiers)

```csharp
public bool Key(string key, string? code, bool down, KeyModifiers modifiers = 0);
```

Offers a key to the shown screens, topmost first. True when one has a focused element.

**Parameters**

- `key` (`string`)
- `code` (`string?`)
- `down` (`bool`)
- `modifiers` (`KeyModifiers`)

**Returns:** `bool`

### <a id="m-prepare-int-int-float"></a>Prepare(int, int, float)

```csharp
public void Prepare(int width, int height, float scale);
```

Creates or resizes the shown screens for a window of this size.

**Parameters**

- `width` (`int`): Window width in device pixels.
- `height` (`int`): Window height in device pixels.
- `scale` (`float`): Device pixels per CSS pixel.

### <a id="m-refresh"></a>Refresh()

```csharp
public void Refresh();
```

Lays every screen out again; see [`PageScreen.Refresh()`](PageScreen.md#m-refresh).

### <a id="m-remove-string"></a>Remove(string)

```csharp
public bool Remove(string name);
```

Stops showing a screen and forgets it.

**Parameters**

- `name` (`string`): The name.

**Returns:** `bool` — True when there was one.

### <a id="m-screenwith-string"></a>ScreenWith(string)

```csharp
public PageScreen? ScreenWith(string name);
```

Finds a named element in the shown screens, topmost first.

**Parameters**

- `name` (`string`): The element's name.

**Returns:** [`PageScreen`](PageScreen.md)?

### <a id="m-sendpointer-pointeraction-float-float-int"></a>SendPointer(PointerAction, float, float, int)

```csharp
public bool SendPointer(PointerAction action, float x, float y, int button = 0);
```

Offers a pointer event to the shown screens, topmost first. True when one took it.

**Parameters**

- `action` (`PointerAction`): What the pointer did.
- `x` (`float`): Device pixels.
- `y` (`float`): Device pixels.
- `button` (`int`): 0 primary, 1 secondary, 2 middle.

**Returns:** `bool`

### <a id="m-setvisible-string-bool"></a>SetVisible(string, bool)

```csharp
public void SetVisible(string name, bool visible);
```

Hides or shows a screen without forgetting it.

**Parameters**

- `name` (`string`): The name.
- `visible` (`bool`): Whether it is drawn and takes input.

### <a id="m-show-string-pagescreen"></a>Show(string, PageScreen)

```csharp
public PageScreen Show(string name, PageScreen screen);
```

Shows a screen under a name; a screen already shown under that name is replaced.

**Parameters**

- `name` (`string`): The name.
- `screen` ([`PageScreen`](PageScreen.md)): The screen.

**Returns:** [`PageScreen`](PageScreen.md)

### <a id="m-text-string"></a>Text(string)

```csharp
public bool Text(string text);
```

Offers typed text to the shown screens, topmost first.

**Parameters**

- `text` (`string`)

**Returns:** `bool`

### <a id="m-wheel-float-float-float-float"></a>Wheel(float, float, float, float)

```csharp
public bool Wheel(float x, float y, float dx, float dy);
```

Offers a wheel step to the shown screens, topmost first.

**Parameters**

- `x` (`float`)
- `y` (`float`)
- `dx` (`float`)
- `dy` (`float`)

**Returns:** `bool`
