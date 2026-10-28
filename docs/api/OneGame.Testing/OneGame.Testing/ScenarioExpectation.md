[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ScenarioExpectation record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ScenarioExpectation`](ScenarioExpectation.md)&gt;

```csharp
public sealed record ScenarioExpectation : IEquatable<ScenarioExpectation>
```

A check on the game's state. Unset fields are not checked.

## Constructors

| Member | Description |
| --- | --- |
| [`ScenarioExpectation(ExpectKind, int)`](#m-ctor-expectkind-int) | A check on the game's state. |

### <a id="m-ctor-expectkind-int"></a>ScenarioExpectation(ExpectKind, int)

```csharp
public ScenarioExpectation(ExpectKind Kind, int Line);
```

A check on the game's state. Unset fields are not checked.

**Parameters**

- `Kind` ([`ExpectKind`](ExpectKind.md))
- `Line` (`int`)

## Properties

| Member | Description |
| --- | --- |
| [`ActionHeld`](#m-actionheld) | Whether the action must be held (action). |
| [`ActionName`](#m-actionname) | The input action to check (action). |
| [`ActionPressed`](#m-actionpressed) | Whether the action must have been pressed this frame (action). |
| [`ActionReleased`](#m-actionreleased) | Whether the action must have been released this frame (action). |
| [`ActionX`](#m-actionx) | The action's value or x (action). |
| [`ActionY`](#m-actiony) | A vector action's y, down positive (action). |
| [`ActiveSet`](#m-activeset) | The action set that must be active (steam-input). |
| [`AssetChecks`](#m-assetchecks) | The checks of an [`ExpectKind.Asset`](ExpectKind.md#m-asset) expectation: `path` first, then `state`, `version` or `error` (a part of the message). |
| [`Contains`](#m-contains) | The first match's text contains this (ui). |
| [`Count`](#m-count) | Number of matching elements (ui). |
| [`DownAction`](#m-downaction) | A digital action that must be down on some controller (steam-input). |
| [`EmitterName`](#m-emittername) | The emitter's name (particles); null counts every emitter shown. |
| [`Enabled`](#m-enabled) | The first match is enabled or disabled (ui). |
| [`Entity`](#m-entity) | Entity spec (state). |
| [`EqualTo`](#m-equalto) | The value must equal this text (numbers compare as numbers). |
| [`Focused`](#m-focused) | The first match has focus or not (ui). |
| [`Id`](#m-id) | Element id (ui). |
| [`Kind`](#m-kind) |  |
| [`Line`](#m-line) |  |
| [`Max`](#m-max) | The value must be at most this (state, numeric). |
| [`MaxDiff`](#m-maxdiff) | Fraction (0-1) of pixels that may differ by more than the tolerance. |
| [`Min`](#m-min) | The value must be at least this (state, numeric). |
| [`Node`](#m-node) | The node of a process cluster the expectation looks at; null for a single game. |
| [`Nodes`](#m-nodes) | The nodes [`ExpectKind.Agree`](ExpectKind.md#m-agree) and [`ExpectKind.Drift`](ExpectKind.md#m-drift) look at; empty means all that apply. |
| [`PickX`](#m-pickx) | Pixels from the left of the 3D view (pick). |
| [`PickY`](#m-picky) | Pixels from the top of the 3D view (pick). |
| [`Pressed`](#m-pressed) | The first match is a toggle control that is switched on or off (ui); a control that is not a toggle never matches. |
| [`Property`](#m-property) | Property name (state). |
| [`Reference`](#m-reference) | Reference PNG, relative to the scenario file (capture). |
| [`Row`](#m-row) | Row of [`ScenarioExpectation.Table`](ScenarioExpectation.md#m-table), from 0 (state). |
| [`SavesChecks`](#m-saveschecks) | The checks of an [`ExpectKind.Saves`](ExpectKind.md#m-saves) expectation: a key (slots, has, lacks, latest, files, settings, label:SLOT, kind:SLOT) and its value. |
| [`Scale`](#m-scale) | Capture UI scale. |
| [`Scope`](#m-scope) | The scope to read on a server that hosts several (cluster scenarios; 1 when absent). |
| [`Selector`](#m-selector) | Selector (ui). |
| [`Server`](#m-server) | The server whose recorded hashes [`ExpectKind.Agree`](ExpectKind.md#m-agree) compares against. |
| [`Size`](#m-size) | Capture size. |
| [`SoundFrames`](#m-soundframes) | Only sounds requested in the last this-many frames count (sound); 0 counts every sound since the scenario began. |
| [`SoundName`](#m-soundname) | The sound's name (sound): the name the clip was loaded under, with or without its file extension. |
| [`StoreChecks`](#m-storechecks) | The checks of a [`ExpectKind.Store`](ExpectKind.md#m-store) expectation: a key (owned, not-owned, pending, connection, unfinished, products, recent) and its value. |
| [`Table`](#m-table) | Hot table name (state), instead of [`ScenarioExpectation.Entity`](ScenarioExpectation.md#m-entity). |
| [`Text`](#m-text) | The first match's text (ui). |
| [`Tolerance`](#m-tolerance) | Per-channel difference (0-255) a pixel may have and still count as equal. |
| [`UpAction`](#m-upaction) | A digital action that must not be down on any controller (steam-input). |
| [`Visible`](#m-visible) | The first match is visible or hidden (ui). |
| [`WithinMs`](#m-withinms) | Cluster state expectations: keep checking for up to this many milliseconds before failing, since a command takes real time to reach the server. |

### <a id="m-actionheld"></a>ActionHeld

```csharp
public bool? ActionHeld { get; init; }
```

Whether the action must be held (action).

**Type:** `bool?`

### <a id="m-actionname"></a>ActionName

```csharp
public string? ActionName { get; init; }
```

The input action to check (action).

**Type:** `string?`

### <a id="m-actionpressed"></a>ActionPressed

```csharp
public bool? ActionPressed { get; init; }
```

Whether the action must have been pressed this frame (action).

**Type:** `bool?`

### <a id="m-actionreleased"></a>ActionReleased

```csharp
public bool? ActionReleased { get; init; }
```

Whether the action must have been released this frame (action).

**Type:** `bool?`

### <a id="m-actionx"></a>ActionX

```csharp
public double? ActionX { get; init; }
```

The action's value or x (action).

**Type:** `double?`

### <a id="m-actiony"></a>ActionY

```csharp
public double? ActionY { get; init; }
```

A vector action's y, down positive (action).

**Type:** `double?`

### <a id="m-activeset"></a>ActiveSet

```csharp
public string? ActiveSet { get; init; }
```

The action set that must be active (steam-input).

**Type:** `string?`

### <a id="m-assetchecks"></a>AssetChecks

```csharp
public (string Key, string Value)[] AssetChecks { get; init; }
```

The checks of an [`ExpectKind.Asset`](ExpectKind.md#m-asset) expectation: `path` first, then `state`, `version` or `error` (a part of the message).

**Type:** `(string Key, string Value)[]`

### <a id="m-contains"></a>Contains

```csharp
public string? Contains { get; init; }
```

The first match's text contains this (ui).

**Type:** `string?`

### <a id="m-count"></a>Count

```csharp
public int? Count { get; init; }
```

Number of matching elements (ui).

**Type:** `int?`

### <a id="m-downaction"></a>DownAction

```csharp
public string? DownAction { get; init; }
```

A digital action that must be down on some controller (steam-input).

**Type:** `string?`

### <a id="m-emittername"></a>EmitterName

```csharp
public string? EmitterName { get; init; }
```

The emitter's name (particles); null counts every emitter shown.

**Type:** `string?`

### <a id="m-enabled"></a>Enabled

```csharp
public bool? Enabled { get; init; }
```

The first match is enabled or disabled (ui).

**Type:** `bool?`

### <a id="m-entity"></a>Entity

```csharp
public string? Entity { get; init; }
```

Entity spec (state).

**Type:** `string?`

### <a id="m-equalto"></a>EqualTo

```csharp
public string? EqualTo { get; init; }
```

The value must equal this text (numbers compare as numbers).

**Type:** `string?`

### <a id="m-focused"></a>Focused

```csharp
public bool? Focused { get; init; }
```

The first match has focus or not (ui).

**Type:** `bool?`

### <a id="m-id"></a>Id

```csharp
public string? Id { get; init; }
```

Element id (ui).

**Type:** `string?`

### <a id="m-kind"></a>Kind

```csharp
public ExpectKind Kind { get; init; }
```

**Type:** [`ExpectKind`](ExpectKind.md)

### <a id="m-line"></a>Line

```csharp
public int Line { get; init; }
```

**Type:** `int`

### <a id="m-max"></a>Max

```csharp
public double? Max { get; init; }
```

The value must be at most this (state, numeric).

**Type:** `double?`

### <a id="m-maxdiff"></a>MaxDiff

```csharp
public double MaxDiff { get; init; }
```

Fraction (0-1) of pixels that may differ by more than the tolerance.

**Type:** `double`

### <a id="m-min"></a>Min

```csharp
public double? Min { get; init; }
```

The value must be at least this (state, numeric).

**Type:** `double?`

### <a id="m-node"></a>Node

```csharp
public string? Node { get; init; }
```

The node of a process cluster the expectation looks at; null for a single game.

**Type:** `string?`

### <a id="m-nodes"></a>Nodes

```csharp
public string[] Nodes { get; init; }
```

The nodes [`ExpectKind.Agree`](ExpectKind.md#m-agree) and [`ExpectKind.Drift`](ExpectKind.md#m-drift) look at; empty means all that apply.

**Type:** `string[]`

### <a id="m-pickx"></a>PickX

```csharp
public double PickX { get; init; }
```

Pixels from the left of the 3D view (pick).

**Type:** `double`

### <a id="m-picky"></a>PickY

```csharp
public double PickY { get; init; }
```

Pixels from the top of the 3D view (pick).

**Type:** `double`

### <a id="m-pressed"></a>Pressed

```csharp
public bool? Pressed { get; init; }
```

The first match is a toggle control that is switched on or off (ui); a control that is not a toggle never matches.

**Type:** `bool?`

### <a id="m-property"></a>Property

```csharp
public string? Property { get; init; }
```

Property name (state).

**Type:** `string?`

### <a id="m-reference"></a>Reference

```csharp
public string? Reference { get; init; }
```

Reference PNG, relative to the scenario file (capture).

**Type:** `string?`

### <a id="m-row"></a>Row

```csharp
public int? Row { get; init; }
```

Row of [`ScenarioExpectation.Table`](ScenarioExpectation.md#m-table), from 0 (state). Without it the property is looked up in the whole table.

**Type:** `int?`

### <a id="m-saveschecks"></a>SavesChecks

```csharp
public (string Key, string Value)[] SavesChecks { get; init; }
```

The checks of an [`ExpectKind.Saves`](ExpectKind.md#m-saves) expectation: a key (slots, has, lacks, latest, files, settings, label:SLOT, kind:SLOT) and its value.

**Type:** `(string Key, string Value)[]`

### <a id="m-scale"></a>Scale

```csharp
public double? Scale { get; init; }
```

Capture UI scale.

**Type:** `double?`

### <a id="m-scope"></a>Scope

```csharp
public int? Scope { get; init; }
```

The scope to read on a server that hosts several (cluster scenarios; 1 when absent).

**Type:** `int?`

### <a id="m-selector"></a>Selector

```csharp
public string? Selector { get; init; }
```

Selector (ui).

**Type:** `string?`

### <a id="m-server"></a>Server

```csharp
public string? Server { get; init; }
```

The server whose recorded hashes [`ExpectKind.Agree`](ExpectKind.md#m-agree) compares against.

**Type:** `string?`

### <a id="m-size"></a>Size

```csharp
public string? Size { get; init; }
```

Capture size.

**Type:** `string?`

### <a id="m-soundframes"></a>SoundFrames

```csharp
public int SoundFrames { get; init; }
```

Only sounds requested in the last this-many frames count (sound); 0 counts every sound since the scenario began.

**Type:** `int`

### <a id="m-soundname"></a>SoundName

```csharp
public string? SoundName { get; init; }
```

The sound's name (sound): the name the clip was loaded under, with or without its file extension.

**Type:** `string?`

### <a id="m-storechecks"></a>StoreChecks

```csharp
public (string Key, string Value)[] StoreChecks { get; init; }
```

The checks of a [`ExpectKind.Store`](ExpectKind.md#m-store) expectation: a key (owned, not-owned, pending, connection, unfinished, products, recent) and its value.

**Type:** `(string Key, string Value)[]`

### <a id="m-table"></a>Table

```csharp
public string? Table { get; init; }
```

Hot table name (state), instead of [`ScenarioExpectation.Entity`](ScenarioExpectation.md#m-entity).

**Type:** `string?`

### <a id="m-text"></a>Text

```csharp
public string? Text { get; init; }
```

The first match's text (ui).

**Type:** `string?`

### <a id="m-tolerance"></a>Tolerance

```csharp
public int Tolerance { get; init; }
```

Per-channel difference (0-255) a pixel may have and still count as equal.

**Type:** `int`

### <a id="m-upaction"></a>UpAction

```csharp
public string? UpAction { get; init; }
```

A digital action that must not be down on any controller (steam-input).

**Type:** `string?`

### <a id="m-visible"></a>Visible

```csharp
public bool? Visible { get; init; }
```

The first match is visible or hidden (ui).

**Type:** `bool?`

### <a id="m-withinms"></a>WithinMs

```csharp
public int WithinMs { get; init; }
```

Cluster state expectations: keep checking for up to this many milliseconds before failing, since a command takes real time to reach the server.

**Type:** `int`
