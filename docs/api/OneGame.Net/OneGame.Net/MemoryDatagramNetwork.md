[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# MemoryDatagramNetwork class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)

```csharp
public sealed class MemoryDatagramNetwork
```

An in-process datagram network: sockets that deliver to each other by address, with no operating system involved. Tests use it to inject datagrams from any source address.

## Constructors

| Member | Description |
| --- | --- |
| [`MemoryDatagramNetwork()`](#m-ctor) |  |

### <a id="m-ctor"></a>MemoryDatagramNetwork()

```csharp
public MemoryDatagramNetwork();
```

## Properties

| Member | Description |
| --- | --- |
| [`Undeliverable`](#m-undeliverable) | Datagrams that found no socket to receive them. |

### <a id="m-undeliverable"></a>Undeliverable

```csharp
public long Undeliverable { get; }
```

Datagrams that found no socket to receive them.

**Type:** `long`

## Methods

| Member | Description |
| --- | --- |
| [`CreateSocket()`](#m-createsocket) | Creates a socket at a fresh loopback address. |
| [`CreateSocket(int)`](#m-createsocket-int) | Creates a socket on a given loopback port. |
| [`Inject(ReadOnlySpan<byte>, SocketAddress, SocketAddress)`](#m-inject-readonlyspan-byte-socketaddress-socketaddress) | Delivers a datagram as if `from` had sent it, whether or not such a socket exists. |

### <a id="m-createsocket"></a>CreateSocket()

```csharp
public IDatagramSocket CreateSocket();
```

Creates a socket at a fresh loopback address.

**Returns:** [`IDatagramSocket`](IDatagramSocket.md)

### <a id="m-createsocket-int"></a>CreateSocket(int)

```csharp
public IDatagramSocket CreateSocket(int port);
```

Creates a socket on a given loopback port.

**Parameters**

- `port` (`int`): The port; must be unused on this network.

**Returns:** [`IDatagramSocket`](IDatagramSocket.md)

### <a id="m-inject-readonlyspan-byte-socketaddress-socketaddress"></a>Inject(ReadOnlySpan&lt;byte&gt;, SocketAddress, SocketAddress)

```csharp
public void Inject(ReadOnlySpan<byte> datagram, SocketAddress from, SocketAddress destination);
```

Delivers a datagram as if `from` had sent it, whether or not such a socket exists. This is how tests play spoofed sources.

**Parameters**

- `datagram` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The bytes.
- `from` ([`SocketAddress`](https://learn.microsoft.com/dotnet/api/system.net.socketaddress)): The claimed sender.
- `destination` ([`SocketAddress`](https://learn.microsoft.com/dotnet/api/system.net.socketaddress)): The receiving socket's address.
