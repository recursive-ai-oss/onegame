[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# UdpTransport static class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)

```csharp
public static class UdpTransport
```

Creates reliable-UDP listeners and connections. Nothing runs on its own: call [`INetConnection.Pump()`](INetConnection.md#m-pump) on connections you opened and [`INetListener.Pump()`](INetListener.md#m-pump) on listeners, from the thread that owns them (a game loop).

**Example**

```csharp
using var listener = UdpTransport.Listen(new IPEndPoint(IPAddress.Loopback, 0));
var port = ((IPEndPoint)listener.LocalEndPoint).Port;
var client = UdpTransport.Connect(new IPEndPoint(IPAddress.Loopback, port));
client.Send("hello"u8);
INetConnection? server = null;
while (server is null)
{
    client.Pump();
    listener.Pump();
    if (listener.TryAccept(out var accepted)) { server = accepted; }
}

ReadOnlySpan<byte> message;
while (!server.TryReceive(out message))
{
    client.Pump();
    listener.Pump();
}

Console.WriteLine(System.Text.Encoding.UTF8.GetString(message));   // hello

client.Disconnect();
```

## Methods

| Member | Description |
| --- | --- |
| [`Connect(IPEndPoint, UdpOptions?, NetClock?)`](#m-connect-ipendpoint-udpoptions-netclock) | Connects to a listener. |
| [`Connect(IDatagramSocket, SocketAddress, UdpOptions?, NetClock?)`](#m-connect-idatagramsocket-socketaddress-udpoptions-netclock) | Connects through a given socket (a memory or conditioned one). |
| [`Listen(IDatagramSocket, UdpOptions?, NetClock?)`](#m-listen-idatagramsocket-udpoptions-netclock) | Starts listening on a given socket (a memory or conditioned one). |
| [`Listen(IPEndPoint, UdpOptions?, NetClock?)`](#m-listen-ipendpoint-udpoptions-netclock) | Starts listening on an operating-system socket. |

### <a id="m-connect-ipendpoint-udpoptions-netclock"></a>Connect(IPEndPoint, UdpOptions?, NetClock?)

```csharp
public static UdpConnection Connect(IPEndPoint remote, UdpOptions? options = null, NetClock? clock = null);
```

Connects to a listener. The connection starts in [`ConnectionState.Connecting`](ConnectionState.md#m-connecting); messages sent now go out when it connects.

**Parameters**

- `remote` ([`IPEndPoint`](https://learn.microsoft.com/dotnet/api/system.net.ipendpoint)): The server's address.
- `options` ([`UdpOptions`](UdpOptions.md)?): Protocol settings; defaults if null.
- `clock` ([`NetClock`](NetClock.md)?): Time source; the real clock if null.

**Returns:** [`UdpConnection`](UdpConnection.md)

### <a id="m-connect-idatagramsocket-socketaddress-udpoptions-netclock"></a>Connect(IDatagramSocket, SocketAddress, UdpOptions?, NetClock?)

```csharp
public static UdpConnection Connect(IDatagramSocket socket, SocketAddress remote, UdpOptions? options = null, NetClock? clock = null);
```

Connects through a given socket (a memory or conditioned one). The connection disposes it when it closes.

**Parameters**

- `socket` ([`IDatagramSocket`](IDatagramSocket.md)): The datagram socket to send through.
- `remote` ([`SocketAddress`](https://learn.microsoft.com/dotnet/api/system.net.socketaddress)): The server's address.
- `options` ([`UdpOptions`](UdpOptions.md)?): Protocol settings; defaults if null.
- `clock` ([`NetClock`](NetClock.md)?): Time source; the real clock if null.

**Returns:** [`UdpConnection`](UdpConnection.md)

### <a id="m-listen-idatagramsocket-udpoptions-netclock"></a>Listen(IDatagramSocket, UdpOptions?, NetClock?)

```csharp
public static UdpListener Listen(IDatagramSocket socket, UdpOptions? options = null, NetClock? clock = null);
```

Starts listening on a given socket (a memory or conditioned one). The listener does not dispose it.

**Parameters**

- `socket` ([`IDatagramSocket`](IDatagramSocket.md)): The datagram socket, for example a [`ConditionedDatagramSocket`](ConditionedDatagramSocket.md) that adds latency and loss in a test.
- `options` ([`UdpOptions`](UdpOptions.md)?): Protocol settings; defaults if null.
- `clock` ([`NetClock`](NetClock.md)?): Time source; the real clock if null.

**Returns:** [`UdpListener`](UdpListener.md)

### <a id="m-listen-ipendpoint-udpoptions-netclock"></a>Listen(IPEndPoint, UdpOptions?, NetClock?)

```csharp
public static UdpListener Listen(IPEndPoint bind, UdpOptions? options = null, NetClock? clock = null);
```

Starts listening on an operating-system socket.

**Parameters**

- `bind` ([`IPEndPoint`](https://learn.microsoft.com/dotnet/api/system.net.ipendpoint)): The local address and port.
- `options` ([`UdpOptions`](UdpOptions.md)?): Protocol settings; defaults if null.
- `clock` ([`NetClock`](NetClock.md)?): Time source; the real clock if null.

**Returns:** [`UdpListener`](UdpListener.md)
