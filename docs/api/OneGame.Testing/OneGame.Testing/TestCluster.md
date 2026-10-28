[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# TestCluster class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class TestCluster : IDisposable
```

The entry point for game tests: starts your game headless in this process and hands back a [`TestClient`](TestClient.md) to drive it. Dispose it when the test ends. It holds one authority node, so single-player rules, saves and UI are tested exactly as they run; use [`TestCluster.Launch()`](TestCluster.md#m-launch-clusterspec-int) for several real processes over the real transport.

**Example**

A test that clicks through a screen, checks state and runs a scenario file:

```csharp
[Fact]
public void PlayServesLevelOne()
{
    using var cluster = TestCluster.Start(new BricksGame(), new TestOptions { Width = 540, Height = 960 });
    cluster.Client.Click("play").Step(2);
    Assert.Equal("1", cluster.Client.Get("Court", "Level"));

    var result = cluster.Run("scenarios/02-brick-broken.yaml");
    Assert.True(result.Passed, string.Join("; ", result.Expectations.Where(e => !e.Passed).Select(e => e.Expected + " but was " + e.Actual)));
}
```

## Properties

| Member | Description |
| --- | --- |
| [`Client`](#m-client) | The first (authority) node: the one most tests drive. |
| [`Nodes`](#m-nodes) | All nodes. |
| [`OutputDirectory`](#m-outputdirectory) | The folder that captures, snapshots and other files written by operations go to. |

### <a id="m-client"></a>Client

```csharp
public TestClient Client { get; }
```

The first (authority) node: the one most tests drive.

**Type:** [`TestClient`](TestClient.md)

### <a id="m-nodes"></a>Nodes

```csharp
public IReadOnlyList<TestClient> Nodes { get; }
```

All nodes. `Nodes[0]` is the authority; today that is the only one.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`TestClient`](TestClient.md)&gt;

### <a id="m-outputdirectory"></a>OutputDirectory

```csharp
public string OutputDirectory { get; }
```

The folder that captures, snapshots and other files written by operations go to. A temporary folder deleted on dispose unless [`TestOptions.OutputDirectory`](TestOptions.md#m-outputdirectory) named one.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) |  |
| [`Launch(ClusterSpec, int?)`](#m-launch-clusterspec-int) | Starts a cluster of real processes (dedicated servers and headless game clients) on this machine, talking over the real transport. |
| [`Run(Scenario, ScenarioRunOptions?)`](#m-run-scenario-scenariorunoptions) | Runs a scenario against the first node: its steps in order (a step that fails stops it), then its expectations. |
| [`Run(string, ScenarioRunOptions?)`](#m-run-string-scenariorunoptions) | Reads a scenario YAML file and runs it against the first node. |
| [`Start(GameDefinition, TestOptions?)`](#m-start-gamedefinition-testoptions) | Starts a cluster with one headless node running `game`. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

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

Runs a scenario against the first node: its steps in order (a step that fails stops it), then its expectations. Failures are in the result, not thrown: check [`ScenarioResult.Passed`](ScenarioResult.md#m-passed).

**Parameters**

- `scenario` ([`Scenario`](Scenario.md)): A parsed scenario.
- `options` ([`ScenarioRunOptions`](ScenarioRunOptions.md)?): Where captures and references live; a folder under [`TestCluster.OutputDirectory`](TestCluster.md#m-outputdirectory) when null.

**Returns:** [`ScenarioResult`](ScenarioResult.md)

### <a id="m-run-string-scenariorunoptions"></a>Run(string, ScenarioRunOptions?)

```csharp
public ScenarioResult Run(string scenarioPath, ScenarioRunOptions? options = null);
```

Reads a scenario YAML file and runs it against the first node.

**Parameters**

- `scenarioPath` (`string`): Path of the `.yaml` file.
- `options` ([`ScenarioRunOptions`](ScenarioRunOptions.md)?): As for the other overload.

**Returns:** [`ScenarioResult`](ScenarioResult.md)

**Exceptions**

- [`ScenarioException`](ScenarioException.md): The file does not parse; the message names the line.

### <a id="m-start-gamedefinition-testoptions"></a>Start(GameDefinition, TestOptions?)

```csharp
public static TestCluster Start(GameDefinition game, TestOptions? options = null);
```

Starts a cluster with one headless node running `game`.

**Parameters**

- `game` ([`GameDefinition`](../../OneGame.Host/OneGame.Host/GameDefinition.md)): The game definition, as the game's own `Program` would run it. Give it in-memory services (for example a `MemoryByteStore`) so tests never touch a player's saves.
- `options` ([`TestOptions`](TestOptions.md)?): Window size, UI scale, seed and folders; defaults when null.

**Returns:** [`TestCluster`](TestCluster.md)

**Exceptions**

- [`ArgumentNullException`](https://learn.microsoft.com/dotnet/api/system.argumentnullexception): `game` is null.
