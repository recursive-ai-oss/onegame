[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Stores

# StoreResult enum

**Namespace:** `OneGame.Stores`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public enum StoreResult : int
```

Outcome of a store operation that changes something. Failures are values, not exceptions, because a missing or offline store is a normal state for a shipped game.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-ok"></a>`Ok` | `0` | The store accepted the request. Some stores confirm asynchronously; see the service for details. |
| <a id="m-unavailable"></a>`Unavailable` | `1` | No store is connected (not launched through the store, client not running, library missing). |
| <a id="m-rejected"></a>`Rejected` | `2` | The store rejected the request, for example an unknown achievement or stat name, or a type mismatch. |
| <a id="m-invalidargument"></a>`InvalidArgument` | `3` | An argument was empty, too long or otherwise unusable. Nothing was sent. |
| <a id="m-notfound"></a>`NotFound` | `4` | The named item does not exist. |
| <a id="m-toolarge"></a>`TooLarge` | `5` | The data is larger than the store allows. Nothing was sent. |
| <a id="m-disabled"></a>`Disabled` | `6` | The player turned the feature off (for example cloud saves in the store settings). |
| <a id="m-failed"></a>`Failed` | `7` | The store reported a failure it gave no further reason for, such as an exceeded quota. |
