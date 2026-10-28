[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# PeerIdentity struct

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PeerIdentity`](PeerIdentity.md)&gt;

```csharp
public readonly struct PeerIdentity : IEquatable<PeerIdentity>
```

Who is on the other end of an accepted channel, as far as the transport can prove.

## Constructors

| Member | Description |
| --- | --- |
| [`PeerIdentity(PeerKey, string)`](#m-ctor-peerkey-string) | Who is on the other end of an accepted channel, as far as the transport can prove. |

### <a id="m-ctor-peerkey-string"></a>PeerIdentity(PeerKey, string)

```csharp
public PeerIdentity(PeerKey Key, string Address);
```

Who is on the other end of an accepted channel, as far as the transport can prove.

**Parameters**

- `Key` ([`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)): The peer's authenticated public key; [`PeerKey.IsEmpty`](../../OneGame.Core/OneGame.Core/PeerKey.md#m-isempty) when the link has no authentication.
- `Address` (`string`): The peer's network address as text, for logs. It is not an identity: addresses change and can be shared.

## Properties

| Member | Description |
| --- | --- |
| [`Address`](#m-address) | The peer's network address as text, for logs. |
| [`IsAuthenticated`](#m-isauthenticated) | True when [`PeerIdentity.Key`](PeerIdentity.md#m-key) was proven by a handshake. |
| [`Key`](#m-key) | The peer's authenticated public key; [`PeerKey.IsEmpty`](../../OneGame.Core/OneGame.Core/PeerKey.md#m-isempty) when the link has no authentication. |

### <a id="m-address"></a>Address

```csharp
public string Address { get; init; }
```

The peer's network address as text, for logs. It is not an identity: addresses change and can be shared.

**Type:** `string`

### <a id="m-isauthenticated"></a>IsAuthenticated

```csharp
public bool IsAuthenticated { get; }
```

True when [`PeerIdentity.Key`](PeerIdentity.md#m-key) was proven by a handshake.

**Type:** `bool`

### <a id="m-key"></a>Key

```csharp
public PeerKey Key { get; init; }
```

The peer's authenticated public key; [`PeerKey.IsEmpty`](../../OneGame.Core/OneGame.Core/PeerKey.md#m-isempty) when the link has no authentication.

**Type:** [`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)

## Methods

| Member | Description |
| --- | --- |
| [`Of(IMessageChannel, string)`](#m-of-imessagechannel-string) | Reads the identity of a channel (an empty key for channels that cannot prove one). |

### <a id="m-of-imessagechannel-string"></a>Of(IMessageChannel, string)

```csharp
public static PeerIdentity Of(IMessageChannel channel, string address);
```

Reads the identity of a channel (an empty key for channels that cannot prove one).

**Parameters**

- `channel` ([`IMessageChannel`](IMessageChannel.md)): The accepted channel.
- `address` (`string`): The peer's address, for logs.

**Returns:** [`PeerIdentity`](PeerIdentity.md)
