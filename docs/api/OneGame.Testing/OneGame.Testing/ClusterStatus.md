[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ClusterStatus record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ClusterStatus`](ClusterStatus.md)&gt;

```csharp
public sealed record ClusterStatus : IEquatable<ClusterStatus>
```

The state of a cluster, node by node.

## Constructors

| Member | Description |
| --- | --- |
| [`ClusterStatus(string, NodeStatusLine[])`](#m-ctor-string-nodestatusline) | The state of a cluster, node by node. |

### <a id="m-ctor-string-nodestatusline"></a>ClusterStatus(string, NodeStatusLine\[\])

```csharp
public ClusterStatus(string Name, NodeStatusLine[] Nodes);
```

The state of a cluster, node by node.

**Parameters**

- `Name` (`string`): The cluster's name.
- `Nodes` ([`NodeStatusLine`](NodeStatusLine.md)\[\]): One line per node.

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | The cluster's name. |
| [`Nodes`](#m-nodes) | One line per node. |

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The cluster's name.

**Type:** `string`

### <a id="m-nodes"></a>Nodes

```csharp
public NodeStatusLine[] Nodes { get; init; }
```

One line per node.

**Type:** [`NodeStatusLine`](NodeStatusLine.md)\[\]
