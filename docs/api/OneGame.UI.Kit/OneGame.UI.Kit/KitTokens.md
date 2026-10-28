[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# KitTokens record

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`KitTokens`](KitTokens.md)&gt;

```csharp
public sealed record KitTokens : IEquatable<KitTokens>
```

The values the default theme is made of. Colors and the font size, corner radius and focus-ring width become theme tokens (`--kit-bg`, `--kit-accent`, `--kit-font-size` and so on) on the root element, and every kit rule reads them with `var()`, so changing a token restyles the whole UI without rebuilding it. Make a theme with `with`: `KitTheme.Create(KitTokens.Dark with { Accent = Color.Hex("#5ab0ff") })`.

## Constructors

| Member | Description |
| --- | --- |
| [`KitTokens()`](#m-ctor) |  |

### <a id="m-ctor"></a>KitTokens()

```csharp
public KitTokens();
```

## Properties

| Member | Description |
| --- | --- |
| [`Accent`](#m-accent) | The accent: primary buttons, filled parts, the selected tab (`--kit-accent`). |
| [`AccentHover`](#m-accenthover) | The accent while hovered (`--kit-accent-hover`). |
| [`AccentText`](#m-accenttext) | Text drawn on the accent (`--kit-accent-text`). |
| [`Active`](#m-active) | Pressed buttons (`--kit-active`). |
| [`Background`](#m-background) | Screen background (`--kit-bg`). |
| [`Border`](#m-border) | Outlines of controls and panels (`--kit-border`). |
| [`Danger`](#m-danger) | Destructive actions (`--kit-danger`). |
| [`Dark`](#m-dark) | The dark theme, the default. |
| [`Field`](#m-field) | Text fields, tracks and list backgrounds (`--kit-field`). |
| [`Focus`](#m-focus) | The focus ring (`--kit-focus`). |
| [`FocusWidth`](#m-focuswidth) | The width of the focus ring (`--kit-focus-width`). |
| [`FontSize`](#m-fontsize) | The base font size (`--kit-font-size`). |
| [`Hover`](#m-hover) | Hovered rows and buttons (`--kit-hover`). |
| [`Light`](#m-light) | A light theme. |
| [`Panel`](#m-panel) | Panels, cards and dialogs (`--kit-panel`). |
| [`Radius`](#m-radius) | The corner radius of controls (`--kit-radius`). |
| [`Raised`](#m-raised) | Buttons and raised parts (`--kit-raised`). |
| [`Scrim`](#m-scrim) | The dimmed layer behind a dialog (`--kit-scrim`). |
| [`Scrollbar`](#m-scrollbar) | Scroll bar thumbs (`--kit-scrollbar`). |
| [`Selection`](#m-selection) | The selected row, option or text (`--kit-select`). |
| [`SelectionText`](#m-selectiontext) | Text on the selection (`--kit-select-text`). |
| [`Text`](#m-text) | Text (`--kit-text`). |
| [`TextDim`](#m-textdim) | Secondary text and placeholders (`--kit-text-dim`). |

### <a id="m-accent"></a>Accent

```csharp
public Color Accent { get; init; }
```

The accent: primary buttons, filled parts, the selected tab (`--kit-accent`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-accenthover"></a>AccentHover

```csharp
public Color AccentHover { get; init; }
```

The accent while hovered (`--kit-accent-hover`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-accenttext"></a>AccentText

```csharp
public Color AccentText { get; init; }
```

Text drawn on the accent (`--kit-accent-text`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-active"></a>Active

```csharp
public Color Active { get; init; }
```

Pressed buttons (`--kit-active`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-background"></a>Background

```csharp
public Color Background { get; init; }
```

Screen background (`--kit-bg`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-border"></a>Border

```csharp
public Color Border { get; init; }
```

Outlines of controls and panels (`--kit-border`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-danger"></a>Danger

```csharp
public Color Danger { get; init; }
```

Destructive actions (`--kit-danger`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-dark"></a>Dark

```csharp
public static KitTokens Dark { get; }
```

The dark theme, the default.

**Type:** [`KitTokens`](KitTokens.md)

### <a id="m-field"></a>Field

```csharp
public Color Field { get; init; }
```

Text fields, tracks and list backgrounds (`--kit-field`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-focus"></a>Focus

```csharp
public Color Focus { get; init; }
```

The focus ring (`--kit-focus`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-focuswidth"></a>FocusWidth

```csharp
public Length FocusWidth { get; init; }
```

The width of the focus ring (`--kit-focus-width`).

**Type:** [`Length`](../../OneGame.UI/OneGame.UI/Length.md)

### <a id="m-fontsize"></a>FontSize

```csharp
public Length FontSize { get; init; }
```

The base font size (`--kit-font-size`).

**Type:** [`Length`](../../OneGame.UI/OneGame.UI/Length.md)

### <a id="m-hover"></a>Hover

```csharp
public Color Hover { get; init; }
```

Hovered rows and buttons (`--kit-hover`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-light"></a>Light

```csharp
public static KitTokens Light { get; }
```

A light theme.

**Type:** [`KitTokens`](KitTokens.md)

### <a id="m-panel"></a>Panel

```csharp
public Color Panel { get; init; }
```

Panels, cards and dialogs (`--kit-panel`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-radius"></a>Radius

```csharp
public Length Radius { get; init; }
```

The corner radius of controls (`--kit-radius`).

**Type:** [`Length`](../../OneGame.UI/OneGame.UI/Length.md)

### <a id="m-raised"></a>Raised

```csharp
public Color Raised { get; init; }
```

Buttons and raised parts (`--kit-raised`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-scrim"></a>Scrim

```csharp
public Color Scrim { get; init; }
```

The dimmed layer behind a dialog (`--kit-scrim`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-scrollbar"></a>Scrollbar

```csharp
public Color Scrollbar { get; init; }
```

Scroll bar thumbs (`--kit-scrollbar`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-selection"></a>Selection

```csharp
public Color Selection { get; init; }
```

The selected row, option or text (`--kit-select`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-selectiontext"></a>SelectionText

```csharp
public Color SelectionText { get; init; }
```

Text on the selection (`--kit-select-text`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-text"></a>Text

```csharp
public Color Text { get; init; }
```

Text (`--kit-text`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-textdim"></a>TextDim

```csharp
public Color TextDim { get; init; }
```

Secondary text and placeholders (`--kit-text-dim`).

**Type:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)
