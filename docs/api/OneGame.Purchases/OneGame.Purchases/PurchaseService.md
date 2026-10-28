[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# PurchaseService class

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class PurchaseService : IDisposable
```

In-app purchases for a game: one API over the platform's store. A game declares its products, connects, loads them, buys and finishes. Everything the store says arrives as [`StoreEvent`](StoreEvent.md)s from [`PurchaseService.Pump()`](PurchaseService.md#m-pump), which the game calls once per frame on its game thread; requests may be made from that thread too. Pick the backend with [`PurchaseBackends`](PurchaseBackends.md).

Purchases are view-side and asynchronous: never call this from sim code. Grant the purchased item through a game command, wait until it is saved, and only then call [`PurchaseService.Finish()`](PurchaseService.md#m-finish-purchasetransaction); the stores refund purchases that are never finished.

**Example**

```csharp
var catalog = new[] { new ProductDefinition("coins_100", ProductKind.Consumable), new ProductDefinition("no_ads", ProductKind.NonConsumable) };
using var store = PurchaseBackends.CreateService(catalog);   // ONEGAME_STORE=fake selects the in-process fake
store.Event += e =>
{
    if (e.Kind == StoreEventKind.Connected) { store.LoadProducts(); }
    if (e.Kind == StoreEventKind.Transaction && e.Transaction is { NeedsFinish: true } t)
    {
        // grant the item with a command, save, then:
        store.Finish(t);
    }
};
store.Connect();
// Once per frame on the game thread: store.Pump();
// From a button: var error = store.Buy("coins_100");
```

## Constructors

| Member | Description |
| --- | --- |
| [`PurchaseService(IPurchaseBackend, IEnumerable<ProductDefinition>)`](#m-ctor-ipurchasebackend-ienumerable-productdefinition) | Creates a service over a backend. |

### <a id="m-ctor-ipurchasebackend-ienumerable-productdefinition"></a>PurchaseService(IPurchaseBackend, IEnumerable&lt;ProductDefinition&gt;)

```csharp
public PurchaseService(IPurchaseBackend backend, IEnumerable<ProductDefinition> catalog);
```

Creates a service over a backend. The catalog lists every product the game sells; ids must be unique and non-empty.

**Parameters**

- `backend` ([`IPurchaseBackend`](IPurchaseBackend.md))
- `catalog` ([`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)&lt;[`ProductDefinition`](ProductDefinition.md)&gt;)

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): An id is empty, too long, or listed twice.

## Properties

| Member | Description |
| --- | --- |
| [`Active`](#m-active) | The most recently created service that has not been disposed; the harness uses it when a game does nothing special. |
| [`Backend`](#m-backend) | The backend, for tests that script a fake store. |
| [`ConnectError`](#m-connecterror) | Why the last connect failed; [`StoreError.None`](StoreError.md#m-none) otherwise. |
| [`Connection`](#m-connection) | Connection state. |
| [`Name`](#m-name) | The backend's short name. |
| [`Products`](#m-products) | The products the store described, in id order. |
| [`Unfinished`](#m-unfinished) | Transactions the game has been told about but not finished, in transaction id order. |

### <a id="m-active"></a>Active

```csharp
public static PurchaseService? Active { get; }
```

The most recently created service that has not been disposed; the harness uses it when a game does nothing special.

**Type:** [`PurchaseService`](PurchaseService.md)?

### <a id="m-backend"></a>Backend

```csharp
public IPurchaseBackend Backend { get; }
```

The backend, for tests that script a fake store.

**Type:** [`IPurchaseBackend`](IPurchaseBackend.md)

### <a id="m-connecterror"></a>ConnectError

```csharp
public StoreError ConnectError { get; }
```

Why the last connect failed; [`StoreError.None`](StoreError.md#m-none) otherwise.

**Type:** [`StoreError`](StoreError.md)

### <a id="m-connection"></a>Connection

```csharp
public StoreConnection Connection { get; }
```

Connection state.

**Type:** [`StoreConnection`](StoreConnection.md)

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The backend's short name.

**Type:** `string`

### <a id="m-products"></a>Products

```csharp
public IReadOnlyList<StoreProduct> Products { get; }
```

The products the store described, in id order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`StoreProduct`](StoreProduct.md)&gt;

### <a id="m-unfinished"></a>Unfinished

```csharp
public IReadOnlyList<PurchaseTransaction> Unfinished { get; }
```

Transactions the game has been told about but not finished, in transaction id order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`PurchaseTransaction`](PurchaseTransaction.md)&gt;

## Events

| Member | Description |
| --- | --- |
| [`Event`](#m-event) | Raised from [`PurchaseService.Pump()`](PurchaseService.md#m-pump) for every store event, after the service updated its own state. |

### <a id="m-event"></a>Event

```csharp
public event Action<StoreEvent>? Event;
```

Raised from [`PurchaseService.Pump()`](PurchaseService.md#m-pump) for every store event, after the service updated its own state.

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`StoreEvent`](StoreEvent.md)&gt;?

## Methods

| Member | Description |
| --- | --- |
| [`Buy(string)`](#m-buy-string) | Starts a purchase. |
| [`Connect()`](#m-connect) | Starts connecting to the store. |
| [`Describe()`](#m-describe) | A snapshot for logs and the harness. |
| [`Dispose()`](#m-dispose) |  |
| [`Finish(PurchaseTransaction)`](#m-finish-purchasetransaction) | Tells the store the game granted a purchased or restored transaction. |
| [`IsOwned(string)`](#m-isowned-string) | True when a non-consumable or subscription is owned: a purchase or restore arrived and no revocation since. |
| [`IsPending(string)`](#m-ispending-string) | True when a purchase of the product is waiting on approval outside the game. |
| [`LoadProducts()`](#m-loadproducts) | Asks the store about every product in the catalog. |
| [`Pump()`](#m-pump) | Delivers what the store reported since the last call: updates the service's state and raises [`PurchaseService.Event`](PurchaseService.md#m-event) for each. |
| [`Restore()`](#m-restore) | Asks the store for the player's existing purchases. |

### <a id="m-buy-string"></a>Buy(string)

```csharp
public StoreError Buy(string productId);
```

Starts a purchase. The result arrives as a [`StoreEventKind.Transaction`](StoreEventKind.md#m-transaction) event.

**Parameters**

- `productId` (`string`): A product id from the catalog.

**Returns:** [`StoreError`](StoreError.md) — An error when the purchase was not started: not connected, not in the catalog, or not loaded from the store yet. [`StoreError.None`](StoreError.md#m-none) when it started.

### <a id="m-connect"></a>Connect()

```csharp
public void Connect();
```

Starts connecting to the store. The answer is a [`StoreEventKind.Connected`](StoreEventKind.md#m-connected) event.

### <a id="m-describe"></a>Describe()

```csharp
public PurchaseStatus Describe();
```

A snapshot for logs and the harness.

**Returns:** [`PurchaseStatus`](PurchaseStatus.md)

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-finish-purchasetransaction"></a>Finish(PurchaseTransaction)

```csharp
public StoreError Finish(PurchaseTransaction transaction);
```

Tells the store the game granted a purchased or restored transaction. Call it once the item is saved; the store refunds unfinished purchases after a few days. The result is a [`StoreEventKind.Finished`](StoreEventKind.md#m-finished) event.

**Parameters**

- `transaction` ([`PurchaseTransaction`](PurchaseTransaction.md))

**Returns:** [`StoreError`](StoreError.md) — An error when the call was not sent.

### <a id="m-isowned-string"></a>IsOwned(string)

```csharp
public bool IsOwned(string productId);
```

True when a non-consumable or subscription is owned: a purchase or restore arrived and no revocation since.

**Parameters**

- `productId` (`string`): A product id from the catalog.

**Returns:** `bool`

### <a id="m-ispending-string"></a>IsPending(string)

```csharp
public bool IsPending(string productId);
```

True when a purchase of the product is waiting on approval outside the game.

**Parameters**

- `productId` (`string`)

**Returns:** `bool`

### <a id="m-loadproducts"></a>LoadProducts()

```csharp
public StoreError LoadProducts();
```

Asks the store about every product in the catalog.

**Returns:** [`StoreError`](StoreError.md) — An error when the request was not sent; [`StoreError.None`](StoreError.md#m-none) when the answer will come as an event.

### <a id="m-pump"></a>Pump()

```csharp
public void Pump();
```

Delivers what the store reported since the last call: updates the service's state and raises [`PurchaseService.Event`](PurchaseService.md#m-event) for each. Call once per frame from the game thread. Cheap when nothing happened.

### <a id="m-restore"></a>Restore()

```csharp
public StoreError Restore();
```

Asks the store for the player's existing purchases. Each arrives as a [`TransactionState.Restored`](TransactionState.md#m-restored) transaction, then [`StoreEventKind.RestoreFinished`](StoreEventKind.md#m-restorefinished). On Apple platforms this may ask the player to sign in, so call it from a "restore purchases" button.

**Returns:** [`StoreError`](StoreError.md) — An error when the request was not sent.
