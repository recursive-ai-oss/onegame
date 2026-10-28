[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ClusterHashReport record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ClusterHashReport`](ClusterHashReport.md)&gt;

```csharp
public sealed record ClusterHashReport : IEquatable<ClusterHashReport>
```

What `cluster hashes` reports: every node's hash, every replica checked against its server at the same tick, and all drift.

## Constructors

| Member | Description |
| --- | --- |
| [`ClusterHashReport(bool, NodeHash[], HashCheck[], DriftEntry[], string[])`](#m-ctor-bool-nodehash-hashcheck-driftentry-string) | What `cluster hashes` reports: every node's hash, every replica checked against its server at the same tick, and all drift. |

### <a id="m-ctor-bool-nodehash-hashcheck-driftentry-string"></a>ClusterHashReport(bool, NodeHash\[\], HashCheck\[\], DriftEntry\[\], string\[\])

```csharp
public ClusterHashReport(bool Ok, NodeHash[] Nodes, HashCheck[] Checks, DriftEntry[] Drift, string[] Problems);
```

What `cluster hashes` reports: every node's hash, every replica checked against its server at the same tick, and all drift.

**Parameters**

- `Ok` (`bool`): True when every check matched and no node reported drift.
- `Nodes` ([`NodeHash`](NodeHash.md)\[\]): Every node's current scopes.
- `Checks` ([`HashCheck`](HashCheck.md)\[\]): One per client.
- `Drift` ([`DriftEntry`](DriftEntry.md)\[\]): Drift reports from the clients.
- `Problems` (`string[]`): One sentence per failure, naming node, scope and tick.

## Properties

| Member | Description |
| --- | --- |
| [`Checks`](#m-checks) | One per client. |
| [`Drift`](#m-drift) | Drift reports from the clients. |
| [`Nodes`](#m-nodes) | Every node's current scopes. |
| [`Ok`](#m-ok) | True when every check matched and no node reported drift. |
| [`Problems`](#m-problems) | One sentence per failure, naming node, scope and tick. |

### <a id="m-checks"></a>Checks

```csharp
public HashCheck[] Checks { get; init; }
```

One per client.

**Type:** [`HashCheck`](HashCheck.md)\[\]

### <a id="m-drift"></a>Drift

```csharp
public DriftEntry[] Drift { get; init; }
```

Drift reports from the clients.

**Type:** [`DriftEntry`](DriftEntry.md)\[\]

### <a id="m-nodes"></a>Nodes

```csharp
public NodeHash[] Nodes { get; init; }
```

Every node's current scopes.

**Type:** [`NodeHash`](NodeHash.md)\[\]

### <a id="m-ok"></a>Ok

```csharp
public bool Ok { get; init; }
```

True when every check matched and no node reported drift.

**Type:** `bool`

### <a id="m-problems"></a>Problems

```csharp
public string[] Problems { get; init; }
```

One sentence per failure, naming node, scope and tick.

**Type:** `string[]`
