[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# StoreProductInfo record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`StoreProductInfo`](StoreProductInfo.md)&gt;

```csharp
public sealed record StoreProductInfo : IEquatable<StoreProductInfo>
```

One product as the store described it, for the `store` operation's `products` action.

## Constructors

| Member | Description |
| --- | --- |
| [`StoreProductInfo(string, string, string, string, long, string)`](#m-ctor-string-string-string-string-long-string) | One product as the store described it, for the `store` operation's `products` action. |

### <a id="m-ctor-string-string-string-string-long-string"></a>StoreProductInfo(string, string, string, string, long, string)

```csharp
public StoreProductInfo(string Id, string Kind, string Title, string Price, long PriceMicros, string Currency);
```

One product as the store described it, for the `store` operation's `products` action.

**Parameters**

- `Id` (`string`): Product id.
- `Kind` (`string`): Consumable, NonConsumable or Subscription.
- `Title` (`string`): Display title.
- `Price` (`string`): The price as shown to the player.
- `PriceMicros` (`long`): The amount in millionths of `Currency`.
- `Currency` (`string`): ISO 4217 code, or empty.

## Properties

| Member | Description |
| --- | --- |
| [`Currency`](#m-currency) | ISO 4217 code, or empty. |
| [`Id`](#m-id) | Product id. |
| [`Kind`](#m-kind) | Consumable, NonConsumable or Subscription. |
| [`Price`](#m-price) | The price as shown to the player. |
| [`PriceMicros`](#m-pricemicros) | The amount in millionths of `Currency`. |
| [`Title`](#m-title) | Display title. |

### <a id="m-currency"></a>Currency

```csharp
public string Currency { get; init; }
```

ISO 4217 code, or empty.

**Type:** `string`

### <a id="m-id"></a>Id

```csharp
public string Id { get; init; }
```

Product id.

**Type:** `string`

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; init; }
```

Consumable, NonConsumable or Subscription.

**Type:** `string`

### <a id="m-price"></a>Price

```csharp
public string Price { get; init; }
```

The price as shown to the player.

**Type:** `string`

### <a id="m-pricemicros"></a>PriceMicros

```csharp
public long PriceMicros { get; init; }
```

The amount in millionths of `Currency`.

**Type:** `long`

### <a id="m-title"></a>Title

```csharp
public string Title { get; init; }
```

Display title.

**Type:** `string`
