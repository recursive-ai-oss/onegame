[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# IAuthenticatedChannel interface

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)  
**Implements:** [`IMessageChannel`](IMessageChannel.md)

```csharp
public interface IAuthenticatedChannel : IMessageChannel
```

A channel whose far end proved who it is during the transport handshake. Transports with encryption implement it; the key is the peer's static public key, and only the holder of the private half could have completed the handshake.

## Properties

| Member | Description |
| --- | --- |
| [`RemoteKey`](#m-remotekey) | The peer's authenticated public key, or null on a link without authentication (an `Insecure` transport, a loopback). |

### <a id="m-remotekey"></a>RemoteKey

```csharp
PeerKey? RemoteKey { get; }
```

The peer's authenticated public key, or null on a link without authentication (an `Insecure` transport, a loopback).

**Type:** [`PeerKey`](../../OneGame.Core/OneGame.Core/PeerKey.md)?
