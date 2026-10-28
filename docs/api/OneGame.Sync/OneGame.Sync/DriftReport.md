[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# DriftReport class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class DriftReport
```

A replica whose state differs from its authority's at the same tick.

## Properties

| Member | Description |
| --- | --- |
| [`AuthorityHash`](#m-authorityhash) | Hash on the authority. |
| [`DetailAvailable`](#m-detailavailable) | False when the authority could no longer supply the per-entity digest for that tick. |
| [`Differences`](#m-differences) | The entities, tables and streams that differ. |
| [`ReplicaHash`](#m-replicahash) | Hash on the replica. |
| [`ReplicaNode`](#m-replicanode) | Name of the node holding the replica. |
| [`Scope`](#m-scope) | The scope. |
| [`Tick`](#m-tick) | The completed tick count at which the hashes differed. |
| [`TimedOut`](#m-timedout) | True when the hashes differed and the authority never answered the request for the per-entity digest in time ([`ClusterOptions.DigestTimeoutTicks`](ClusterOptions.md#m-digesttimeoutticks)). |
| [`Upstream`](#m-upstream) | Name of the node the replica follows (the authority or a relay), so a mismatch names the hop `Upstream -> ReplicaNode`. |

### <a id="m-authorityhash"></a>AuthorityHash

```csharp
public ulong AuthorityHash { get; }
```

Hash on the authority.

**Type:** `ulong`

### <a id="m-detailavailable"></a>DetailAvailable

```csharp
public bool DetailAvailable { get; }
```

False when the authority could no longer supply the per-entity digest for that tick.

**Type:** `bool`

### <a id="m-differences"></a>Differences

```csharp
public IReadOnlyList<StateDifference> Differences { get; }
```

The entities, tables and streams that differ. Empty when [`DriftReport.DetailAvailable`](DriftReport.md#m-detailavailable) is false.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`StateDifference`](StateDifference.md)&gt;

### <a id="m-replicahash"></a>ReplicaHash

```csharp
public ulong ReplicaHash { get; }
```

Hash on the replica.

**Type:** `ulong`

### <a id="m-replicanode"></a>ReplicaNode

```csharp
public string ReplicaNode { get; }
```

Name of the node holding the replica.

**Type:** `string`

### <a id="m-scope"></a>Scope

```csharp
public ScopeId Scope { get; }
```

The scope.

**Type:** [`ScopeId`](ScopeId.md)

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; }
```

The completed tick count at which the hashes differed.

**Type:** `long`

### <a id="m-timedout"></a>TimedOut

```csharp
public bool TimedOut { get; }
```

True when the hashes differed and the authority never answered the request for the per-entity digest in time ([`ClusterOptions.DigestTimeoutTicks`](ClusterOptions.md#m-digesttimeoutticks)).

**Type:** `bool`

### <a id="m-upstream"></a>Upstream

```csharp
public string? Upstream { get; }
```

Name of the node the replica follows (the authority or a relay), so a mismatch names the hop `Upstream -> ReplicaNode`. Null when the link does not know its peer.

**Type:** `string?`

## Methods

| Member | Description |
| --- | --- |
| [`ToJson()`](#m-tojson) | The report as JSON. |
| [`ToString()`](#m-tostring) | One-line summary. |

### <a id="m-tojson"></a>ToJson()

```csharp
public string ToJson();
```

The report as JSON.

**Returns:** `string`

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

One-line summary.

**Returns:** `string`
