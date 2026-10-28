[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# StoreErrorKind enum

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)

```csharp
public enum StoreErrorKind : int
```

The reasons a store operation fails, in words a game can branch on.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-none"></a>`None` | `0` | No error. |
| <a id="m-unavailable"></a>`Unavailable` | `1` | The store is not there: not connected, not signed in, purchases switched off, or the library is missing. |
| <a id="m-network"></a>`Network` | `2` | The store could not be reached. Trying again later may work. |
| <a id="m-productnotfound"></a>`ProductNotFound` | `3` | The store does not sell that product (misspelled id, not published for this build or country). |
| <a id="m-alreadyowned"></a>`AlreadyOwned` | `4` | The player already owns it, or has an unfinished purchase of it. |
| <a id="m-notallowed"></a>`NotAllowed` | `5` | The player or device may not buy (parental controls, billing not supported). |
| <a id="m-invalidrequest"></a>`InvalidRequest` | `6` | The call was malformed or the game set the store up wrongly (unknown product, missing account setup). |
| <a id="m-timeout"></a>`Timeout` | `7` | The store did not answer in time. |
| <a id="m-failed"></a>`Failed` | `8` | The store failed and gave no better reason; [`StoreError.Message`](StoreError.md#m-message) has its text. |
