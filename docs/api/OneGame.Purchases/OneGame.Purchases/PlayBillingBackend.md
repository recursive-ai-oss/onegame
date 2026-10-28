[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# PlayBillingBackend class

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)  
**Implements:** [`IPurchaseBackend`](IPurchaseBackend.md), [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class PlayBillingBackend : IPurchaseBackend, IDisposable
```

Google Play Billing on Android. The Billing Library is Java, so the app carries one small class of ours (`dev.onegame.store.PlayBridge`) that owns the `BillingClient` and queues every answer as a line of JSON; this class calls it through JNI and reads the lines when the game pumps. Requests and [`PlayBillingBackend.TryTake()`](PlayBillingBackend.md#m-trytake-storeevent) must run on the game thread, the one the platform layer attached to the Java VM. The export step adds the class, the Billing Library and the manifest entries when the game opts in (see `docs/interfaces/store.md`).

## Constructors

| Member | Description |
| --- | --- |
| [`PlayBillingBackend()`](#m-ctor) | Creates the backend and the Play billing client. |

### <a id="m-ctor"></a>PlayBillingBackend()

```csharp
public PlayBillingBackend();
```

Creates the backend and the Play billing client.

**Exceptions**

- [`PlatformNotSupportedException`](https://learn.microsoft.com/dotnet/api/system.platformnotsupportedexception): Not running as an Android app.

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
