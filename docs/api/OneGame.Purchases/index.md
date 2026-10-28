[API reference](../index.md) › OneGame.Purchases

# OneGame.Purchases

In-app purchases: one store-neutral API over StoreKit, Google Play Billing and a scriptable fake store.

**Package:** `RecursiveAI.OneGame.Purchases`

```sh
dotnet add package RecursiveAI.OneGame.Purchases
```

## OneGame.Purchases

| Type | Kind | Description |
| --- | --- | --- |
| [`FakeOutcome`](OneGame.Purchases/FakeOutcome.md) | enum | What the fake store does with the next purchase of a product. |
| [`FakeStore`](OneGame.Purchases/FakeStore.md) | class | A deterministic, scriptable store that runs in-process on every platform, so a game and its scenarios can test purchase flows with no store account. |
| [`IPurchaseBackend`](OneGame.Purchases/IPurchaseBackend.md) | interface | What a purchase backend implements. |
| [`PlayBillingBackend`](OneGame.Purchases/PlayBillingBackend.md) | class | Google Play Billing on Android. |
| [`ProductDefinition`](OneGame.Purchases/ProductDefinition.md) | struct | A product the game sells, declared by the game and looked up in the store by [`ProductDefinition.Id`](OneGame.Purchases/ProductDefinition.md#m-id). |
| [`ProductKind`](OneGame.Purchases/ProductKind.md) | enum | What a product is. |
| [`PurchaseBackends`](OneGame.Purchases/PurchaseBackends.md) | static class | Chooses the purchase backend for the running platform. |
| [`PurchaseService`](OneGame.Purchases/PurchaseService.md) | class | In-app purchases for a game: one API over the platform's store. |
| [`PurchaseStatus`](OneGame.Purchases/PurchaseStatus.md) | record | A snapshot of the service for logs and the harness. |
| [`PurchaseTransaction`](OneGame.Purchases/PurchaseTransaction.md) | record | One purchase as the store reports it. |
| [`StoreConnection`](OneGame.Purchases/StoreConnection.md) | enum | Where the connection to the store stands. |
| [`StoreError`](OneGame.Purchases/StoreError.md) | struct | A failure value: what went wrong and the store's own message for logs. |
| [`StoreErrorKind`](OneGame.Purchases/StoreErrorKind.md) | enum | The reasons a store operation fails, in words a game can branch on. |
| [`StoreEvent`](OneGame.Purchases/StoreEvent.md) | record | One thing the store told the game. |
| [`StoreEventBuffer`](OneGame.Purchases/StoreEventBuffer.md) | class | A thread-safe, bounded queue that carries events from a store's threads to the game thread. |
| [`StoreEventKind`](OneGame.Purchases/StoreEventKind.md) | enum | What kind of thing happened in the store. |
| [`StoreKitBackend`](OneGame.Purchases/StoreKitBackend.md) | class | The App Store on iOS and macOS through StoreKit's Objective-C API (the original, transaction-queue StoreKit), reached with the Objective-C runtime like the rest of the Apple platform code. |
| [`StoreLimits`](OneGame.Purchases/StoreLimits.md) | static class | Limits on everything read from a store, so a hostile or broken store cannot make the game allocate without bound. |
| [`StoreProduct`](OneGame.Purchases/StoreProduct.md) | record | A product as the store describes it to this player: localized text and a price already formatted for their country. |
| [`TransactionState`](OneGame.Purchases/TransactionState.md) | enum | Where a transaction stands. |
| [`UnavailableStore`](OneGame.Purchases/UnavailableStore.md) | class | A store that is not there: connecting fails with [`StoreErrorKind.Unavailable`](OneGame.Purchases/StoreErrorKind.md#m-unavailable) and says why. |
