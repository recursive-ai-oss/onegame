[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavDebugShapes class

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)

```csharp
public sealed class NavDebugShapes
```

Debug geometry for navigation: blocked cells, costed areas, paths and searches, as plain lines and rectangles in world units. It is view-side data (floats, never state): collect it in a view while the debug overlay is on and draw it with the 2D drawing calls.

```csharp
if (d.DebugVisible)
{
    _shapes.Clear();
    _shapes.AddGrid(Map);
    _shapes.AddPath(Map, Nav);
    foreach (var r in _shapes.Rects) { d.FillRect(new Rect(r.Position.X, r.Position.Y, r.Size.X, r.Size.Y), Color.FromRgba(r.Rgba), z: 9); }
    foreach (var l in _shapes.Lines) { d.Line(l.From, l.To, Color.FromRgba(l.Rgba), l.Width, z: 10); }
}
```

## Constructors

| Member | Description |
| --- | --- |
| [`NavDebugShapes()`](#m-ctor) |  |

### <a id="m-ctor"></a>NavDebugShapes()

```csharp
public NavDebugShapes();
```

## Fields

| Member | Description |
| --- | --- |
| [`AreaColor`](#m-areacolor) | Colour of costed areas. |
| [`BlockedColor`](#m-blockedcolor) | Colour of blocked cells. |
| [`MarkerColor`](#m-markercolor) | Colour of the start and goal markers. |
| [`PathColor`](#m-pathcolor) | Colour of paths. |
| [`WalkedColor`](#m-walkedcolor) | Colour of the part of a path already walked. |

### <a id="m-areacolor"></a>AreaColor

```csharp
public const uint AreaColor = 3500159088;
```

Colour of costed areas.

**Type:** `uint`

### <a id="m-blockedcolor"></a>BlockedColor

```csharp
public const uint BlockedColor = 3224383648;
```

Colour of blocked cells.

**Type:** `uint`

### <a id="m-markercolor"></a>MarkerColor

```csharp
public const uint MarkerColor = 4294967295;
```

Colour of the start and goal markers.

**Type:** `uint`

### <a id="m-pathcolor"></a>PathColor

```csharp
public const uint PathColor = 820027647;
```

Colour of paths.

**Type:** `uint`

### <a id="m-walkedcolor"></a>WalkedColor

```csharp
public const uint WalkedColor = 2155905184;
```

Colour of the part of a path already walked.

**Type:** `uint`

## Properties

| Member | Description |
| --- | --- |
| [`Lines`](#m-lines) | The lines collected so far. |
| [`Rects`](#m-rects) | The rectangles collected so far. |

### <a id="m-lines"></a>Lines

```csharp
public IReadOnlyList<NavDebugLine> Lines { get; }
```

The lines collected so far.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`NavDebugLine`](NavDebugLine.md)&gt;

### <a id="m-rects"></a>Rects

```csharp
public IReadOnlyList<NavDebugRect> Rects { get; }
```

The rectangles collected so far.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`NavDebugRect`](NavDebugRect.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`AddGrid(NavGrid)`](#m-addgrid-navgrid) | Adds the blocked and costed cells (horizontal runs of equal cells become one rectangle) and the grid's outline. |
| [`AddPath(NavGrid, in NavState)`](#m-addpath-navgrid-navstate) | Adds a unit's path (walked part grey, the rest green) with squares on the start and goal; a running search shows only the markers. |
| [`Clear()`](#m-clear) | Forgets everything collected. |

### <a id="m-addgrid-navgrid"></a>AddGrid(NavGrid)

```csharp
public void AddGrid(NavGrid grid);
```

Adds the blocked and costed cells (horizontal runs of equal cells become one rectangle) and the grid's outline.

**Parameters**

- `grid` ([`NavGrid`](NavGrid.md)): The map.

### <a id="m-addpath-navgrid-navstate"></a>AddPath(NavGrid, in NavState)

```csharp
public void AddPath(NavGrid grid, in NavState state);
```

Adds a unit's path (walked part grey, the rest green) with squares on the start and goal; a running search shows only the markers.

**Parameters**

- `grid` ([`NavGrid`](NavGrid.md)): The map the state belongs to.
- `state` ([`NavState`](NavState.md)): The unit's navigation.

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Forgets everything collected.
