[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SyncError enum

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public enum SyncError : int
```

Why decoding untrusted bytes failed. Decoders never throw on bad input; they set the first error and stop.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | No error. |
| <a id="m-truncated"></a>`Truncated` | `1` | The input ended before the value was complete. |
| <a id="m-varintoverflow"></a>`VarIntOverflow` | `2` | A variable-length integer was longer than its type allows. |
| <a id="m-limitexceeded"></a>`LimitExceeded` | `3` | A length, count or size was above its limit, or larger than the bytes left in the input. |
| <a id="m-invalidvalue"></a>`InvalidValue` | `4` | A value was outside what its type allows (a bool that is not 0 or 1, an integer out of range, bad UTF-8, unknown delta bits). |
| <a id="m-badheader"></a>`BadHeader` | `5` | The input did not start with the expected magic bytes or format version. |
| <a id="m-unknowntype"></a>`UnknownType` | `6` | An entity or table type in the input is not registered. |
| <a id="m-unsupportedversion"></a>`UnsupportedVersion` | `7` | The input holds a schema version this build cannot read (newer than the code, or older with no migration). |
| <a id="m-trailingbytes"></a>`TrailingBytes` | `8` | Bytes were left over after a value that should have used the whole input. |
| <a id="m-mismatch"></a>`Mismatch` | `9` | The input refers to an entity or table that does not exist or does not match. |
