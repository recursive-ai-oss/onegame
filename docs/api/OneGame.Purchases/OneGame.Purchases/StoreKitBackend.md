[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# StoreKitBackend class

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)  
**Implements:** [`IPurchaseBackend`](IPurchaseBackend.md), [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class StoreKitBackend : IPurchaseBackend, IDisposable
```

The App Store on iOS and macOS through StoreKit's Objective-C API (the original, transaction-queue StoreKit), reached with the Objective-C runtime like the rest of the Apple platform code. StoreKit 2 is Swift-only, so using it would mean building and shipping a Swift library next to a NativeAOT game; the original API is deprecated for new features but still ships in every system and covers everything a game sells: products, purchases, restores, unfinished transactions and the app receipt for server checks. Requests run on the main thread, which is also where StoreKit calls back.

## Constructors

| Member | Description |
| --- | --- |
| [`StoreKitBackend()`](#m-ctor) | Creates the backend and registers the observer class with the Objective-C runtime. |

### <a id="m-ctor"></a>StoreKitBackend()

```csharp
public StoreKitBackend();
```

Creates the backend and registers the observer class with the Objective-C runtime.

**Exceptions**

- [`PlatformNotSupportedException`](https://learn.microsoft.com/dotnet/api/system.platformnotsupportedexception): Not running on an Apple platform, or StoreKit could not be loaded.

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | Short store name: `fake`, `storekit`, `play`. |
| [`RestoreIsComplete`](#m-restoreiscomplete) | True when a restore lists every purchase the player still owns (Google Play), so anything owned before and missing now was refunded. |

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
| [`Dispose()`](#m-dispose) |  |
| [`Finish(PurchaseTransaction, ProductKind)`](#m-finish-purchasetransaction-productkind) | Tells the store the game granted a transaction: consumes it (consumables) or acknowledges it (the rest). |
| [`LoadProducts(IReadOnlyList<ProductDefinition>)`](#m-loadproducts-ireadonlylist-productdefinition) | Asks the store about the products. |
| [`Restore()`](#m-restore) | Asks for the player's existing purchases. |
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

### <a id="m-loadproducts-ireadonlylist-productdefinition"></a>LoadProducts(IReadOnlyList&lt;ProductDefinition&gt;)

```csharp
public void LoadProducts(IReadOnlyList<ProductDefinition> products);
```

Asks the store about the products. Ends with a [`StoreEventKind.ProductsLoaded`](StoreEventKind.md#m-productsloaded) event.

**Parameters**

- `products` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ProductDefinition`](ProductDefinition.md)&gt;)

### <a id="m-restore"></a>Restore()

```csharp
public void Restore();
```

Asks for the player's existing purchases. Each arrives as a [`StoreEventKind.Transaction`](StoreEventKind.md#m-transaction) event, then [`StoreEventKind.RestoreFinished`](StoreEventKind.md#m-restorefinished).

### <a id="m-trytake-storeevent"></a>TryTake(out StoreEvent)

```csharp
public bool TryTake(out StoreEvent storeEvent);
```

Takes the next event the store produced, if any.

**Parameters**

- `storeEvent` ([`StoreEvent`](StoreEvent.md))

**Returns:** `bool`
