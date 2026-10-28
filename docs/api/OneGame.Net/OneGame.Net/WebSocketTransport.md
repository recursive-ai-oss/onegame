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
| [`Secure(Func<INetConnection>, NetSecurity?, WebSocketOptions?, NetClock?)`](#m-secure-func-inetconnection-netsecurity-websocketoptions-netclock) | Applies the security setting to a connection something else opens (the browser's WebSocket): Required wraps it in the encrypted records, Insecure returns it as it is. |

### <a id="m-connect-ipendpoint-websocketoptions-netclock"></a>Connect(IPEndPoint, WebSocketOptions?, NetClock?)

```csharp
public static INetConnection Connect(IPEndPoint remote, WebSocketOptions? options = null, NetClock? clock = null);
```

Connects to a WebSocket server. The connection starts in [`ConnectionState.Connecting`](ConnectionState.md#m-connecting); messages sent now go out when it connects.

**Parameters**

- `remote` ([`IPEndPoint`](https://learn.microsoft.com/dotnet/api/system.net.ipendpoint)): The server's address.
- `options` ([`WebSocketOptions`](WebSocketOptions.md)?): Settings; [`WebSocketOptions.Security`](WebSocketOptions.md#m-security) is required ([`NetSecurity.ForClient()`](NetSecurity.md#m-forclient-ienumerable-peerkey-string-netkeypair-string), or [`NetSecurity.Insecure`](NetSecurity.md#m-insecure) for tests and local tools).
- `clock` ([`NetClock`](NetClock.md)?): Time source; the real clock if null.

**Returns:** [`INetConnection`](INetConnection.md)

**Exceptions**

- [`SecurityNotConfiguredException`](SecurityNotConfiguredException.md): No security was chosen, or a Required client trusts no server key.

### <a id="m-listen-ipendpoint-websocketoptions-netclock"></a>Listen(IPEndPoint, WebSocketOptions?, NetClock?)

```csharp
public static INetListener Listen(IPEndPoint bind, WebSocketOptions? options = null, NetClock? clock = null);
```

Starts listening for WebSocket clients on a TCP port.

**Parameters**

- `bind` ([`IPEndPoint`](https://learn.microsoft.com/dotnet/api/system.net.ipendpoint)): The local address and port.
- `options` ([`WebSocketOptions`](WebSocketOptions.md)?): Settings; [`WebSocketOptions.Security`](WebSocketOptions.md#m-security) is required ([`NetSecurity.ForServer()`](NetSecurity.md#m-forserver-netkeypair-string-netkeypair), or [`NetSecurity.Insecure`](NetSecurity.md#m-insecure) for tests and local tools).
- `clock` ([`NetClock`](NetClock.md)?): Time source; the real clock if null.

**Returns:** [`INetListener`](INetListener.md)

**Exceptions**

- [`SecurityNotConfiguredException`](SecurityNotConfiguredException.md): No security was chosen, or a Required listener has no server key.

### <a id="m-secure-func-inetconnection-netsecurity-websocketoptions-netclock"></a>Secure(Func&lt;INetConnection&gt;, NetSecurity?, WebSocketOptions?, NetClock?)

```csharp
public static INetConnection Secure(Func<INetConnection> open, NetSecurity? security, WebSocketOptions? options = null, NetClock? clock = null);
```

Applies the security setting to a connection something else opens (the browser's WebSocket): Required wraps it in the encrypted records, Insecure returns it as it is.

**Parameters**

- `open` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)&lt;[`INetConnection`](INetConnection.md)&gt;): Opens a fresh connection; called again when the server rejects the key tried and another is trusted.
- `security` ([`NetSecurity`](NetSecurity.md)?): The client's security setting.
- `options` ([`WebSocketOptions`](WebSocketOptions.md)?): Limits and timeouts; defaults if null.
- `clock` ([`NetClock`](NetClock.md)?): Time source; the real clock if null.

**Returns:** [`INetConnection`](INetConnection.md)

**Exceptions**

- [`SecurityNotConfiguredException`](SecurityNotConfiguredException.md): No security was chosen, or a Required client trusts no server key.
