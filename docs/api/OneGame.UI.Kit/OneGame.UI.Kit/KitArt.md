[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# KitArt static class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)

```csharp
public static class KitArt
```

The SVG art that ships with the kit: a frame for [`UiKit.Frame()`](UiKit.md#m-frame-string-float-element), sort arrows, a drop-down chevron, a check mark, and input glyphs for keyboard, mouse and gamepad. It is plain shapes in a neutral style, drawn for this engine. Register all of it with the art atlas once: `foreach (var a in KitArt.Assets) atlas.AddSvg(a.Id, a.Svg, new NineSlice(a.Slice, a.Slice, a.Slice, a.Slice));`

## Fields

| Member | Description |
| --- | --- |
| [`Check`](#m-check) | A check mark. |
| [`ChevronDown`](#m-chevrondown) | The drop-down chevron. |
| [`Frame`](#m-frame) | The nine-slice frame behind [`UiKit.Frame()`](UiKit.md#m-frame-string-float-element). |
| [`FrameSlice`](#m-frameslice) | The unstretched border of [`KitArt.Frame`](KitArt.md#m-frame) in UI units, to pass as `slice` to [`UiKit.Frame()`](UiKit.md#m-frame-string-float-element). |
| [`KeyCap`](#m-keycap) | The blank key cap behind keyboard glyphs (nine-slice, [`KitArt.KeySlice`](KitArt.md#m-keyslice)). |
| [`KeySlice`](#m-keyslice) | The unstretched border of [`KitArt.KeyCap`](KitArt.md#m-keycap) in UI units. |
| [`SortAscending`](#m-sortascending) | The ascending sort arrow. |
| [`SortDescending`](#m-sortdescending) | The descending sort arrow. |

### <a id="m-check"></a>Check

```csharp
public const string Check = "kit/icon/check";
```

A check mark.

**Type:** `string`

### <a id="m-chevrondown"></a>ChevronDown

```csharp
public const string ChevronDown = "kit/icon/chevron-down";
```

The drop-down chevron.

**Type:** `string`

### <a id="m-frame"></a>Frame

```csharp
public const string Frame = "kit/frame";
```

The nine-slice frame behind [`UiKit.Frame()`](UiKit.md#m-frame-string-float-element).

**Type:** `string`

### <a id="m-frameslice"></a>FrameSlice

```csharp
public const float FrameSlice = 12f;
```

The unstretched border of [`KitArt.Frame`](KitArt.md#m-frame) in UI units, to pass as `slice` to [`UiKit.Frame()`](UiKit.md#m-frame-string-float-element).

**Type:** `float`

### <a id="m-keycap"></a>KeyCap

```csharp
public const string KeyCap = "kit/glyph/key";
```

The blank key cap behind keyboard glyphs (nine-slice, [`KitArt.KeySlice`](KitArt.md#m-keyslice)).

**Type:** `string`

### <a id="m-keyslice"></a>KeySlice

```csharp
public const float KeySlice = 8f;
```

The unstretched border of [`KitArt.KeyCap`](KitArt.md#m-keycap) in UI units.

**Type:** `float`

### <a id="m-sortascending"></a>SortAscending

```csharp
public const string SortAscending = "kit/icon/sort-asc";
```

The ascending sort arrow.

**Type:** `string`

### <a id="m-sortdescending"></a>SortDescending

```csharp
public const string SortDescending = "kit/icon/sort-desc";
```

The descending sort arrow.

**Type:** `string`

## Properties

| Member | Description |
| --- | --- |
| [`Assets`](#m-assets) | Every asset, ready to register. |

### <a id="m-assets"></a>Assets

```csharp
public static IReadOnlyList<KitArtAsset> Assets { get; }
```

Every asset, ready to register.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`KitArtAsset`](KitArtAsset.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`GlyphId(InputDevice, string)`](#m-glyphid-inputdevice-string) | The asset id of the glyph for a control: `kit/glyph/pad/a`, `kit/glyph/mouse/left`, or the key cap (or an arrow key's picture) for the keyboard. |

### <a id="m-glyphid-inputdevice-string"></a>GlyphId(InputDevice, string)

```csharp
public static string GlyphId(InputDevice device, string name);
```

The asset id of the glyph for a control: `kit/glyph/pad/a`, `kit/glyph/mouse/left`, or the key cap (or an arrow key's picture) for the keyboard.

**Parameters**

- `device` ([`InputDevice`](InputDevice.md))
- `name` (`string`)

**Returns:** `string`
