[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# ConflictPolicy enum

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public enum ConflictPolicy : byte
```

What to do with other actions that already use a source being assigned.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-allow"></a>`Allow` | `0` | Leave them; both actions fire. Conflicts are still reported. |
| <a id="m-replace"></a>`Replace` | `1` | Take the source away from the other actions in the same context. |
