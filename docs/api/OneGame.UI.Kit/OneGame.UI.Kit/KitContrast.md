[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# KitContrast static class

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)

```csharp
public static class KitContrast
```

Color contrast as WCAG 2.x defines it (Understanding SC 1.4.3 and 1.4.11): relative luminance of sRGB, and the ratio `(L1 + 0.05) / (L2 + 0.05)`. The theme uses it to pick legible state colors, and [`ThemeLint`](ThemeLint.md) uses it to report the pairs that fail.

## Fields

| Member | Description |
| --- | --- |
| [`IndicatorAa`](#m-indicatoraa) | The ratio large text and non-text indicators such as a focus ring must reach (WCAG 1.4.3 and 1.4.11 AA). |
| [`TextAa`](#m-textaa) | The ratio normal text must reach (WCAG 1.4.3 AA). |

### <a id="m-indicatoraa"></a>IndicatorAa

```csharp
public const double IndicatorAa = 3;
```

The ratio large text and non-text indicators such as a focus ring must reach (WCAG 1.4.3 and 1.4.11 AA).

**Type:** `double`

### <a id="m-textaa"></a>TextAa

```csharp
public const double TextAa = 4.5;
```

The ratio normal text must reach (WCAG 1.4.3 AA).

**Type:** `double`

## Methods

| Member | Description |
| --- | --- |
| [`Ensure(Color, ReadOnlySpan<Color>, double, double)`](#m-ensure-color-readonlyspan-color-double-double) | Returns `color` unchanged when it reaches `minimum` against every background; otherwise the nearest color on the way to white or to black (whichever gets there with the smaller change) that does. |
| [`Luminance(Color)`](#m-luminance-color) | The relative luminance of a color, 0 (black) to 1 (white). |
| [`Mix(Color, Color, double)`](#m-mix-color-color-double) | Blends two colors in sRGB: `t` 0 gives `a`, 1 gives `b`. |
| [`Over(Color, Color)`](#m-over-color-color) | Composites `top` over an opaque `bottom`. |
| [`Ratio(Color, Color)`](#m-ratio-color-color) | The contrast ratio between two colors, 1 to 21. |
| [`ToHex(Color)`](#m-tohex-color) | A color as `#rrggbb` (with alpha as `#rrggbbaa` when it is not opaque). |
| [`Worst(Color, ReadOnlySpan<Color>)`](#m-worst-color-readonlyspan-color) | The lowest ratio of `color` against any of the backgrounds. |

### <a id="m-ensure-color-readonlyspan-color-double-double"></a>Ensure(Color, ReadOnlySpan&lt;Color&gt;, double, double)

```csharp
public static Color Ensure(Color color, ReadOnlySpan<Color> backgrounds, double minimum, double repairTo = 0);
```

Returns `color` unchanged when it reaches `minimum` against every background; otherwise the nearest color on the way to white or to black (whichever gets there with the smaller change) that does. When no color can, the better of black and white.

**Parameters**

- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md)): The wanted color.
- `backgrounds` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`Color`](../../OneGame.UI/OneGame.UI/Color.md)&gt;): The colors it is drawn on.
- `minimum` (`double`): The ratio a color must reach to be left alone.
- `repairTo` (`double`): The ratio to reach when it has to be changed (at least `minimum`; 0 for the same). A repaired color gets some margin, so it does not sit exactly on the line.

**Returns:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-luminance-color"></a>Luminance(Color)

```csharp
public static double Luminance(Color c);
```

The relative luminance of a color, 0 (black) to 1 (white). Alpha is ignored; composite first with [`KitContrast.Over()`](KitContrast.md#m-over-color-color).

**Parameters**

- `c` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md))

**Returns:** `double`

### <a id="m-mix-color-color-double"></a>Mix(Color, Color, double)

```csharp
public static Color Mix(Color a, Color b, double t);
```

Blends two colors in sRGB: `t` 0 gives `a`, 1 gives `b`. The result is opaque.

**Parameters**

- `a` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md))
- `b` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md))
- `t` (`double`)

**Returns:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-over-color-color"></a>Over(Color, Color)

```csharp
public static Color Over(Color top, Color bottom);
```

Composites `top` over an opaque `bottom`.

**Parameters**

- `top` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md))
- `bottom` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md))

**Returns:** [`Color`](../../OneGame.UI/OneGame.UI/Color.md)

### <a id="m-ratio-color-color"></a>Ratio(Color, Color)

```csharp
public static double Ratio(Color foreground, Color background);
```

The contrast ratio between two colors, 1 to 21. A translucent foreground is first composited over the background.

**Parameters**

- `foreground` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md))
- `background` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md))

**Returns:** `double`

### <a id="m-tohex-color"></a>ToHex(Color)

```csharp
public static string ToHex(Color c);
```

A color as `#rrggbb` (with alpha as `#rrggbbaa` when it is not opaque).

**Parameters**

- `c` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md))

**Returns:** `string`

### <a id="m-worst-color-readonlyspan-color"></a>Worst(Color, ReadOnlySpan&lt;Color&gt;)

```csharp
public static double Worst(Color color, ReadOnlySpan<Color> backgrounds);
```

The lowest ratio of `color` against any of the backgrounds.

**Parameters**

- `color` ([`Color`](../../OneGame.UI/OneGame.UI/Color.md))
- `backgrounds` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`Color`](../../OneGame.UI/OneGame.UI/Color.md)&gt;)

**Returns:** `double`
