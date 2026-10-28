[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# Visibility enum

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public enum Visibility : int
```

Who may receive an entity or one of its synced properties. The authority enforces it when it builds snapshots and deltas for a subscriber; a value that is not sent never reaches the subscriber's bytes at all. Every level is on top of the scope's read trust.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-everyone"></a>`Everyone` | `0` | Every subscriber that may read the scope. |
| <a id="m-owner"></a>`Owner` | `1` | Only the owner of the entity (the connection whose player matches the entity's [`OwnerKeyAttribute`](OwnerKeyAttribute.md) property). |
| <a id="m-team"></a>`Team` | `2` | The owner and everyone on the entity's team ([`TeamKeyAttribute`](TeamKeyAttribute.md) property matches the connection's team). |
| <a id="m-hidden"></a>`Hidden` | `3` | Never leaves the authority: no subscriber receives it, whatever its trust. |
