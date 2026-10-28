[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# NetSecurity class

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)

```csharp
public sealed class NetSecurity
```

How a transport secures its links, passed as `UdpOptions.Security` or `WebSocketOptions.Security`. A transport opened without one throws [`SecurityNotConfiguredException`](SecurityNotConfiguredException.md): there is no silent plaintext default.

- **Server:**[`NetSecurity.ForServer()`](NetSecurity.md#m-forserver-netkeypair-string-netkeypair) with the server's key pair. Clients pin its [`NetKeyPair.PublicKey`](../../OneGame.Core/OneGame.Core/NetKeyPair.md#m-publickey).
- **Client:**[`NetSecurity.ForClient()`](NetSecurity.md#m-forclient-ienumerable-peerkey-string-netkeypair) with the server public keys it trusts (a set, so a server can rotate keys) and optionally a key pair that names this player.
- **Tests and local tools:**[`NetSecurity.Insecure`](NetSecurity.md#m-insecure).

The handshake is Noise IK (`Noise_IK_25519_ChaChaPoly_SHA256`): the client already knows the server's key, so it is one round trip, and the client's key is sent encrypted. See `docs/interfaces/transport-security.md`.

## Constructors

| Member | Description |
| --- | --- |
| [`NetSecurity()`](#m-ctor) |  |

### <a id="m-ctor"></a>NetSecurity()

```csharp
public NetSecurity();
```

## Properties

| Member | Description |
| --- | --- |
| [`GameId`](#m-gameid) | Names the game and version of the protocol the peers speak. |
| [`Identity`](#m-identity) | Client side: the key pair that names this player. |
| [`Insecure`](#m-insecure) | Plaintext links, for tests and local tools. |
| [`Mode`](#m-mode) | Required or Insecure. |
| [`PerServerIdentity`](#m-perserveridentity) | Client side: when true (the default of [`NetSecurity.ForClient()`](NetSecurity.md#m-forclient-ienumerable-peerkey-string-netkeypair)), the key a server sees is derived from [`NetSecurity.Identity`](NetSecurity.md#m-identity) and that server's public key ([`NetKeyPair.DeriveFor()`](../../OneGame.Core/OneGame.Core/NetKeyPair.md#m-derivefor-peerkey)), so two servers cannot tell they are talking to the same player by comparing keys. |
| [`ServerKeys`](#m-serverkeys) | Server (listener) side: the key pairs this server answers as. |
| [`TrustedServerKeys`](#m-trustedserverkeys) | Client (connecting) side: the server public keys this client accepts, in order of preference. |

### <a id="m-gameid"></a>GameId

```csharp
public string GameId { get; init; }
```

Names the game and version of the protocol the peers speak. It is mixed into the handshake, so peers with different values cannot complete one; use the same text on both ends (for example `"my-game/3"`). Default: empty.

**Type:** `string`

### <a id="m-identity"></a>Identity

```csharp
public NetKeyPair? Identity { get; init; }
```

Client side: the key pair that names this player. The server sees its public key after the handshake and can map it to a player ([`PlayerRegistry`](../../OneGame.Sync/OneGame.Sync/PlayerRegistry.md)), which is how a player stays the same across reconnects. Keep it in the game's save storage. If null, each connecting transport makes a fresh key pair, so identity lasts one connection object.

**Type:** [`NetKeyPair`](../../OneGame.Core/OneGame.Core/NetKeyPair.md)?

### <a id="m-insecure"></a>Insecure

```csharp
public static NetSecurity Insecure { get; }
```

Plaintext links, for tests and local tools. Search for it to find every place a game opts out.

**Type:** [`NetSecurity`](NetSecurity.md)

### <a id="m-mode"></a>Mode

```csharp
public SecurityMode Mode { get; init; }
```

Required or Insecure.

**Type:** [`SecurityMode`](SecurityMode.md)

### <a id="m-perserveridentity"></a>PerServerIdentity

```csharp
public bool PerServerIdentity { get; init; }
```

Client side: when true (the default of [`NetSecurity.ForClient()`](NetSecurity.md#m-forclient-ienumerable-peerkey-string-netkeypair)), the key a server sees is derived from [`NetSecurity.Identity`](NetSecurity.md#m-identity) and that server's public key ([`NetKeyPair.DeriveFor()`](../../OneGame.Core/OneGame.Core/NetKeyPair.md#m-derivefor-peerkey)), so two servers cannot tell they are talking to the same player by comparing keys. Server-to-server links turn it off, because peers name each other by their real keys.

**Type:** `bool`

### <a id="m-serverkeys"></a>ServerKeys

```csharp
public IReadOnlyList<NetKeyPair> ServerKeys { get; init; }
```

Server (listener) side: the key pairs this server answers as. The first is its primary key. More than one lets a server rotate: add the new pair, ship clients the new public key, and remove the old pair later.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`NetKeyPair`](../../OneGame.Core/OneGame.Core/NetKeyPair.md)&gt;

### <a id="m-trustedserverkeys"></a>TrustedServerKeys

```csharp
public IReadOnlyList<PeerKey> TrustedServerKeys { get; init; }
```

Client (connecting) side: the server public keys this client accepts, in order of preference. A connection attempt starts with the first and tries the next after a rejection, so a rotation can overlap.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`AlsoTrusting(IEnumerable<PeerKey>)`](#m-alsotrusting-ienumerable-peerkey) | The same settings for a server that also connects to other servers (a game's own servers talking to each other). |
| [`ForClient(IEnumerable<PeerKey>, string, NetKeyPair?)`](#m-forclient-ienumerable-peerkey-string-netkeypair) | Settings for a connecting client. |
| [`ForServer(NetKeyPair, string, NetKeyPair[])`](#m-forserver-netkeypair-string-netkeypair) | Settings for a listening server. |
| [`IdentityKeyFor(PeerKey)`](#m-identitykeyfor-peerkey) | The key a server with public key `serverKey` sees for this client: what that server's player registry is keyed by. |

### <a id="m-alsotrusting-ienumerable-peerkey"></a>AlsoTrusting(IEnumerable&lt;PeerKey&gt;)

```csharp
public NetSecurity AlsoTrusting(IEnumerable<PeerKey> trusted);
```

The same settings for a server that also connects to other servers (a game's own servers talking to each other).

**Parameters**

- `trusted` ([`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)&lt;[`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)&gt;): Public keys of the servers this one connects to.

**Returns:** [`NetSecurity`](NetSecurity.md)

### <a id="m-forclient-ienumerable-peerkey-string-netkeypair"></a>ForClient(IEnumerable&lt;PeerKey&gt;, string, NetKeyPair?)

```csharp
public static NetSecurity ForClient(IEnumerable<PeerKey> serverKeys, string gameId = "", NetKeyPair? identity = null);
```

Settings for a connecting client.

**Parameters**

- `serverKeys` ([`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)&lt;[`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)&gt;): The server public keys this client trusts, preferred first.
- `gameId` (`string`): The game and protocol name both ends use.
- `identity` ([`NetKeyPair`](../../OneGame.Core/OneGame.Core/NetKeyPair.md)?): This player's key pair, kept between runs; null for a fresh one per connection.

**Returns:** [`NetSecurity`](NetSecurity.md)

### <a id="m-forserver-netkeypair-string-netkeypair"></a>ForServer(NetKeyPair, string, NetKeyPair\[\])

```csharp
public static NetSecurity ForServer(NetKeyPair key, string gameId = "", params NetKeyPair[] more);
```

Settings for a listening server.

**Parameters**

- `key` ([`NetKeyPair`](../../OneGame.Core/OneGame.Core/NetKeyPair.md)): The server's key pair (its private half is a secret).
- `gameId` (`string`): The game and protocol name both ends use.
- `more` ([`NetKeyPair`](../../OneGame.Core/OneGame.Core/NetKeyPair.md)\[\]): Further key pairs the server also answers as, for rotation.

**Returns:** [`NetSecurity`](NetSecurity.md)

### <a id="m-identitykeyfor-peerkey"></a>IdentityKeyFor(PeerKey)

```csharp
public PeerKey IdentityKeyFor(PeerKey serverKey);
```

The key a server with public key `serverKey` sees for this client: what that server's player registry is keyed by. Empty when there is no [`NetSecurity.Identity`](NetSecurity.md#m-identity) yet.

**Parameters**

- `serverKey` ([`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)): The server's public key.

**Returns:** [`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)
