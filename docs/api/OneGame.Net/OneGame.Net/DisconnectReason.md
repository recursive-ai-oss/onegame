[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# DisconnectReason enum

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)

```csharp
public enum DisconnectReason : byte
```

Why a connection ended. Values travel on the wire, so they never change meaning.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | Not closed yet. |
| <a id="m-normal"></a>`Normal` | `1` | One side closed on purpose. |
| <a id="m-timeout"></a>`Timeout` | `2` | Nothing was heard from the peer for the timeout. |
| <a id="m-connecttimeout"></a>`ConnectTimeout` | `3` | The handshake did not finish in time. |
| <a id="m-protocolviolation"></a>`ProtocolViolation` | `4` | The peer sent something the protocol forbids (an oversize message, a bad frame sequence). |
| <a id="m-sendqueuefull"></a>`SendQueueFull` | `5` | More was queued to send than the configured limit; the peer is too slow or gone. |
| <a id="m-serverfull"></a>`ServerFull` | `6` | The server has no room for another connection. |
| <a id="m-versionmismatch"></a>`VersionMismatch` | `7` | The peer speaks a protocol version this side does not. |
| <a id="m-rejected"></a>`Rejected` | `8` | The server refused the handshake. |
| <a id="m-kicked"></a>`Kicked` | `9` | The server removed this client. |
| <a id="m-transporterror"></a>`TransportError` | `10` | The operating system reported a socket failure. |
| <a id="m-shutdown"></a>`Shutdown` | `11` | The peer is shutting down. |
| <a id="m-messagetoolarge"></a>`MessageTooLarge` | `12` | A message was larger than the limit. |
| <a id="m-securityrequired"></a>`SecurityRequired` | `13` | One side runs with encryption and the other without; neither falls back to the other's mode. |
| <a id="m-handshakefailed"></a>`HandshakeFailed` | `14` | The secure handshake failed: the server did not recognise the key the client trusts, or the game ids differ. |
| <a id="m-packetlimit"></a>`PacketLimit` | `15` | The connection used up its packet numbers (2^32 packets in one direction); connect again for fresh keys. |
