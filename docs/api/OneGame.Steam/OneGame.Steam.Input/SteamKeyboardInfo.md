[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamKeyboardInfo record

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SteamKeyboardInfo`](SteamKeyboardInfo.md)&gt;

```csharp
public sealed record SteamKeyboardInfo : IEquatable<SteamKeyboardInfo>
```

The on-screen keyboard, for the report.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamKeyboardInfo(bool, int, int, int, int, int)`](#m-ctor-bool-int-int-int-int-int) | The on-screen keyboard, for the report. |

### <a id="m-ctor-bool-int-int-int-int-int"></a>SteamKeyboardInfo(bool, int, int, int, int, int)

```csharp
public SteamKeyboardInfo(bool Visible, int X, int Y, int Width, int Height, int Shown);
```

The on-screen keyboard, for the report.

**Parameters**

- `Visible` (`bool`): True while the game has asked for it and not dismissed it.
- `X` (`int`): Left of the text field it avoids, in window pixels.
- `Y` (`int`): Top of the text field.
- `Width` (`int`): Width of the text field.
- `Height` (`int`): Height of the text field.
- `Shown` (`int`): How many times it has been shown.

## Properties

| Member | Description |
| --- | --- |
| [`Height`](#m-height) | Height of the text field. |
| [`Shown`](#m-shown) | How many times it has been shown. |
| [`Visible`](#m-visible) | True while the game has asked for it and not dismissed it. |
| [`Width`](#m-width) | Width of the text field. |
| [`X`](#m-x) | Left of the text field it avoids, in window pixels. |
| [`Y`](#m-y) | Top of the text field. |

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Height of the text field.

**Type:** `int`

### <a id="m-shown"></a>Shown

```csharp
public int Shown { get; init; }
```

How many times it has been shown.

**Type:** `int`

### <a id="m-visible"></a>Visible

```csharp
public bool Visible { get; init; }
```

True while the game has asked for it and not dismissed it.

**Type:** `bool`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Width of the text field.

**Type:** `int`

### <a id="m-x"></a>X

```csharp
public int X { get; init; }
```

Left of the text field it avoids, in window pixels.

**Type:** `int`

### <a id="m-y"></a>Y

```csharp
public int Y { get; init; }
```

Top of the text field.

**Type:** `int`
