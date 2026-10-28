[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# AuthorityScope class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class AuthorityScope
```

A scope whose authority is this node: it owns the simulating [`AuthorityScope.World`](AuthorityScope.md#m-world), accepts commands, and publishes a snapshot then one frame per tick to each subscriber. Subscribers are grouped by [`Audience`](../../OneGame.Core/OneGame.Core/Audience.md); each group gets the view its audience is allowed to see (see [`Visibility`](../../OneGame.Core/OneGame.Core/Visibility.md)), so private data never reaches a connection that may not have it.

## Properties

| Member | Description |
| --- | --- |
| [`ActiveRecorder`](#m-activerecorder) | The recorder, while one is running. |
| [`CommandsForwarded`](#m-commandsforwarded) | Commands passed on to the node an entity moved to, over the scope's life. |
| [`CommandsHeld`](#m-commandsheld) | Commands held while their entity was in transit, over the scope's life. |
| [`CommandsRun`](#m-commandsrun) | Total commands the world has run for this scope, counting every outcome. |
| [`Durability`](#m-durability) | When command answers and transfer confirmations may leave this scope. |
| [`Hash`](#m-hash) | The scope's current hash (everything, including what no subscriber may see). |
| [`Id`](#m-id) | The scope id. |
| [`IsRelay`](#m-isrelay) | True when this scope serves a replica onward instead of simulating (see [`Node.Relay()`](Node.md#m-relay-replicascope)). |
| [`Node`](#m-node) | The node that holds the authority. |
| [`Outcomes`](#m-outcomes) | The most recent command outcomes (up to 256), oldest first. |
| [`RandomStreams`](#m-randomstreams) | Whether subscribers receive the random streams. |
| [`SavedTick`](#m-savedtick) | The world tick of the last [`AuthorityScope.Save()`](AuthorityScope.md#m-save) or [`AuthorityScope.Load()`](AuthorityScope.md#m-load-readonlyspan-byte); -1 before either. |
| [`Subscribers`](#m-subscribers) | The connections currently subscribed. |
| [`TransferGate`](#m-transfergate) | Called on the receiving scope for each offer; return false to refuse. |
| [`TransferLinks`](#m-transferlinks) | Finds the link to a destination scope's node for transfers left unfinished by a restart. |
| [`TransferOfferTicks`](#m-transferofferticks) | Ticks an offer may go unanswered before the transfer is aborted and the entities come back. |
| [`TransferResendTicks`](#m-transferresendticks) | Ticks between resends of an offer or commit that has not been answered. |
| [`Transfers`](#m-transfers) | Transfers that have not finished. |
| [`Upstream`](#m-upstream) | For a relay, the replica it serves from; null for a scope this node is authority of. |
| [`World`](#m-world) | The simulating world. |

### <a id="m-activerecorder"></a>ActiveRecorder

```csharp
public Recorder? ActiveRecorder { get; }
```

The recorder, while one is running.

**Type:** [`Recorder`](Recorder.md)?

### <a id="m-commandsforwarded"></a>CommandsForwarded

```csharp
public int CommandsForwarded { get; }
```

Commands passed on to the node an entity moved to, over the scope's life.

**Type:** `int`

### <a id="m-commandsheld"></a>CommandsHeld

```csharp
public int CommandsHeld { get; }
```

Commands held while their entity was in transit, over the scope's life.

**Type:** `int`

### <a id="m-commandsrun"></a>CommandsRun

```csharp
public long CommandsRun { get; }
```

Total commands the world has run for this scope, counting every outcome.

**Type:** `long`

### <a id="m-durability"></a>Durability

```csharp
public Durability Durability { get; set; }
```

When command answers and transfer confirmations may leave this scope. [`Durability.Immediate`](Durability.md#m-immediate) (default) sends them as they happen; [`Durability.AfterSave`](Durability.md#m-aftersave) holds them until [`AuthorityScope.Save()`](AuthorityScope.md#m-save) has captured the state they describe, so a node restored from its last save never contradicts something it already told a peer.

**Type:** [`Durability`](Durability.md)

### <a id="m-hash"></a>Hash

```csharp
public ulong Hash { get; }
```

The scope's current hash (everything, including what no subscriber may see).

**Type:** `ulong`

### <a id="m-id"></a>Id

```csharp
public ScopeId Id { get; }
```

The scope id.

**Type:** [`ScopeId`](ScopeId.md)

### <a id="m-isrelay"></a>IsRelay

```csharp
public bool IsRelay { get; }
```

True when this scope serves a replica onward instead of simulating (see [`Node.Relay()`](Node.md#m-relay-replicascope)).

**Type:** `bool`

### <a id="m-node"></a>Node

```csharp
public Node Node { get; }
```

The node that holds the authority.

**Type:** [`Node`](Node.md)

### <a id="m-outcomes"></a>Outcomes

```csharp
public IEnumerable<CommandOutcome> Outcomes { get; }
```

The most recent command outcomes (up to 256), oldest first. Handler rejections show up here.

**Type:** [`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)&lt;[`CommandOutcome`](CommandOutcome.md)&gt;

### <a id="m-randomstreams"></a>RandomStreams

```csharp
public Visibility RandomStreams { get; set; }
```

Whether subscribers receive the random streams. [`Visibility.Everyone`](../../OneGame.Core/OneGame.Core/Visibility.md#m-everyone) (the default) sends them, which a replica needs to predict or to match the authority's hash; [`Visibility.Hidden`](../../OneGame.Core/OneGame.Core/Visibility.md#m-hidden) keeps them on the authority, for games where the next shuffle must stay secret. Changing it gives every subscriber a fresh snapshot.

**Type:** [`Visibility`](../../OneGame.Core/OneGame.Core/Visibility.md)

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The value is neither Everyone nor Hidden.

### <a id="m-savedtick"></a>SavedTick

```csharp
public long SavedTick { get; }
```

The world tick of the last [`AuthorityScope.Save()`](AuthorityScope.md#m-save) or [`AuthorityScope.Load()`](AuthorityScope.md#m-load-readonlyspan-byte); -1 before either.

**Type:** `long`

### <a id="m-subscribers"></a>Subscribers

```csharp
public IReadOnlyList<Connection> Subscribers { get; }
```

The connections currently subscribed.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Connection`](Connection.md)&gt;

### <a id="m-transfergate"></a>TransferGate

```csharp
public Func<TransferOffer, bool>? TransferGate { get; set; }
```

Called on the receiving scope for each offer; return false to refuse. Without a gate, any peer with `Trust.Own` on this scope may hand entities in.

**Type:** [`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)&lt;[`TransferOffer`](TransferOffer.md)`, bool>?`

### <a id="m-transferlinks"></a>TransferLinks

```csharp
public Func<ScopeId, Link?>? TransferLinks { get; set; }
```

Finds the link to a destination scope's node for transfers left unfinished by a restart. After [`AuthorityScope.Load()`](AuthorityScope.md#m-load-readonlyspan-byte), a committed transfer in the log has no connection until this returns one.

**Type:** [`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)&lt;[`ScopeId`](ScopeId.md), [`Link`](Link.md)?&gt;?

### <a id="m-transferofferticks"></a>TransferOfferTicks

```csharp
public int TransferOfferTicks { get; set; }
```

Ticks an offer may go unanswered before the transfer is aborted and the entities come back. Default 120.

**Type:** `int`

### <a id="m-transferresendticks"></a>TransferResendTicks

```csharp
public int TransferResendTicks { get; set; }
```

Ticks between resends of an offer or commit that has not been answered. Default 10.

**Type:** `int`

### <a id="m-transfers"></a>Transfers

```csharp
public IReadOnlyList<Transfer> Transfers { get; }
```

Transfers that have not finished.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Transfer`](Transfer.md)&gt;

### <a id="m-upstream"></a>Upstream

```csharp
public ReplicaScope? Upstream { get; }
```

For a relay, the replica it serves from; null for a scope this node is authority of.

**Type:** [`ReplicaScope`](ReplicaScope.md)?

### <a id="m-world"></a>World

```csharp
public World World { get; }
```

The simulating world.

**Type:** [`World`](../../OneGame.Core/OneGame.Core/World.md)

## Events

| Member | Description |
| --- | --- |
| [`TransferCommitted`](#m-transfercommitted) | Raised when this scope decided to commit a transfer, before the commit message is sent. |
| [`TransferFinished`](#m-transferfinished) | Raised when a transfer reaches [`TransferState.Complete`](TransferState.md#m-complete) or [`TransferState.Aborted`](TransferState.md#m-aborted). |
| [`TransferInstalled`](#m-transferinstalled) | Raised on the receiving scope when a transfer was installed, with the transfer id and the ids the entities got here. |

### <a id="m-transfercommitted"></a>TransferCommitted

```csharp
public event Action<Transfer>? TransferCommitted;
```

Raised when this scope decided to commit a transfer, before the commit message is sent. With [`Durability.AfterSave`](Durability.md#m-aftersave) the message waits for the next [`AuthorityScope.Save()`](AuthorityScope.md#m-save).

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`Transfer`](Transfer.md)&gt;?

### <a id="m-transferfinished"></a>TransferFinished

```csharp
public event Action<Transfer>? TransferFinished;
```

Raised when a transfer reaches [`TransferState.Complete`](TransferState.md#m-complete) or [`TransferState.Aborted`](TransferState.md#m-aborted).

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`Transfer`](Transfer.md)&gt;?

### <a id="m-transferinstalled"></a>TransferInstalled

```csharp
public event Action<ulong, IReadOnlyList<EntityId>>? TransferInstalled;
```

Raised on the receiving scope when a transfer was installed, with the transfer id and the ids the entities got here. A repeated commit does not raise it again.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<ulong,` [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)&gt;&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`BeginTransfer(Link, ScopeId, IEnumerable<EntityId>)`](#m-begintransfer-link-scopeid-ienumerable-entityid) | Starts moving entities to a scope on another node. |
| [`Load(ReadOnlySpan<byte>)`](#m-load-readonlyspan-byte) | Replaces the scope's state with a save. |
| [`Save()`](#m-save) | Writes a save file of the scope's current state. |
| [`StartRecording(RecordingOptions?)`](#m-startrecording-recordingoptions) | Starts recording: takes the start snapshot now, then logs every command and the hash checkpoints. |
| [`StepAndPublish()`](#m-stepandpublish) | Runs one tick and publishes the resulting frame. |
| [`ViewHashFor(Connection)`](#m-viewhashfor-connection) | The hash of the view this connection holds of the scope: what its replica's [`ReplicaScope.ViewHash`](ReplicaScope.md#m-viewhash) must equal at the same tick. |

### <a id="m-begintransfer-link-scopeid-ienumerable-entityid"></a>BeginTransfer(Link, ScopeId, IEnumerable&lt;EntityId&gt;)

```csharp
public Transfer BeginTransfer(Link link, ScopeId destination, IEnumerable<EntityId> entities);
```

Starts moving entities to a scope on another node. Call it between ticks from host code, never from sim code. The entities leave the world after the next tick and arrive in the destination scope under new ids; see the sync interface document for the protocol.

**Parameters**

- `link` ([`Link`](Link.md)): This node's link to the destination node; that node must trust it with `Own` on `destination`.
- `destination` ([`ScopeId`](ScopeId.md)): The scope on the other node.
- `entities` ([`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)&lt;[`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)&gt;): Ids of entities in this scope. Entities may not refer to each other or to anything left behind by id.

**Returns:** [`Transfer`](Transfer.md)

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): This scope is a relay, or its world has no [`TransferLog`](TransferLog.md).
- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The list is empty or too long, an entity does not exist or is already moving, or the link belongs to another node.

### <a id="m-load-readonlyspan-byte"></a>Load(ReadOnlySpan&lt;byte&gt;)

```csharp
public LoadResult Load(ReadOnlySpan<byte> save);
```

Replaces the scope's state with a save. Subscribers get a fresh snapshot. The world must have been built the same way as the one saved.

**Parameters**

- `save` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** [`LoadResult`](LoadResult.md)

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): A recording is running.

### <a id="m-save"></a>Save()

```csharp
public byte[] Save();
```

Writes a save file of the scope's current state.

**Returns:** `byte[]`

### <a id="m-startrecording-recordingoptions"></a>StartRecording(RecordingOptions?)

```csharp
public Recorder StartRecording(RecordingOptions? options = null);
```

Starts recording: takes the start snapshot now, then logs every command and the hash checkpoints.

**Parameters**

- `options` ([`RecordingOptions`](RecordingOptions.md)?)

**Returns:** [`Recorder`](Recorder.md)

**Exceptions**

- [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception): A recording is already running.

### <a id="m-stepandpublish"></a>StepAndPublish()

```csharp
public void StepAndPublish();
```

Runs one tick and publishes the resulting frame.

### <a id="m-viewhashfor-connection"></a>ViewHashFor(Connection)

```csharp
public ulong ViewHashFor(Connection connection);
```

The hash of the view this connection holds of the scope: what its replica's [`ReplicaScope.ViewHash`](ReplicaScope.md#m-viewhash) must equal at the same tick. For a connection that sees everything it equals [`AuthorityScope.Hash`](AuthorityScope.md#m-hash).

**Parameters**

- `connection` ([`Connection`](Connection.md))

**Returns:** `ulong`
