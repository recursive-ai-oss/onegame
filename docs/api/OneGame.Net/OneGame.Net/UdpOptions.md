[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# UdpOptions class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)

```csharp
public sealed class UdpOptions
```

Settings for the reliable-UDP transport. Both ends should use the same [`UdpOptions.MaxMessageBytes`](UdpOptions.md#m-maxmessagebytes); a peer that sends more is disconnected.

## Constructors

| Member | Description |
| --- | --- |
| [`UdpOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>UdpOptions()

```csharp
public UdpOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`ConnectRetryMs`](#m-connectretryms) | Client handshake packets repeat at this interval. |
| [`ConnectTimeoutMs`](#m-connecttimeoutms) | Give up on the handshake after this long. |
| [`KeepAliveMs`](#m-keepalivems) | Send an empty packet after this long without sending anything. |
| [`LingerMs`](#m-lingerms) | A flushing disconnect waits at most this long for the peer to acknowledge queued data. |
| [`MaxConnections`](#m-maxconnections) | Listener only: connections allowed at once. |
| [`MaxDatagramsPerPump`](#m-maxdatagramsperpump) | Most datagrams read from one socket per pump, so a flood cannot starve the game loop. |
| [`MaxMessageBytes`](#m-maxmessagebytes) | Largest message accepted by Send and from the wire. |
| [`MaxReceiveQueueBytes`](#m-maxreceivequeuebytes) | Most completed-message bytes waiting for TryReceive. |
| [`MaxRtoMs`](#m-maxrtoms) | Upper bound of the retransmission timeout before backoff. |
| [`MaxSendQueueBytes`](#m-maxsendqueuebytes) | Most message bytes queued for sending (including unacknowledged). |
| [`MinCongestionWindowPackets`](#m-mincongestionwindowpackets) | The congestion window never shrinks below this many datagrams, and starts there. |
| [`MinRtoMs`](#m-minrtoms) | Lower bound of the retransmission timeout. |
| [`Mtu`](#m-mtu) | Largest datagram written to the socket, in bytes. |
| [`ReceiveWindow`](#m-receivewindow) | How many fragments past the delivered point the receiver will hold out of order. |
| [`TimeoutMs`](#m-timeoutms) | Close with [`DisconnectReason.Timeout`](DisconnectReason.md#m-timeout) after this long without a valid packet. |

### <a id="m-connectretryms"></a>ConnectRetryMs

```csharp
public int ConnectRetryMs { get; init; }
```

Client handshake packets repeat at this interval.

**Type:** `int`

### <a id="m-connecttimeoutms"></a>ConnectTimeoutMs

```csharp
public int ConnectTimeoutMs { get; init; }
```

Give up on the handshake after this long.

**Type:** `int`

### <a id="m-keepalivems"></a>KeepAliveMs

```csharp
public int KeepAliveMs { get; init; }
```

Send an empty packet after this long without sending anything.

**Type:** `int`

### <a id="m-lingerms"></a>LingerMs

```csharp
public int LingerMs { get; init; }
```

A flushing disconnect waits at most this long for the peer to acknowledge queued data.

**Type:** `int`

### <a id="m-maxconnections"></a>MaxConnections

```csharp
public int MaxConnections { get; init; }
```

Listener only: connections allowed at once.

**Type:** `int`

### <a id="m-maxdatagramsperpump"></a>MaxDatagramsPerPump

```csharp
public int MaxDatagramsPerPump { get; init; }
```

Most datagrams read from one socket per pump, so a flood cannot starve the game loop.

**Type:** `int`

### <a id="m-maxmessagebytes"></a>MaxMessageBytes

```csharp
public int MaxMessageBytes { get; init; }
```

Largest message accepted by Send and from the wire.

**Type:** `int`

### <a id="m-maxreceivequeuebytes"></a>MaxReceiveQueueBytes

```csharp
public int MaxReceiveQueueBytes { get; init; }
```

Most completed-message bytes waiting for TryReceive. Past it the receiver stops acknowledging new data, which stalls the sender instead of growing memory.

**Type:** `int`

### <a id="m-maxrtoms"></a>MaxRtoMs

```csharp
public int MaxRtoMs { get; init; }
```

Upper bound of the retransmission timeout before backoff.

**Type:** `int`

### <a id="m-maxsendqueuebytes"></a>MaxSendQueueBytes

```csharp
public int MaxSendQueueBytes { get; init; }
```

Most message bytes queued for sending (including unacknowledged). Past it the connection closes with [`DisconnectReason.SendQueueFull`](DisconnectReason.md#m-sendqueuefull).

**Type:** `int`

### <a id="m-mincongestionwindowpackets"></a>MinCongestionWindowPackets

```csharp
public int MinCongestionWindowPackets { get; init; }
```

The congestion window never shrinks below this many datagrams, and starts there. Game traffic is small and steady, and random loss on a wireless link is not congestion, so the usual collapse to one or two packets after a loss would starve a sync stream.

**Type:** `int`

### <a id="m-minrtoms"></a>MinRtoMs

```csharp
public int MinRtoMs { get; init; }
```

Lower bound of the retransmission timeout.

**Type:** `int`

### <a id="m-mtu"></a>Mtu

```csharp
public int Mtu { get; init; }
```

Largest datagram written to the socket, in bytes. 1200 fits every IPv6 path (minimum link MTU 1280). Range 576 to 1472.

**Type:** `int`

### <a id="m-receivewindow"></a>ReceiveWindow

```csharp
public int ReceiveWindow { get; init; }
```

How many fragments past the delivered point the receiver will hold out of order. A power of two from 16 to 4096.

**Type:** `int`

### <a id="m-timeoutms"></a>TimeoutMs

```csharp
public int TimeoutMs { get; init; }
```

Close with [`DisconnectReason.Timeout`](DisconnectReason.md#m-timeout) after this long without a valid packet.

**Type:** `int`
