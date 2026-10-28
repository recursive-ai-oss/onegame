[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# ProductDefinition struct

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ProductDefinition`](ProductDefinition.md)&gt;

```csharp
public readonly struct ProductDefinition : IEquatable<ProductDefinition>
```

A product the game sells, declared by the game and looked up in the store by [`ProductDefinition.Id`](ProductDefinition.md#m-id).

## Constructors

| Member | Description |
| --- | --- |
| [`ProductDefinition(string, ProductKind)`](#m-ctor-string-productkind) | A product the game sells, declared by the game and looked up in the store by [`ProductDefinition.Id`](ProductDefinition.md#m-id). |

### <a id="m-ctor-string-productkind"></a>ProductDefinition(string, ProductKind)

```csharp
public ProductDefinition(string Id, ProductKind Kind);
```

A product the game sells, declared by the game and looked up in the store by [`ProductDefinition.Id`](ProductDefinition.md#m-id).

**Parameters**

- `Id` (`string`): The store's product id (the same text in App Store Connect and Play Console).
- `Kind` ([`ProductKind`](ProductKind.md)): Consumable, non-consumable or subscription.

## Properties

| Member | Description |
| --- | --- |
| [`Id`](#m-id) | The store's product id (the same text in App Store Connect and Play Console). |
| [`Kind`](#m-kind) | Consumable, non-consumable or subscription. |

### <a id="m-id"></a>Id

```csharp
public string Id { get; init; }
```

The store's product id (the same text in App Store Connect and Play Console).

**Type:** `string`

### <a id="m-kind"></a>Kind

```csharp
public ProductKind Kind { get; init; }
```

Consumable, non-consumable or subscription.

**Type:** [`ProductKind`](ProductKind.md)
