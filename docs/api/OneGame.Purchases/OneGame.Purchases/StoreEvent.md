[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# StoreEvent record

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`StoreEvent`](StoreEvent.md)&gt;

```csharp
public sealed record StoreEvent : IEquatable<StoreEvent>
```

One thing the store told the game. Events reach the game only from [`PurchaseService.Pump()`](PurchaseService.md#m-pump), on the thread that calls it.

## Constructors

| Member | Description |
| --- | --- |
| [`StoreEvent(StoreEventKind)`](#m-ctor-storeeventkind) | One thing the store told the game. |

### <a id="m-ctor-storeeventkind"></a>StoreEvent(StoreEventKind)

```csharp
public StoreEvent(StoreEventKind Kind);
```

One thing the store told the game. Events reach the game only from [`PurchaseService.Pump()`](PurchaseService.md#m-pump), on the thread that calls it.

**Parameters**

- `Kind` ([`StoreEventKind`](StoreEventKind.md))

## Properties

| Member | Description |
| --- | --- |
| [`Error`](#m-error) | Set when the operation behind the event failed. |
| [`Kind`](#m-kind) |  |
| [`Products`](#m-products) | For [`StoreEventKind.ProductsLoaded`](StoreEventKind.md#m-productsloaded). |
| [`Transaction`](#m-transaction) | For [`StoreEventKind.Transaction`](StoreEventKind.md#m-transaction) and [`StoreEventKind.Finished`](StoreEventKind.md#m-finished). |
| [`UnknownProductIds`](#m-unknownproductids) | For [`StoreEventKind.ProductsLoaded`](StoreEventKind.md#m-productsloaded): ids the store does not know. |

### <a id="m-error"></a>Error

```csharp
public StoreError Error { get; init; }
```

Set when the operation behind the event failed.

**Type:** [`StoreError`](StoreError.md)

### <a id="m-kind"></a>Kind

```csharp
public StoreEventKind Kind { get; init; }
```

**Type:** [`StoreEventKind`](StoreEventKind.md)

### <a id="m-products"></a>Products

```csharp
public IReadOnlyList<StoreProduct> Products { get; init; }
```

For [`StoreEventKind.ProductsLoaded`](StoreEventKind.md#m-productsloaded).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`StoreProduct`](StoreProduct.md)&gt;

### <a id="m-transaction"></a>Transaction

```csharp
public PurchaseTransaction? Transaction { get; init; }
```

For [`StoreEventKind.Transaction`](StoreEventKind.md#m-transaction) and [`StoreEventKind.Finished`](StoreEventKind.md#m-finished).

**Type:** [`PurchaseTransaction`](PurchaseTransaction.md)?

### <a id="m-unknownproductids"></a>UnknownProductIds

```csharp
public IReadOnlyList<string> UnknownProductIds { get; init; }
```

For [`StoreEventKind.ProductsLoaded`](StoreEventKind.md#m-productsloaded): ids the store does not know.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`
