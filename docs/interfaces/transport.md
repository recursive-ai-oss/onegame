# Transport: reliable UDP and WebSocket behind `IMessageChannel`

Source: `src/OneGame.Net`, namespace `OneGame.Net`. Depends only on `OneGame.Sync` (for `IMessageChannel`) and the .NET runtime sockets. Nothing here changes `IMessageChannel`, `Node` or `Cluster`.

Everything is pumped by the caller on its own thread: sockets are non-blocking and no thread is started. Nothing allocates per message once warm (buffers come from a size-classed pool).

## The common surface

```csharp
INetConnection : IMessageChannel      // Send(span), TryReceive(out span): whole, ordered, reliable messages
  State (Connecting|Connected|Closing|Closed), CloseInfo (Reason, ByRemote, Code), Stats, RemoteName
  event Connected, event Disconnected(conn, DisconnectInfo)
  Pump()                              // client-made connections must be pumped; accepted ones are pumped by their listener
  Disconnect(reason = Normal, flush = false)
INetListener : IDisposable            // LocalEndPoint, ConnectionCount, PendingHandshakes, TryAccept(out conn), Pump()
```

- `Send` before the handshake finishes queues; it goes out on connect. `Send` on a closing or closed connection is ignored (never throws). A message over `MaxMessageBytes` throws `MessageTooLargeException` (an `ArgumentException`) before anything is queued. A send queue past `MaxSendQueueBytes` closes the connection with `SendQueueFull`.
- `TryReceive`'s span is valid until the next call. An unread inbox stops the transport from taking more (UDP stops acknowledging, WebSocket stops reading), so memory stays bounded and the sender stalls instead.
- `Connected` fires on client connections; accepted connections are already connected when `TryAccept` returns them. `Disconnected` fires once, inside `Pump` or `Disconnect`. Messages that arrived before a close can still be read afterwards.
- `DisconnectReason` values travel on the wire and never change meaning. `Code` is the UDP detail byte or the WebSocket close code. `ByRemote` is false for timeouts and local protocol-violation closes.
- `NetStats`: `RttMs`, `RttVarMs`, packets/bytes/messages, `Retransmits`, `InboundLoss` (gaps in packet numbers), `OutboundLoss` (share of fragment sends that were repeats), queue and window sizes. `NetClock` (`NetClock.System`, `ManualNetClock`) is the only time source, so tests run network seconds instantly.

## Reliable UDP (`UdpTransport`, `UdpListener`, `UdpConnection`)

```csharp
var server = UdpTransport.Listen(new IPEndPoint(IPAddress.Any, 7777), new UdpOptions());
var client = UdpTransport.Connect(new IPEndPoint(host, 7777));
// each frame: server.Pump(); client.Pump(); if (server.TryAccept(out var c)) ...
```

`UdpConnection` adds `SendUnreliable`/`TryReceiveUnreliable`: one datagram (`MaxUnreliableBytes`), sent once, duplicates dropped, no order. `UdpOptions` has the limits (MTU 1200, `MaxMessageBytes` 1 MiB, queues, `ReceiveWindow`, timeouts, `MinCongestionWindowPackets`). **Both ends must use the same `Mtu` and `MaxMessageBytes`**; a peer that sends a longer message is disconnected with `ProtocolViolation`.

Datagram formats (little endian). Byte 0 = `version << 4 | kind`; the version is 1, and an unknown version is dropped (a request with one gets `Reject(VersionMismatch)`).

| Kind | Layout | Notes |
|---|---|---|
| 0 Request (c→s) | `b0, nonce u64`, zero padded to ≥ 256 bytes | The padding makes every reply smaller than the request. |
| 1 Challenge (s→c) | `b0, nonce, cookie[16]` | `cookie = HMAC-SHA256(secret, address ‖ nonce ‖ epoch)`, epoch = 10 s; the server stores nothing. |
| 2 Response (c→s) | `b0, nonce, cookie[16]`, padded to ≥ 256 | Valid for the current or previous epoch. Only now does a connection exist (random non-zero 32-bit id), limited by `MaxConnections`. |
| 3 Accept (s→c) | `b0, nonce, connId u32` | Repeated if the Response repeats. |
| 4 Reject (s→c) | `b0, nonce, reason` | `ServerFull`, `VersionMismatch`. |
| 5 Data | `b0, connId u32, packetNo u32, ackBase u32, sack u64, window u16, frames…` | `packetNo` drops duplicates and replays (64-wide window). `ackBase` is the next fragment expected, bit *i* of `sack` is fragment `ackBase+1+i`, `window` is the free reorder slots. |
| 6 Disconnect | `b0, connId u32, reason, detail` | Sent three times. A listener answers a Data datagram for a connection id it has never had with one Disconnect(`Shutdown`) for that id, so a client of a server that was killed and started again learns at its next keep-alive instead of at its timeout (a 7-byte reply to a datagram of at least the header size; a Disconnect is never answered). |

Frames in a Data packet: `0` ends; reliable fragment `0x01 | flags<<2, seq u32, len u16, bytes` (flags: 1 begin, 2 end); unreliable `0x02, len u16, bytes`. A message is a run of fragments from begin to end with consecutive sequence numbers; the receiver joins them in order. Frames are validated before anything is applied; a datagram with a bad frame changes no state.

Reliability: cumulative plus selective acks on every packet (a pure ack goes out only when nothing else does); a fragment is resent when its timer (RFC 6298 estimate, doubled per repeat) fires or when a datagram sent three or more datagrams after it was acknowledged; the congestion window starts at and never falls below `MinCongestionWindowPackets` (default 12) and otherwise follows slow start, additive increase and a 30 % cut at most once per round trip. Flow control is the receiver's free window. Keepalive after `KeepAliveMs` idle; `Timeout` after `TimeoutMs` without a valid new packet.

**Encryption and authentication: none yet.** Byte 0's version nibble and the end of every datagram leave room for a tag. What a party on the path can do today: read everything; drop, delay or replay datagrams (replays inside the 64-packet window or older are rejected, but a captured datagram can be injected once); forge a Disconnect for a connection once it has seen the connection id (ids are random, and the source address must also match, so an off-path attacker cannot); inject Data packets into a connection it has observed; hijack nothing before the cookie exchange (an off-path spoofer never learns a cookie). A spoofed-source flood costs the server hashing and one 25-byte reply per request, no memory (`PendingHandshakes` is always 0). Trust decisions stay with the sync layer's per-connection `Trust`; do not treat a UDP connection as authenticated until a later unit adds keys.

## WebSocket (`WebSocketTransport`, `WebSocketListener`, `WebSocketConnection`)

RFC 6455 over plain TCP (no TLS: terminate it in front, or add it later). Binary messages only; a text frame closes the connection with 1003. Client frames are masked with a fresh random key, server frames are not; the wrong direction closes with 1002. The parser also rejects reserved bits and opcodes, fragmented or oversize control frames, non-minimal lengths, a continuation with nothing to continue, a new message inside a fragmented one, bad close payloads and non-UTF-8 close reasons. A message over `MaxMessageBytes` is refused (1009) from its frame header, before any payload byte is stored. Long outgoing messages are split into continuation frames (`SendFragmentBytes`) so pings and closes interleave. Pings are answered; the library pings when idle and measures RTT from the pong. The closing handshake carries the reason: registered codes where one fits (1000, 1001, 1002, 1009) and `4000 + reason` otherwise.

The opening handshake accepts only a well-formed `GET … HTTP/1.1` upgrade (Host, `Upgrade: websocket`, `Connection: Upgrade`, a 16-byte base64 key, version 13); anything else gets 400, a wrong version gets 426, a browser `Origin` outside `AllowedOrigins` (when set) gets 403. Header blocks are capped at `MaxHeaderBytes`; at most `MaxPendingHandshakes` sockets wait in the handshake and each is dropped after `HandshakeTimeoutMs`. SHA-1 for the accept value is written out in `Sha1.cs` (it is a handshake check, not security).

## Network conditioner (for tests)

`ConditionedDatagramSocket` wraps any `IDatagramSocket` (system or in-memory) and applies `NetworkConditions` to what it sends: latency, jitter, loss, duplication, reordering (a held-back datagram), a bandwidth cap with a queue limit, all seeded. `ConditionedConnection` / `ConditionedListener` wrap any `INetConnection` (WebSocket): latency, jitter and bandwidth act as on the wire; loss becomes a retransmission-timeout delay, and order and duplicates are untouched, because TCP already hides them. `MemoryDatagramNetwork` is an in-process datagram network whose `Inject` can claim any source address.

## Using it with `Node`

`Node.ConnectTo` still builds loopback pairs only, and `Link`/`Connection` have internal constructors, so a node cannot yet be given a network channel. The tests bridge two in-process nodes without touching Sync (`NodeBridge` in `tests/OneGame.Net.Tests`: every message a node writes goes through the transport and comes back out on the other side). Multi-node sync needs a public way to attach an `IMessageChannel`; see the request in the unit report.
