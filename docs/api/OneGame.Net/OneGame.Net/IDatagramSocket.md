[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# IDatagramSocket interface

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public interface IDatagramSocket : IDisposable
```

The datagram socket the UDP transport talks to. The real one is [`SystemDatagramSocket`](SystemDatagramSocket.md); tests use [`MemoryDatagramNetwork`](MemoryDatagramNetwork.md) sockets, and [`ConditionedDatagramSocket`](ConditionedDatagramSocket.md) wraps either.

## Properties

| Member | Description |
| --- | --- |
| [`LocalAddress`](#m-localaddress) | The address this socket receives on. |

### <a id="m-localaddress"></a>LocalAddress

```csharp
SocketAddress LocalAddress { get; }
```

The address this socket receives on.

**Type:** [`SocketAddress`](https://learn.microsoft.com/dotnet/api/system.net.socketaddress)

## Methods

| Member | Description |
| --- | --- |
| [`Receive(Span<byte>, SocketAddress)`](#m-receive-span-byte-socketaddress) | Takes the next waiting datagram into `buffer` and writes the sender into `from`; returns its length, or -1 if none is waiting. |
| [`Send(ReadOnlySpan<byte>, SocketAddress)`](#m-send-readonlyspan-byte-socketaddress) | Sends one datagram. |

### <a id="m-receive-span-byte-socketaddress"></a>Receive(Span&lt;byte&gt;, SocketAddress)

```csharp
int Receive(Span<byte> buffer, SocketAddress from);
```

Takes the next waiting datagram into `buffer` and writes the sender into `from`; returns its length, or -1 if none is waiting.

**Parameters**

- `buffer` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`)
- `from` ([`SocketAddress`](https://learn.microsoft.com/dotnet/api/system.net.socketaddress))

**Returns:** `int`

### <a id="m-send-readonlyspan-byte-socketaddress"></a>Send(ReadOnlySpan&lt;byte&gt;, SocketAddress)

```csharp
void Send(ReadOnlySpan<byte> datagram, SocketAddress destination);
```

Sends one datagram. A datagram the network cannot take is dropped, as UDP allows.

**Parameters**

- `datagram` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)
- `destination` ([`SocketAddress`](https://learn.microsoft.com/dotnet/api/system.net.socketaddress))
