[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Durability enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum Durability : int
```

When a scope lets answers and confirmations leave relative to saving its state.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-immediate"></a>`Immediate` | `0` | Send as soon as it happens. |
| <a id="m-aftersave"></a>`AfterSave` | `1` | Hold command answers and transfer confirmations until [`AuthorityScope.Save()`](AuthorityScope.md#m-save) has captured the state they describe. |
