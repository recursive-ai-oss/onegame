[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# WindowPlacement struct

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`WindowPlacement`](WindowPlacement.md)&gt;

```csharp
public readonly struct WindowPlacement : IEquatable<WindowPlacement>
```

Where a window is and how it is shown, as a plain value a game can keep in its settings and apply to a later window. [`WindowPlacement.X`](WindowPlacement.md#m-x), [`WindowPlacement.Y`](WindowPlacement.md#m-y), [`WindowPlacement.Width`](WindowPlacement.md#m-width) and [`WindowPlacement.Height`](WindowPlacement.md#m-height) describe the *normal* (restored) client rectangle in screen pixels, even while the window is maximized or fullscreen, so restoring from either state returns to where the player left it.

## Constructors

| Member | Description |
| --- | --- |
| [`WindowPlacement(int, int, int, int, WindowState, bool)`](#m-ctor-int-int-int-int-windowstate-bool) | Where a window is and how it is shown, as a plain value a game can keep in its settings and apply to a later window. |

### <a id="m-ctor-int-int-int-int-windowstate-bool"></a>WindowPlacement(int, int, int, int, WindowState, bool)

```csharp
public WindowPlacement(int X, int Y, int Width, int Height, WindowState State = 0, bool HasPosition = true);
```

Where a window is and how it is shown, as a plain value a game can keep in its settings and apply to a later window. [`WindowPlacement.X`](WindowPlacement.md#m-x), [`WindowPlacement.Y`](WindowPlacement.md#m-y), [`WindowPlacement.Width`](WindowPlacement.md#m-width) and [`WindowPlacement.Height`](WindowPlacement.md#m-height) describe the *normal* (restored) client rectangle in screen pixels, even while the window is maximized or fullscreen, so restoring from either state returns to where the player left it.

**Parameters**

- `X` (`int`): Left edge of the client area on the virtual screen. Meaningful only when `HasPosition`.
- `Y` (`int`): Top edge of the client area on the virtual screen. Meaningful only when `HasPosition`.
- `Width` (`int`): Client width in pixels.
- `Height` (`int`): Client height in pixels.
- `State` ([`WindowState`](WindowState.md)): Normal, maximized or fullscreen.
- `HasPosition` (`bool`): False where the window system does not let a program read or choose a window's position (Wayland, phones, browsers).

## Properties

| Member | Description |
| --- | --- |
| [`HasPosition`](#m-hasposition) | False where the window system does not let a program read or choose a window's position (Wayland, phones, browsers). |
| [`Height`](#m-height) | Client height in pixels. |
| [`State`](#m-state) | Normal, maximized or fullscreen. |
| [`Width`](#m-width) | Client width in pixels. |
| [`X`](#m-x) | Left edge of the client area on the virtual screen. |
| [`Y`](#m-y) | Top edge of the client area on the virtual screen. |

### <a id="m-hasposition"></a>HasPosition

```csharp
public bool HasPosition { get; init; }
```

False where the window system does not let a program read or choose a window's position (Wayland, phones, browsers).

**Type:** `bool`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Client height in pixels.

**Type:** `int`

### <a id="m-state"></a>State

```csharp
public WindowState State { get; init; }
```

Normal, maximized or fullscreen.

**Type:** [`WindowState`](WindowState.md)

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Client width in pixels.

**Type:** `int`

### <a id="m-x"></a>X

```csharp
public int X { get; init; }
```

Left edge of the client area on the virtual screen. Meaningful only when `HasPosition`.

**Type:** `int`

### <a id="m-y"></a>Y

```csharp
public int Y { get; init; }
```

Top edge of the client area on the virtual screen. Meaningful only when `HasPosition`.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`ClampTo(IReadOnlyList<ScreenRect>)`](#m-clampto-ireadonlylist-screenrect) | Moves and shrinks the normal rectangle so it can be reached on the given screens: when less than a title bar's worth of it is visible on any screen (a monitor that was unplugged, a resolution that dropped), it is fitted into the nearest screen's work area. |
| [`Format()`](#m-format) | Writes the placement as text for a settings file, such as `100,80,1280x720,maximized` or `-,-,1280x720,normal`. |
| [`SizeOnly(int, int, WindowState)`](#m-sizeonly-int-int-windowstate) | A placement that fixes only size and state, for window systems that choose the position themselves. |
| [`TryParse(string?, out WindowPlacement)`](#m-tryparse-string-windowplacement) | Reads text written by [`WindowPlacement.Format()`](WindowPlacement.md#m-format). |

### <a id="m-clampto-ireadonlylist-screenrect"></a>ClampTo(IReadOnlyList&lt;ScreenRect&gt;)

```csharp
public WindowPlacement ClampTo(IReadOnlyList<ScreenRect> workAreas);
```

Moves and shrinks the normal rectangle so it can be reached on the given screens: when less than a title bar's worth of it is visible on any screen (a monitor that was unplugged, a resolution that dropped), it is fitted into the nearest screen's work area. A placement that is already reachable is returned unchanged. The first area is taken as the primary screen.

**Parameters**

- `workAreas` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ScreenRect`](ScreenRect.md)&gt;): The usable rectangle of each connected screen. Empty leaves the placement as it is.

**Returns:** [`WindowPlacement`](WindowPlacement.md) — The placement to apply.

### <a id="m-format"></a>Format()

```csharp
public string Format();
```

Writes the placement as text for a settings file, such as `100,80,1280x720,maximized` or `-,-,1280x720,normal`.

**Returns:** `string` — The text; [`WindowPlacement.TryParse()`](WindowPlacement.md#m-tryparse-string-windowplacement) reads it back.

### <a id="m-sizeonly-int-int-windowstate"></a>SizeOnly(int, int, WindowState)

```csharp
public static WindowPlacement SizeOnly(int width, int height, WindowState state = 0);
```

A placement that fixes only size and state, for window systems that choose the position themselves.

**Parameters**

- `width` (`int`): Client width in pixels.
- `height` (`int`): Client height in pixels.
- `state` ([`WindowState`](WindowState.md)): Normal, maximized or fullscreen.

**Returns:** [`WindowPlacement`](WindowPlacement.md) — The placement, with [`WindowPlacement.HasPosition`](WindowPlacement.md#m-hasposition) false.

### <a id="m-tryparse-string-windowplacement"></a>TryParse(string?, out WindowPlacement)

```csharp
public static bool TryParse(string? text, out WindowPlacement placement);
```

Reads text written by [`WindowPlacement.Format()`](WindowPlacement.md#m-format). Anything else, including absurd sizes, is refused.

**Parameters**

- `text` (`string?`): The text.
- `placement` ([`WindowPlacement`](WindowPlacement.md)): The placement when the text is valid.

**Returns:** `bool` — True when the text was valid.
