[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# WebSocketListener class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)  
**Implements:** [`INetListener`](INetListener.md), [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class WebSocketListener : INetListener, IDisposable
```

The server end of the WebSocket transport.

## Properties

| Member | Description |
| --- | --- |
| [`ConnectionCount`](#m-connectioncount) | Connections currently alive (connected or closing). |
| [`LocalEndPoint`](#m-localendpoint) | The address the listener is bound to. |
| [`PendingHandshakes`](#m-pendinghandshakes) | Handshakes the server holds state for right now. |

### <a id="m-connectioncount"></a>ConnectionCount

```csharp
public int ConnectionCount { get; }
```

Connections currently alive (connected or closing).

**Type:** `int`

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

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

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
