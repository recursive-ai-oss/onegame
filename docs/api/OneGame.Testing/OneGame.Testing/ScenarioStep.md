[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ScenarioStep record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ScenarioStep`](ScenarioStep.md)&gt;

```csharp
public sealed record ScenarioStep : IEquatable<ScenarioStep>
```

One step of a scenario. Only the fields its [`ScenarioStep.Kind`](ScenarioStep.md#m-kind) uses are set.

## Constructors

| Member | Description |
| --- | --- |
| [`ScenarioStep(StepKind, int)`](#m-ctor-stepkind-int) | One step of a scenario. |

### <a id="m-ctor-stepkind-int"></a>ScenarioStep(StepKind, int)

```csharp
public ScenarioStep(StepKind Kind, int Line);
```

One step of a scenario. Only the fields its [`ScenarioStep.Kind`](ScenarioStep.md#m-kind) uses are set.

**Parameters**

- `Kind` ([`StepKind`](StepKind.md))
- `Line` (`int`)

## Properties

| Member | Description |
| --- | --- |
| [`Args`](#m-args) | Command arguments, or the value to set (first element). |
| [`ControlArgs`](#m-controlargs) | The arguments of the `camera` operation, as JSON, for a [`StepKind.Camera`](StepKind.md#m-camera) step. |
| [`Count`](#m-count) | Frames to run. |
| [`Dx`](#m-dx) | Wheel delta x. |
| [`Dy`](#m-dy) | Wheel delta y. |
| [`Entity`](#m-entity) | Entity spec for commands and patches. |
| [`Expect`](#m-expect) | The expectation of an [`StepKind.Expect`](StepKind.md#m-expect) step. |
| [`Finger`](#m-finger) | The finger of a [`StepKind.Touch`](StepKind.md#m-touch) step. |
| [`Id`](#m-id) | Element id. |
| [`KeyState`](#m-keystate) | For a [`StepKind.Press`](StepKind.md#m-press) step: `press` (default), `down` or `up`. |
| [`Kind`](#m-kind) |  |
| [`Line`](#m-line) |  |
| [`Modifiers`](#m-modifiers) | Modifier keys held during a key press. |
| [`Name`](#m-name) | Command name or property name. |
| [`Node`](#m-node) | The node of a process cluster the step is aimed at; null for a single game. |
| [`Path`](#m-path) | File path for a capture or snapshot. |
| [`Row`](#m-row) | Row of [`ScenarioStep.Table`](ScenarioStep.md#m-table), from 0. |
| [`Scale`](#m-scale) | Capture UI scale. |
| [`Selector`](#m-selector) | Element selector. |
| [`Size`](#m-size) | Capture size. |
| [`Table`](#m-table) | Hot table name, for a `set:` of a table cell (instead of [`ScenarioStep.Entity`](ScenarioStep.md#m-entity)). |
| [`Text`](#m-text) | Key name, controller action, screen, fixture or typed text, by kind. |
| [`Tick`](#m-tick) | The tick a [`StepKind.Wait`](StepKind.md#m-wait) step waits for. |
| [`TimeoutMs`](#m-timeoutms) | How long a [`StepKind.Wait`](StepKind.md#m-wait) step may take, in milliseconds. |
| [`X`](#m-x) | Window x. |
| [`Y`](#m-y) | Window y. |

### <a id="m-args"></a>Args

```csharp
public string[] Args { get; init; }
```

Command arguments, or the value to set (first element).

**Type:** `string[]`

### <a id="m-controlargs"></a>ControlArgs

```csharp
public string? ControlArgs { get; init; }
```

The arguments of the `camera` operation, as JSON, for a [`StepKind.Camera`](StepKind.md#m-camera) step.

**Type:** `string?`

### <a id="m-count"></a>Count

```csharp
public int Count { get; init; }
```

Frames to run.

**Type:** `int`

### <a id="m-dx"></a>Dx

```csharp
public double Dx { get; init; }
```

Wheel delta x.

**Type:** `double`

### <a id="m-dy"></a>Dy

```csharp
public double Dy { get; init; }
```

Wheel delta y.

**Type:** `double`

### <a id="m-entity"></a>Entity

```csharp
public string? Entity { get; init; }
```

Entity spec for commands and patches.

**Type:** `string?`

### <a id="m-expect"></a>Expect

```csharp
public ScenarioExpectation? Expect { get; init; }
```

The expectation of an [`StepKind.Expect`](StepKind.md#m-expect) step.

**Type:** [`ScenarioExpectation`](ScenarioExpectation.md)?

### <a id="m-finger"></a>Finger

```csharp
public int Finger { get; init; }
```

The finger of a [`StepKind.Touch`](StepKind.md#m-touch) step.

**Type:** `int`

### <a id="m-id"></a>Id

```csharp
public string? Id { get; init; }
```

Element id.

**Type:** `string?`

### <a id="m-keystate"></a>KeyState

```csharp
public string? KeyState { get; init; }
```

For a [`StepKind.Press`](StepKind.md#m-press) step: `press` (default), `down` or `up`.

**Type:** `string?`

### <a id="m-kind"></a>Kind

```csharp
public StepKind Kind { get; init; }
```

**Type:** [`StepKind`](StepKind.md)

### <a id="m-line"></a>Line

```csharp
public int Line { get; init; }
```

**Type:** `int`

### <a id="m-modifiers"></a>Modifiers

```csharp
public string[] Modifiers { get; init; }
```

Modifier keys held during a key press.

**Type:** `string[]`

### <a id="m-name"></a>Name

```csharp
public string? Name { get; init; }
```

Command name or property name.

**Type:** `string?`

### <a id="m-node"></a>Node

```csharp
public string? Node { get; init; }
```

The node of a process cluster the step is aimed at; null for a single game.

**Type:** `string?`

### <a id="m-path"></a>Path

```csharp
public string? Path { get; init; }
```

File path for a capture or snapshot.

**Type:** `string?`

### <a id="m-row"></a>Row

```csharp
public int? Row { get; init; }
```

Row of [`ScenarioStep.Table`](ScenarioStep.md#m-table), from 0.

**Type:** `int?`

### <a id="m-scale"></a>Scale

```csharp
public double? Scale { get; init; }
```

Capture UI scale.

**Type:** `double?`

### <a id="m-selector"></a>Selector

```csharp
public string? Selector { get; init; }
```

Element selector.

**Type:** `string?`

### <a id="m-size"></a>Size

```csharp
public string? Size { get; init; }
```

Capture size.

**Type:** `string?`

### <a id="m-table"></a>Table

```csharp
public string? Table { get; init; }
```

Hot table name, for a `set:` of a table cell (instead of [`ScenarioStep.Entity`](ScenarioStep.md#m-entity)).

**Type:** `string?`

### <a id="m-text"></a>Text

```csharp
public string? Text { get; init; }
```

Key name, controller action, screen, fixture or typed text, by kind.

**Type:** `string?`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The tick a [`StepKind.Wait`](StepKind.md#m-wait) step waits for.

**Type:** `long`

### <a id="m-timeoutms"></a>TimeoutMs

```csharp
public int TimeoutMs { get; init; }
```

How long a [`StepKind.Wait`](StepKind.md#m-wait) step may take, in milliseconds.

**Type:** `int`

### <a id="m-x"></a>X

```csharp
public double X { get; init; }
```

Window x.

**Type:** `double`

### <a id="m-y"></a>Y

```csharp
public double Y { get; init; }
```

Window y.

**Type:** `double`
