[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Connection class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class Connection
```

The authority's end of a connection: what the peer is trusted to do, and the entry point for commands as that peer.

## Properties

| Member | Description |
| --- | --- |
| [`Channel`](#m-channel) | The channel the connection was accepted on: the transport itself, so a game can ask it who the peer is ([`IAuthenticatedChannel`](IAuthenticatedChannel.md)) or close it. |
| [`Id`](#m-id) | Id unique within the node. |
| [`Name`](#m-name) | Name for reports. |
| [`Node`](#m-node) | The node this end belongs to (the one that holds the authority scopes). |
| [`Trust`](#m-trust) | What the peer may do and who it is. |

### <a id="m-channel"></a>Channel

```csharp
public IMessageChannel Channel { get; }
```

The channel the connection was accepted on: the transport itself, so a game can ask it who the peer is ([`IAuthenticatedChannel`](IAuthenticatedChannel.md)) or close it. Sending through it directly skips the node's send-queue pacing.

**Type:** [`IMessageChannel`](IMessageChannel.md)

### <a id="m-id"></a>Id

```csharp
public int Id { get; }
```

Id unique within the node.

**Type:** `int`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

Name for reports.

**Type:** `string`

### <a id="m-node"></a>Node

```csharp
public Node Node { get; }
```

The node this end belongs to (the one that holds the authority scopes).

**Type:** [`Node`](Node.md)

### <a id="m-trust"></a>Trust

```csharp
public Trust Trust { get; set; }
```

What the peer may do and who it is. May be changed at any time, between ticks: the next command or subscription is checked against the new record, and every scope the peer is subscribed to is re-evaluated at once. A peer that may no longer read a scope is told it was revoked and gets nothing more; a peer whose player or team changed receives a fresh snapshot of what its new audience may see (it gains the data it may now see and loses the rest).

**Type:** [`Trust`](Trust.md)

## Methods

| Member | Description |
| --- | --- |
| [`SetInterest(ScopeId, ReadOnlySpan<uint>)`](#m-setinterest-scopeid-readonlyspan-uint) | Sets which interest cells this connection receives in a scope of its node; see [`AuthorityScope.SetInterest()`](AuthorityScope.md#m-setinterest-connection-readonlyspan-uint). |
| [`SubmitCommand(ScopeId, ReadOnlySpan<byte>)`](#m-submitcommand-scopeid-readonlyspan-byte) | Submits a command in its wire form (entity id, command id, arguments), as read from a script or a log. |
| [`SubmitCommand(ScopeId, EntityId, uint, ReadOnlySpan<byte>)`](#m-submitcommand-scopeid-entityid-uint-readonlyspan-byte) | Submits a command as this connection, exactly as if it had arrived over the channel: the trust record is checked, a refusal is filed and sent back, and an accepted command runs at the start of the scope's next tick. |

### <a id="m-setinterest-scopeid-readonlyspan-uint"></a>SetInterest(ScopeId, ReadOnlySpan&lt;uint&gt;)

```csharp
public bool SetInterest(ScopeId scope, ReadOnlySpan<uint> keys);
```

Sets which interest cells this connection receives in a scope of its node; see [`AuthorityScope.SetInterest()`](AuthorityScope.md#m-setinterest-connection-readonlyspan-uint). Works the same for clients and peer nodes.

**Parameters**

- `scope` ([`ScopeId`](ScopeId.md)): The scope id.
- `keys` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<uint>`): The cells to receive.

**Returns:** `bool` — True when the set changed; false if unchanged or the node has no such scope.

### <a id="m-submitcommand-scopeid-readonlyspan-byte"></a>SubmitCommand(ScopeId, ReadOnlySpan&lt;byte&gt;)

```csharp
public SubmitResult SubmitCommand(ScopeId scope, ReadOnlySpan<byte> wire);
```

Submits a command in its wire form (entity id, command id, arguments), as read from a script or a log.

**Parameters**

- `scope` ([`ScopeId`](ScopeId.md))
- `wire` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** [`SubmitResult`](SubmitResult.md)

### <a id="m-submitcommand-scopeid-entityid-uint-readonlyspan-byte"></a>SubmitCommand(ScopeId, EntityId, uint, ReadOnlySpan&lt;byte&gt;)

```csharp
public SubmitResult SubmitCommand(ScopeId scope, EntityId entity, uint commandId, ReadOnlySpan<byte> payload);
```

Submits a command as this connection, exactly as if it had arrived over the channel: the trust record is checked, a refusal is filed and sent back, and an accepted command runs at the start of the scope's next tick.

**Parameters**

- `scope` ([`ScopeId`](ScopeId.md)): The scope to command.
- `entity` ([`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)): The target entity.
- `commandId` (`uint`): The command id (see the generated `Commands`).
- `payload` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The encoded arguments.

**Returns:** [`SubmitResult`](SubmitResult.md)
