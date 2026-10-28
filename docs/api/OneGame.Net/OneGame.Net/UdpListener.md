[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# UdpListener class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)  
**Implements:** [`INetListener`](INetListener.md), [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class UdpListener : INetListener, IDisposable
```

The server end of the reliable-UDP transport. A client's first datagram only earns it a small cookie challenge; the server keeps no state for it until the client echoes the cookie, which proves it can receive at the address it claims. Only then does a connection exist, so a flood of spoofed requests costs the server a few hash computations and no memory.

## Properties

| Member | Description |
| --- | --- |
| [`ChallengesSent`](#m-challengessent) | Cookie challenges sent. |
| [`ConnectionCount`](#m-connectioncount) | Connections currently alive (connected or closing). |
| [`Connections`](#m-connections) | The live connections, oldest first. |
| [`DatagramsIgnored`](#m-datagramsignored) | Datagrams ignored: wrong version, too short, unknown connection, wrong source, or a bad cookie. |
| [`LocalEndPoint`](#m-localendpoint) | The address the listener is bound to. |
| [`PendingHandshakes`](#m-pendinghandshakes) | Handshakes the server holds state for right now. |

### <a id="m-challengessent"></a>ChallengesSent

```csharp
public long ChallengesSent { get; }
```

Cookie challenges sent.

**Type:** `long`

### <a id="m-connectioncount"></a>ConnectionCount

```csharp
public int ConnectionCount { get; }
```

Connections currently alive (connected or closing).

**Type:** `int`

### <a id="m-connections"></a>Connections

```csharp
public IReadOnlyList<UdpConnection> Connections { get; }
```

The live connections, oldest first. Do not change the list.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`UdpConnection`](UdpConnection.md)&gt;

### <a id="m-datagramsignored"></a>DatagramsIgnored

```csharp
public long DatagramsIgnored { get; }
```

Datagrams ignored: wrong version, too short, unknown connection, wrong source, or a bad cookie.

**Type:** `long`

### <a id="m-localendpoint"></a>LocalEndPoint

```csharp
public EndPoint LocalEndPoint { get; }
```

The address the listener is bound to.

**Type:** [`EndPoint`](https://learn.microsoft.com/dotnet/api/system.net.endpoint)

### <a id="m-pendinghandshakes"></a>PendingHandshakes

```csharp
public int PendingHandshakes { get; }
```

Handshakes the server holds state for right now. Always 0 for UDP, whose handshake is stateless until the client proves its address.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) |  |
| [`Pump()`](#m-pump) | Processes incoming datagrams or connections and pumps every accepted connection. |
| [`TryAccept(out INetConnection)`](#m-tryaccept-inetconnection) | Takes the next newly connected peer, or returns false. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

### <a id="m-pump"></a>Pump()

```csharp
public void Pump();
```

Processes incoming datagrams or connections and pumps every accepted connection.

### <a id="m-tryaccept-inetconnection"></a>TryAccept(out INetConnection)

```csharp
public bool TryAccept(out INetConnection connection);
```

Takes the next newly connected peer, or returns false.

**Parameters**

- `connection` ([`INetConnection`](INetConnection.md))

**Returns:** `bool`
