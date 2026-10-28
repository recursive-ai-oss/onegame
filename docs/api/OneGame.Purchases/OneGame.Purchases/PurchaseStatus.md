[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# PurchaseStatus record

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PurchaseStatus`](PurchaseStatus.md)&gt;

```csharp
public sealed record PurchaseStatus : IEquatable<PurchaseStatus>
```

A snapshot of the service for logs and the harness.

## Constructors

| Member | Description |
| --- | --- |
| [`PurchaseStatus(string, string, string?, string[], string[], string[], string[], string[])`](#m-ctor-string-string-string-string-string-string-string-string) | A snapshot of the service for logs and the harness. |

### <a id="m-ctor-string-string-string-string-string-string-string-string"></a>PurchaseStatus(string, string, string?, string\[\], string\[\], string\[\], string\[\], string\[\])

```csharp
public PurchaseStatus(string Store, string Connection, string? ConnectError, string[] Products, string[] Owned, string[] Unfinished, string[] Pending, string[] Recent);
```

A snapshot of the service for logs and the harness.

**Parameters**

- `Store` (`string`): Backend name.
- `Connection` (`string`): Connection state as text.
- `ConnectError` (`string?`): The connect failure, or null.
- `Products` (`string[]`): Product ids the store described.
- `Owned` (`string[]`): Product ids the player owns (non-consumables and subscriptions).
- `Unfinished` (`string[]`): Transactions waiting for [`PurchaseService.Finish()`](PurchaseService.md#m-finish-purchasetransaction) as `id:product:state`.
- `Pending` (`string[]`): Product ids with a purchase waiting on approval.
- `Recent` (`string[]`): The latest events, oldest first, as short text.

## Properties

| Member | Description |
| --- | --- |
| [`ConnectError`](#m-connecterror) | The connect failure, or null. |
| [`Connection`](#m-connection) | Connection state as text. |
| [`Owned`](#m-owned) | Product ids the player owns (non-consumables and subscriptions). |
| [`Pending`](#m-pending) | Product ids with a purchase waiting on approval. |
| [`Products`](#m-products) | Product ids the store described. |
| [`Recent`](#m-recent) | The latest events, oldest first, as short text. |
| [`Store`](#m-store) | Backend name. |
| [`Unfinished`](#m-unfinished) | Transactions waiting for [`PurchaseService.Finish()`](PurchaseService.md#m-finish-purchasetransaction) as `id:product:state`. |

### <a id="m-connecterror"></a>ConnectError

```csharp
public string? ConnectError { get; init; }
```

The connect failure, or null.

**Type:** `string?`

### <a id="m-connection"></a>Connection

```csharp
public string Connection { get; init; }
```

Connection state as text.

**Type:** `string`

### <a id="m-owned"></a>Owned

```csharp
public string[] Owned { get; init; }
```

Product ids the player owns (non-consumables and subscriptions).

**Type:** `string[]`

### <a id="m-pending"></a>Pending

```csharp
public string[] Pending { get; init; }
```

Product ids with a purchase waiting on approval.

**Type:** `string[]`

### <a id="m-products"></a>Products

```csharp
public string[] Products { get; init; }
```

Product ids the store described.

**Type:** `string[]`

### <a id="m-recent"></a>Recent

```csharp
public string[] Recent { get; init; }
```

The latest events, oldest first, as short text.

**Type:** `string[]`

### <a id="m-store"></a>Store

```csharp
public string Store { get; init; }
```

Backend name.

**Type:** `string`

### <a id="m-unfinished"></a>Unfinished

```csharp
public string[] Unfinished { get; init; }
```

Transactions waiting for [`PurchaseService.Finish()`](PurchaseService.md#m-finish-purchasetransaction) as `id:product:state`.

**Type:** `string[]`
