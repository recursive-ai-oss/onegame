[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# NetStats struct

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)

```csharp
public struct NetStats
```

Counters and estimates for one connection. All values are since the connection was created.

## Properties

| Member | Description |
| --- | --- |
| [`BytesReceived`](#m-bytesreceived) | Bytes read from the socket and accepted. |
| [`BytesSent`](#m-bytessent) | Bytes written to the socket. |
| [`CongestionWindowBytes`](#m-congestionwindowbytes) | Current congestion window in bytes (UDP only). |
| [`InFlightBytes`](#m-inflightbytes) | Bytes sent and awaiting acknowledgement. |
| [`InboundLoss`](#m-inboundloss) | Estimated share of the peer's datagrams that never arrived (0 to 1), from gaps in packet numbers. |
| [`MessagesReceived`](#m-messagesreceived) | Whole messages handed out by TryReceive or waiting for it. |
| [`MessagesSent`](#m-messagessent) | Whole messages queued by Send. |
| [`OutboundLoss`](#m-outboundloss) | Share of reliable fragment transmissions that were repeats (0 to 1): a proxy for loss of our own datagrams. |
| [`PacketsDropped`](#m-packetsdropped) | Datagrams dropped as malformed, duplicate or stale. |
| [`PacketsReceived`](#m-packetsreceived) | Datagrams or frames accepted. |
| [`PacketsSent`](#m-packetssent) | Datagrams (UDP) or frames (WebSocket) sent. |
| [`Retransmits`](#m-retransmits) | Reliable fragments sent again (timeout or fast retransmit). |
| [`RttMs`](#m-rttms) | Smoothed round-trip time in milliseconds; 0 until measured. |
| [`RttVarMs`](#m-rttvarms) | Round-trip time variation in milliseconds. |
| [`SendQueueBytes`](#m-sendqueuebytes) | Bytes of messages queued and not yet acknowledged. |

### <a id="m-bytesreceived"></a>BytesReceived

```csharp
public long BytesReceived { get; set; }
```

Bytes read from the socket and accepted.

**Type:** `long`

### <a id="m-bytessent"></a>BytesSent

```csharp
public long BytesSent { get; set; }
```

Bytes written to the socket.

**Type:** `long`

### <a id="m-congestionwindowbytes"></a>CongestionWindowBytes

```csharp
public int CongestionWindowBytes { get; set; }
```

Current congestion window in bytes (UDP only).

**Type:** `int`

### <a id="m-inflightbytes"></a>InFlightBytes

```csharp
public int InFlightBytes { get; set; }
```

Bytes sent and awaiting acknowledgement.

**Type:** `int`

### <a id="m-inboundloss"></a>InboundLoss

```csharp
public double InboundLoss { get; set; }
```

Estimated share of the peer's datagrams that never arrived (0 to 1), from gaps in packet numbers.

**Type:** `double`

### <a id="m-messagesreceived"></a>MessagesReceived

```csharp
public long MessagesReceived { get; set; }
```

Whole messages handed out by TryReceive or waiting for it.

**Type:** `long`

### <a id="m-messagessent"></a>MessagesSent

```csharp
public long MessagesSent { get; set; }
```

Whole messages queued by Send.

**Type:** `long`

### <a id="m-outboundloss"></a>OutboundLoss

```csharp
public double OutboundLoss { get; set; }
```

Share of reliable fragment transmissions that were repeats (0 to 1): a proxy for loss of our own datagrams.

**Type:** `double`

### <a id="m-packetsdropped"></a>PacketsDropped

```csharp
public long PacketsDropped { get; set; }
```

Datagrams dropped as malformed, duplicate or stale.

**Type:** `long`

### <a id="m-packetsreceived"></a>PacketsReceived

```csharp
public long PacketsReceived { get; set; }
```

Datagrams or frames accepted.

**Type:** `long`

### <a id="m-packetssent"></a>PacketsSent

```csharp
public long PacketsSent { get; set; }
```

Datagrams (UDP) or frames (WebSocket) sent.

**Type:** `long`

### <a id="m-retransmits"></a>Retransmits

```csharp
public long Retransmits { get; set; }
```

Reliable fragments sent again (timeout or fast retransmit).

**Type:** `long`

### <a id="m-rttms"></a>RttMs

```csharp
public double RttMs { get; set; }
```

Smoothed round-trip time in milliseconds; 0 until measured.

**Type:** `double`

### <a id="m-rttvarms"></a>RttVarMs

```csharp
public double RttVarMs { get; set; }
```

Round-trip time variation in milliseconds.

**Type:** `double`

### <a id="m-sendqueuebytes"></a>SendQueueBytes

```csharp
public int SendQueueBytes { get; set; }
```

Bytes of messages queued and not yet acknowledged.

**Type:** `int`
