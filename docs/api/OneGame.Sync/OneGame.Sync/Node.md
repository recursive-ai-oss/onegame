[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Node class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class Node
```

A participant: it holds the scopes it is authority for, the replicas it subscribes to, and the connections that carry both. Nothing runs on its own; [`Node.Pump()`](Node.md#m-pump) processes whatever has arrived, and [`Cluster`](Cluster.md) calls it in a fixed order.

## Constructors

| Member | Description |
| --- | --- |
| [`Node(string, ClusterOptions?)`](#m-ctor-string-clusteroptions) | Creates a node. |

### <a id="m-ctor-string-clusteroptions"></a>Node(string, ClusterOptions?)

```csharp
public Node(string name, ClusterOptions? options = null);
```

Creates a node.

**Parameters**

- `name` (`string`): Name for reports.
- `options` ([`ClusterOptions`](ClusterOptions.md)?): Behavior switches; defaults if null.

## Properties

| Member | Description |
| --- | --- |
| [`AuthorityScopes`](#m-authorityscopes) | Scopes this node is authority for. |
| [`Connections`](#m-connections) | Connections other nodes made to this one. |
| [`DriftCount`](#m-driftcount) | Total drift reports ever filed (the list keeps only the latest 256). |
| [`DriftReports`](#m-driftreports) | Drift found by this node's replicas. |
| [`FollowResolver`](#m-followresolver) | Called when a scope this node follows reports that entities moved to another scope: return where to subscribe to it, or null to stay out. |
| [`Name`](#m-name) | The node's name. |
| [`Options`](#m-options) | The options this node follows. |
| [`ProtocolErrors`](#m-protocolerrors) | Messages that could not be applied (at most 256 kept). |
| [`Refusals`](#m-refusals) | The most recent refused commands and subscriptions (at most 256): those this node refused as an authority, and those it was told about as a submitter. |
| [`Relays`](#m-relays) | The relays this node runs (see [`Node.Relay()`](Node.md#m-relay-replicascope)). |
| [`ReplicaScopes`](#m-replicascopes) | Scopes this node subscribes to. |

### <a id="m-authorityscopes"></a>AuthorityScopes

```csharp
public IReadOnlyList<AuthorityScope> AuthorityScopes { get; }
```

Scopes this node is authority for.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`AuthorityScope`](AuthorityScope.md)&gt;

### <a id="m-connections"></a>Connections

```csharp
public IReadOnlyList<Connection> Connections { get; }
```

Connections other nodes made to this one.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`Connection`](Connection.md)&gt;

### <a id="m-driftcount"></a>DriftCount

```csharp
public int DriftCount { get; }
```

Total drift reports ever filed (the list keeps only the latest 256).

**Type:** `int`

### <a id="m-driftreports"></a>DriftReports

```csharp
public IReadOnlyList<DriftReport> DriftReports { get; }
```

Drift found by this node's replicas.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`DriftReport`](DriftReport.md)&gt;

### <a id="m-followresolver"></a>FollowResolver

```csharp
public Func<ScopeId, FollowTarget?>? FollowResolver { get; set; }
```

Called when a scope this node follows reports that entities moved to another scope: return where to subscribe to it, or null to stay out. Without a resolver the node does not follow by itself.

**Type:** [`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)&lt;[`ScopeId`](ScopeId.md), [`FollowTarget`](FollowTarget.md)?&gt;?

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The node's name.

**Type:** `string`

### <a id="m-options"></a>Options

```csharp
public ClusterOptions Options { get; }
```

The options this node follows.

**Type:** [`ClusterOptions`](ClusterOptions.md)

### <a id="m-protocolerrors"></a>ProtocolErrors

```csharp
public IReadOnlyList<ProtocolError> ProtocolErrors { get; }
```

Messages that could not be applied (at most 256 kept).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ProtocolError`](ProtocolError.md)&gt;

### <a id="m-refusals"></a>Refusals

```csharp
public IReadOnlyList<CommandRefusal> Refusals { get; }
```

The most recent refused commands and subscriptions (at most 256): those this node refused as an authority, and those it was told about as a submitter.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`CommandRefusal`](CommandRefusal.md)&gt;

### <a id="m-relays"></a>Relays

```csharp
public IReadOnlyList<AuthorityScope> Relays { get; }
```

The relays this node runs (see [`Node.Relay()`](Node.md#m-relay-replicascope)).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`AuthorityScope`](AuthorityScope.md)&gt;

### <a id="m-replicascopes"></a>ReplicaScopes

```csharp
public IReadOnlyList<ReplicaScope> ReplicaScopes { get; }
```

Scopes this node subscribes to.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ReplicaScope`](ReplicaScope.md)&gt;

## Events

| Member | Description |
| --- | --- |
| [`ReplicaAdded`](#m-replicaadded) | Raised when the node subscribed to a scope by itself, through [`Node.FollowResolver`](Node.md#m-followresolver). |

### <a id="m-replicaadded"></a>ReplicaAdded

```csharp
public event Action<ReplicaScope>? ReplicaAdded;
```

Raised when the node subscribed to a scope by itself, through [`Node.FollowResolver`](Node.md#m-followresolver).

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`ReplicaScope`](ReplicaScope.md)&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`Accept(IMessageChannel, Trust, string?)`](#m-accept-imessagechannel-trust-string) | Server side: a channel accepted from a listener becomes a connection with the given trust. |
| [`AddScope(ScopeId, World)`](#m-addscope-scopeid-world) | Makes this node the authority of a scope. |
| [`ConnectTo(Node, Trust, string?)`](#m-connectto-node-trust-string) | Connects this node to `server` over a loopback pair. |
| [`ConnectTo(Node, Trust, IMessageChannel, IMessageChannel, string?)`](#m-connectto-node-trust-imessagechannel-imessagechannel-string) | Connects this node to `server` over two channel ends the caller made. |
| [`ConnectVia(IMessageChannel, string?)`](#m-connectvia-imessagechannel-string) | Client side of a connection to an authority in another process: this node talks to it over `channel` (a network transport's channel). |
| [`Detach(Connection)`](#m-detach-connection) | Forgets a connection that has gone away (its transport closed): every scope stops publishing to it and [`Node.Connections`](Node.md#m-connections) no longer lists it. |
| [`Detach(Link)`](#m-detach-link) | Forgets a link whose transport is gone and everything this node followed over it: its replicas become [`ReplicaState.Unsubscribed`](ReplicaState.md#m-unsubscribed) (they keep their last state) and their pending commands complete as dropped. |
| [`FindAuthority(ScopeId)`](#m-findauthority-scopeid) | The authority scope with this id, or null. |
| [`Pump()`](#m-pump) | Handles every message that has arrived. |
| [`Reconnect(Link, IMessageChannel)`](#m-reconnect-link-imessagechannel) | Carries on over a new channel to the same authority after the old one died: the link keeps its replicas, each of which asks for a fresh snapshot (their worlds are replaced, hashes agree again once it arrives). |
| [`Relay(ReplicaScope)`](#m-relay-replicascope) | Serves a replica onward: other nodes subscribe to this node for the same scope id and receive what the replica receives, filtered for each of them, with their commands passed up. |
| [`Resubscribe(ReplicaScope)`](#m-resubscribe-replicascope) | Subscribes again with a replica that was refused, revoked or unsubscribed (for example after the authority granted it more trust). |
| [`Subscribe(Link, ScopeId, World)`](#m-subscribe-link-scopeid-world) | Subscribes to a scope on the other end of `link`. |
| [`Unsubscribe(ReplicaScope)`](#m-unsubscribe-replicascope) | Stops following a scope: tells the authority to stop sending, and drops the replica from [`Node.ReplicaScopes`](Node.md#m-replicascopes). |

### <a id="m-accept-imessagechannel-trust-string"></a>Accept(IMessageChannel, Trust, string?)

```csharp
public Connection Accept(IMessageChannel channel, Trust trust, string? name = null);
```

Server side: a channel accepted from a listener becomes a connection with the given trust. The remote node can then subscribe and command as `trust` allows; [`Node.Pump()`](Node.md#m-pump) reads its messages.

**Parameters**

- `channel` ([`IMessageChannel`](IMessageChannel.md)): The accepted channel.
- `trust` ([`Trust`](Trust.md)): What the peer may read, command and own, and who it is.
- `name` (`string?`): Name for reports.

**Returns:** [`Connection`](Connection.md)

### <a id="m-addscope-scopeid-world"></a>AddScope(ScopeId, World)

```csharp
public AuthorityScope AddScope(ScopeId id, World world);
```

Makes this node the authority of a scope.

**Parameters**

- `id` ([`ScopeId`](ScopeId.md)): The scope id; unique on this node.
- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): A fully built authority world.

**Returns:** [`AuthorityScope`](AuthorityScope.md)

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The world is a replica, or the scope id is taken.

### <a id="m-connectto-node-trust-string"></a>ConnectTo(Node, Trust, string?)

```csharp
public Link ConnectTo(Node server, Trust trust, string? name = null);
```

Connects this node to `server` over a loopback pair.

**Parameters**

- `server` ([`Node`](Node.md)): The node that will hold the authority scopes.
- `trust` ([`Trust`](Trust.md)): What this node is trusted to do on the server.
- `name` (`string?`): Name for reports; defaults to this node's name.

**Returns:** [`Link`](Link.md)

### <a id="m-connectto-node-trust-imessagechannel-imessagechannel-string"></a>ConnectTo(Node, Trust, IMessageChannel, IMessageChannel, string?)

```csharp
public Link ConnectTo(Node server, Trust trust, IMessageChannel clientEnd, IMessageChannel serverEnd, string? name = null);
```

Connects this node to `server` over two channel ends the caller made. A test can wrap the ends to record the exact bytes that travel; a transport hands in its own.

**Parameters**

- `server` ([`Node`](Node.md)): The node that will hold the authority scopes.
- `trust` ([`Trust`](Trust.md)): What this node is trusted to do on the server.
- `clientEnd` ([`IMessageChannel`](IMessageChannel.md)): The end this node reads snapshots and frames from and sends commands to.
- `serverEnd` ([`IMessageChannel`](IMessageChannel.md)): The end the server reads commands from and sends snapshots and frames to.
- `name` (`string?`): Name for reports; defaults to this node's name.

**Returns:** [`Link`](Link.md)

### <a id="m-connectvia-imessagechannel-string"></a>ConnectVia(IMessageChannel, string?)

```csharp
public Link ConnectVia(IMessageChannel channel, string? peerName = null);
```

Client side of a connection to an authority in another process: this node talks to it over `channel` (a network transport's channel). Subscribe through the returned link as with [`Node.ConnectTo()`](Node.md#m-connectto-node-trust-string). The trust is the authority's decision, made when it accepts the channel; nothing is passed here.

**Parameters**

- `channel` ([`IMessageChannel`](IMessageChannel.md)): A connected channel to the remote node.
- `peerName` (`string?`): The remote node's name, for drift reports; "remote" if omitted.

**Returns:** [`Link`](Link.md)

### <a id="m-detach-connection"></a>Detach(Connection)

```csharp
public bool Detach(Connection connection);
```

Forgets a connection that has gone away (its transport closed): every scope stops publishing to it and [`Node.Connections`](Node.md#m-connections) no longer lists it. Commands it already queued still run. Safe to call twice.

**Parameters**

- `connection` ([`Connection`](Connection.md)): A connection from [`Node.Accept()`](Node.md#m-accept-imessagechannel-trust-string) or `ConnectTo` on this node.

**Returns:** `bool` — False if it was not (or no longer) attached.

### <a id="m-detach-link"></a>Detach(Link)

```csharp
public bool Detach(Link link);
```

Forgets a link whose transport is gone and everything this node followed over it: its replicas become [`ReplicaState.Unsubscribed`](ReplicaState.md#m-unsubscribed) (they keep their last state) and their pending commands complete as dropped.

**Parameters**

- `link` ([`Link`](Link.md)): A link made by `ConnectTo` or [`Node.ConnectVia()`](Node.md#m-connectvia-imessagechannel-string) on this node.

**Returns:** `bool` — False if it was not attached.

### <a id="m-findauthority-scopeid"></a>FindAuthority(ScopeId)

```csharp
public AuthorityScope? FindAuthority(ScopeId id);
```

The authority scope with this id, or null.

**Parameters**

- `id` ([`ScopeId`](ScopeId.md))

**Returns:** [`AuthorityScope`](AuthorityScope.md)?

### <a id="m-pump"></a>Pump()

```csharp
public bool Pump();
```

Handles every message that has arrived. Returns true if there was any.

**Returns:** `bool`

### <a id="m-reconnect-link-imessagechannel"></a>Reconnect(Link, IMessageChannel)

```csharp
public void Reconnect(Link link, IMessageChannel channel);
```

Carries on over a new channel to the same authority after the old one died: the link keeps its replicas, each of which asks for a fresh snapshot (their worlds are replaced, hashes agree again once it arrives). Commands still waiting for an answer complete as dropped and predictions are rebuilt. Replicas that were refused, revoked or unsubscribed stay as they are.

**Parameters**

- `link` ([`Link`](Link.md)): A link from [`Node.ConnectVia()`](Node.md#m-connectvia-imessagechannel-string) on this node.
- `channel` ([`IMessageChannel`](IMessageChannel.md)): A connected channel to the (restarted) authority.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The link belongs to another node or was detached.

### <a id="m-relay-replicascope"></a>Relay(ReplicaScope)

```csharp
public AuthorityScope Relay(ReplicaScope upstream);
```

Serves a replica onward: other nodes subscribe to this node for the same scope id and receive what the replica receives, filtered for each of them, with their commands passed up. The connection the replica uses must carry [`Trust.AsRelay()`](Trust.md#m-asrelay).

**Parameters**

- `upstream` ([`ReplicaScope`](ReplicaScope.md)): A replica made by [`Node.Subscribe()`](Node.md#m-subscribe-link-scopeid-world) on this node.

**Returns:** [`AuthorityScope`](AuthorityScope.md)

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The replica belongs to another node, or this node already serves that scope id.

### <a id="m-resubscribe-replicascope"></a>Resubscribe(ReplicaScope)

```csharp
public void Resubscribe(ReplicaScope replica);
```

Subscribes again with a replica that was refused, revoked or unsubscribed (for example after the authority granted it more trust). The replica's world is replaced by a fresh snapshot.

**Parameters**

- `replica` ([`ReplicaScope`](ReplicaScope.md)): The replica; it must belong to this node.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The replica belongs to another node.

### <a id="m-subscribe-link-scopeid-world"></a>Subscribe(Link, ScopeId, World)

```csharp
public ReplicaScope Subscribe(Link link, ScopeId id, World world);
```

Subscribes to a scope on the other end of `link`. The replica fills in as messages are pumped.

**Parameters**

- `link` ([`Link`](Link.md)): A link made by `ConnectTo` on this node.
- `id` ([`ScopeId`](ScopeId.md)): The scope.
- `world` ([`World`](../../OneGame.Core/OneGame.Core/World.md)): An empty replica world with the same entity registry and hot tables as the authority's.

**Returns:** [`ReplicaScope`](ReplicaScope.md)

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The world is not a replica, or the link belongs to another node.

### <a id="m-unsubscribe-replicascope"></a>Unsubscribe(ReplicaScope)

```csharp
public bool Unsubscribe(ReplicaScope replica);
```

Stops following a scope: tells the authority to stop sending, and drops the replica from [`Node.ReplicaScopes`](Node.md#m-replicascopes). The replica's world keeps the last state it had and is no longer updated; late messages for it are ignored.

**Parameters**

- `replica` ([`ReplicaScope`](ReplicaScope.md)): A replica made by [`Node.Subscribe()`](Node.md#m-subscribe-link-scopeid-world) on this node.

**Returns:** `bool` — False if the replica was not subscribed on this node.
