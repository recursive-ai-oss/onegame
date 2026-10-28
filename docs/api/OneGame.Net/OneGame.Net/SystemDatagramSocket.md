[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# SystemDatagramSocket class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)  
**Implements:** [`IDatagramSocket`](IDatagramSocket.md), [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class SystemDatagramSocket : IDatagramSocket, IDisposable
```

A non-blocking operating-system UDP socket.

## Constructors

| Member | Description |
| --- | --- |
| [`SystemDatagramSocket(IPEndPoint)`](#m-ctor-ipendpoint) | Binds a socket. |

### <a id="m-ctor-ipendpoint"></a>SystemDatagramSocket(IPEndPoint)

```csharp
public SystemDatagramSocket(IPEndPoint bind);
```

Binds a socket.

**Parameters**

- `bind` ([`IPEndPoint`](https://learn.microsoft.com/dotnet/api/system.net.ipendpoint)): The local address; port 0 picks a free port.

## Properties

| Member | Description |
| --- | --- |
| [`LocalAddress`](#m-localaddress) | The address this socket receives on. |

### <a id="m-localaddress"></a>LocalAddress

```csharp
public SocketAddress LocalAddress { get; }
```

The address this socket receives on.

**Type:** [`SocketAddress`](https://learn.microsoft.com/dotnet/api/system.net.socketaddress)

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) |  |
| [`Receive(Span<byte>, SocketAddress)`](#m-receive-span-byte-socketaddress) | Takes the next waiting datagram into `buffer` and writes the sender into `from`; returns its length, or -1 if none is waiting. |
| [`Send(ReadOnlySpan<byte>, SocketAddress)`](#m-send-readonlyspan-byte-socketaddress) | Sends one datagram. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-receive-span-byte-socketaddress"></a>Receive(Span&lt;byte&gt;, SocketAddress)

```csharp
public int Receive(Span<byte> buffer, SocketAddress from);
```

Takes the next waiting datagram into `buffer` and writes the sender into `from`; returns its length, or -1 if none is waiting.

**Parameters**

- `buffer` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`)
- `from` ([`SocketAddress`](https://learn.microsoft.com/dotnet/api/system.net.socketaddress))

**Returns:** `int`

### <a id="m-send-readonlyspan-byte-socketaddress"></a>Send(ReadOnlySpan&lt;byte&gt;, SocketAddress)

```csharp
public void Send(ReadOnlySpan<byte> datagram, SocketAddress destination);
```

Sends one datagram. A datagram the network cannot take is dropped, as UDP allows.

**Parameters**

- `datagram` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)
- `destination` ([`SocketAddress`](https://learn.microsoft.com/dotnet/api/system.net.socketaddress))
