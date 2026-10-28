[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# ConditionedDatagramSocket class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)  
**Implements:** [`IDatagramSocket`](IDatagramSocket.md), [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class ConditionedDatagramSocket : IDatagramSocket, IDisposable
```

Wraps a datagram socket and applies [`NetworkConditions`](NetworkConditions.md) to what it sends. Delayed datagrams go out when the socket is next used, so keep pumping.

## Constructors

| Member | Description |
| --- | --- |
| [`ConditionedDatagramSocket(IDatagramSocket, NetworkConditions, NetClock, bool)`](#m-ctor-idatagramsocket-networkconditions-netclock-bool) | Wraps `inner`. |

### <a id="m-ctor-idatagramsocket-networkconditions-netclock-bool"></a>ConditionedDatagramSocket(IDatagramSocket, NetworkConditions, NetClock, bool)

```csharp
public ConditionedDatagramSocket(IDatagramSocket inner, NetworkConditions conditions, NetClock clock, bool ownsInner = true);
```

Wraps `inner`.

**Parameters**

- `inner` ([`IDatagramSocket`](IDatagramSocket.md)): The real socket.
- `conditions` ([`NetworkConditions`](NetworkConditions.md)): What to do to traffic.
- `clock` ([`NetClock`](NetClock.md)): The clock the delays follow; share it with the transports above.
- `ownsInner` (`bool`): Dispose `inner` with this socket.

## Properties

| Member | Description |
| --- | --- |
| [`Dropped`](#m-dropped) | Datagrams dropped on purpose (loss or a full link). |
| [`Duplicated`](#m-duplicated) | Datagrams delivered twice on purpose. |
| [`LocalAddress`](#m-localaddress) | The address this socket receives on. |
| [`Reordered`](#m-reordered) | Datagrams held back on purpose. |

### <a id="m-dropped"></a>Dropped

```csharp
public long Dropped { get; }
```

Datagrams dropped on purpose (loss or a full link).

**Type:** `long`

### <a id="m-duplicated"></a>Duplicated

```csharp
public long Duplicated { get; }
```

Datagrams delivered twice on purpose.

**Type:** `long`

### <a id="m-localaddress"></a>LocalAddress

```csharp
public SocketAddress LocalAddress { get; }
```

The address this socket receives on.

**Type:** [`SocketAddress`](https://learn.microsoft.com/dotnet/api/system.net.socketaddress)

### <a id="m-reordered"></a>Reordered

```csharp
public long Reordered { get; }
```

Datagrams held back on purpose.

**Type:** `long`

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
