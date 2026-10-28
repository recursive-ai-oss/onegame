[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ClusterState record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ClusterState`](ClusterState.md)&gt;

```csharp
public sealed record ClusterState : IEquatable<ClusterState>
```

A running cluster as `cluster.json` records it, enough for another process to find every node.

## Constructors

| Member | Description |
| --- | --- |
| [`ClusterState(string, string, int, NodeState[])`](#m-ctor-string-string-int-nodestate) | A running cluster as `cluster.json` records it, enough for another process to find every node. |

### <a id="m-ctor-string-string-int-nodestate"></a>ClusterState(string, string, int, NodeState\[\])

```csharp
public ClusterState(string Name, string Dir, int Owner, NodeState[] Nodes);
```

A running cluster as `cluster.json` records it, enough for another process to find every node.

**Parameters**

- `Name` (`string`): The cluster's name.
- `Dir` (`string`): The folder with logs, sockets and this file.
- `Owner` (`int`): The process that started it.
- `Nodes` ([`NodeState`](NodeState.md)\[\]): The nodes in start order.

## Properties

| Member | Description |
| --- | --- |
| [`Dir`](#m-dir) | The folder with logs, sockets and this file. |
| [`Name`](#m-name) | The cluster's name. |
| [`Nodes`](#m-nodes) | The nodes in start order. |
| [`Owner`](#m-owner) | The process that started it. |

### <a id="m-dir"></a>Dir

```csharp
public string Dir { get; init; }
```

The folder with logs, sockets and this file.

**Type:** `string`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The cluster's name.

**Type:** `string`

### <a id="m-nodes"></a>Nodes

```csharp
public NodeState[] Nodes { get; init; }
```

The nodes in start order.

**Type:** [`NodeState`](NodeState.md)\[\]

### <a id="m-owner"></a>Owner

```csharp
public int Owner { get; init; }
```

The process that started it.

**Type:** `int`
