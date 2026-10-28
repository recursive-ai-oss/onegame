[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# Scenario record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`Scenario`](Scenario.md)&gt;

```csharp
public sealed record Scenario : IEquatable<Scenario>
```

A scenario: where to start, what to do, and what must hold afterwards. Scenarios are normally YAML files read with [`ScenarioParser`](ScenarioParser.md) and run with [`TestCluster.Run()`](TestCluster.md#m-run-string-scenariorunoptions) or `onegame test`; the same record can be built in code.

**Example**

A scenario file. Steps run in order; an `expect` step checks at that moment, the final `expect` list after the last step:

```csharp
name: buy-sword
steps:
  - fixture: rich-player
  - click: shop-open
  - step: 2
  - click: buy-sword
  - step: 1
  - capture: { path: shop.png, size: 1280x720 }
expect:
  - state: { entity: Player, property: Gold, equals: 50 }
  - ui: { id: sword-owned, visible: true }
```

## Constructors

| Member | Description |
| --- | --- |
| [`Scenario()`](#m-ctor) |  |

### <a id="m-ctor"></a>Scenario()

```csharp
public Scenario();
```

> **Obsolete.** Constructors of types with required members are not supported in this version of your compiler.

## Properties

| Member | Description |
| --- | --- |
| [`Description`](#m-description) | A sentence about what the scenario shows. |
| [`Expect`](#m-expect) | Expectations checked after the last step. |
| [`Fixture`](#m-fixture) | Fixture to load first. |
| [`Name`](#m-name) | The scenario's name. |
| [`Seed`](#m-seed) | World seed, when it should differ from the game's. |
| [`Snapshot`](#m-snapshot) | Snapshot file to load first, relative to the scenario file. |
| [`SourcePath`](#m-sourcepath) | The file it was read from. |
| [`Steps`](#m-steps) | The steps, in order. |
| [`Window`](#m-window) | Window size to run at, such as `800x600`. |

### <a id="m-description"></a>Description

```csharp
public string? Description { get; init; }
```

A sentence about what the scenario shows.

**Type:** `string?`

### <a id="m-expect"></a>Expect

```csharp
public IReadOnlyList<ScenarioExpectation> Expect { get; init; }
```

Expectations checked after the last step.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ScenarioExpectation`](ScenarioExpectation.md)&gt;

### <a id="m-fixture"></a>Fixture

```csharp
public string? Fixture { get; init; }
```

Fixture to load first.

**Type:** `string?`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The scenario's name.

**Type:** `string`

### <a id="m-seed"></a>Seed

```csharp
public ulong? Seed { get; init; }
```

World seed, when it should differ from the game's.

**Type:** `ulong?`

### <a id="m-snapshot"></a>Snapshot

```csharp
public string? Snapshot { get; init; }
```

Snapshot file to load first, relative to the scenario file.

**Type:** `string?`

### <a id="m-sourcepath"></a>SourcePath

```csharp
public string? SourcePath { get; init; }
```

The file it was read from.

**Type:** `string?`

### <a id="m-steps"></a>Steps

```csharp
public IReadOnlyList<ScenarioStep> Steps { get; init; }
```

The steps, in order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ScenarioStep`](ScenarioStep.md)&gt;

### <a id="m-window"></a>Window

```csharp
public string? Window { get; init; }
```

Window size to run at, such as `800x600`.

**Type:** `string?`
