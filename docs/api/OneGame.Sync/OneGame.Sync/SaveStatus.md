[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# SaveStatus enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum SaveStatus : int
```

How loading a save ended.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-ok"></a>`Ok` | `0` | Loaded. |
| <a id="m-badfile"></a>`BadFile` | `1` | The file is not a save, or is cut short. |
| <a id="m-unsupportedformat"></a>`UnsupportedFormat` | `2` | The file's container format is newer than this build reads. |
| <a id="m-corrupt"></a>`Corrupt` | `3` | The payload does not match its recorded hash: the file is damaged. |
| <a id="m-seedmismatch"></a>`SeedMismatch` | `4` | The save was made with another world seed than the world it is loaded into. |
| <a id="m-restorefailed"></a>`RestoreFailed` | `5` | The snapshot could not be decoded (unknown type, a schema with no migration, bad data). |
| <a id="m-hashmismatch"></a>`HashMismatch` | `6` | The loaded state does not hash to the recorded hash. |
