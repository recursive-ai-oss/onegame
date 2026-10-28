[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# DifferenceChange enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum DifferenceChange : byte
```

How a part of the state differs between two digests.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-changed"></a>`Changed` | `0` | Present on both sides with different state. |
| <a id="m-onlyinexpected"></a>`OnlyInExpected` | `1` | Present only in the expected state (the authority, or the recording). |
| <a id="m-onlyinactual"></a>`OnlyInActual` | `2` | Present only in the actual state (the replica, or the replay). |
