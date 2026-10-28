[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Storage

# SlotKind enum

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum SlotKind : byte
```

Why a slot was written.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-manual"></a>`Manual` | `0` | The player chose to save. |
| <a id="m-auto"></a>`Auto` | `1` | The game saved by itself (see [`SaveSlots.Autosave()`](SaveSlots.md#m-autosave-world-string)). |
| <a id="m-quick"></a>`Quick` | `2` | A quick save (see [`SaveSlots.QuickSave()`](SaveSlots.md#m-quicksave-world-string)). |
