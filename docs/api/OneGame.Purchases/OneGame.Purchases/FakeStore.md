[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# FakeStore class

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)  
**Implements:** [`IPurchaseBackend`](IPurchaseBackend.md), [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class FakeStore : IPurchaseBackend, IDisposable
```

A deterministic, scriptable store that runs in-process on every platform, so a game and its scenarios can test purchase flows with no store account. It follows the rules the real stores share: a non-consumable can be bought once, a consumable cannot be bought again until the last purchase is finished, unfinished purchases come back on a restore, and answers arrive as events in the order the requests were made. Transaction ids are `fake-1`, `fake-2`, and so on.

## Constructors

| Member | Description |
| --- | --- |
| [`FakeStore(IEnumerable<StoreProduct>)`](#m-ctor-ienumerable-storeproduct) | Creates a store selling the given products. |

### <a id="m-ctor-ienumerable-storeproduct"></a>FakeStore(IEnumerable&lt;StoreProduct&gt;)

```csharp
public FakeStore(IEnumerable<StoreProduct> products);
```

Creates a store selling the given products.

**Parameters**

- `products` ([`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)&lt;[`StoreProduct`](StoreProduct.md)&gt;)

## Properties

| Member | Description |
| --- | --- |
| [`ConnectFailure`](#m-connectfailure) | When set, the next [`FakeStore.Connect()`](FakeStore.md#m-connect) fails with this error (and the one after succeeds again unless it is set again). |
| [`Name`](#m-name) | Short store name: `fake`, `storekit`, `play`. |
| [`RestoreIsComplete`](#m-restoreiscomplete) | True when a restore lists every purchase the player still owns (Google Play), so anything owned before and missing now was refunded. |

### <a id="m-connectfailure"></a>ConnectFailure

```csharp
public StoreError? ConnectFailure { get; set; }
```

When set, the next [`FakeStore.Connect()`](FakeStore.md#m-connect) fails with this error (and the one after succeeds again unless it is set again).

**Type:** [`StoreError`](StoreError.md)?

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

Short store name: `fake`, `storekit`, `play`.

**Type:** `string`

### <a id="m-restoreiscomplete"></a>RestoreIsComplete

```csharp
public bool RestoreIsComplete { get; }
```

True when a restore lists every purchase the player still owns (Google Play), so anything owned before and missing now was refunded. False when a restore only replays past purchases (StoreKit).

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Buy(ProductDefinition)`](#m-buy-productdefinition) | Starts the store's purchase flow for a product the store has described. |
| [`Connect()`](#m-connect) | Starts connecting. |
| [`Disconnect()`](#m-disconnect) | Drops the connection, as a store service that died would. |
| [`Dispose()`](#m-dispose) |  |
| [`Finish(PurchaseTransaction, ProductKind)`](#m-finish-purchasetransaction-productkind) | Tells the store the game granted a transaction: consumes it (consumables) or acknowledges it (the rest). |
| [`For(IEnumerable<ProductDefinition>)`](#m-for-ienumerable-productdefinition) | A store that sells every given definition, with generated titles and prices of 0.99 and up in USD. |
| [`LoadProducts(IReadOnlyList<ProductDefinition>)`](#m-loadproducts-ireadonlylist-productdefinition) | Asks the store about the products. |
| [`Owned()`](#m-owned) | The product ids the player owns now (non-consumables and subscriptions that were bought and not revoked), in id order. |
| [`ResolvePending(string, bool)`](#m-resolvepending-string-bool) | Settles a pending purchase: approved becomes [`TransactionState.Purchased`](TransactionState.md#m-purchased), declined becomes [`TransactionState.Failed`](TransactionState.md#m-failed). |
| [`Restore()`](#m-restore) | Asks for the player's existing purchases. |
| [`Revoke(string)`](#m-revoke-string) | Takes a purchase back, as a refund does: the player no longer owns the product and the game is told with a [`TransactionState.Revoked`](TransactionState.md#m-revoked) transaction. |
| [`Script(string, FakeOutcome, StoreError?)`](#m-script-string-fakeoutcome-storeerror) | Queues the outcome of a later purchase of the product. |
| [`TryTake(out StoreEvent)`](#m-trytake-storeevent) | Takes the next event the store produced, if any. |

### <a id="m-buy-productdefinition"></a>Buy(ProductDefinition)

```csharp
public void Buy(ProductDefinition product);
```

Starts the store's purchase flow for a product the store has described. Ends with a [`StoreEventKind.Transaction`](StoreEventKind.md#m-transaction) event.

**Parameters**

- `product` ([`ProductDefinition`](ProductDefinition.md))

### <a id="m-connect"></a>Connect()

```csharp
public void Connect();
```

Starts connecting. Ends with a [`StoreEventKind.Connected`](StoreEventKind.md#m-connected) event, successful or not.

### <a id="m-disconnect"></a>Disconnect()

```csharp
public void Disconnect();
```

Drops the connection, as a store service that died would.

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-finish-purchasetransaction-productkind"></a>Finish(PurchaseTransaction, ProductKind)

```csharp
public void Finish(PurchaseTransaction transaction, ProductKind kind);
```

Tells the store the game granted a transaction: consumes it (consumables) or acknowledges it (the rest). Ends with a [`StoreEventKind.Finished`](StoreEventKind.md#m-finished) event.

**Parameters**

- `transaction` ([`PurchaseTransaction`](PurchaseTransaction.md))
- `kind` ([`ProductKind`](ProductKind.md))

### <a id="m-for-ienumerable-productdefinition"></a>For(IEnumerable&lt;ProductDefinition&gt;)

```csharp
public static FakeStore For(IEnumerable<ProductDefinition> definitions);
```

A store that sells every given definition, with generated titles and prices of 0.99 and up in USD.

**Parameters**

- `definitions` ([`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)&lt;[`ProductDefinition`](ProductDefinition.md)&gt;)

**Returns:** [`FakeStore`](FakeStore.md)

### <a id="m-loadproducts-ireadonlylist-productdefinition"></a>LoadProducts(IReadOnlyList&lt;ProductDefinition&gt;)

```csharp
public void LoadProducts(IReadOnlyList<ProductDefinition> products);
```

Asks the store about the products. Ends with a [`StoreEventKind.ProductsLoaded`](StoreEventKind.md#m-productsloaded) event.

**Parameters**

- `products` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ProductDefinition`](ProductDefinition.md)&gt;)

### <a id="m-owned"></a>Owned()

```csharp
public IReadOnlyList<string> Owned();
```

The product ids the player owns now (non-consumables and subscriptions that were bought and not revoked), in id order.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-resolvepending-string-bool"></a>ResolvePending(string, bool)

```csharp
public bool ResolvePending(string productId, bool approve);
```

Settles a pending purchase: approved becomes [`TransactionState.Purchased`](TransactionState.md#m-purchased), declined becomes [`TransactionState.Failed`](TransactionState.md#m-failed).

**Parameters**

- `productId` (`string`)
- `approve` (`bool`)

**Returns:** `bool` — False when no purchase of that product was pending.

### <a id="m-restore"></a>Restore()

```csharp
public void Restore();
```

Asks for the player's existing purchases. Each arrives as a [`StoreEventKind.Transaction`](StoreEventKind.md#m-transaction) event, then [`StoreEventKind.RestoreFinished`](StoreEventKind.md#m-restorefinished).

### <a id="m-revoke-string"></a>Revoke(string)

```csharp
public bool Revoke(string productId);
```

Takes a purchase back, as a refund does: the player no longer owns the product and the game is told with a [`TransactionState.Revoked`](TransactionState.md#m-revoked) transaction.

**Parameters**

- `productId` (`string`)

**Returns:** `bool` — False when the player has no purchase of it.

### <a id="m-script-string-fakeoutcome-storeerror"></a>Script(string, FakeOutcome, StoreError?)

```csharp
public void Script(string productId, FakeOutcome outcome, StoreError? failure = default);
```

Queues the outcome of a later purchase of the product. Outcomes are used in order, one per purchase; after the script runs out a purchase succeeds.

**Parameters**

- `productId` (`string`): The product.
- `outcome` ([`FakeOutcome`](FakeOutcome.md)): What happens.
- `failure` ([`StoreError`](StoreError.md)?): For [`FakeOutcome.Fail`](FakeOutcome.md#m-fail): the error. Defaults to [`StoreErrorKind.Failed`](StoreErrorKind.md#m-failed).

### <a id="m-trytake-storeevent"></a>TryTake(out StoreEvent)

```csharp
public bool TryTake(out StoreEvent storeEvent);
```

Takes the next event the store produced, if any.

**Parameters**

- `storeEvent` ([`StoreEvent`](StoreEvent.md))

**Returns:** `bool`
