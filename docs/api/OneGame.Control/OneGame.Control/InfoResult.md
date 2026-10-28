[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# InfoResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`InfoResult`](InfoResult.md)&gt;

```csharp
public sealed record InfoResult : IEquatable<InfoResult>
```

Result of `info`, the operation to call first: which game this is, where it is in time, and what it is drawing with.

## Constructors

| Member | Description |
| --- | --- |
| [`InfoResult(string, long, long, string, int, int, float, bool, string?, int, string[], string[], string, string?, int, string, long, float?)`](#m-ctor-string-long-long-string-int-int-float-bool-string-int-string-string-string-string-int-string-long-float) | Result of `info`, the operation to call first: which game this is, where it is in time, and what it is drawing with. |

### <a id="m-ctor-string-long-long-string-int-int-float-bool-string-int-string-string-string-string-int-string-long-float"></a>InfoResult(string, long, long, string, int, int, float, bool, string?, int, string\[\], string\[\], string, string?, int, string, long, float?)

```csharp
public InfoResult(string Game, long Tick, long Frame, string Hash, int Width, int Height, float Scale, bool Headless, string? Screen, int Entities, string[] Screens, string[] Fixtures, string Stage, string? Adapter, int Drift, string Backend, long Presented, float? ScaleOverride = default);
```

Result of `info`, the operation to call first: which game this is, where it is in time, and what it is drawing with.

**Parameters**

- `Game` (`string`): The game's name.
- `Tick` (`long`): The world's simulation tick.
- `Frame` (`long`): Frames run (a frame is one tick plus UI and drawing).
- `Hash` (`string`): The state hash of all synced state, 16 hex digits.
- `Width` (`int`): Window width in pixels.
- `Height` (`int`): Window height in pixels.
- `Scale` (`float`): The UI scale in effect.
- `Headless` (`bool`): True when there is no window (tests and servers).
- `Screen` (`string?`): The name of the open UI screen, or null.
- `Entities` (`int`): How many entities the world holds.
- `Screens` (`string[]`): Names that `screen_open` accepts.
- `Fixtures` (`string[]`): Names that `fixture_load` accepts.
- `Stage` (`string`): The name of the render stage drawing the game.
- `Adapter` (`string?`): The graphics adapter's name, or null when drawing on the CPU.
- `Drift` (`int`): Drift reports filed by the local replica; it must stay 0.
- `Backend` (`string`): The platform backend, such as `headless`, `x11` or `wayland` (the platform backend's name in lower case).
- `Presented` (`long`): Frames presented to the window so far.
- `ScaleOverride` (`float?`): The UI scale set by `ui_scale`, or null when the scale follows the window.

## Properties

| Member | Description |
| --- | --- |
| [`Adapter`](#m-adapter) | The graphics adapter's name, or null when drawing on the CPU. |
| [`Backend`](#m-backend) | The platform backend, such as `headless`, `x11` or `wayland` (the platform backend's name in lower case). |
| [`Drift`](#m-drift) | Drift reports filed by the local replica; it must stay 0. |
| [`Entities`](#m-entities) | How many entities the world holds. |
| [`Fixtures`](#m-fixtures) | Names that `fixture_load` accepts. |
| [`Frame`](#m-frame) | Frames run (a frame is one tick plus UI and drawing). |
| [`Game`](#m-game) | The game's name. |
| [`Hash`](#m-hash) | The state hash of all synced state, 16 hex digits. |
| [`Headless`](#m-headless) | True when there is no window (tests and servers). |
| [`Height`](#m-height) | Window height in pixels. |
| [`Presented`](#m-presented) | Frames presented to the window so far. |
| [`Scale`](#m-scale) | The UI scale in effect. |
| [`ScaleOverride`](#m-scaleoverride) | The UI scale set by `ui_scale`, or null when the scale follows the window. |
| [`Screen`](#m-screen) | The name of the open UI screen, or null. |
| [`Screens`](#m-screens) | Names that `screen_open` accepts. |
| [`Stage`](#m-stage) | The name of the render stage drawing the game. |
| [`Tick`](#m-tick) | The world's simulation tick. |
| [`Width`](#m-width) | Window width in pixels. |

### <a id="m-adapter"></a>Adapter

```csharp
public string? Adapter { get; init; }
```

The graphics adapter's name, or null when drawing on the CPU.

**Type:** `string?`

### <a id="m-backend"></a>Backend

```csharp
public string Backend { get; init; }
```

The platform backend, such as `headless`, `x11` or `wayland` (the platform backend's name in lower case).

**Type:** `string`

### <a id="m-drift"></a>Drift

```csharp
public int Drift { get; init; }
```

Drift reports filed by the local replica; it must stay 0.

**Type:** `int`

### <a id="m-entities"></a>Entities

```csharp
public int Entities { get; init; }
```

How many entities the world holds.

**Type:** `int`

### <a id="m-fixtures"></a>Fixtures

```csharp
public string[] Fixtures { get; init; }
```

Names that `fixture_load` accepts.

**Type:** `string[]`

### <a id="m-frame"></a>Frame

```csharp
public long Frame { get; init; }
```

Frames run (a frame is one tick plus UI and drawing).

**Type:** `long`

### <a id="m-game"></a>Game

```csharp
public string Game { get; init; }
```

The game's name.

**Type:** `string`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The state hash of all synced state, 16 hex digits.

**Type:** `string`

### <a id="m-headless"></a>Headless

```csharp
public bool Headless { get; init; }
```

True when there is no window (tests and servers).

**Type:** `bool`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

Window height in pixels.

**Type:** `int`

### <a id="m-presented"></a>Presented

```csharp
public long Presented { get; init; }
```

Frames presented to the window so far.

**Type:** `long`

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; init; }
```

The UI scale in effect.

**Type:** `float`

### <a id="m-scaleoverride"></a>ScaleOverride

```csharp
public float? ScaleOverride { get; init; }
```

The UI scale set by `ui_scale`, or null when the scale follows the window.

**Type:** `float?`

### <a id="m-screen"></a>Screen

```csharp
public string? Screen { get; init; }
```

The name of the open UI screen, or null.

**Type:** `string?`

### <a id="m-screens"></a>Screens

```csharp
public string[] Screens { get; init; }
```

Names that `screen_open` accepts.

**Type:** `string[]`

### <a id="m-stage"></a>Stage

```csharp
public string Stage { get; init; }
```

The name of the render stage drawing the game.

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The world's simulation tick.

**Type:** `long`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

Window width in pixels.

**Type:** `int`
