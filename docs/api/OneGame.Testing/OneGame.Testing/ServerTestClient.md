[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ServerTestClient class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)

```csharp
public sealed class ServerTestClient
```

A client of a [`TestServer`](TestServer.md): a node holding a replica of one scope.

## Properties

| Member | Description |
| --- | --- |
| [`Node`](#m-node) | The client's node. |
| [`Replica`](#m-replica) | The replica of the scope. |
| [`World`](#m-world) | The replica world; views read it. |

### <a id="m-node"></a>Node

```csharp
public Node Node { get; }
```

The client's node.

**Type:** [`Node`](../../OneGame.Sync/OneGame.Sync/Node.md)

### <a id="m-replica"></a>Replica

```csharp
public ReplicaScope Replica { get; }
```

The replica of the scope.

**Type:** [`ReplicaScope`](../../OneGame.Sync/OneGame.Sync/ReplicaScope.md)

### <a id="m-world"></a>World

```csharp
public World World { get; }
```

The replica world; views read it.

**Type:** [`World`](../../OneGame.Core/OneGame.Core/World.md)

## Methods

| Member | Description |
| --- | --- |
| [`Send(EntityId, string, string[])`](#m-send-entityid-string-string) | Sends a command as a player would: forwarded to the server, run on its next tick. |

### <a id="m-send-entityid-string-string"></a>Send(EntityId, string, string\[\])

```csharp
public CommandStatus Send(EntityId entity, string command, params string[] args);
```

Sends a command as a player would: forwarded to the server, run on its next tick.

**Parameters**

- `entity` ([`EntityId`](../../OneGame.Core/OneGame.Core/EntityId.md)): The entity.
- `command` (`string`): The command name.
- `args` (`string[]`): Arguments as text.

**Returns:** [`CommandStatus`](../../OneGame.Core/OneGame.Core/CommandStatus.md)
