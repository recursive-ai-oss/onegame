[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# TestCluster class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class TestCluster : IDisposable
```

An in-process cluster of games for tests. Today it is one authority node; when the sync stack gains more nodes they appear in [`TestCluster.Nodes`](TestCluster.md#m-nodes) and the same client calls drive each.

## Properties

| Member | Description |
| --- | --- |
| [`Client`](#m-client) | The first node. |
| [`Nodes`](#m-nodes) | The nodes. |
| [`OutputDirectory`](#m-outputdirectory) | The folder operations write to. |

### <a id="m-client"></a>Client

```csharp
public TestClient Client { get; }
```

The first node.

**Type:** [`TestClient`](TestClient.md)

### <a id="m-nodes"></a>Nodes

```csharp
public IReadOnlyList<TestClient> Nodes { get; }
```

The nodes. `Nodes[0]` is the authority.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`TestClient`](TestClient.md)&gt;

### <a id="m-outputdirectory"></a>OutputDirectory

```csharp
public string OutputDirectory { get; }
```

The folder operations write to.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) |  |
| [`Launch(ClusterSpec, int?)`](#m-launch-clusterspec-int) | Starts a cluster of real processes (dedicated servers and headless game clients) on this machine, talking over the real transport. |
| [`Run(Scenario, ScenarioRunOptions?)`](#m-run-scenario-scenariorunoptions) | Runs a scenario against the first node. |
| [`Run(string, ScenarioRunOptions?)`](#m-run-string-scenariorunoptions) | Reads and runs a scenario file against the first node. |
| [`Start(GameDefinition, TestOptions?)`](#m-start-gamedefinition-testoptions) | Starts a cluster with one headless node. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

### <a id="m-launch-clusterspec-int"></a>Launch(ClusterSpec, int?)

```csharp
public static ProcessCluster Launch(ClusterSpec spec, int? parentPid = default);
```

Starts a cluster of real processes (dedicated servers and headless game clients) on this machine, talking over the real transport. Dispose it to stop them; see [`ProcessCluster`](ProcessCluster.md).

**Parameters**

- `spec` ([`ClusterSpec`](ClusterSpec.md)): The nodes.
- `parentPid` (`int?`): The process the nodes watch, so they stop if it dies; this process when null.

**Returns:** [`ProcessCluster`](ProcessCluster.md)

### <a id="m-run-scenario-scenariorunoptions"></a>Run(Scenario, ScenarioRunOptions?)

```csharp
public ScenarioResult Run(Scenario scenario, ScenarioRunOptions? options = null);
```

Runs a scenario against the first node.

**Parameters**

- `scenario` ([`Scenario`](Scenario.md))
- `options` ([`ScenarioRunOptions`](ScenarioRunOptions.md)?)

**Returns:** [`ScenarioResult`](ScenarioResult.md)

### <a id="m-run-string-scenariorunoptions"></a>Run(string, ScenarioRunOptions?)

```csharp
public ScenarioResult Run(string scenarioPath, ScenarioRunOptions? options = null);
```

Reads and runs a scenario file against the first node.

**Parameters**

- `scenarioPath` (`string`)
- `options` ([`ScenarioRunOptions`](ScenarioRunOptions.md)?)

**Returns:** [`ScenarioResult`](ScenarioResult.md)

### <a id="m-start-gamedefinition-testoptions"></a>Start(GameDefinition, TestOptions?)

```csharp
public static TestCluster Start(GameDefinition game, TestOptions? options = null);
```

Starts a cluster with one headless node.

**Parameters**

- `game` ([`GameDefinition`](../../OneGame.Host/OneGame.Host/GameDefinition.md))
- `options` ([`TestOptions`](TestOptions.md)?)

**Returns:** [`TestCluster`](TestCluster.md)
