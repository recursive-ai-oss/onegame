[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# DisplayList class

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public sealed class DisplayList
```

What the UI hands the renderer each frame: draw commands in paint order, plus the text they refer to. The same instance is reused; [`DisplayList.Version`](DisplayList.md#m-version) changes when the content does.

## Constructors

| Member | Description |
| --- | --- |
| [`DisplayList()`](#m-ctor) |  |

### <a id="m-ctor"></a>DisplayList()

```csharp
public DisplayList();
```

## Properties

| Member | Description |
| --- | --- |
| [`Commands`](#m-commands) | The commands in paint order. |
| [`Height`](#m-height) | Screen height in pixels. |
| [`Lines`](#m-lines) | The lines of all text runs. |
| [`Scale`](#m-scale) | Pixels per UI unit; SVG art is rasterized at this scale. |
| [`TextRuns`](#m-textruns) | The text runs that [`DrawKind.Text`](DrawKind.md#m-text) commands refer to. |
| [`Version`](#m-version) | Increases whenever the commands change; a renderer can skip re-recording when it is unchanged. |
| [`Width`](#m-width) | Screen width in pixels. |

### <a id="m-commands"></a>Commands

```csharp
public ReadOnlySpan<DrawCommand> Commands { get; }
```

The commands in paint order.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`DrawCommand`](DrawCommand.md)&gt;

### <a id="m-height"></a>Height

```csharp
public float Height { get; }
```

Screen height in pixels.

**Type:** `float`

### <a id="m-lines"></a>Lines

```csharp
public ReadOnlySpan<PlacedLine> Lines { get; }
```

The lines of all text runs.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`PlacedLine`](PlacedLine.md)&gt;

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; }
```

Pixels per UI unit; SVG art is rasterized at this scale.

**Type:** `float`

### <a id="m-textruns"></a>TextRuns

```csharp
public ReadOnlySpan<TextRun> TextRuns { get; }
```

The text runs that [`DrawKind.Text`](DrawKind.md#m-text) commands refer to.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`TextRun`](TextRun.md)&gt;

### <a id="m-version"></a>Version

```csharp
public int Version { get; }
```

Increases whenever the commands change; a renderer can skip re-recording when it is unchanged.

**Type:** `int`

### <a id="m-width"></a>Width

```csharp
public float Width { get; }
```

Screen width in pixels.

**Type:** `float`

## Methods

| Member | Description |
| --- | --- |
| [`ToText()`](#m-totext) | One line per command in a stable text form, for tests and for reading in a terminal. |

### <a id="m-totext"></a>ToText()

```csharp
public string ToText();
```

One line per command in a stable text form, for tests and for reading in a terminal.

**Returns:** `string`
