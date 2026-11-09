[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# WindowResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`WindowResult`](WindowResult.md)&gt;

```csharp
public sealed record WindowResult : IEquatable<WindowResult>
```

Result of `window`: the window's placement.

## Constructors

| Member | Description |
| --- | --- |
| [`WindowResult(int?, int?, int, int, string, int, int, bool, string, WorkAreaInfo[])`](#m-ctor-int-int-int-int-string-int-int-bool-string-workareainfo) | Result of `window`: the window's placement. |

### <a id="m-ctor-int-int-int-int-string-int-int-bool-string-workareainfo"></a>WindowResult(int?, int?, int, int, string, int, int, bool, string, WorkAreaInfo\[\])

```csharp
public WindowResult(int? X, int? Y, int Width, int Height, string State, int ClientWidth, int ClientHeight, bool CanPlace, string Text, WorkAreaInfo[] WorkAreas);
```

Result of `window`: the window's placement.

**Parameters**

- `X` (`int?`): Left edge of the normal client rectangle, or null where the window system places windows itself.
- `Y` (`int?`): Top edge of the normal client rectangle, or null where the window system places windows itself.
- `Width` (`int`): Width of the normal (restored) client rectangle in pixels.
- `Height` (`int`): Height of the normal (restored) client rectangle in pixels.
- `State` (`string`): `normal`, `maximized` or `fullscreen`.
- `ClientWidth` (`int`): The client width right now, in pixels (the screen size while maximized).
- `ClientHeight` (`int`): The client height right now, in pixels.
- `CanPlace` (`bool`): Whether this window system lets the game read and set the position.
- `Text` (`string`): The placement as a settings string that `text` accepts back.
- `WorkAreas` ([`WorkAreaInfo`](WorkAreaInfo.md)\[\]): The usable rectangle of each connected screen, primary first; empty where unknown.

## Properties

| Member | Description |
| --- | --- |
| [`CanPlace`](#m-canplace) | Whether this window system lets the game read and set the position. |
| [`ClientHeight`](#m-clientheight) | The client height right now, in pixels. |
| [`ClientWidth`](#m-clientwidth) | The client width right now, in pixels (the screen size while maximized). |
| [`Height`](#m-height) | Height of the normal (restored) client rectangle in pixels. |
| [`State`](#m-state) | `normal`, `maximized` or `fullscreen`. |
| [`Text`](#m-text) | The placement as a settings string that `text` accepts back. |
| [`Width`](#m-width) | Width of the normal (restored) client rectangle in pixels. |
| [`WorkAreas`](#m-workareas) | The usable rectangle of each connected screen, primary first; empty where unknown. |
| [`X`](#m-x) | Left edge of the normal client rectangle, or null where the window system places windows itself. |
| [`Y`](#m-y) | Top edge of the normal client rectangle, or null where the window system places windows itself. |

### <a id="m-canplace"></a>CanPlace

```csharp
public bool CanPlace { get; init; }
```

Whether this window system lets the game read and set the position.

**Type:** `bool`

### <a id="m-clientheight"></a>ClientHeight

```csharp
public int ClientHeight { get; init; }
```

The client height right now, in pixels.

**Type:** `int`

### <a id="m-clientwidth"></a>ClientWidth

```csharp
public int ClientWidth { get; init; }
```

The client width right now, in pixels (the screen size while maximized).

**Type:** `int`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Height of the normal (restored) client rectangle in pixels.

**Type:** `int`

### <a id="m-state"></a>State

```csharp
public string State { get; init; }
```

`normal`, `maximized` or `fullscreen`.

**Type:** `string`

### <a id="m-text"></a>Text

```csharp
public string Text { get; init; }
```

The placement as a settings string that `text` accepts back.

**Type:** `string`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Width of the normal (restored) client rectangle in pixels.

**Type:** `int`

### <a id="m-workareas"></a>WorkAreas

```csharp
public WorkAreaInfo[] WorkAreas { get; init; }
```

The usable rectangle of each connected screen, primary first; empty where unknown.

**Type:** [`WorkAreaInfo`](WorkAreaInfo.md)\[\]

### <a id="m-x"></a>X

```csharp
public int? X { get; init; }
```

Left edge of the normal client rectangle, or null where the window system places windows itself.

**Type:** `int?`

### <a id="m-y"></a>Y

```csharp
public int? Y { get; init; }
```

Top edge of the normal client rectangle, or null where the window system places windows itself.

**Type:** `int?`
