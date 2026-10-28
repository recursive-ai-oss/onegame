[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ScenarioResult record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ScenarioResult`](ScenarioResult.md)&gt;

```csharp
public sealed record ScenarioResult : IEquatable<ScenarioResult>
```

Outcome of a scenario.

## Constructors

| Member | Description |
| --- | --- |
| [`ScenarioResult(string, string?, bool, long, string, long, StepResult[], ExpectationResult[], PlayedSound[]?)`](#m-ctor-string-string-bool-long-string-long-stepresult-expectationresult-playedsound) | Outcome of a scenario. |

### <a id="m-ctor-string-string-bool-long-string-long-stepresult-expectationresult-playedsound"></a>ScenarioResult(string, string?, bool, long, string, long, StepResult\[\], ExpectationResult\[\], PlayedSound\[\]?)

```csharp
public ScenarioResult(string Name, string? Source, bool Passed, long Tick, string Hash, long ElapsedMs, StepResult[] Steps, ExpectationResult[] Expectations, PlayedSound[]? Sounds = null);
```

Outcome of a scenario.

**Parameters**

- `Name` (`string`): The scenario's name.
- `Source` (`string?`): The file it came from.
- `Passed` (`bool`): True when every step ran and every expectation held.
- `Tick` (`long`): The world tick at the end.
- `Hash` (`string`): The state hash at the end.
- `ElapsedMs` (`long`): Wall time of the run in milliseconds.
- `Steps` ([`StepResult`](StepResult.md)\[\]): Every step.
- `Expectations` ([`ExpectationResult`](ExpectationResult.md)\[\]): Every expectation that was checked.
- `Sounds` ([`PlayedSound`](../../OneGame.Audio/OneGame.Audio/PlayedSound.md)\[\]?): The sounds the game asked the audio service to play during the run, oldest first (the audio event log); null when the game runs no audio service.

## Properties

| Member | Description |
| --- | --- |
| [`ElapsedMs`](#m-elapsedms) | Wall time of the run in milliseconds. |
| [`Expectations`](#m-expectations) | Every expectation that was checked. |
| [`Hash`](#m-hash) | The state hash at the end. |
| [`Name`](#m-name) | The scenario's name. |
| [`Passed`](#m-passed) | True when every step ran and every expectation held. |
| [`Sounds`](#m-sounds) | The sounds the game asked the audio service to play during the run, oldest first (the audio event log); null when the game runs no audio service. |
| [`Source`](#m-source) | The file it came from. |
| [`Steps`](#m-steps) | Every step. |
| [`Tick`](#m-tick) | The world tick at the end. |

### <a id="m-elapsedms"></a>ElapsedMs

```csharp
public long ElapsedMs { get; init; }
```

Wall time of the run in milliseconds.

**Type:** `long`

### <a id="m-expectations"></a>Expectations

```csharp
public ExpectationResult[] Expectations { get; init; }
```

Every expectation that was checked.

**Type:** [`ExpectationResult`](ExpectationResult.md)\[\]

### <a id="m-hash"></a>Hash

```csharp
public string Hash { get; init; }
```

The state hash at the end.

**Type:** `string`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The scenario's name.

**Type:** `string`

### <a id="m-passed"></a>Passed

```csharp
public bool Passed { get; init; }
```

True when every step ran and every expectation held.

**Type:** `bool`

### <a id="m-sounds"></a>Sounds

```csharp
public PlayedSound[]? Sounds { get; init; }
```

The sounds the game asked the audio service to play during the run, oldest first (the audio event log); null when the game runs no audio service.

**Type:** [`PlayedSound`](../../OneGame.Audio/OneGame.Audio/PlayedSound.md)\[\]?

### <a id="m-source"></a>Source

```csharp
public string? Source { get; init; }
```

The file it came from.

**Type:** `string?`

### <a id="m-steps"></a>Steps

```csharp
public StepResult[] Steps { get; init; }
```

Every step.

**Type:** [`StepResult`](StepResult.md)\[\]

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The world tick at the end.

**Type:** `long`
