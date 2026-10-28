[API reference](../index.md) › OneGame.Testing

# OneGame.Testing

The C\# test API: in-process clusters, scenarios, recordings and captures.

**Package:** `RecursiveAI.OneGame.Testing`

```sh
dotnet add package RecursiveAI.OneGame.Testing
```

## OneGame.Testing

| Type | Kind | Description |
| --- | --- | --- |
| [`ClusterException`](OneGame.Testing/ClusterException.md) | class | A process cluster could not be started, reached or stopped. |
| [`ClusterHashReport`](OneGame.Testing/ClusterHashReport.md) | record | What `cluster hashes` reports: every node's hash, every replica checked against its server at the same tick, and all drift. |
| [`ClusterJson`](OneGame.Testing/ClusterJson.md) | class | Source-generated JSON for the cluster's files and reports. |
| [`ClusterSpec`](OneGame.Testing/ClusterSpec.md) | record | The nodes of a process cluster and how long to wait for them. |
| [`ClusterState`](OneGame.Testing/ClusterState.md) | record | A running cluster as `cluster.json` records it, enough for another process to find every node. |
| [`ClusterStatus`](OneGame.Testing/ClusterStatus.md) | record | The state of a cluster, node by node. |
| [`Determinism`](OneGame.Testing/Determinism.md) | static class | Checks that a scripted run is reproducible: twice from scratch, through a snapshot, and through a recording. |
| [`Determinism.Drive`](OneGame.Testing/Determinism.Drive.md) | delegate | Does something to the game before frame number `frame` runs: send a command, click, press a key. |
| [`DeterminismException`](OneGame.Testing/DeterminismException.md) | class | A determinism check found two runs that should be identical and are not. |
| [`DeterminismReport`](OneGame.Testing/DeterminismReport.md) | record | The outcome of a determinism check. |
| [`DownReport`](OneGame.Testing/DownReport.md) | record | What `cluster down` did. |
| [`DriftEntry`](OneGame.Testing/DriftEntry.md) | record | A drift report filed by a client's replica. |
| [`EndpointState`](OneGame.Testing/EndpointState.md) | record | A listening address of a server node. |
| [`ExpectKind`](OneGame.Testing/ExpectKind.md) | enum | What an expectation looks at. |
| [`ExpectationResult`](OneGame.Testing/ExpectationResult.md) | record | Outcome of one expectation. |
| [`HashCheck`](OneGame.Testing/HashCheck.md) | record | A client replica compared with its server's recorded hash at the replica's tick. |
| [`ImageCompare`](OneGame.Testing/ImageCompare.md) | static class | Per-pixel image comparison for capture checks. |
| [`ImageDiff`](OneGame.Testing/ImageDiff.md) | struct | How two images differ. |
| [`LiveClient`](OneGame.Testing/LiveClient.md) | class | A client of a running process's live control socket: one connection that sends operations as MCP tool calls and returns their JSON results. |
| [`NodeHash`](OneGame.Testing/NodeHash.md) | record | One scope's tick and hash on one node. |
| [`NodeRole`](OneGame.Testing/NodeRole.md) | enum | What a node of a process cluster is. |
| [`NodeSpec`](OneGame.Testing/NodeSpec.md) | record | One process of a cluster. |
| [`NodeState`](OneGame.Testing/NodeState.md) | record | One node as recorded in `cluster.json`. |
| [`NodeStatusLine`](OneGame.Testing/NodeStatusLine.md) | record | What `cluster status` reports for one node. |
| [`ProcessCluster`](OneGame.Testing/ProcessCluster.md) | class | A cluster of real processes on this machine: each node of a [`ClusterSpec`](OneGame.Testing/ClusterSpec.md) is its own OS process (a dedicated server or a headless game connected to one) talking over the real transport, each with its live-control socket. |
| [`Scenario`](OneGame.Testing/Scenario.md) | record | A scenario: where to start, what to do, and what must hold afterwards. |
| [`ScenarioException`](OneGame.Testing/ScenarioException.md) | class | A scenario file is wrong. |
| [`ScenarioExpectation`](OneGame.Testing/ScenarioExpectation.md) | record | A check on the game's state. |
| [`ScenarioJson`](OneGame.Testing/ScenarioJson.md) | class | Source-generated JSON for scenario results. |
| [`ScenarioParser`](OneGame.Testing/ScenarioParser.md) | static class | Reads scenario files. |
| [`ScenarioResult`](OneGame.Testing/ScenarioResult.md) | record | Outcome of a scenario. |
| [`ScenarioRunOptions`](OneGame.Testing/ScenarioRunOptions.md) | record | Settings for running scenarios. |
| [`ScenarioRunner`](OneGame.Testing/ScenarioRunner.md) | class | Runs scenarios against a game through the control layer, the same operations an agent calls over MCP. |
| [`ScenarioStep`](OneGame.Testing/ScenarioStep.md) | record | One step of a scenario. |
| [`ScopeSpec`](OneGame.Testing/ScopeSpec.md) | record | A scope a server node hosts. |
| [`ServerTestClient`](OneGame.Testing/ServerTestClient.md) | class | A client of a [`TestServer`](OneGame.Testing/TestServer.md): a node holding a replica of one scope. |
| [`StepKind`](OneGame.Testing/StepKind.md) | enum | What a scenario step does. |
| [`StepResult`](OneGame.Testing/StepResult.md) | record | Outcome of one step. |
| [`TestClient`](OneGame.Testing/TestClient.md) | class | A headless game under test, driven through the control layer like a live one. |
| [`TestCluster`](OneGame.Testing/TestCluster.md) | class | An in-process cluster of games for tests. |
| [`TestOptions`](OneGame.Testing/TestOptions.md) | record | Options for [`TestCluster`](OneGame.Testing/TestCluster.md). |
| [`TestServer`](OneGame.Testing/TestServer.md) | class | A server under test, with in-process clients on the loopback transport. |
| [`TestingModule`](OneGame.Testing/TestingModule.md) | static class | Identifies the Testing assembly; a placeholder until its first feature lands. |
