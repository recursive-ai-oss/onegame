# Transport security (design, primitives built)

Status: the primitives and the handshake core exist in `OneGame.Core` and are tested; nothing is wired into `OneGame.Net` yet. This document says how they will be used, what is still undecided, and what an attacker on the network path can and cannot do once they are. `transport.md` describes today's plaintext wire.

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

## 2. Protocol choice

Cipher suite is fixed: `25519_ChaChaPoly_SHA256`. No AES-GCM (phones without hardware or a platform provider), no negotiation of primitives, no post-quantum key exchange (Noise rev 34 has none; game traffic is not worth recording for a decade, and this can be revisited when Noise defines it).

Patterns, recommended:

- **IK** when the client knows the server's static key (pinned or from a directory). One round trip, the client's static key is sent encrypted, the server authenticates the client's key during the handshake. This is the normal case.
- **XX** when it does not (trust on first use: the client records the key it was shown, as SSH does; direct-connect and development). Three messages, both statics encrypted.
- Nothing weaker (no NN): an unauthenticated encrypted link is not offered.

Prologue, used by both patterns: `"onegame/2" ‖ gameId ‖ transport tag ‖` (UDP only) `cookie ‖ client nonce`. A handshake therefore only completes between peers of the same game and version, and a captured first message is useless with any other cookie.

## 3. UDP

The cookie exchange stays exactly as it is: Request, Challenge, and an unauthenticated server that stores nothing until the cookie comes back. The version nibble in byte 0 becomes 2 for secure datagrams. Layout changes:

| Kind | v2 layout | Notes |
|---|---|---|
| 0 Request | unchanged (padded to 256) | Cheap to answer, as now. |
| 1 Challenge | unchanged | |
| 2 Response | `b0, nonce, cookie[16], noise msg 1`, padded to ≥ 256 | IK message 1 is `e, es, s, ss`, payload = hello (limits, no secrets, see §6). The server checks the cookie first (one HMAC), then spends two DH operations. |
| 3 Accept | `b0, nonce, connId u32, noise msg 2` | IK message 2 is `e, ee, se`; the payload carries the server's reset token (§3.4). The server splits immediately and creates the connection, but keeps msg 2 to repeat it if the Response repeats, until the first authenticated Data arrives. |
| 5 Data | `b0, connId u32, packetNo u64, AEAD(frames ‖ acks) ‖ tag[16]` | `b0 ‖ connId ‖ packetNo` is the associated data. The ack base, SACK and window fields now travel inside the ciphertext. |
| 6 Disconnect | same form as Data (an authenticated frame inside) | |
| XX | Response carries msg 1 (`e`), Accept carries msg 2, and a new kind 7 `Finish (c→s)` carries msg 3, which the server needs before it considers the connection confirmed. |

**3.1 Nonces.** Each direction has its own key (Split), and the 64-bit `packetNo` is the Noise nonce (4 zero bytes ‖ little-endian counter). A sender never reuses a number; it closes the connection at 2^40 packets (never reached in practice) instead of wrapping. The 4-byte packet number of v1 grows to 8 so the nonce is explicit and no reconstruction logic is needed.

**3.2 Replay window.** The receiver keeps the highest authenticated number and a 256-bit bitmap (the reliability layer can have more than the current 64 packets in flight at high bandwidth). Order of checks, as in RFC 4303 §3.4.3: reject a number already seen or older than the window before decrypting (cheap), decrypt, and only after the tag verifies mark it seen. Anything failing is dropped without a reply and without changing state, so garbage and replays cost one AEAD attempt. Datagrams outside the window are lost to the reliability layer, which resends.

**3.3 Rekeying.** Not built. Forward secrecy comes from the per-connection ephemeral keys; a session that must outlive that reconnects (and resumes, §5). Bit 3 of `b0` is reserved as a key-phase bit so a Noise `Rekey` scheme can be added without another wire version. Hard limit instead: 2^40 packets per direction.

**3.4 Disconnects and restarts.** An unauthenticated Disconnect would let a path attacker kill any connection, so v2 ignores it. The one case that needs it, "the server restarted and no longer knows this connection", uses a stateless reset in the style of RFC 9000 §10.3: the server derives `token = HMAC-SHA-256(HKDF(serverStaticPrivate, "reset"), connId)[..16]`, tells the client in the encrypted Accept payload, and answers unknown-connection Data with a datagram ending in that token. The token survives restarts because it comes from the static key. Forging a reset requires the token, so the attacker gains nothing they could not get by dropping packets.

**3.5 Cost.** Overhead is 29 bytes (header 13 + tag 16) against 23 today, so frame payload shrinks by 6 bytes inside the 1200-byte MTU. Server cost per accepted handshake is about four X25519 operations (≈0.3 ms each in a release build on this host) after the cookie has proven the source address; the per-source and global handshake rate limits (new `MaxHandshakesPerSecond`) bound what an attacker with real addresses can burn. Seal plus open of a 1200-byte packet takes about 4 µs (about 290 MB/s per operation on one core, measured on a loaded host).

## 4. WebSocket

Noise runs inside the WebSocket as binary messages. TCP supplies ordering and address validation, so no cookie, packet number or replay window is needed; the Noise counter is implicit and a lost or reordered record cannot happen without breaking the stream (it fails authentication and the connection closes).

- The first binary message each way is the handshake (client msg 1, server msg 2, and for XX client msg 3), prefixed with one byte naming pattern and version. The server's subprotocol or URL path may also carry the game id; the prologue binds it either way.
- After the handshake each Noise message (≤ 65535 bytes) is one WebSocket message: `AEAD(flag ‖ chunk)`, flag 0 = last chunk, 1 = more. A logical message over 65 KB is split into chunks; the receiver enforces `MaxMessageBytes` on the running total before it buffers anything. Pings and pongs stay WebSocket-level and cost nothing; they carry no data.
- A WebSocket Close frame is not authenticated (any TCP-level attacker can inject one). A graceful close is therefore an authenticated record (`flag 2`) followed by the Close frame; a bare Close or a reset is reported as an abnormal, unauthenticated end, which game code must treat like a drop and reconnect from.
- Handshake DoS: the existing `MaxPendingHandshakes` and `HandshakeTimeoutMs` cap sockets that have not completed the Noise handshake; the same per-source rate limit applies to the DH work.

## 5. Identity across reconnects, and `Trust.Who`

After the handshake the transport exposes `RemoteStatic` (32 bytes, authenticated, because only the holder of the private key could complete `ss`/`se`). The sync layer's `Trust` (`Who.Player`, `Who.Team`, `Own`, …) is still decided by the game's policy, never by the transport. New seam (stage 2): `Node.Accept(channel, Func<PeerIdentity, Trust?>)`, called once with `RemoteStatic` (and, for XX, whether the key is new). The policy returns the connection's `Trust` or null to refuse. The default policy maps a key to a player through a small registry the game owns (the master in a multi-server game), assigning a fresh player id on first sight.

Reconnect then works by key: a client with the same static key resolves to the same player, and the server can reattach the new connection to the entity the old one owned (today a reconnecting client comes back as a new player; this removes that). Rules that keep it safe:

- Identity is bound only after the first authenticated Data packet (or XX msg 3), never at msg 1, because msg 1 alone can be replayed by a path attacker within the cookie epoch; this prevents replaying a captured msg 1 to evict a live session.
- One live session per player: a new authenticated session for a player replaces the old connection after the old one has been silent for a short grace period (or at once if it is already closed).
- The static key is a device credential, not an account. Proving that a key belongs to a Steam or store account is a game-level step: the client sends its platform auth blob (for example a Steam session ticket) as the first encrypted application message after the handshake, and the policy verifies it. It is deliberately not in msg 1, whose payload has no forward secrecy under IK (a later leak of the server key exposes it).
- A client key lives in the game's save storage. A browser build keeps it only where persistent storage exists; until then web identity lasts one page load.

Alternatives considered and set aside: server-issued resumption tickets (need the ticket key shared by every server in the game and an expiry clock, so reconnects between servers become a key-distribution problem; a static client key is stateless and already crosses servers); a pre-shared secret per player (needs a secure channel to hand it out, which is the problem being solved).

## 6. Decisions

Each is a recommendation, not a decision.

**(a) Compatibility with released 1.x clients and servers.**

1. *Hard version bump:* v2 only; a v1 peer gets `Reject(VersionMismatch)` (already defined). Simplest and no downgrade, but every deployed game must update clients and servers in lockstep.
2. *Negotiation with a policy:* the server accepts v1 and v2 (`Security = Optional`), a client tries v2 and may fall back to v1 only if its own policy allows. A forged Reject is a downgrade attack, so the fallback is off by default on the client.
3. *Plaintext as an explicit opt-in mode:* `Security.Insecure`, for tests, loopback and LAN prototypes.

**Recommend 2 plus 3.** Server `Required` by default, with `Optional` as a rollout window that games turn on for one release and then remove; client `RequireSecurity = true` by default; `Insecure` is a named value, easy to grep for. For **existing games' default**: the library default is `Required`, and listening or connecting without keys configured throws a typed `SecurityNotConfiguredException` whose message names the fix. That is a loud failure on first run after upgrading, rather than a quiet plaintext default. In-process single-player and `TestCluster`'s in-memory channels are not network paths and are unaffected; UDP/WebSocket tests choose `Insecure` explicitly.

**(b) Server keys: who has them and how clients learn them.**

1. *Pinned in the client build:* the game ships its servers' public keys. Strongest and simplest for first-party servers. Does not scale to third-party hosts (each has its own key, shipped before the host exists).
2. *Handed out by the master / a directory:* the server list carries `(address, public key)` and is fetched over a link the client already trusts (pinned master key). A third-party host registers its public key with the master; no certificates and no signing primitive needed, and rotation is a directory update. Requires the master to be the root of trust (it already is for shared economies).
3. *Keyless anonymous mode (XX, trust on first use):* the client remembers the key it saw per `host:port`; a changed key is a loud error. Protects against passive and later active attackers, not first contact. For direct connect, LAN and development.

**Recommend 1 for first-party, 2 for hosted servers, 3 as the explicit fallback.** A client holds a set of acceptable keys per server (so a host can rotate with overlap). `onegame run` and `onegame new` generate development keys under the game's folder so samples run unconfigured, and the server key file is the only thing a host or game developer must manage: generate (`onegame keys`), keep the private half off the client, publish the public half. A compromised server key means rotating it in the directory or the client build; there is no revocation list, expiry or wall-clock dependency.

**(c) Browsers.** An https page may not open `ws://` (mixed content; `ws://localhost` is exempt), so a deployed web build needs `wss://`, which means TLS terminated by the host with a certificate (a reverse proxy such as Caddy or nginx with an ACME certificate, or a CDN). The engine's WebSocket listener stays plain TCP behind it.

1. *Noise inside WebSocket for everyone, and wss where the page is https (double encryption):* one code path and one identity model on every platform; client static keys work in browsers, which TLS cannot give. Costs: ChaCha20 and X25519 run in the .NET WebAssembly interpreter (measured, see §6.1: about 55 MB/s of bulk crypto and a 12 to 15 ms client-side handshake in the interpreter), and an extra layer a proxy cannot inspect.
2. *wss alone for the web:* TLS authenticates the server through the web PKI and costs nothing in wasm (the browser does it). The browser has no client identity then, so identity would need the auth-blob route only, and the server has two security models to maintain.

**Recommend 1**, with the wasm cost measured at the start of the wiring work; if it proves too slow, switch the web build to option 2 for bulk traffic while keeping the Noise handshake for identity (the handshake hash can be exported as a channel binding). Dev workflow is unaffected (`http://localhost` pages can use `ws://`).

**6.1 Measured in the browser.** Method: the same `OneGame.Core/Crypto` sources built as a `Microsoft.NET.Sdk.WebAssembly` app in Release with the export's settings (interpreter, no AOT, no workload, invariant globalization), served by `tools/web/run-page.mjs` and run in headless Chrome (`--disable-gpu`, no GPU needed), then the same sources as a CoreCLR console app on the same machine minutes apart. Each figure is a loop of at least 1.5 s after a warm-up; "handshake" is both peers in one process, so a client alone pays about half. The host was a busy 32-thread desktop (load average 25 to 40 from other work, Ryzen 9 7950X3D), so treat the ratios as better than the absolute values. Chrome ran three times; the range is shown.

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

## 7. What a path attacker can still do afterwards

- Read the addresses, ports, packet sizes and timing; see that a game handshake happens (the first two datagrams are cleartext) and the connection id. Padding hides handshake sizes only.
- Drop, delay, reorder or duplicate datagrams (duplicates are discarded by the replay window); block a connection entirely; cause a failed connect by forging Reject or Challenge.
- Flood the server with datagrams; each costs one table lookup and at most one failed AEAD open (about 4 µs). Spoofed Requests still cost one HMAC and a 25-byte reply, no memory.
- On WebSocket: inject a TCP reset or Close frame, which ends the connection but is reported as unauthenticated.
- With an unpinned or first-use server key: impersonate the server to a client that has never seen the real key (the usual TOFU limit).
- If the client allows downgrade (option 2 in (a)): force plaintext. Off by default.

It can no longer: read or alter payloads, inject or replay packets into a connection, hijack a session or an identity (binding needs the first authenticated packet, which needs the client's ephemeral key), or learn a player's identity from the handshake without the server's static private key (IK) or at all (XX, from msg 3).

## 8. For the reviewer

Read in this order: `Noise.cs` against spec §5 (state machine, DH order for `es`/`se`, pre-messages, `Split`, failure handling), `ChaCha20Poly1305.cs` (`Open` checks the tag before it decrypts and zeroes the output on failure; in-place use; counters), `X25519.cs` (limb bounds in `Mul` and `Carry`: sums stay under 2^63 because every `Add`/`Sub` carries; `Store`'s canonical reduction), and the cases the vector tests cannot reach: the explicit-counter methods, the 2^64 - 1 counter reservation, `NoiseHandshake` misuse (wrong turn, missing keys throw `InvalidOperationException`/`ArgumentException`; wire errors return false). The fuzz targets are `aead-open` (any bytes; failure must zero the output; seal/open must round trip) and `noise-read` (honest handshake up to a chosen stage, then arbitrary bytes for the next message).
