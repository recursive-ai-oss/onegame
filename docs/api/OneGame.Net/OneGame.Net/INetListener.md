[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# INetListener interface

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public interface INetListener : IDisposable
```

A server endpoint that turns incoming handshakes into [`INetConnection`](INetConnection.md)s.

## Properties

| Member | Description |
| --- | --- |
| [`ConnectionCount`](#m-connectioncount) | Connections currently alive (connected or closing). |
| [`LocalEndPoint`](#m-localendpoint) | The address the listener is bound to. |
| [`PendingHandshakes`](#m-pendinghandshakes) | Handshakes the server holds state for right now. |

### <a id="m-connectioncount"></a>ConnectionCount

```csharp
int ConnectionCount { get; }
```

Connections currently alive (connected or closing).

**Type:** `int`

### <a id="m-localendpoint"></a>LocalEndPoint

```csharp
EndPoint LocalEndPoint { get; }
```

The address the listener is bound to.

**Type:** [`EndPoint`](https://learn.microsoft.com/dotnet/api/system.net.endpoint)

### <a id="m-pendinghandshakes"></a>PendingHandshakes

```csharp
int PendingHandshakes { get; }
```

Handshakes the server holds state for right now. Always 0 for UDP, whose handshake is stateless until the client proves its address.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Pump()`](#m-pump) | Processes incoming datagrams or connections and pumps every accepted connection. |
| [`TryAccept(out INetConnection)`](#m-tryaccept-inetconnection) | Takes the next newly connected peer, or returns false. |

### <a id="m-pump"></a>Pump()

```csharp
void Pump();
```

Processes incoming datagrams or connections and pumps every accepted connection.

### <a id="m-tryaccept-inetconnection"></a>TryAccept(out INetConnection)

```csharp
bool TryAccept(out INetConnection connection);
```

Takes the next newly connected peer, or returns false.

**Parameters**

- `connection` ([`INetConnection`](INetConnection.md))

**Returns:** `bool`
