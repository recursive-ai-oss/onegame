[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ProcessCluster class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class ProcessCluster : IDisposable
```

A cluster of real processes on this machine: each node of a [`ClusterSpec`](ClusterSpec.md) is its own OS process (a dedicated server or a headless game connected to one) talking over the real transport, each with its live-control socket. The cluster starts them, calls operations on any node, compares every client's hash with its server's at the same tick, and stops them all.

Every node is told to watch the process that started it (`--parent`), so a crashed test leaves nothing running. [`ProcessCluster.Down()`](ProcessCluster.md#m-down) stops the nodes by pid and checks that none is left.

**Example**

Start a spec file, run a scenario whose steps are aimed at nodes, check that every client's replica agrees with its server, and stop everything:

```csharp
using var cluster = TestCluster.Launch(ClusterSpec.Load("cluster.yaml"));
var pids = cluster.Pids.ToArray();
var result = cluster.Run("cluster/play.yaml");
Assert.True(result.Passed);

var report = cluster.Hashes();
Assert.True(report.Ok, string.Join("; ", report.Problems));

cluster.Kill("islandA");     // crash a server...
cluster.Start("islandA");    // ...and bring it back
cluster.Down();
Assert.All(pids, pid => Assert.False(ProcessCluster.IsAlive(pid)));
```

## Properties

| Member | Description |
| --- | --- |
| [`Dir`](#m-dir) | The folder with logs, sockets and `cluster.json`. |
| [`Pids`](#m-pids) | The process ids of every node. |
| [`State`](#m-state) | The nodes as started: name, role, process id, live-control socket, log file and the addresses servers listen on. |
| [`StateFile`](#m-statefile) | The path of `cluster.json`. |

### <a id="m-dir"></a>Dir

```csharp
public string Dir { get; }
```

The folder with logs, sockets and `cluster.json`. A folder the cluster made itself (no `Dir` in the spec) is deleted by [`ProcessCluster.Down()`](ProcessCluster.md#m-down).

**Type:** `string`

### <a id="m-pids"></a>Pids

```csharp
public IReadOnlyList<int> Pids { get; }
```

The process ids of every node.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<int>`

### <a id="m-state"></a>State

```csharp
public ClusterState State { get; }
```

The nodes as started: name, role, process id, live-control socket, log file and the addresses servers listen on. The same data is in `cluster.json` in [`ProcessCluster.Dir`](ProcessCluster.md#m-dir).

**Type:** [`ClusterState`](ClusterState.md)

### <a id="m-statefile"></a>StateFile

```csharp
public string StateFile { get; }
```

The path of `cluster.json`.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`Attach(string)`](#m-attach-string) | Finds the nodes of a cluster that another process started, from its `cluster.json`. |
| [`Call(string, string, string?)`](#m-call-string-string-string) | Runs a control operation on one node (any operation [`ProcessCluster.Operations()`](ProcessCluster.md#m-operations-string) lists) and returns its JSON. |
| [`Dispose()`](#m-dispose) | Stops the cluster when this object started it (see [`ProcessCluster.Down()`](ProcessCluster.md#m-down)); one found with [`ProcessCluster.Attach()`](ProcessCluster.md#m-attach-string) is left running. |
| [`Down()`](#m-down) | Stops every node (asking first, then killing what does not stop), checks that none is left, and removes the sockets. |
| [`Hashes()`](#m-hashes) | Every node's current tick and hash, every client's replica compared with its server's recorded hash at the replica's tick, and all drift reports. |
| [`IsAlive(int)`](#m-isalive-int) | Whether a process with this id exists and has not exited. |
| [`Kill(string)`](#m-kill-string) | Kills a node's process outright (SIGKILL: nothing is saved or announced on the way out), like a crash or a power cut. |
| [`LogTail(string, int)`](#m-logtail-string-int) | The last lines of a node's console output, for a failure message. |
| [`Node(string)`](#m-node-string) | The node's recorded state: process id, socket, log file, listening addresses. |
| [`Operations(string)`](#m-operations-string) | The names of the operations a node offers. |
| [`Restart(string)`](#m-restart-string) | Kills a node's process and starts it again (see [`ProcessCluster.Kill()`](ProcessCluster.md#m-kill-string) and [`ProcessCluster.Start()`](ProcessCluster.md#m-start-string)). |
| [`Run(Scenario)`](#m-run-scenario) | Runs a scenario against the cluster: steps and expectations aimed at nodes, in order. |
| [`Run(string)`](#m-run-string) | Reads a scenario file and runs it against the cluster. |
| [`Start(string)`](#m-start-string) | Starts a node that was killed (or never ran) again with the same arguments, data folder and ports, and waits until it is up: a server listening, a client connected and subscribed. |
| [`Status()`](#m-status) | Each node's pid, whether it is alive, and its own status. |
| [`TickOf(string)`](#m-tickof-string) | The tick of the node's scope (a server's first scope, or a client's replica). |
| [`Up(ClusterSpec, int?)`](#m-up-clusterspec-int) | Starts every node of the spec and waits until each is up (servers listening, clients connected and subscribed). |
| [`WaitForTick(string, long, int)`](#m-waitfortick-string-long-int) | Waits until a node's tick reaches `tick`. |

### <a id="m-attach-string"></a>Attach(string)

```csharp
public static ProcessCluster Attach(string stateFile);
```

Finds the nodes of a cluster that another process started, from its `cluster.json`.

**Parameters**

- `stateFile` (`string`): The file, or the folder that holds it.

**Returns:** [`ProcessCluster`](ProcessCluster.md)

**Exceptions**

- [`ClusterException`](ClusterException.md): There is no such cluster.

### <a id="m-call-string-string-string"></a>Call(string, string, string?)

```csharp
public string Call(string node, string op, string? argsJson = null);
```

Runs a control operation on one node (any operation [`ProcessCluster.Operations()`](ProcessCluster.md#m-operations-string) lists) and returns its JSON.

**Parameters**

- `node` (`string`): The node's name.
- `op` (`string`): The operation (see the control layer's table, or the server's).
- `argsJson` (`string?`): A JSON object, or null.

**Returns:** `string`

**Exceptions**

- [`ClusterException`](ClusterException.md): There is no node of that name, or its process cannot be reached.
- [`ClusterException`](ClusterException.md): No such node.
- [`ControlException`](../../OneGame.Control/OneGame.Control/ControlException.md): The operation failed on the node.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Stops the cluster when this object started it (see [`ProcessCluster.Down()`](ProcessCluster.md#m-down)); one found with [`ProcessCluster.Attach()`](ProcessCluster.md#m-attach-string) is left running.

### <a id="m-down"></a>Down()

```csharp
public DownReport Down();
```

Stops every node (asking first, then killing what does not stop), checks that none is left, and removes the sockets.

**Returns:** [`DownReport`](DownReport.md)

**Exceptions**

- [`ClusterException`](ClusterException.md): A node is still alive after being killed.

### <a id="m-hashes"></a>Hashes()

```csharp
public ClusterHashReport Hashes();
```

Every node's current tick and hash, every client's replica compared with its server's recorded hash at the replica's tick, and all drift reports.

**Returns:** [`ClusterHashReport`](ClusterHashReport.md)

### <a id="m-isalive-int"></a>IsAlive(int)

```csharp
public static bool IsAlive(int pid);
```

Whether a process with this id exists and has not exited.

**Parameters**

- `pid` (`int`)

**Returns:** `bool`

### <a id="m-kill-string"></a>Kill(string)

```csharp
public void Kill(string node);
```

Kills a node's process outright (SIGKILL: nothing is saved or announced on the way out), like a crash or a power cut. The node stays in the cluster, not running, until [`ProcessCluster.Start()`](ProcessCluster.md#m-start-string).

**Parameters**

- `node` (`string`): The node's name.

**Exceptions**

- [`ClusterException`](ClusterException.md): No such node.

### <a id="m-logtail-string-int"></a>LogTail(string, int)

```csharp
public string LogTail(string node, int lines = 40);
```

The last lines of a node's console output, for a failure message. Read it when a node did not come up or went away.

**Parameters**

- `node` (`string`): The node's name.
- `lines` (`int`): How many lines from the end; 40 by default.

**Returns:** `string`

### <a id="m-node-string"></a>Node(string)

```csharp
public NodeState Node(string name);
```

The node's recorded state: process id, socket, log file, listening addresses.

**Parameters**

- `name` (`string`)

**Returns:** [`NodeState`](NodeState.md)

**Exceptions**

- [`ClusterException`](ClusterException.md): There is no node of that name (code `no_node`).
- [`ClusterException`](ClusterException.md): No such node.

### <a id="m-operations-string"></a>Operations(string)

```csharp
public IReadOnlyList<string> Operations(string node);
```

The names of the operations a node offers.

**Parameters**

- `node` (`string`)

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-restart-string"></a>Restart(string)

```csharp
public void Restart(string node);
```

Kills a node's process and starts it again (see [`ProcessCluster.Kill()`](ProcessCluster.md#m-kill-string) and [`ProcessCluster.Start()`](ProcessCluster.md#m-start-string)).

**Parameters**

- `node` (`string`): The node's name.

### <a id="m-run-scenario"></a>Run(Scenario)

```csharp
public ScenarioResult Run(Scenario scenario);
```

Runs a scenario against the cluster: steps and expectations aimed at nodes, in order.

**Parameters**

- `scenario` ([`Scenario`](Scenario.md))

**Returns:** [`ScenarioResult`](ScenarioResult.md)

### <a id="m-run-string"></a>Run(string)

```csharp
public ScenarioResult Run(string scenarioPath);
```

Reads a scenario file and runs it against the cluster. Steps and expectations name the node they aim at (`node: bot1`).

**Parameters**

- `scenarioPath` (`string`)

**Returns:** [`ScenarioResult`](ScenarioResult.md)

**Exceptions**

- [`ScenarioException`](ScenarioException.md): The file does not parse.

### <a id="m-start-string"></a>Start(string)

```csharp
public void Start(string node);
```

Starts a node that was killed (or never ran) again with the same arguments, data folder and ports, and waits until it is up: a server listening, a client connected and subscribed. Servers load what they saved; a client is a new connection and, to the game, a new player.

**Parameters**

- `node` (`string`): The node's name.

**Exceptions**

- [`ClusterException`](ClusterException.md): The node is still running, did not start, or this object did not start the cluster.

### <a id="m-status"></a>Status()

```csharp
public ClusterStatus Status();
```

Each node's pid, whether it is alive, and its own status.

**Returns:** [`ClusterStatus`](ClusterStatus.md)

### <a id="m-tickof-string"></a>TickOf(string)

```csharp
public long TickOf(string node);
```

The tick of the node's scope (a server's first scope, or a client's replica).

**Parameters**

- `node` (`string`)

**Returns:** `long`

### <a id="m-up-clusterspec-int"></a>Up(ClusterSpec, int?)

```csharp
public static ProcessCluster Up(ClusterSpec spec, int? parentPid = default);
```

Starts every node of the spec and waits until each is up (servers listening, clients connected and subscribed).

**Parameters**

- `spec` ([`ClusterSpec`](ClusterSpec.md)): The cluster.
- `parentPid` (`int?`): The process the nodes watch; this process when null; 0 for none (nodes that outlive the caller, stopped with [`ProcessCluster.Down()`](ProcessCluster.md#m-down)). A node exits within about a second of the watched process ending.

**Returns:** [`ProcessCluster`](ProcessCluster.md)

**Exceptions**

- [`ClusterException`](ClusterException.md): A node did not start; everything started so far is stopped again.

### <a id="m-waitfortick-string-long-int"></a>WaitForTick(string, long, int)

```csharp
public bool WaitForTick(string node, long tick, int timeoutMs = 30000);
```

Waits until a node's tick reaches `tick`. A client pumps its connection while it waits; a server must be ticking on its own clock.

**Parameters**

- `node` (`string`)
- `tick` (`long`)
- `timeoutMs` (`int`)

**Returns:** `bool` — True when it got there before the timeout.
