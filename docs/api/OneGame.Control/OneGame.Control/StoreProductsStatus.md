[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# StoreProductsStatus record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`StoreProductsStatus`](StoreProductsStatus.md)&gt;

```csharp
public sealed record StoreProductsStatus : IEquatable<StoreProductsStatus>
```

The products a purchase service has loaded, in id order.

## Constructors

| Member | Description |
| --- | --- |
| [`StoreProductsStatus(StoreProductInfo[])`](#m-ctor-storeproductinfo) | The products a purchase service has loaded, in id order. |

### <a id="m-ctor-storeproductinfo"></a>StoreProductsStatus(StoreProductInfo\[\])

```csharp
public StoreProductsStatus(StoreProductInfo[] Products);
```

The products a purchase service has loaded, in id order.

**Parameters**

- `Products` ([`StoreProductInfo`](StoreProductInfo.md)\[\]): The products.

## Properties

| Member | Description |
| --- | --- |
| [`Products`](#m-products) | The products. |

### <a id="m-products"></a>Products

```csharp
public StoreProductInfo[] Products { get; init; }
```

The products.

**Type:** [`StoreProductInfo`](StoreProductInfo.md)\[\]
