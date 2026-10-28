[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# ReplicaScope class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class ReplicaScope
```

This node's copy of a scope it does not own. Its [`ReplicaScope.World`](ReplicaScope.md#m-world) is a replica: it only applies what the authority sends, and its entities reject writes. Views read it; input goes out as commands (`entity.Send.X(...)` on a replica entity).

## Properties

| Member | Description |
| --- | --- |
| [`FramesApplied`](#m-framesapplied) | Frames applied so far. |
| [`Hash`](#m-hash) | The hash of everything the replica world holds. |
| [`Id`](#m-id) | The scope id. |
| [`IsSilent`](#m-issilent) | True when nothing has arrived from the authority for [`ClusterOptions.SilenceTimeoutMs`](ClusterOptions.md#m-silencetimeoutms) while this replica expected frames. |
| [`LastSequence`](#m-lastsequence) | The sequence number of the command most recently forwarded to the authority, or 0 if none yet. |
| [`LastTick`](#m-lasttick) | The tick of the last snapshot or frame applied. |
| [`Link`](#m-link) | The link to the authority's node. |
| [`Node`](#m-node) | The node holding this replica. |
| [`PendingCommands`](#m-pendingcommands) | How many forwarded commands have not been answered yet. |
| [`Prediction`](#m-prediction) | The client-side prediction running on this replica, or null (see [`ReplicaScope.EnablePrediction()`](ReplicaScope.md#m-enableprediction-world-predictionoptions)). |
| [`Results`](#m-results) | The answers received for forwarded commands, oldest first (the latest 256). |
| [`Resyncs`](#m-resyncs) | How many times this replica asked for a fresh snapshot after the first. |
| [`State`](#m-state) | Lifecycle state. |
| [`ViewHash`](#m-viewhash) | The hash of the view this replica holds: the authority computes the same value for the same audience ([`AuthorityScope.ViewHashFor()`](AuthorityScope.md#m-viewhashfor-connection)), so equality at the same tick means nothing it may see has drifted, and nothing it may not see was sent. |
| [`World`](#m-world) | The replica world. |

### <a id="m-framesapplied"></a>FramesApplied

```csharp
public long FramesApplied { get; }
```

Frames applied so far.

**Type:** `long`

### <a id="m-hash"></a>Hash

```csharp
public ulong Hash { get; }
```

The hash of everything the replica world holds. Equals the authority's [`AuthorityScope.Hash`](AuthorityScope.md#m-hash) only when this replica is allowed to see everything.

**Type:** `ulong`

### <a id="m-id"></a>Id

```csharp
public ScopeId Id { get; }
```

The scope id.

**Type:** [`ScopeId`](ScopeId.md)

### <a id="m-issilent"></a>IsSilent

```csharp
public bool IsSilent { get; }
```

True when nothing has arrived from the authority for [`ClusterOptions.SilenceTimeoutMs`](ClusterOptions.md#m-silencetimeoutms) while this replica expected frames. Cleared by the next message from it. The world keeps its last state; commands still waiting have completed as [`CommandVerdict.TimedOut`](CommandVerdict.md#m-timedout).

**Type:** `bool`

### <a id="m-lastsequence"></a>LastSequence

```csharp
public uint LastSequence { get; }
```

The sequence number of the command most recently forwarded to the authority, or 0 if none yet. Read it right after `entity.Send.X(...)` to match the answer in [`ReplicaScope.Results`](ReplicaScope.md#m-results).

**Type:** `uint`

### <a id="m-lasttick"></a>LastTick

```csharp
public long LastTick { get; }
```

The tick of the last snapshot or frame applied.

**Type:** `long`

### <a id="m-link"></a>Link

```csharp
public Link Link { get; }
```

The link to the authority's node.

**Type:** [`Link`](Link.md)

### <a id="m-node"></a>Node

```csharp
public Node Node { get; }
```

The node holding this replica.

**Type:** [`Node`](Node.md)

### <a id="m-pendingcommands"></a>PendingCommands

```csharp
public int PendingCommands { get; }
```

How many forwarded commands have not been answered yet.

**Type:** `int`

### <a id="m-prediction"></a>Prediction

```csharp
public Prediction? Prediction { get; }
```

The client-side prediction running on this replica, or null (see [`ReplicaScope.EnablePrediction()`](ReplicaScope.md#m-enableprediction-world-predictionoptions)).

**Type:** [`Prediction`](Prediction.md)?

### <a id="m-results"></a>Results

```csharp
public IReadOnlyList<CommandResult> Results { get; }
```

The answers received for forwarded commands, oldest first (the latest 256).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`CommandResult`](CommandResult.md)&gt;

### <a id="m-resyncs"></a>Resyncs

```csharp
public int Resyncs { get; }
```

How many times this replica asked for a fresh snapshot after the first.

**Type:** `int`

### <a id="m-state"></a>State

```csharp
public ReplicaState State { get; }
```

Lifecycle state.

**Type:** [`ReplicaState`](ReplicaState.md)

### <a id="m-viewhash"></a>ViewHash

```csharp
public ulong ViewHash { get; }
```

The hash of the view this replica holds: the authority computes the same value for the same audience ([`AuthorityScope.ViewHashFor()`](AuthorityScope.md#m-viewhashfor-connection)), so equality at the same tick means nothing it may see has drifted, and nothing it may not see was sent.

**Type:** `ulong`

### <a id="m-world"></a>World

```csharp
public World World { get; }
```

The replica world. Read it; never write it.

**Type:** [`World`](../../OneGame.Core/OneGame.Core/World.md)

## Events

| Member | Description |
| --- | --- |
| [`AuthorityResumed`](#m-authorityresumed) | Raised when a silent authority is heard from again. |
| [`AuthoritySilent`](#m-authoritysilent) | Raised once when the replica becomes [`ReplicaScope.IsSilent`](ReplicaScope.md#m-issilent). |
| [`CommandCompleted`](#m-commandcompleted) | Raised for every answer to a forwarded command: executed, rejected with the handler's reason, refused, or dropped. |

### <a id="m-authorityresumed"></a>AuthorityResumed

```csharp
public event Action<ReplicaScope>? AuthorityResumed;
```

Raised when a silent authority is heard from again.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`ReplicaScope`](ReplicaScope.md)&gt;?

### <a id="m-authoritysilent"></a>AuthoritySilent

```csharp
public event Action<ReplicaScope>? AuthoritySilent;
```

Raised once when the replica becomes [`ReplicaScope.IsSilent`](ReplicaScope.md#m-issilent).

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`ReplicaScope`](ReplicaScope.md)&gt;?

### <a id="m-commandcompleted"></a>CommandCompleted

```csharp
public event Action<CommandResult>? CommandCompleted;
```

Raised for every answer to a forwarded command: executed, rejected with the handler's reason, refused, or dropped.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`CommandResult`](CommandResult.md)&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`EnablePrediction(World, PredictionOptions?)`](#m-enableprediction-world-predictionoptions) | Turns on client-side prediction: commands marked `[Command(Predicted = true)]` also run at once on `predicted`, which is kept equal to this replica's confirmed state plus the commands the authority has not applied yet. |

### <a id="m-enableprediction-world-predictionoptions"></a>EnablePrediction(World, PredictionOptions?)

```csharp
public Prediction EnablePrediction(World predicted, PredictionOptions? options = null);
```

Turns on client-side prediction: commands marked `[Command(Predicted = true)]` also run at once on `predicted`, which is kept equal to this replica's confirmed state plus the commands the authority has not applied yet. [`ReplicaScope.World`](ReplicaScope.md#m-world) stays the exact authority state; views that want the responsive picture read [`Prediction.World`](Prediction.md#m-world).

**Parameters**

- `predicted` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): A world built like [`ReplicaScope.World`](ReplicaScope.md#m-world) (same registry, tables and systems, in the same order) with role [`WorldRole.Predicted`](../../OneGame.Core/OneGame.Core/WorldRole.md#m-predicted).
- `options` ([`PredictionOptions`](PredictionOptions.md)?): Lead, limits and rollback detection.

**Returns:** [`Prediction`](Prediction.md)

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The world's role is not [`WorldRole.Predicted`](../../OneGame.Core/OneGame.Core/WorldRole.md#m-predicted), or it is this replica's own world.
- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): Prediction is already enabled on this replica.
