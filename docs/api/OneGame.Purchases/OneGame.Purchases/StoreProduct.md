[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# StoreProduct record

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`StoreProduct`](StoreProduct.md)&gt;

```csharp
public sealed record StoreProduct : IEquatable<StoreProduct>
```

A product as the store describes it to this player: localized text and a price already formatted for their country.

## Constructors

| Member | Description |
| --- | --- |
| [`StoreProduct(string, ProductKind, string, string, string, long, string)`](#m-ctor-string-productkind-string-string-string-long-string) | A product as the store describes it to this player: localized text and a price already formatted for their country. |

### <a id="m-ctor-string-productkind-string-string-string-long-string"></a>StoreProduct(string, ProductKind, string, string, string, long, string)

```csharp
public StoreProduct(string Id, ProductKind Kind, string Title, string Description, string Price, long PriceMicros, string Currency);
```

A product as the store describes it to this player: localized text and a price already formatted for their country.

**Parameters**

- `Id` (`string`): The store's product id.
- `Kind` ([`ProductKind`](ProductKind.md)): The kind the game declared.
- `Title` (`string`): Localized title.
- `Description` (`string`): Localized description.
- `Price` (`string`): The price as it should be shown, such as `$4.99`.
- `PriceMicros` (`long`): The price in millionths of `Currency`, for sorting and comparisons; 0 when unknown.
- `Currency` (`string`): ISO 4217 currency code, or empty when unknown.

## Properties

| Member | Description |
| --- | --- |
| [`Currency`](#m-currency) | ISO 4217 currency code, or empty when unknown. |
| [`Description`](#m-description) | Localized description. |
| [`Id`](#m-id) | The store's product id. |
| [`Kind`](#m-kind) | The kind the game declared. |
| [`Price`](#m-price) | The price as it should be shown, such as `$4.99`. |
| [`PriceMicros`](#m-pricemicros) | The price in millionths of `Currency`, for sorting and comparisons; 0 when unknown. |
| [`Title`](#m-title) | Localized title. |

### <a id="m-currency"></a>Currency

```csharp
public string Currency { get; init; }
```

ISO 4217 currency code, or empty when unknown.

**Type:** `string`

### <a id="m-description"></a>Description

```csharp
public string Description { get; init; }
```

Localized description.

**Type:** `string`

### <a id="m-id"></a>Id

```csharp
public string Id { get; init; }
```

The store's product id.

**Type:** `string`

### <a id="m-kind"></a>Kind

```csharp
public ProductKind Kind { get; init; }
```

The kind the game declared.

**Type:** [`ProductKind`](ProductKind.md)

### <a id="m-price"></a>Price

```csharp
public string Price { get; init; }
```

The price as it should be shown, such as `$4.99`.

**Type:** `string`

### <a id="m-pricemicros"></a>PriceMicros

```csharp
public long PriceMicros { get; init; }
```

The price in millionths of `Currency`, for sorting and comparisons; 0 when unknown.

**Type:** `long`

### <a id="m-title"></a>Title

```csharp
public string Title { get; init; }
```

Localized title.

**Type:** `string`
