[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Storage

# SlotStatus enum

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum SlotStatus : int
```

How a slot operation ended.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-ok"></a>`Ok` | `0` | It worked. |
| <a id="m-nosuchslot"></a>`NoSuchSlot` | `1` | There is no slot of that name. |
| <a id="m-invalidname"></a>`InvalidName` | `2` | The slot name is not allowed (see [`SaveSlots.IsValidSlotName()`](SaveSlots.md#m-isvalidslotname-string)). |
| <a id="m-writefailed"></a>`WriteFailed` | `3` | The store could not write the file. The slot's earlier content, if any, is intact. |
| <a id="m-toolarge"></a>`TooLarge` | `4` | The slot file is larger than [`SaveSlotOptions.MaxSaveBytes`](SaveSlotOptions.md#m-maxsavebytes). |
| <a id="m-badslotfile"></a>`BadSlotFile` | `5` | The file is not a slot, or is cut short or damaged. |
| <a id="m-unsupportedformat"></a>`UnsupportedFormat` | `6` | The slot file's format is newer than this build reads. |
| <a id="m-saverejected"></a>`SaveRejected` | `7` | The save inside was refused by the world (see [`SlotResult.Load`](SlotResult.md#m-load)). The world is unspecified: discard it. |
