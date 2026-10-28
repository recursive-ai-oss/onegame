[API reference](../../index.md) › [OneGame.Navigation](../index.md) › OneGame.Navigation

# NavState struct

**Namespace:** `OneGame.Navigation`  
**Package:** [`RecursiveAI.OneGame.Navigation`](../index.md)  
**Implements:** [`ISyncValue`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md)&lt;[`NavState`](NavState.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NavState`](NavState.md)&gt;

```csharp
public readonly struct NavState : ISyncValue<NavState>, IEquatable<NavState>
```

One unit's navigation, as a value you keep in a synced property: what it asked for, how far the search got, and the path it found. Because it is synced state it is saved with the world, restored from a snapshot, sent to replicas and covered by the drift hash. It is immutable; [`NavSearcher`](NavSearcher.md) and [`NavState.Follow()`](NavState.md#m-follow-navgrid-fvec2-fixed) return the next state, and you assign it back.

```csharp
[Synced] public partial NavState Nav { get; private set; }
```

What a restore needs is only here: a search in progress is described by its start, goal and the number of iterations spent, and replaying that many iterations gives the identical search, so no search internals are stored. A stored path that does not fit the grid it is used with (an edited save) is dropped, never followed.

## Properties

| Member | Description |
| --- | --- |
| [`AgentSize`](#m-agentsize) | The unit's width in cells (at least 1). |
| [`Cells`](#m-cells) | The path as cell indices from start to goal; empty unless a path was found. |
| [`Goal`](#m-goal) | Index of the goal cell. |
| [`HasPath`](#m-haspath) | True while a path is ready to follow. |
| [`Moves`](#m-moves) | The unit's moves in the search. |
| [`Next`](#m-next) | Index into [`NavState.Cells`](NavState.md#m-cells) of the waypoint the unit is walking to. |
| [`Options`](#m-options) | The options this state searches with. |
| [`Spent`](#m-spent) | Search iterations spent so far on the current search. |
| [`Stamp`](#m-stamp) | The [`NavGrid.Stamp`](NavGrid.md#m-stamp) the search or path was made for. |
| [`Start`](#m-start) | Index (`y * width + x`) of the cell the search starts from; it follows the unit as it walks, so a repeat search starts where it is. |
| [`Status`](#m-status) | Where the search or the walk stands. |

### <a id="m-agentsize"></a>AgentSize

```csharp
public int AgentSize { get; }
```

The unit's width in cells (at least 1).

**Type:** `int`

### <a id="m-cells"></a>Cells

```csharp
public ReadOnlySpan<int> Cells { get; }
```

The path as cell indices from start to goal; empty unless a path was found.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<int>`

### <a id="m-goal"></a>Goal

```csharp
public int Goal { get; }
```

Index of the goal cell.

**Type:** `int`

### <a id="m-haspath"></a>HasPath

```csharp
public bool HasPath { get; }
```

True while a path is ready to follow.

**Type:** `bool`

### <a id="m-moves"></a>Moves

```csharp
public NavMoves Moves { get; }
```

The unit's moves in the search.

**Type:** [`NavMoves`](NavMoves.md)

### <a id="m-next"></a>Next

```csharp
public int Next { get; }
```

Index into [`NavState.Cells`](NavState.md#m-cells) of the waypoint the unit is walking to.

**Type:** `int`

### <a id="m-options"></a>Options

```csharp
public NavOptions Options { get; }
```

The options this state searches with.

**Type:** [`NavOptions`](NavOptions.md)

### <a id="m-spent"></a>Spent

```csharp
public int Spent { get; }
```

Search iterations spent so far on the current search.

**Type:** `int`

### <a id="m-stamp"></a>Stamp

```csharp
public ulong Stamp { get; }
```

The [`NavGrid.Stamp`](NavGrid.md#m-stamp) the search or path was made for.

**Type:** `ulong`

### <a id="m-start"></a>Start

```csharp
public int Start { get; }
```

Index (`y * width + x`) of the cell the search starts from; it follows the unit as it walks, so a repeat search starts where it is.

**Type:** `int`

### <a id="m-status"></a>Status

```csharp
public NavStatus Status { get; }
```

Where the search or the walk stands.

**Type:** [`NavStatus`](NavStatus.md)

## Methods

| Member | Description |
| --- | --- |
| [`Equals(NavState)`](#m-equals-navstate) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`Follow(NavGrid, ref FVec2, Fixed)`](#m-follow-navgrid-fvec2-fixed) | Walks a unit along the path: moves `position` up to `distance` toward the next waypoint (cell centre), then on to the following ones with what is left. |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). |
| [`ToString()`](#m-tostring) |  |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-equals-navstate"></a>Equals(NavState)

```csharp
public bool Equals(NavState other);
```

Implements [`IEquatable<NavState>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`NavState`](NavState.md))

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

Overrides [`ValueType.Equals()`](https://learn.microsoft.com/dotnet/api/system.valuetype.equals).

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-follow-navgrid-fvec2-fixed"></a>Follow(NavGrid, ref FVec2, Fixed)

```csharp
public NavState Follow(NavGrid grid, ref FVec2 position, Fixed distance);
```

Walks a unit along the path: moves `position` up to `distance` toward the next waypoint (cell centre), then on to the following ones with what is left. Reaching the last one makes the status [`NavStatus.Arrived`](NavStatus.md#m-arrived). Integer arithmetic only, so every node walks the same way.

**Parameters**

- `grid` ([`NavGrid`](NavGrid.md)): The grid the path was found on.
- `position` ([`FVec2`](../../OneGame.Core/OneGame.Core/FVec2.md)): The unit's position, moved in place.
- `distance` ([`Fixed`](../../OneGame.Core/OneGame.Core/Fixed.md)): How far the unit may travel this tick (speed times the tick length); not negative.

**Returns:** [`NavState`](NavState.md) — The next state: unchanged unless a waypoint was reached.

**Exceptions**

- [`NavRangeException`](NavRangeException.md): The position is beyond ±2^21 units.
- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): The distance is negative.

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

Overrides [`ValueType.GetHashCode()`](https://learn.microsoft.com/dotnet/api/system.valuetype.gethashcode).

**Returns:** `int`

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public static NavState Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

**Returns:** [`NavState`](NavState.md)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

Overrides [`ValueType.ToString()`](https://learn.microsoft.com/dotnet/api/system.valuetype.tostring).

**Returns:** `string`

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
public void Write(SyncWriter writer);
```

Writes the value in the sync format.

**Parameters**

- `writer` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md))

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
public void WriteJson(JsonOut json);
```

Writes the value as JSON for inspection.

**Parameters**

- `json` ([`JsonOut`](../../OneGame.Core/OneGame.Core/JsonOut.md))

## Operators

| Member | Description |
| --- | --- |
| [`operator !=(NavState, NavState)`](#m-operator-navstate-navstate) | Inequality. |
| [`operator ==(NavState, NavState)`](#m-operator-navstate-navstate-2) | Equality of every part. |

### <a id="m-operator-navstate-navstate"></a>operator !=(NavState, NavState)

```csharp
public static bool operator !=(NavState a, NavState b);
```

Inequality.

**Parameters**

- `a` ([`NavState`](NavState.md))
- `b` ([`NavState`](NavState.md))

**Returns:** `bool`

### <a id="m-operator-navstate-navstate-2"></a>operator ==(NavState, NavState)

```csharp
public static bool operator ==(NavState a, NavState b);
```

Equality of every part.

**Parameters**

- `a` ([`NavState`](NavState.md))
- `b` ([`NavState`](NavState.md))

**Returns:** `bool`
