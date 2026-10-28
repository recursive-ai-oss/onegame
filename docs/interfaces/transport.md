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
var server = UdpTransport.Listen(new IPEndPoint(IPAddress.Any, 7777), new UdpOptions { Security = NetSecurity.ForServer(NetKeyPair.Load("server.key"), "my-game/1") });
var client = UdpTransport.Connect(new IPEndPoint(host, 7777), new UdpOptions { Security = NetSecurity.ForClient([serverPublicKey], "my-game/1", myIdentity) });
// NetSecurity.Insecure is the explicit opt-out for tests and local tools. A null Security is an error (SecurityNotConfiguredException).
// each frame: server.Pump(); client.Pump(); if (server.TryAccept(out var c)) ...
```

`UdpConnection` adds `SendUnreliable`/`TryReceiveUnreliable`: one datagram (`MaxUnreliableBytes`), sent once, duplicates dropped, no order. `UdpOptions` has the limits (MTU 1200, `MaxMessageBytes` 1 MiB, queues, `ReceiveWindow`, timeouts, `MinCongestionWindowPackets`). **Both ends must use the same `Mtu` and `MaxMessageBytes`**; a peer that sends a longer message is disconnected with `ProtocolViolation`.

Datagram formats (little endian). Byte 0 = `version << 4 | kind`. Version 2 is the encrypted protocol (the default; `Security` is required); version 1 is the plaintext protocol, spoken only by an `Insecure` listener or client. A Required end that receives the other version answers `Reject(SecurityRequired)` in the version the peer speaks and never falls back, so a path attacker cannot talk a client into plaintext. Any other version is dropped (a request with one gets `Reject(VersionMismatch)`). The layouts below are version 1; version 2 differs where `transport-security.md` section 4 says and the next table shows.

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

**Randomness and hashing.** Nothing in `OneGame.Net` calls `System.Security.Cryptography`: on Android the runtime's providers need a system crypto library the app does not have, and the process aborts on first use. The cookie's HMAC-SHA-256 (RFC 2104 over FIPS 180-4) and the WebSocket accept key's SHA-1 are written out in `OneGame.Core` and `Sha1.cs`, and tests compare them with the platform's own hashes and the RFC vectors, so cookies and handshakes are byte-identical to what earlier builds sent (an old client against a new server, and the reverse, play a full Lanterns cluster). Connection ids, the cookie secret and WebSocket masking keys come from `OneGame.Core.Entropy.Fill`. Its default source is `/dev/urandom` on Linux, Android and Apple systems (the kernel's generator, or `arc4random` on Apple), and the runtime's own provider on Windows, where it is part of the OS. A platform without that device (the browser) installs its own with `Entropy.Use(IEntropySource)` before the first connection, for example one over `crypto.getRandomValues`. Entropy is for connections only: simulation code still draws from named streams, and the analyzer reports `Entropy` like `Random` (OG0007).

**Version 2 datagrams (encrypted).** Handshake: Noise `IK` (`Noise_IK_25519_ChaChaPoly_SHA256`) carried inside the cookie exchange, so a spoofed address still costs the server a hash and no memory. The prologue is `"onegame/2" 00 gameId 00 version nonce cookie`: a different game id, version, nonce or cookie fails the handshake.

| Kind | Layout (version 2) | Notes |
|---|---|---|
| 0 Request, 1 Challenge | as version 1 | Same sizes and cookie. |
| 2 Response (c→s) | `b0, nonce, cookie[16], len u16, noise message 1`, padded to ≥ 256 | Message 1 is `e, es, s, ss`: the client's ephemeral key and its static key sealed to the server's. Checked after the cookie, the tombstone list, capacity and the handshake budgets, and before any state exists beyond an *unconfirmed* connection. |
| 3 Accept (s→c) | `b0, nonce, connId u32, len u16, noise message 2` | Message 2 (`e, ee, se`) carries the reset token (16 bytes) and the connection id again; the client refuses an Accept whose clear id differs. A repeated Response is answered with the same Accept. |
| 4 Reject | as version 1 | Adds `SecurityRequired` and `HandshakeFailed` (the server holds no key the client trusts, or the game ids differ). Unauthenticated, so a client treats both as permanent only for the key it tried: it tries its other trusted keys first, then gives up. |
| 5 Data | `b0, connId u32, packetNo u32` (9 bytes, the AEAD's additional data), then ciphertext of `ackBase, sack, window, frames…` and a 16-byte tag | ChaCha20-Poly1305, nonce = four zero bytes and the packet number, one key per direction. Overhead 16 bytes per packet, and acks are inside the ciphertext, so they cannot be forged. Packet numbers strictly increase, retransmissions get a new number, and the receiver's own 64-packet window drops replays before any decryption. |
| 6 Disconnect | `b0, connId u32, sealed(reason, detail)` | Sealed under a nonce no packet number can reach (2^32); only the holder of the keys can close a session. |
| 7 Reset (s→c) | `b0, connId u32, token[16]` | Answer to Data for a connection id the server never had (it restarted). The token is an HMAC of the id under a key derived from the server's key, so only the real server can make it; the client compares in constant time. Rate limited. |

A server surfaces a connection (`TryAccept`) and sends it data only after the first authenticated packet from the client, so a replayed message 1 never gives the game a connection or makes the server send to a victim address. Until then the handshake is *unconfirmed*: it has its own budget (`MaxUnconfirmed`, `MaxUnconfirmedPerSource`, `UnconfirmedTimeoutMs`) and does not count against `MaxConnections`. A finished handshake's `(nonce, address)` stays on a tombstone list for as long as its cookie is valid, so a captured Response cannot start a second one. Handshakes are limited per source (an IPv4 address, or an IPv6 /64), by a shared budget, and by a reserved budget for sources that recently completed a session, all checked before any Diffie-Hellman work (`UdpOptions.MaxHandshakesPer…`). `INetConnection.RemoteKey` is the peer's proven public key, `IsEncrypted` says the link is secure, and `ChannelBinding` is a per-session value for binding login proofs (see `transport-security.md` section 6).

**What a party on the path can do with version 2:** see everything about the traffic except its content (sizes, timing, addresses and the connection id); drop, delay or reorder datagrams; replay an old Response (ignored); stop a handshake by forging or dropping a reply, which a client survives (a reply that fails to authenticate is thrown away and the real one is still accepted); close nothing (a forged Disconnect or Reset fails its tag). They cannot read or alter messages, inject or replay data, impersonate the server to a client that pins its key, or learn a client's static key (it is sent encrypted to the server's key; a later theft of the server key does expose first messages captured earlier). On version 1, which `Insecure` selects, they can do everything the old list allowed: read, inject, replay once, and forge closes. WebSocket is covered in its own section.

## WebSocket (`WebSocketTransport`, `WebSocketListener`, `WebSocketConnection`)

RFC 6455 over plain TCP (no TLS in the engine: for `wss://` terminate it in front, see `transport-security.md`). With `Security` Required the messages carry Noise records, described there; this section is the plain layer underneath, and `Insecure` is exactly this layer. Binary messages only; a text frame closes the connection with 1003. Client frames are masked with a fresh random key, server frames are not; the wrong direction closes with 1002. The parser also rejects reserved bits and opcodes, fragmented or oversize control frames, non-minimal lengths, a continuation with nothing to continue, a new message inside a fragmented one, bad close payloads and non-UTF-8 close reasons. A message over `MaxMessageBytes` is refused (1009) from its frame header, before any payload byte is stored. Long outgoing messages are split into continuation frames (`SendFragmentBytes`) so pings and closes interleave. Pings are answered; the library pings when idle and measures RTT from the pong. The closing handshake carries the reason: registered codes where one fits (1000, 1001, 1002, 1009) and `4000 + reason` otherwise.

The opening handshake accepts only a well-formed `GET … HTTP/1.1` upgrade (Host, `Upgrade: websocket`, `Connection: Upgrade`, a 16-byte base64 key, version 13); anything else gets 400, a wrong version gets 426, a browser `Origin` outside `AllowedOrigins` (when set) gets 403. Header blocks are capped at `MaxHeaderBytes`; at most `MaxPendingHandshakes` sockets wait in the handshake and each is dropped after `HandshakeTimeoutMs`. SHA-1 for the accept value is written out in `Sha1.cs` (it is a handshake check, not security).

## Network conditioner (for tests)

`ConditionedDatagramSocket` wraps any `IDatagramSocket` (system or in-memory) and applies `NetworkConditions` to what it sends: latency, jitter, loss, duplication, reordering (a held-back datagram), a bandwidth cap with a queue limit, all seeded. `ConditionedConnection` / `ConditionedListener` wrap any `INetConnection` (WebSocket): latency, jitter and bandwidth act as on the wire; loss becomes a retransmission-timeout delay, and order and duplicates are untouched, because TCP already hides them. `MemoryDatagramNetwork` is an in-process datagram network whose `Inject` can claim any source address.

## Using it with `Node`

`Node.ConnectVia(channel)` (client) and `Node.Accept(channel, trust)` (server) take any `IMessageChannel`, so a connection from either transport plugs in directly. When a connection closes the server calls `Node.Detach(connection)`; a client that wants the server back opens a new connection and calls `Node.Reconnect(link, connection)` (`RemoteSession` does both). Messages over `MaxMessageBytes` never reach the transport from the sync layer: snapshots and frames are split below `ClusterOptions.PieceBytes` (`sync.md`).
