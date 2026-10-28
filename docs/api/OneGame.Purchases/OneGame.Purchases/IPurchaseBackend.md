[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# IPurchaseBackend interface

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public interface IPurchaseBackend : IDisposable
```

What a purchase backend implements. Requests are made from the game thread; answers may be produced on any thread and are posted to a [`StoreEventBuffer`](StoreEventBuffer.md), which [`PurchaseService.Pump()`](PurchaseService.md#m-pump) drains on the game thread. A request never blocks on the store.

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | Short store name: `fake`, `storekit`, `play`. |
| [`RestoreIsComplete`](#m-restoreiscomplete) | True when a restore lists every purchase the player still owns (Google Play), so anything owned before and missing now was refunded. |

### <a id="m-name"></a>Name

```csharp
string Name { get; }
```

Short store name: `fake`, `storekit`, `play`.

**Type:** `string`

### <a id="m-restoreiscomplete"></a>RestoreIsComplete

```csharp
bool RestoreIsComplete { get; }
```

True when a restore lists every purchase the player still owns (Google Play), so anything owned before and missing now was refunded. False when a restore only replays past purchases (StoreKit).

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Buy(ProductDefinition)`](#m-buy-productdefinition) | Starts the store's purchase flow for a product the store has described. |
| [`Connect()`](#m-connect) | Starts connecting. |
| [`Finish(PurchaseTransaction, ProductKind)`](#m-finish-purchasetransaction-productkind) | Tells the store the game granted a transaction: consumes it (consumables) or acknowledges it (the rest). |
| [`LoadProducts(IReadOnlyList<ProductDefinition>)`](#m-loadproducts-ireadonlylist-productdefinition) | Asks the store about the products. |
| [`Restore()`](#m-restore) | Asks for the player's existing purchases. |
| [`TryTake(out StoreEvent)`](#m-trytake-storeevent) | Takes the next event the store produced, if any. |

### <a id="m-buy-productdefinition"></a>Buy(ProductDefinition)

```csharp
void Buy(ProductDefinition product);
```

Starts the store's purchase flow for a product the store has described. Ends with a [`StoreEventKind.Transaction`](StoreEventKind.md#m-transaction) event.

**Parameters**

- `product` ([`ProductDefinition`](ProductDefinition.md))

### <a id="m-connect"></a>Connect()

```csharp
void Connect();
```

Starts connecting. Ends with a [`StoreEventKind.Connected`](StoreEventKind.md#m-connected) event, successful or not.

### <a id="m-finish-purchasetransaction-productkind"></a>Finish(PurchaseTransaction, ProductKind)

```csharp
void Finish(PurchaseTransaction transaction, ProductKind kind);
```

Tells the store the game granted a transaction: consumes it (consumables) or acknowledges it (the rest). Ends with a [`StoreEventKind.Finished`](StoreEventKind.md#m-finished) event.

**Parameters**

- `transaction` ([`PurchaseTransaction`](PurchaseTransaction.md))
- `kind` ([`ProductKind`](ProductKind.md))

### <a id="m-loadproducts-ireadonlylist-productdefinition"></a>LoadProducts(IReadOnlyList&lt;ProductDefinition&gt;)

```csharp
void LoadProducts(IReadOnlyList<ProductDefinition> products);
```

Asks the store about the products. Ends with a [`StoreEventKind.ProductsLoaded`](StoreEventKind.md#m-productsloaded) event.

**Parameters**

- `products` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ProductDefinition`](ProductDefinition.md)&gt;)

### <a id="m-restore"></a>Restore()

```csharp
void Restore();
```

Asks for the player's existing purchases. Each arrives as a [`StoreEventKind.Transaction`](StoreEventKind.md#m-transaction) event, then [`StoreEventKind.RestoreFinished`](StoreEventKind.md#m-restorefinished).

### <a id="m-trytake-storeevent"></a>TryTake(out StoreEvent)

```csharp
bool TryTake(out StoreEvent storeEvent);
```

Takes the next event the store produced, if any.

**Parameters**

- `storeEvent` ([`StoreEvent`](StoreEvent.md))

**Returns:** `bool`
