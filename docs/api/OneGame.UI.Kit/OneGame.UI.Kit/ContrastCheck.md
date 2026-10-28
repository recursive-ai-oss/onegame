[API reference](../../index.md) › [OneGame.UI.Kit](../index.md) › OneGame.UI.Kit

# ContrastCheck record

**Namespace:** `OneGame.UI.Kit`  
**Package:** [`RecursiveAI.OneGame.UI.Kit`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ContrastCheck`](ContrastCheck.md)&gt;

```csharp
public sealed record ContrastCheck : IEquatable<ContrastCheck>
```

One foreground and background pair of the theme and how it fares against WCAG 2.x AA.

## Constructors

| Member | Description |
| --- | --- |
| [`ContrastCheck(string, string, string, string, string, double, double, bool, string)`](#m-ctor-string-string-string-string-string-double-double-bool-string) | One foreground and background pair of the theme and how it fares against WCAG 2.x AA. |

### <a id="m-ctor-string-string-string-string-string-double-double-bool-string"></a>ContrastCheck(string, string, string, string, string, double, double, bool, string)

```csharp
public ContrastCheck(string Name, string Foreground, string Background, string ForegroundColor, string BackgroundColor, double Ratio, double Required, bool Passed, string Kind);
```

One foreground and background pair of the theme and how it fares against WCAG 2.x AA.

**Parameters**

- `Name` (`string`): What is checked, such as `text on panel` or `focus ring on field`.
- `Foreground` (`string`): The foreground token, such as `--kit-text`.
- `Background` (`string`): The background token.
- `ForegroundColor` (`string`): The foreground as `#rrggbb`, or `missing` when the token is not defined.
- `BackgroundColor` (`string`): The background as `#rrggbb`, or `missing`.
- `Ratio` (`double`): The contrast ratio, 1 to 21 (0 when a token is missing).
- `Required` (`double`): The ratio the pair must reach: 4.5 for text, 3 for disabled text and indicators.
- `Passed` (`bool`): True when `Ratio` reaches `Required`.
- `Kind` (`string`): `text`, `disabled-text` or `indicator` (focus ring, control outline, filled accent, scroll thumb).

## Properties

| Member | Description |
| --- | --- |
| [`Background`](#m-background) | The background token. |
| [`BackgroundColor`](#m-backgroundcolor) | The background as `#rrggbb`, or `missing`. |
| [`Foreground`](#m-foreground) | The foreground token, such as `--kit-text`. |
| [`ForegroundColor`](#m-foregroundcolor) | The foreground as `#rrggbb`, or `missing` when the token is not defined. |
| [`Kind`](#m-kind) | `text`, `disabled-text` or `indicator` (focus ring, control outline, filled accent, scroll thumb). |
| [`Name`](#m-name) | What is checked, such as `text on panel` or `focus ring on field`. |
| [`Passed`](#m-passed) | True when `Ratio` reaches `Required`. |
| [`Ratio`](#m-ratio) | The contrast ratio, 1 to 21 (0 when a token is missing). |
| [`Required`](#m-required) | The ratio the pair must reach: 4.5 for text, 3 for disabled text and indicators. |

### <a id="m-background"></a>Background

```csharp
public string Background { get; init; }
```

The background token.

**Type:** `string`

### <a id="m-backgroundcolor"></a>BackgroundColor

```csharp
public string BackgroundColor { get; init; }
```

The background as `#rrggbb`, or `missing`.

**Type:** `string`

### <a id="m-foreground"></a>Foreground

```csharp
public string Foreground { get; init; }
```

The foreground token, such as `--kit-text`.

**Type:** `string`

### <a id="m-foregroundcolor"></a>ForegroundColor

```csharp
public string ForegroundColor { get; init; }
```

The foreground as `#rrggbb`, or `missing` when the token is not defined.

**Type:** `string`

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; init; }
```

`text`, `disabled-text` or `indicator` (focus ring, control outline, filled accent, scroll thumb).

**Type:** `string`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

What is checked, such as `text on panel` or `focus ring on field`.

**Type:** `string`

### <a id="m-passed"></a>Passed

```csharp
public bool Passed { get; init; }
```

True when `Ratio` reaches `Required`.

**Type:** `bool`

### <a id="m-ratio"></a>Ratio

```csharp
public double Ratio { get; init; }
```

The contrast ratio, 1 to 21 (0 when a token is missing).

**Type:** `double`

### <a id="m-required"></a>Required

```csharp
public double Required { get; init; }
```

The ratio the pair must reach: 4.5 for text, 3 for disabled text and indicators.

**Type:** `double`
