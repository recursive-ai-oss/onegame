[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ScenarioRunOptions record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ScenarioRunOptions`](ScenarioRunOptions.md)&gt;

```csharp
public sealed record ScenarioRunOptions : IEquatable<ScenarioRunOptions>
```

Settings for running scenarios.

## Constructors

| Member | Description |
| --- | --- |
| [`ScenarioRunOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>ScenarioRunOptions()

```csharp
public ScenarioRunOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`ArtifactsDirectory`](#m-artifactsdirectory) | Where actual and diff images of failed capture checks, and captures saved by steps, are written. |
| [`BaseDirectory`](#m-basedirectory) | Folder that relative paths in the scenario (snapshots, reference images) are read from. |
| [`MinCaptureMaxDiff`](#m-mincapturemaxdiff) | Loosens every capture check to at least this fraction of differing pixels. |
| [`MinCaptureTolerance`](#m-mincapturetolerance) | Loosens every capture check to at least this per-channel tolerance. |
| [`UpdateReferences`](#m-updatereferences) | Write reference images that are missing or differ instead of failing. |

### <a id="m-artifactsdirectory"></a>ArtifactsDirectory

```csharp
public string? ArtifactsDirectory { get; init; }
```

Where actual and diff images of failed capture checks, and captures saved by steps, are written.

**Type:** `string?`

### <a id="m-basedirectory"></a>BaseDirectory

```csharp
public string? BaseDirectory { get; init; }
```

Folder that relative paths in the scenario (snapshots, reference images) are read from. Defaults to the scenario file's folder.

**Type:** `string?`

### <a id="m-mincapturemaxdiff"></a>MinCaptureMaxDiff

```csharp
public double MinCaptureMaxDiff { get; init; }
```

Loosens every capture check to at least this fraction of differing pixels.

**Type:** `double`

### <a id="m-mincapturetolerance"></a>MinCaptureTolerance

```csharp
public int MinCaptureTolerance { get; init; }
```

Loosens every capture check to at least this per-channel tolerance. References are drawn by one GPU driver; another driver may place anti-aliased edge pixels differently.

**Type:** `int`

### <a id="m-updatereferences"></a>UpdateReferences

```csharp
public bool UpdateReferences { get; init; }
```

Write reference images that are missing or differ instead of failing. Also set by the `ONEGAME_UPDATE_REFERENCES=1` environment variable.

**Type:** `bool`
