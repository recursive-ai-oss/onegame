[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# RefusalReason enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum RefusalReason : byte
```

Why a command or subscription was refused.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-nottrustedtocommand"></a>`NotTrustedToCommand` | `1` | The connection's trust does not allow commands to the scope. |
| <a id="m-nottrustedtoread"></a>`NotTrustedToRead` | `2` | The connection's trust does not allow reading the scope. |
| <a id="m-unknownscope"></a>`UnknownScope` | `3` | The authority node has no such scope. |
| <a id="m-malformed"></a>`Malformed` | `4` | The command bytes were not a valid command. |
| <a id="m-nottrustedtoown"></a>`NotTrustedToOwn` | `5` | The command is `OwnerOnly` and the connection's trust does not own the scope. |
| <a id="m-notowner"></a>`NotOwner` | `6` | The command is `OwnerOnly` and the connection's player is not the target entity's owner. |
| <a id="m-readrevoked"></a>`ReadRevoked` | `7` | The connection's trust no longer allows reading the scope, so the authority stopped sending it. |
| <a id="m-transfernotaccepted"></a>`TransferNotAccepted` | `8` | An authority transfer was not accepted: the scope has no transfer log or the destination's gate said no. |
