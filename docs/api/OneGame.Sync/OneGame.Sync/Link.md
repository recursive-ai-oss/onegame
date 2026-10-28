[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Link class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class Link
```

The client end of a connection to another node: where a node sends subscriptions and commands, and receives snapshots and frames.

## Properties

| Member | Description |
| --- | --- |
| [`Channel`](#m-channel) | The channel. |
| [`Connection`](#m-connection) | The server's end of the same connection. |
| [`IsRemote`](#m-isremote) | True when the authority is in another process, reached over a channel from [`Node.ConnectVia()`](Node.md#m-connectvia-imessagechannel-string). |
| [`Owner`](#m-owner) | The node that holds this end. |
| [`PeerName`](#m-peername) | Name of the node at the other end, used to name the hop in drift reports. |
| [`Server`](#m-server) | The node at the other end when it lives in this process; null for a link to a remote authority ([`Node.ConnectVia()`](Node.md#m-connectvia-imessagechannel-string)). |

### <a id="m-channel"></a>Channel

```csharp
public IMessageChannel Channel { get; }
```

The channel. Tests may send raw bytes through it to play a misbehaving peer.

**Type:** [`IMessageChannel`](IMessageChannel.md)

### <a id="m-connection"></a>Connection

```csharp
public Connection Connection { get; }
```

The server's end of the same connection. Only set when the server is in this process; check [`Link.IsRemote`](Link.md#m-isremote) first, it is null on a remote link.

**Type:** [`Connection`](Connection.md)

### <a id="m-isremote"></a>IsRemote

```csharp
public bool IsRemote { get; }
```

True when the authority is in another process, reached over a channel from [`Node.ConnectVia()`](Node.md#m-connectvia-imessagechannel-string). Hashes are then compared in-band only.

**Type:** `bool`

### <a id="m-owner"></a>Owner

```csharp
public Node Owner { get; }
```

The node that holds this end.

**Type:** [`Node`](Node.md)

### <a id="m-peername"></a>PeerName

```csharp
public string PeerName { get; }
```

Name of the node at the other end, used to name the hop in drift reports.

**Type:** `string`

### <a id="m-server"></a>Server

```csharp
public Node? Server { get; }
```

The node at the other end when it lives in this process; null for a link to a remote authority ([`Node.ConnectVia()`](Node.md#m-connectvia-imessagechannel-string)).

**Type:** [`Node`](Node.md)?
