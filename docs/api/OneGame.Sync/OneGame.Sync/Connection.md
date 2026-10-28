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
| [`Channel`](#m-channel) | The channel to the peer. |
| [`Id`](#m-id) | Id unique within the node. |
| [`Name`](#m-name) | Name for reports. |
| [`Node`](#m-node) | The node this end belongs to (the one that holds the authority scopes). |
| [`Trust`](#m-trust) | What the peer may do and who it is. |

### <a id="m-channel"></a>Channel

```csharp
public IMessageChannel Channel { get; }
```

The channel to the peer. Tests may send raw bytes through it to play a misbehaving authority.

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
| [`SubmitCommand(ScopeId, ReadOnlySpan<byte>)`](#m-submitcommand-scopeid-readonlyspan-byte) | Submits a command in its wire form (entity id, command id, arguments), as read from a script or a log. |
| [`SubmitCommand(ScopeId, EntityId, uint, ReadOnlySpan<byte>)`](#m-submitcommand-scopeid-entityid-uint-readonlyspan-byte) | Submits a command as this connection, exactly as if it had arrived over the channel: the trust record is checked, a refusal is filed and sent back, and an accepted command runs at the start of the scope's next tick. |

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
