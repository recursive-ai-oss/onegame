# Transport security

Status: the primitives, the Noise core and the UDP wiring are built and tested; servers, game clients, the `onegame` tool, clusters and the Lanterns sample use them. The WebSocket side is described in the WebSocket section of this file by whoever builds it. Section 11 lists what an independent review of the design found and what was done about each finding. `transport.md` has the datagram layouts.

## 1. What exists (`src/OneGame.Core/Crypto`, all `internal`)

| File | What | Specification |
|---|---|---|
| `X25519.cs` | `PublicKey`, `Agree` (false for an all-zero result) | RFC 7748 §5, §6.1 |
| `ChaCha20Poly1305.cs` | `Seal`/`Open` into a caller buffer, in place allowed, zero allocation | RFC 8439 §2 |
| `Hkdf.cs` | `Extract`/`Expand`, and the Noise HKDF | RFC 5869; Noise §4.3 |
| `Sha256.cs` (existing) | SHA-256, HMAC-SHA-256 | FIPS 180-4, RFC 2104 |
| `Noise.cs` | `NoiseCipherState`, `NoiseHandshake` for the 15 fundamental patterns, `Noise_*_25519_ChaChaPoly_SHA256` | Noise Protocol Framework rev 34 |

Nothing calls `System.Security.Cryptography`: phones lack the system library (see `transport.md`, randomness). Keys and ephemerals come from `Entropy.Fill`; a platform without a source must install one before the first handshake, and a missing source fails the connection rather than falling back to anything weaker.

**Constant time.** "Constant time" here means: no branch, no array index and no loop bound depends on a secret (keys, scalars, plaintext, MAC state), and the one secret-dependent decision, accepting or rejecting a tag, is made on a constant-time comparison. ChaCha20 is add/rotate/xor; Poly1305 and the X25519 field arithmetic are fixed-length integer multiply/add/shift (ten 25.5-bit limbs in `long`); the ladder swaps with a mask (`cswap`) and the inversion is a fixed addition chain; scalar bits are only ever used as mask inputs. Loops branch on public limb indices only. How it was checked: by reading each routine for the rule above, plus a dudect-style timing test (`CryptoTests.ConstantTimeSmokeCheck`, explicit, `dotnet <test app> -explicit only -method '*ConstantTimeSmokeCheck'`): X25519 with a sparse versus a random scalar, tag compare with the first versus the last tag byte wrong, and seal with a zero versus a random key all gave |t| below 1 against the usual 4.5 threshold (worker, loaded). That is evidence, not proof: the JIT is not obliged to keep branch-free source branch-free, and the WebAssembly interpreter has no timing guarantees (a network observer cannot resolve them, but a co-resident process could). A reviewer should read the generated code for `Poly1305.Finish`, `X25519.CSwap` and `HmacSha256.FixedTimeEquals`. Secrets held in stack buffers are wiped with a non-inlined clear on exit; `NoiseCipherState` keys live in a heap array wiped by `Clear`.

**Handshake core.** `new NoiseHandshake(pattern, initiator, prologue, staticPrivate, remoteStatic)` then `WriteMessage`/`ReadMessage` alternately, then `Split(out send, out receive)`. `ReadMessage` returns false for a short message, a failed tag, or a degenerate DH result (all-zero shared secret from a low-order key); the handshake is then dead and must be discarded (spec §5.3). It knows nothing about sockets, retransmission or framing. `NoiseCipherState` also has `SealAt`/`OpenAt` with an explicit counter for datagram transports (spec §11.4), and `Rekey`.

**Tested against** (`tests/OneGame.Core.Tests/CryptoTests.cs`, vectors in `testdata/crypto/`): RFC 8439 block, Poly1305, AEAD and Appendix A.5; RFC 7748 §5.2 (two vectors, 1 and 1000 iterations, and the million-iteration vector on request) and §6.1; RFC 5869 A.1 and A.3; Wycheproof `chacha20_poly1305` (325 cases: 256 valid, 60 invalid, 9 wrong-size refused by argument checks) and `x25519` (518 cases: 264 valid, 254 acceptable including 31 low-order keys with an all-zero secret, which `Agree` reports as false); the Noise vectors published for `Noise_*_25519_ChaChaPoly_SHA256` for all 15 patterns implemented (15 protocols, 90 messages, checking each handshake ciphertext, the handshake hash and the transport messages). Fuzz targets `aead-open` and `noise-read` (seeds in `testdata/fuzz`). Seal/open allocate 0 bytes per packet.


## 2. API (public surface; change only by agreement)

Types and where they live:

| Type | Assembly | What |
|---|---|---|
| `PeerKey` | Core | A 32-byte public key; text form is 43 characters of base64url. `Parse`, `TryParse`, `Bytes`, `IsEmpty`, equality. |
| `NetKeyPair` | Core | `Generate()`, `FromPrivateKey`, `Load(path)`/`Save(path)` (a text file with `private:` and `public:` lines, mode 0600 where the platform has modes), `PublicKey`. The private half never prints. **It is the developer's secret.** |
| `KeyFormatException` | Core | A key or key file is malformed. |
| `SecurityMode` | Net | `Required` (default) or `Insecure`. No `Optional`, no fallback. |
| `NetSecurity` | Net | Passed as `UdpOptions.Security` / `WebSocketOptions.Security`. `ForServer(key, gameId, more...)`, `ForClient(serverKeys, gameId, identity?)`, `Insecure`, `AlsoTrusting(keys)` (a server that also connects to other servers). Properties: `Mode`, `GameId`, `ServerKeys` (a server's key pairs, primary first), `TrustedServerKeys` (a client's accepted server keys, preferred first), `Identity` (the client's key pair). |
| `SecurityNotConfiguredException` | Net | A transport opened with `Security == null`, a Required listener with no server key, or a Required client with no trusted key. The message names the fix. |
| `INetConnection.IsEncrypted`, `.RemoteKey` | Net / Sync | `RemoteKey` (from `IAuthenticatedChannel`) is the peer's authenticated key once connected, null on an `Insecure` link. Both are default interface members (false / null). |
| `DisconnectReason.SecurityRequired`, `HandshakeFailed`, `PacketLimit` | Net | One side is Insecure and the other Required; the server does not know the key the client trusts, or the game ids differ; the packet-number space is used up. |
| `IAuthenticatedChannel`, `PeerIdentity`, `Node.Accept(channel, policy)` | Sync | The hook that turns a proven key into a `Trust`. `policy(PeerIdentity)` returns the trust, or null to refuse; null means the caller closes the channel. `PeerIdentity.Key` is empty on an unauthenticated link. |
| `PlayerRegistry` | Sync | Maps a `PeerKey` to a stable player id (`PlayerFor(key, out isNew)`), saved with the game's data (`Save`, `TryLoad`). A reconnect with the same key is the same player. It remembers at most `maxPlayers` keys (anyone can make keys, so a game sets this to what it can hold); `TryPlayerFor` returns false instead of adding past the limit. |
| `NetSecurity.PerServerIdentity`, `IdentityKeyFor(serverKey)`, `NetKeyPair.DeriveFor(serverKey)` | Net / Core | On by default for `ForClient`: the key a server sees is derived from the player's root key and that server's key, so two servers cannot link a player by comparing keys. `IdentityKeyFor` is what a given server's registry is keyed by. Server-to-server links leave it off. |
| `INetConnection.ChannelBinding` | Net | 32 bytes both ends derive from the handshake; see section 6 (login proofs). |
| `KeyStore`, `KeyFiles` | Core | `Create(folder, name, force)`, `Ensure(...)`, `DevelopmentFolder(scope)`: what `onegame keys` and the development tools use. |
| `ServerConfig.Security` (`security:`), `ServerHost.Security`, `ClientRequest.Key`, `ConnectOptions.Security` | Server, Host | Wiring for dedicated servers and game clients (below). |

Typical server and client:

```csharp
var key = NetKeyPair.Load("server.key");                          // made by `onegame keys`
var listener = UdpTransport.Listen(bind, new UdpOptions { Security = NetSecurity.ForServer(key, "my-game/1") });
var players = new PlayerRegistry();
// each frame, for every connection the listener accepted:
var connection = node.Accept(channel, id => Trust.Player(scope).As(players.PlayerFor(id.Key, out _)));

var me = File.Exists("me.key") ? NetKeyPair.Load("me.key") : NetKeyPair.Generate();   // keep it in the game's save folder
var udp = UdpTransport.Connect(endpoint, new UdpOptions { Security = NetSecurity.ForClient([PeerKey.Parse("...")], "my-game/1", me) });
```

**What a channel implementation must do** (UDP does it in `UdpConnection`/`UdpListener`; the WebSocket transport follows the same rules):

1. On `Listen`/`Connect`, call `NetSecurity.RequireForListening(options.Security, nameof(Options))` / `RequireForConnecting(...)`. They throw `SecurityNotConfiguredException` and return the settings. For `Insecure` keep today's plaintext behaviour exactly.
2. In `Required` mode run `SecureHandshake` (internal, `OneGame.Net`): `Client(security, keyIndex, bind)` then `WriteFirst`/`ReadReply`, or `Server(security, bind)` then `ReadFirst`/`WriteReply`, then `Finish(out send, out receive)`. The handshake is Noise IK with prologue `"onegame/2" 00 gameId 00 bind`; `bind` is transport-specific data both ends know (UDP: client nonce and cookie; WebSocket: empty). A client with several trusted keys passes the next `keyIndex` after a `HandshakeFailed` rejection.
3. Encrypt every byte after the handshake with the two ciphers from `Finish`. A datagram transport uses `DatagramSender` (it owns the counter: a packet number may be used once, numbers only go up, the limit is far below 2^64 - 1) and `DatagramReceiver` (it owns the replay window); the caller never chooses a nonce. A stream transport uses `NoiseCipherState.Encrypt`/`Decrypt` in order. A cipher with no key throws or returns false, never plaintext. `Rekey` is not used. `IsEncrypted` is true, and `RemoteKey` is set **before** `Connected` fires on the client and before `TryAccept` hands the connection out on the server.
4. Never act on unauthenticated bytes beyond the handshake itself. A message that fails authentication is dropped and changes no state (UDP) or closes the connection (a stream).
5. A `Required` end that meets an `Insecure` peer (or the reverse) closes with `SecurityRequired` (or `VersionMismatch` where only the version is visible). Never fall back.
6. Handshake work is limited before any DH operation: per source (IPv4 address or IPv6 /64), shared, and a reserved share for sources that recently completed a session, plus a separate small budget and tombstone list for unconfirmed handshakes (`transport.md`).
7. A graceful close is authenticated; an unauthenticated close or reset is reported as a drop (`ByRemote = false`).

## 3. Protocol choice

Cipher suite is fixed: `25519_ChaChaPoly_SHA256`. No AES-GCM, no negotiation of primitives, no post-quantum exchange (Noise rev 34 has none).

**Only IK.** The client always knows the server's key (pinned, or from the master or a directory), so there is one round trip, the client's static key travels encrypted in message 1, and the server authenticates the client's key during the handshake. There is no XX and no trust-on-first-use (decision (b)). A client with no identity key gets a fresh one per connection object, so it is anonymous but still encrypted and authenticated *to* the server.

Prologue: `"onegame/2" 00 gameId 00 bind`, so a handshake only completes between peers of the same game and protocol, and (UDP) a captured first message is useless with any other cookie or client nonce.

## 4. UDP

The cookie exchange is unchanged: Request, Challenge, and a server that stores nothing until the cookie comes back. The version nibble of byte 0 is 2 for secure datagrams and 1 for plaintext, and a peer never answers a version it does not run in kind.

| Kind | Secure layout (version 2) | Notes |
|---|---|---|
| 0 Request | `b0, nonce u64`, padded to ≥ 256 | As today. |
| 1 Challenge | `b0, nonce, cookie[16]` | As today. |
| 2 Response | `b0, nonce, cookie[16], len u16, noise msg 1`, padded to ≥ 256 | `bind = nonce ‖ cookie`. The server checks the cookie (one HMAC) and the handshake rate limit before the DH work. |
| 3 Accept | `b0, nonce, connId u32, len u16, noise msg 2` | Payload of msg 2 is the 16-byte reset token (§4.4). Repeated, byte for byte, if the Response repeats. |
| 4 Reject | `b0, nonce, reason` | `SecurityRequired` (plaintext Request to a Required server, answered in the Request's version), `HandshakeFailed` (no server key matched or wrong game id), `ServerFull`, `VersionMismatch`. Unauthenticated, so it can only end a connect attempt, which an attacker on the path can do by dropping packets anyway. |
| 5 Data | `b0, connId u32, packetNo u32, AEAD(ackBase u32, sack u64, window u16, frames…) ‖ tag[16]` | The first 9 bytes are the associated data. Nonce = the packet number, in a per-direction key. The acks are inside the ciphertext. |
| 6 Disconnect | `b0, connId u32, AEAD(reason, code) ‖ tag[16]` | Sent three times, authenticated with nonce 2^32 (a number no packet can use). An unauthenticated Disconnect is ignored. |
| 7 Reset | `b0, connId u32, token[16]` | See §4.4. |

**4.1 Nonces and the packet limit.** Each direction has its own key (Split). The existing 32-bit packet number is the Noise nonce (four zero bytes, then the counter as a little-endian 64-bit value), unique per packet. A connection closes with `PacketLimit` after `UdpOptions.MaxPacketsPerKey` packets in one direction (default 2^32 − 2^16, about 50 days at 1000 packets a second); the client reconnects, which makes new keys. This replaces the design's 8-byte packet number: it saves 4 bytes on every packet, and the limit is enforced, not assumed.

**4.2 Replay window.** The reliability layer's 64-packet window drops duplicates and stale numbers. For a secure connection it is checked *before* decrypting (a cheap reject) and the packet is marked seen *only after* the tag verified, so a forged or replayed packet changes no state. Anything that fails is dropped without a reply.

**4.3 Who counts as connected.** The server creates the connection when message 1 authenticates but does not hand it out (`TryAccept`) until the first authenticated Data packet arrives; the client sends one at once after Accept. A replayed message 1 (the cookie is valid for 20 s) therefore creates a connection nobody can use, which times out unseen, and cannot evict a live session.

**4.4 Restarts and stateless reset.** A server that no longer knows a connection id answers Data for it with Reset: `token = HMAC(key derived from its primary private key, connId)[..16]`, which the client received in the encrypted Accept. The client closes with `Shutdown` only for a Reset whose token it holds (constant-time compare). The token survives a restart because it comes from the static key, and an attacker without it cannot forge one.

**4.5 Rekeying.** Not built; forward secrecy is the per-connection ephemeral key, and `PacketLimit` bounds key use. **4.6 Handshake rate limit.** `UdpOptions.MaxHandshakesPerSecond` (default 200, per listener) is checked after the cookie and before any DH: excess Responses are dropped, not queued. **4.7 Cost.** Overhead is 16 bytes (the tag) per packet; the payload of a full packet shrinks by 16, and `MaxUnreliableBytes` shrinks with it.

## 5. WebSocket

Noise runs inside the WebSocket as binary messages. TCP supplies ordering and address validation, so no cookie, packet number or replay window is needed; the Noise counter is implicit and a lost or reordered record cannot happen without breaking the stream (it fails authentication and the connection closes).

- The first binary message each way is the handshake (client msg 1, server msg 2), `bind` empty. The server's path or subprotocol may carry the game id; the prologue binds it either way.
- After the handshake each Noise message (≤ 65535 bytes) is one WebSocket message: `AEAD(flag ‖ chunk)`, flag 0 = last chunk, 1 = more, 2 = graceful close. The receiver enforces `MaxMessageBytes` on the running total before it buffers anything.
- A WebSocket Close frame is not authenticated. A graceful close is an authenticated record (flag 2) followed by the Close frame; a bare Close or reset is reported as a drop (`ByRemote = false`).
- `MaxPendingHandshakes`, `HandshakeTimeoutMs` and `MaxHandshakesPerSecond` bound the sockets and DH work of unfinished handshakes.
- On an https page the browser needs `wss://`: the host terminates TLS in front of the plain listener (see the user guide, operator steps). Noise runs inside it regardless.

## 6. Identity across reconnects, and `Trust.Who`

After the handshake `INetConnection.RemoteKey` is the peer's authenticated static key. The transport never decides trust; the game does, in the `Node.Accept` policy (or `ServerHost.Authenticate`, whose `ClientRequest` now carries `Key`). The usual policy maps the key to a player with `PlayerRegistry` and returns `Trust.Player(...).As(playerId)`.

A reconnect (`RemoteSession` opens a new connection with the same `NetSecurity.Identity`) resolves to the same key and so to the same player id, and the server can hand the new connection the entity the old one owned. Rules:

- Identity binds only to a connection that has delivered an authenticated Data packet (§4.3), never at message 1.
- One live session per player: a new session for a player replaces the old connection.
- The key is a device credential, not an account. A game that needs an account sends a platform login proof (for example a Steam ticket) as the first encrypted application message; it is deliberately not in message 1, whose payload has no forward secrecy toward a later leak of the server key. **A proof must be bound to the session**, or a malicious host that receives it could present it to another server and be taken for the player: the ticket's own identity or audience field (where the platform API has one) must carry `hex(ChannelBinding)` and the server's public key, and the verifier checks both. Where a platform ticket cannot carry a binding, the account link is made once at the master, which is the only party that accepts raw tickets and then vouches (signs or MACs) that "key K, as the master's own servers see it, belongs to player P"; game servers check that statement and never see a replayable ticket. `INetConnection.ChannelBinding` exists for exactly this. The engine does not verify tickets itself; the exact field of the Steamworks ticket API is for the game that adds it to confirm against the partner documentation.
- Because a client's key differs per server (`PerServerIdentity`), a player's account link is per server too: a hosted server learns the player's key *for that server* and nothing that links the player elsewhere. A copied save file (the root key) is still a takeover of every server that player used; there is no client-key revocation or rotation yet.
- A client key belongs in the game's save storage. A browser keeps it only where persistent storage exists; otherwise web identity lasts one page load.

Set aside: server-issued resumption tickets (need a ticket key shared by every server and a clock) and per-player shared secrets (need a secure channel to hand them out).

## 7. Decisions

**(a) [PM] Compatibility: negotiation by mode, no downgrade.** Default `Required`; `Insecure` is explicit and named, for tests and development tools. A Required end never falls back and a mismatch closes with `SecurityRequired`. There is no `Optional` rollout mode: no released game uses onegame networking, so a rollout window protects nobody. Opening a transport without settings throws `SecurityNotConfiguredException`. This breaks 1.9 clients and servers on purpose (CHANGELOG says so). In-process single-player and `TestCluster`'s in-memory channels are not network paths and are unaffected.

**(b) [PM] Keys: pinned, or from the master or directory, as a set per server; no trust on first use.** A client holds the acceptable public keys per server (a rotation overlaps: the server adds a pair, clients get the new key, the old pair is removed later), pinned in a first-party build or handed out with the server list by the master. No known-hosts store. Development and LAN stay zero-ceremony because the tools make keys and hand them over the same way a pinned key travels: `onegame run --connect`, `onegame cluster`, process clusters, `TestCluster` over real transport and the samples generate keys automatically (§9). `onegame keys` makes key files. The private key is the developer's secret.

**(c) [PM] Browsers: Noise inside WebSocket everywhere, plus `wss://` on https pages.** One code path and one identity model on every platform; TLS gives browsers nothing for client identity. The certificate for `wss://` is an operator step (the "Operating a server" section of the user guide). The interpreter cost was measured (§7.1) and is not a reason to drop it.

**(d) [PM] Agreed:** handshake rate limiting, login blobs in the first encrypted message, per-page-load web identity, and the packet-size budget (16 bytes per UDP packet, see §4.7).

Other points. IK message 1 has no forward secrecy toward a later leak of the server's static key (the client's static key is exposed), so nothing secret goes in it. Unauthenticated bytes remain only in Request, Challenge, Reject, and Reset (checked by token); none leads to state or key exposure. No wall clock is used (the cookie epoch is the transport's monotonic clock). Entropy on the web comes from `Entropy.Use` over `crypto.getRandomValues`, which the browser backend installs before the first connection. Out of scope: Steam relay and lobbies, signed directory entries, post-quantum exchange.

**7.1 Measured in the browser.** Method: the same `OneGame.Core/Crypto` sources built as a `Microsoft.NET.Sdk.WebAssembly` app in Release with the export's settings (interpreter, no AOT, no workload, invariant globalization), served by `tools/web/run-page.mjs` and run in headless Chrome (`--disable-gpu`, no GPU needed), then the same sources as a CoreCLR console app on the same machine minutes apart. Each figure is a loop of at least 1.5 s after a warm-up; "handshake" is both peers in one process, so a client alone pays about half. The host was a busy 32-thread desktop (load average 25 to 40 from other work, Ryzen 9 7950X3D), so treat the ratios as better than the absolute values. Chrome ran three times; the range is shown.

| | Chrome, .NET wasm interpreter | CoreCLR, same host | Ratio |
|---|---|---|---|
| ChaCha20-Poly1305 seal, 1200 B | 43,000 to 53,000 /s (52 to 64 MB/s) | 398,000 /s | 8x |
| open, 1200 B | 53,000 to 54,000 /s | 316,000 /s | 6x |
| seal, 64 B | 389,000 to 445,000 /s | 2,805,000 /s | 7x |
| X25519 `Agree` | 405 to 437 /s (2.3 to 2.5 ms) | 2,109 /s (0.47 ms) | 5x |
| Noise IK handshake, both peers | 28.0 to 29.5 ms | 4.5 ms | 6x |
| Noise XX handshake, both peers | 23.2 to 24.3 ms | 3.4 ms | 7x |

What it means. A 60 Hz game sends and receives at most 60 packets a second each way, so about 120 AEAD operations a second at roughly 19 to 23 µs each for a full 1200-byte packet: about 0.25 % of one core, and even 10 times that traffic stays near 3 %. The interpreter is not a bulk-crypto problem. A browser client's handshake costs about 12 to 15 ms of main-thread time (four DH operations plus two public-key derivations), once per connection; the native server pays about 2 ms. That is one dropped frame, so do it before gameplay starts or from a worker, and keep the client's own public key cached instead of re-deriving it (saves about 2.4 ms). It does not change the server-browser flow: a list of servers is a directory fetch, and only the chosen server gets a handshake. Not measured: phones' browsers (expect several times slower than this desktop, so a 50 to 100 ms handshake), Safari and Firefox, and the WebSocket path end to end. None of that changes the recommendation, so the wasm cost is not a reason to rely on wss alone.

**(d) Other points found.**

- *Handshake CPU is the new amplification target.* The cookie proves an address, not a real client; a botnet can still ask for DH work. Recommend `MaxHandshakesPerSecond` (global and per source) with a drop-not-queue policy, default sized to a few hundred per second per core.
- *msg 1 payload under IK has no forward secrecy toward a later leak of the server's static key* (the client's static key is exposed that way, so key-to-connection linkability of recorded traffic). Mitigated by carrying nothing secret in it (see §5). Where this matters, use XX; it costs one more flight.
- *Unauthenticated bytes still exist:* Request, Challenge, Reject and the stateless reset. None leads to state or to a key; the only effect of forging one is a refused or failed connect, which a path attacker can cause by dropping packets anyway.
- *Time:* the design uses no wall clock (no certificate or ticket expiry). The cookie epoch is the transport's existing monotonic clock.
- *Entropy on the web* depends on `Entropy.Use` with `crypto.getRandomValues`; the browser backend must install it before the first connection (it already does for cookies and masking keys).
- *Tag budget:* a few kinds of in-flight framing (unreliable messages near `MaxUnreliableBytes`) lose 6 bytes; `MaxUnreliableBytes` must be recomputed.
- *Interop with plaintext tooling:* Wireshark and the conditioner see ciphertext. Tests that need to see frames use `Insecure`; the harness's own `net` op reports counters, not payloads, so it is unaffected.
- *Out of scope here:* Steam relay and lobbies; signed directory entries or certificates (Ed25519); post-quantum exchange.


## 8. What a path attacker can still do afterwards

- Read the addresses, ports, packet sizes and timing; see that a game handshake happens (the first two datagrams are cleartext) and the connection id. Padding hides handshake sizes only.
- Drop, delay, reorder or duplicate datagrams (duplicates are discarded by the replay window); block a connection entirely; cause a failed connect by forging Reject or Challenge.
- Flood the server with datagrams; each costs one table lookup and at most one failed AEAD open (about 4 µs). Spoofed Requests still cost one HMAC and a 25-byte reply, no memory.
- On WebSocket: inject a TCP reset or Close frame, which ends the connection but is reported as unauthenticated.
- With an unpinned or first-use server key: impersonate the server to a client that has never seen the real key (the usual TOFU limit).
- If the client allows downgrade (option 2 in (a)): force plaintext. Off by default.

It can no longer: read or alter payloads, inject or replay packets into a connection, hijack a session or an identity (binding needs the first authenticated packet, which needs the client's ephemeral key), or learn a player's identity from the handshake without the server's static private key (IK) or at all (XX, from msg 3).

## 9. Keys, configuration and tooling

- `onegame keys [--out <dir>] [--name <name>] [--force] [--json]` writes `<name>.key` (private, mode 0600) and `<name>.pub` (public, one line) and prints the public key (default folder `keys/`, default name `server`). It never overwrites an existing key unless `--force`, and adds a `.gitignore` with `*.key` when the folder has none. JSON: `{"ok":true,"name":...,"privateFile":...,"publicFile":...,"publicKey":...}`.
- **The private key is the developer's secret.** Keep server keys on the server only, outside the repository; ship only the public key in a client build or a server list. Anyone holding a server's private key can impersonate that server to every client that trusts its public key. `onegame export` refuses to build when a content folder holds a private key file. A client's key is the player's: it belongs in the game's save folder, and a copy of it is a copy of the player.
- **Server settings.** Configuration: `security: { key: server.key, game: my-game/1 }` (`key` may be a list, the first is primary and the rest let clients still on an older key connect; relative paths resolve against the configuration file; `insecure: true` for tests). Flags: `--key <file>` (repeatable), `--game-id <text>`, `--insecure`. A server that listens with no key and no `insecure` fails to start with a message that names the fix (`SecurityNotConfiguredException`; `ServerApp` exits with code 2). Code can set `ServerHost.Security` itself.
- **Client settings.** Flags for any game using `GameApp`: `--server-key <public key or .pub file>[,...]` (several allowed, preferred first, for rotation), `--identity <file>` (the player's key; made and saved if missing), `--game-id`, `--insecure`. In code, `ConnectOptions.Security = NetSecurity.ForClient(keys, gameId, identity)`. With no server key the connection fails at once with the typed error; with a key the server does not hold, the session ends with `HandshakeFailed` and does not retry for a minute (`SecurityRequired` and `HandshakeFailed` are permanent like `Rejected`). A client with no identity key makes one for the life of the session, so its reconnects are the same player; keep an identity file for the same player across runs.
- **Insecure and development keys cannot be reached by accident.** `--insecure`, `security: insecure` and a key marked `# DEV KEY` are refused unless the process says it is a development run (`ONEGAME_DEV=1`, which `onegame server run`, `onegame cluster`, test clusters and the CLI set for what they start). A development run that is given no key makes a development key for itself. A server that is allowed to run insecure or with a development key logs a warning at every start. Code that sets `ServerHost.Security` to `NetSecurity.Insecure` has chosen it in code and gets the same warning.
- **Zero-ceremony development.** `onegame run --connect <address>` adds `--server-key` and `--identity` from a per-user folder (`ONEGAME_KEYS`, else the local application data folder, under `onegame/dev-keys/`), making the keys on first use; a server started by `onegame server run` (or any development run) uses the same server key when it is given none. `onegame cluster` and process clusters make one key per node in the cluster's own working folder, give each server `--key`, each client `--server-key` (the server it connects to) and `--identity` (kept while the cluster lives, so a restarted client is the same player), and let a node's arguments name another server's public key as `{key:NAME}`. Nothing here is a different code path from production; only who generated the key differs. Development keys never live in a game folder, so no export or commit takes them, and they are marked so a real server refuses them.
- **First-party builds** pin the public key in the client (`NetSecurity.ForClient([PeerKey.Parse("...")], "my-game/1")`). **Hosted servers** list their public key with the master or directory that already hands out addresses, and the client takes the key from the same entry. A server key set lets the host rotate without a flag day. Servers of one game find each other's keys the same way they find their addresses (Lanterns: `--option peer.key.N=<key or file>` beside `peer.N=<address>`; a peer whose key is not listed is refused).
- **Key rotation under IK.** The client picks the server key before the server can say which it holds, so a client with several trusted keys tries the first, and after a `HandshakeFailed` rejection the next. A server tries each of its keys against message 1. Both lists should be short. The master that lists a third-party server's key should check that the server can hold it (complete a handshake) before listing it.
- **Not built:** trust on first use (deferred; no development or LAN case needed it), signed key lists, revocation of a client key, resumption tickets.

## 10. For the reviewer

Read in this order: `Noise.cs` against spec §5 (state machine, DH order for `es`/`se`, pre-messages, `Split`, failure handling), `ChaCha20Poly1305.cs` (`Open` checks the tag before it decrypts and zeroes the output on failure; in-place use; counters), `X25519.cs` (limb bounds in `Mul` and `Carry`: sums stay under 2^63 because every `Add`/`Sub` carries; `Store`'s canonical reduction), and the cases the vector tests cannot reach: the explicit-counter methods, the 2^64 - 1 counter reservation, `NoiseHandshake` misuse (wrong turn, missing keys throw `InvalidOperationException`/`ArgumentException`; wire errors return false). The fuzz targets are `aead-open` (any bytes; failure must zero the output; seal/open must round trip) and `noise-read` (honest handshake up to a chosen stage, then arbitrary bytes for the next message).

## 11. Independent review of the design and primitives: findings and what was done

An independent reviewer read the primitives and the design (no bugs found in X25519, ChaCha20-Poly1305, HKDF or the Noise state machine). Their findings, by id:

| Id | Finding | Done |
|---|---|---|
| H1 | A platform ticket sent to a host is replayable by that host | Section 6: proofs are bound to `ChannelBinding` and the server key, or linked once through the master. `ChannelBinding` is public API. The engine does not verify tickets. |
| M1 | IK message 1 replay; surfacing a connection too early; unconfirmed handshakes | UDP: connections surface and send only after the first authenticated packet; separate unconfirmed budget (total and per source, short timeout, not counted against `MaxConnections`); tombstones for the cookie lifetime; capacity checked before any DH. **WebSocket part is the WebSocket work's.** |
| M2 | Handshake CPU; weak limits; recomputed public key | The server's public key is computed once (also the client's); per-source budgets with IPv6 /64 grouping, a shared cap and a reserved share for recently confirmed sources; the cheap checks come first; the doc no longer says "about four" (a server handshake is five DH operations). |
| M3 | A keyless cipher fails open | `NoiseCipherState` throws on `Encrypt` and returns false on `Decrypt` with no key; pass-through exists only inside `NoiseHandshake`. Test. |
| M4 | Caller-chosen counters; counter 2^64-1 | `SealAt`/`OpenAt` are gone. `DatagramSender` owns the numbers (strictly increasing, one use each, limit below 2^32, so 2^64-1 is unreachable); `DatagramReceiver` owns the replay window. Every transmission, retransmissions included, has a new packet number. Tests. |
| M5 | Downgrade and `Insecure` | No `Optional`. A client with a trusted key never falls back (a Required end rejects the other version). The datagram version is in the prologue (`Wire.Bind`); the Noise pattern name is already in the handshake hash. `Insecure` and development keys need `ONEGAME_DEV=1` when asked for by flag or configuration, and log a warning at every start (section 9). The WebSocket pattern byte is the WebSocket work's. |
| M6 | Development keys becoming production keys | Dev keys live in a per-user folder or a cluster's working folder, never a game folder; they carry a `# DEV KEY` mark that real servers refuse; `export` refuses private key files in content folders; docs say clients pin public keys only. |
| M7 | One static client key links a player across servers; first-sight registry unbounded | Per-server derived client key (`PerServerIdentity`, default for `ForClient`). `PlayerRegistry` has a size limit (`TryPlayerFor`). Rotation and revocation of a client key are not built (documented). |
| M8 | Poly1305 reduction untested; surviving mutants | A big-integer reference test with crafted edge inputs (RFC 8439 A.3 #5 pinned: tag 3), known-answer `Rekey`, counter exhaustion, fail-closed ciphers. The early-exit tag compare is covered only by the explicit timing check (`ConstantTimeSmokeCheck`), which still is not in the default run. |
| L1, L7 | A half-written or failed handshake is stuck | A write that fails on buffer size marks the handshake failed; the client reads the server's reply on a fork, so a forged or damaged reply is thrown away and the real one is still accepted. Tests. |
| L2, L4, L5 | Key length; partial overlap; output not zeroed | Fixed (key must be 32 bytes; shifted overlap throws; `Open` zeroes the output on every false). |
| L6 | Accept's connection id outside the transcript | The id is also inside the encrypted message 2; the client compares. |
| L8 | Reset details | Constant-time compare, per-second limit on resets, a live id is never answered with a token (as before). A separate random reset key (instead of one derived from the static key) is deferred. |
| N1, N4 | Doc accuracy; key rotation under IK | Fixed above and in section 9. |
| N2 | `Entropy.Use` is process-wide | Plugins are trusted by the operator; anything in the process can replace the random source. Said here. |
| Deferred | L3 (stack residue of temporaries in `Mul`/`Invert`, `_ePriv` kept until `Split`), L10 (`SetEphemeralForTest` is compiled in, `internal` to the engine and its tests), L9 and the WebSocket framing findings (WebSocket work) | Low risk: each needs a memory-read primitive or a test-only call. |

