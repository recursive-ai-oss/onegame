[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Cluster class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class Cluster
```

A set of nodes in one process, joined by loopback links and stepped together. A single-player game is a cluster with one authority node and one local replica; tests build bigger ones. The cluster only decides the order things happen in: each tick it delivers input, steps every authority scope, publishes the frames and delivers them.

## Constructors

| Member | Description |
| --- | --- |
| [`Cluster(ClusterOptions?)`](#m-ctor-clusteroptions) | Creates a cluster. |

### <a id="m-ctor-clusteroptions"></a>Cluster(ClusterOptions?)

```csharp
public Cluster(ClusterOptions? options = null);
```

Creates a cluster.

**Parameters**

- `options` ([`ClusterOptions`](ClusterOptions.md)?): Options shared by all nodes; defaults if null.

## Properties

| Member | Description |
| --- | --- |
| [`DriftReports`](#m-driftreports) | Every drift report filed by the nodes' replicas. |
| [`Driver`](#m-driver) | The fixed-step driver [`Cluster.Frame()`](Cluster.md#m-frame-timespan) uses. |
| [`Nodes`](#m-nodes) | The nodes, in the order they step. |
| [`Options`](#m-options) | The options every node of this cluster follows. |
| [`Refusals`](#m-refusals) | Every refused command and subscription on every node. |
| [`Tick`](#m-tick) | The number of cluster ticks stepped. |

### <a id="m-driftreports"></a>DriftReports

```csharp
public IReadOnlyList<DriftReport> DriftReports { get; }
```

Every drift report filed by the nodes' replicas.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`DriftReport`](DriftReport.md)&gt;

### <a id="m-driver"></a>Driver

```csharp
public FixedStepDriver Driver { get; set; }
```

The fixed-step driver [`Cluster.Frame()`](Cluster.md#m-frame-timespan) uses. Replace its settings (pause, time scale) here.

**Type:** [`FixedStepDriver`](FixedStepDriver.md)

### <a id="m-nodes"></a>Nodes

```csharp
public IReadOnlyList<Node> Nodes { get; }
```

The nodes, in the order they step.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Node`](Node.md)&gt;

### <a id="m-options"></a>Options

```csharp
public ClusterOptions Options { get; }
```

The options every node of this cluster follows.

**Type:** [`ClusterOptions`](ClusterOptions.md)

### <a id="m-refusals"></a>Refusals

```csharp
public IReadOnlyList<CommandRefusal> Refusals { get; }
```

Every refused command and subscription on every node.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`CommandRefusal`](CommandRefusal.md)&gt;

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; }
```

The number of cluster ticks stepped.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`AddLocalReplica(AuthorityScope, World, Trust?, string)`](#m-addlocalreplica-authorityscope-world-trust-string) | Adds a local player: a new node connected to the scope's node, subscribed to the scope, with a replica world to read. |
| [`AddNode(string)`](#m-addnode-string) | Adds a node. |
| [`AddScope(Node, ScopeId, World)`](#m-addscope-node-scopeid-world) | Makes `authority` the authority of a new scope. |
| [`CheckReplicas()`](#m-checkreplicas) | Compares every replica to its authority right now, and describes any difference. |
| [`Connect(Node, Node, Trust)`](#m-connect-node-node-trust) | Connects two nodes. |
| [`Frame(TimeSpan)`](#m-frame-timespan) | Feeds one rendered frame's elapsed time to the [`Cluster.Driver`](Cluster.md#m-driver) and runs the ticks it says are due. |
| [`Hashes()`](#m-hashes) | The tick and hash of every scope on every node. |
| [`RemoveNode(Node)`](#m-removenode-node) | Takes a node out of the cluster as if its process died: it is no longer stepped or pumped, and whatever its channels carried goes unanswered. |
| [`Settle()`](#m-settle) | Delivers pending messages without stepping, so new subscriptions get their snapshot. |
| [`Step(int)`](#m-step-int) | Runs `ticks` ticks. |

### <a id="m-addlocalreplica-authorityscope-world-trust-string"></a>AddLocalReplica(AuthorityScope, World, Trust?, string)

```csharp
public ReplicaScope AddLocalReplica(AuthorityScope scope, World replicaWorld, Trust? trust = default, string name = "client");
```

Adds a local player: a new node connected to the scope's node, subscribed to the scope, with a replica world to read. The replica fills in on the next [`Cluster.Step()`](Cluster.md#m-step-int) (or call [`Cluster.Settle()`](Cluster.md#m-settle)).

**Parameters**

- `scope` ([`AuthorityScope`](AuthorityScope.md)): The authority scope.
- `replicaWorld` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): An empty replica world with the same registry and tables as the authority's.
- `trust` ([`Trust`](Trust.md)?): What the local player may do; [`Trust.Full`](Trust.md#m-full) if null.
- `name` (`string`): The new node's name.

**Returns:** [`ReplicaScope`](ReplicaScope.md)

### <a id="m-addnode-string"></a>AddNode(string)

```csharp
public Node AddNode(string name);
```

Adds a node.

**Parameters**

- `name` (`string`)

**Returns:** [`Node`](Node.md)

### <a id="m-addscope-node-scopeid-world"></a>AddScope(Node, ScopeId, World)

```csharp
public AuthorityScope AddScope(Node authority, ScopeId id, World world);
```

Makes `authority` the authority of a new scope.

**Parameters**

- `authority` ([`Node`](Node.md))
- `id` ([`ScopeId`](ScopeId.md))
- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md))

**Returns:** [`AuthorityScope`](AuthorityScope.md)

### <a id="m-checkreplicas"></a>CheckReplicas()

```csharp
public IReadOnlyList<DriftReport> CheckReplicas();
```

Compares every replica to its authority right now, and describes any difference. Empty means in sync.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`DriftReport`](DriftReport.md)&gt;

### <a id="m-connect-node-node-trust"></a>Connect(Node, Node, Trust)

```csharp
public Link Connect(Node client, Node server, Trust trust);
```

Connects two nodes.

**Parameters**

- `client` ([`Node`](Node.md)): The node that will subscribe and send commands.
- `server` ([`Node`](Node.md)): The node holding the authority.
- `trust` ([`Trust`](Trust.md)): What the client is trusted to do.

**Returns:** [`Link`](Link.md)

### <a id="m-frame-timespan"></a>Frame(TimeSpan)

```csharp
public int Frame(TimeSpan frameTime);
```

Feeds one rendered frame's elapsed time to the [`Cluster.Driver`](Cluster.md#m-driver) and runs the ticks it says are due. Returns how many ran.

**Parameters**

- `frameTime` ([`TimeSpan`](https://learn.microsoft.com/dotnet/api/system.timespan))

**Returns:** `int`

### <a id="m-hashes"></a>Hashes()

```csharp
public IReadOnlyList<ScopeHash> Hashes();
```

The tick and hash of every scope on every node.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ScopeHash`](ScopeHash.md)&gt;

### <a id="m-removenode-node"></a>RemoveNode(Node)

```csharp
public bool RemoveNode(Node node);
```

Takes a node out of the cluster as if its process died: it is no longer stepped or pumped, and whatever its channels carried goes unanswered. A test restarts it by building a new node from a save.

**Parameters**

- `node` ([`Node`](Node.md))

**Returns:** `bool` — False if the node was not part of this cluster.

### <a id="m-settle"></a>Settle()

```csharp
public void Settle();
```

Delivers pending messages without stepping, so new subscriptions get their snapshot.

### <a id="m-step-int"></a>Step(int)

```csharp
public void Step(int ticks = 1);
```

Runs `ticks` ticks.

**Parameters**

- `ticks` (`int`)

**Exceptions**

- [`DriftException`](DriftException.md): A replica drifted and [`ClusterOptions.FatalDrift`](ClusterOptions.md#m-fataldrift) is on.
