[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# WebSocketTransport static class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)

```csharp
public static class WebSocketTransport
```

Creates WebSocket listeners and connections.

## Methods

| Member | Description |
| --- | --- |
| [`Connect(IPEndPoint, WebSocketOptions?, NetClock?)`](#m-connect-ipendpoint-websocketoptions-netclock) | Connects to a WebSocket server. |
| [`Listen(IPEndPoint, WebSocketOptions?, NetClock?)`](#m-listen-ipendpoint-websocketoptions-netclock) | Starts listening for WebSocket clients on a TCP port. |

### <a id="m-connect-ipendpoint-websocketoptions-netclock"></a>Connect(IPEndPoint, WebSocketOptions?, NetClock?)

```csharp
public static WebSocketConnection Connect(IPEndPoint remote, WebSocketOptions? options = null, NetClock? clock = null);
```

Connects to a WebSocket server. The connection starts in [`ConnectionState.Connecting`](ConnectionState.md#m-connecting); messages sent now go out when it connects.

**Parameters**

- `remote` ([`IPEndPoint`](https://learn.microsoft.com/dotnet/api/system.net.ipendpoint)): The server's address.
- `options` ([`WebSocketOptions`](WebSocketOptions.md)?): Settings; defaults if null.
- `clock` ([`NetClock`](NetClock.md)?): Time source; the real clock if null.

**Returns:** [`WebSocketConnection`](WebSocketConnection.md)

### <a id="m-listen-ipendpoint-websocketoptions-netclock"></a>Listen(IPEndPoint, WebSocketOptions?, NetClock?)

```csharp
public static WebSocketListener Listen(IPEndPoint bind, WebSocketOptions? options = null, NetClock? clock = null);
```

Starts listening for WebSocket clients on a TCP port.

**Parameters**

- `bind` ([`IPEndPoint`](https://learn.microsoft.com/dotnet/api/system.net.ipendpoint)): The local address and port.
- `options` ([`WebSocketOptions`](WebSocketOptions.md)?): Settings; defaults if null.
- `clock` ([`NetClock`](NetClock.md)?): Time source; the real clock if null.

**Returns:** [`WebSocketListener`](WebSocketListener.md)
