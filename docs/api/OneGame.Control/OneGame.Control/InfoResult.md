[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# InfoResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`InfoResult`](InfoResult.md)&gt;

```csharp
public sealed record InfoResult : IEquatable<InfoResult>
```

Result of `info`. `Drift` counts drift reports filed by the local replica; it must stay 0.

## Constructors

| Member | Description |
| --- | --- |
| [`InfoResult(string, long, long, string, int, int, float, bool, string?, int, string[], string[], string, string?, int, string, long, float?)`](#m-ctor-string-long-long-string-int-int-float-bool-string-int-string-string-string-string-int-string-long-float) | Result of `info`. |

### <a id="m-ctor-string-long-long-string-int-int-float-bool-string-int-string-string-string-string-int-string-long-float"></a>InfoResult(string, long, long, string, int, int, float, bool, string?, int, string\[\], string\[\], string, string?, int, string, long, float?)

```csharp
public InfoResult(string Game, long Tick, long Frame, string Hash, int Width, int Height, float Scale, bool Headless, string? Screen, int Entities, string[] Screens, string[] Fixtures, string Stage, string? Adapter, int Drift, string Backend, long Presented, float? ScaleOverride = default);
```

Result of `info`. `Drift` counts drift reports filed by the local replica; it must stay 0.

**Parameters**

- `Game` (`string`)
- `Tick` (`long`)
- `Frame` (`long`)
- `Hash` (`string`)
- `Width` (`int`)
- `Height` (`int`)
- `Scale` (`float`)
- `Headless` (`bool`)
- `Screen` (`string?`)
- `Entities` (`int`)
- `Screens` (`string[]`)
- `Fixtures` (`string[]`)
- `Stage` (`string`)
- `Adapter` (`string?`)
- `Drift` (`int`)
- `Backend` (`string`)
- `Presented` (`long`)
- `ScaleOverride` (`float?`)

## Properties

| Member | Description |
| --- | --- |
| [`Adapter`](#m-adapter) |  |
| [`Backend`](#m-backend) |  |
| [`Drift`](#m-drift) |  |
| [`Entities`](#m-entities) |  |
| [`Fixtures`](#m-fixtures) |  |
| [`Frame`](#m-frame) |  |
| [`Game`](#m-game) |  |
| [`Hash`](#m-hash) |  |
| [`Headless`](#m-headless) |  |
| [`Height`](#m-height) |  |
| [`Presented`](#m-presented) |  |
| [`Scale`](#m-scale) |  |
| [`ScaleOverride`](#m-scaleoverride) |  |
| [`Screen`](#m-screen) |  |
| [`Screens`](#m-screens) |  |
| [`Stage`](#m-stage) |  |
| [`Tick`](#m-tick) |  |
| [`Width`](#m-width) |  |

### <a id="m-adapter"></a>Adapter

```csharp
public string? Adapter { get; init; }
```

**Type:** `string?`

### <a id="m-backend"></a>Backend

```csharp
public string Backend { get; init; }
```

**Type:** `string`

### <a id="m-drift"></a>Drift

```csharp
public int Drift { get; init; }
```

**Type:** `int`

### <a id="m-entities"></a>Entities

```csharp
public int Entities { get; init; }
```

**Type:** `int`

### <a id="m-fixtures"></a>Fixtures

```csharp
public string[] Fixtures { get; init; }
```

**Type:** `string[]`

### <a id="m-frame"></a>Frame

```csharp
public long Frame { get; init; }
```

**Type:** `long`

### <a id="m-game"></a>Game

```csharp
public string Game { get; init; }
```

**Type:** `string`

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

**Type:** `string`

### <a id="m-headless"></a>Headless

```csharp
public bool Headless { get; init; }
```

**Type:** `bool`

### <a id="m-height"></a>Height

```csharp
public int Height { get; init; }
```

**Type:** `int`

### <a id="m-presented"></a>Presented

```csharp
public long Presented { get; init; }
```

**Type:** `long`

### <a id="m-scale"></a>Scale

```csharp
public float Scale { get; init; }
```

**Type:** `float`

### <a id="m-scaleoverride"></a>ScaleOverride

```csharp
public float? ScaleOverride { get; init; }
```

**Type:** `float?`

### <a id="m-screen"></a>Screen

```csharp
public string? Screen { get; init; }
```

**Type:** `string?`

### <a id="m-screens"></a>Screens

```csharp
public string[] Screens { get; init; }
```

**Type:** `string[]`

### <a id="m-stage"></a>Stage

```csharp
public string Stage { get; init; }
```

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

**Type:** `long`

### <a id="m-width"></a>Width

```csharp
public int Width { get; init; }
```

**Type:** `int`
