[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# PurchaseTransaction record

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PurchaseTransaction`](PurchaseTransaction.md)&gt;

```csharp
public sealed record PurchaseTransaction : IEquatable<PurchaseTransaction>
```

One purchase as the store reports it. Everything here came from outside the process: treat the ids and proof as opaque text, and check [`PurchaseTransaction.ServerProof`](PurchaseTransaction.md#m-serverproof) on your own server before granting anything that matters.

## Constructors

| Member | Description |
| --- | --- |
| [`PurchaseTransaction(string, string, TransactionState, int, long, string, string, string, StoreError)`](#m-ctor-string-string-transactionstate-int-long-string-string-string-storeerror) | One purchase as the store reports it. |

### <a id="m-ctor-string-string-transactionstate-int-long-string-string-string-storeerror"></a>PurchaseTransaction(string, string, TransactionState, int, long, string, string, string, StoreError)

```csharp
public PurchaseTransaction(string TransactionId, string ProductId, TransactionState State, int Quantity, long PurchaseTimeUnixMs, string ServerProof, string SignedData, string Signature, StoreError Error);
```

One purchase as the store reports it. Everything here came from outside the process: treat the ids and proof as opaque text, and check [`PurchaseTransaction.ServerProof`](PurchaseTransaction.md#m-serverproof) on your own server before granting anything that matters.

**Parameters**

- `TransactionId` (`string`): The store's id for this transaction. Stable across restores; empty for a purchase that never started.
- `ProductId` (`string`): The product bought.
- `State` ([`TransactionState`](TransactionState.md)): Where the transaction stands.
- `Quantity` (`int`): How many were bought (1 unless the store says otherwise).
- `PurchaseTimeUnixMs` (`long`): When the store says it happened, in milliseconds since 1970-01-01 UTC; 0 when unknown.
- `ServerProof` (`string`): What a server needs to verify the purchase with the store: the base64 app receipt on Apple, the purchase token on Google Play. Empty for the fake store.
- `SignedData` (`string`): Google Play: the purchase's JSON, which [`PurchaseTransaction.Signature`](PurchaseTransaction.md#m-signature) signs. Empty elsewhere.
- `Signature` (`string`): Google Play: the base64 signature over [`PurchaseTransaction.SignedData`](PurchaseTransaction.md#m-signeddata). Empty elsewhere.
- `Error` ([`StoreError`](StoreError.md)): Why a [`TransactionState.Failed`](TransactionState.md#m-failed) purchase failed.

## Properties

| Member | Description |
| --- | --- |
| [`Error`](#m-error) | Why a [`TransactionState.Failed`](TransactionState.md#m-failed) purchase failed. |
| [`NeedsFinish`](#m-needsfinish) | True for states that wait for [`PurchaseService.Finish()`](PurchaseService.md#m-finish-purchasetransaction): [`TransactionState.Purchased`](TransactionState.md#m-purchased) and [`TransactionState.Restored`](TransactionState.md#m-restored). |
| [`ProductId`](#m-productid) | The product bought. |
| [`PurchaseTimeUnixMs`](#m-purchasetimeunixms) | When the store says it happened, in milliseconds since 1970-01-01 UTC; 0 when unknown. |
| [`Quantity`](#m-quantity) | How many were bought (1 unless the store says otherwise). |
| [`ServerProof`](#m-serverproof) | What a server needs to verify the purchase with the store: the base64 app receipt on Apple, the purchase token on Google Play. |
| [`Signature`](#m-signature) | Google Play: the base64 signature over [`PurchaseTransaction.SignedData`](PurchaseTransaction.md#m-signeddata). |
| [`SignedData`](#m-signeddata) | Google Play: the purchase's JSON, which [`PurchaseTransaction.Signature`](PurchaseTransaction.md#m-signature) signs. |
| [`State`](#m-state) | Where the transaction stands. |
| [`TransactionId`](#m-transactionid) | The store's id for this transaction. |

### <a id="m-error"></a>Error

```csharp
public StoreError Error { get; init; }
```

Why a [`TransactionState.Failed`](TransactionState.md#m-failed) purchase failed.

**Type:** [`StoreError`](StoreError.md)

### <a id="m-needsfinish"></a>NeedsFinish

```csharp
public bool NeedsFinish { get; }
```

True for states that wait for [`PurchaseService.Finish()`](PurchaseService.md#m-finish-purchasetransaction): [`TransactionState.Purchased`](TransactionState.md#m-purchased) and [`TransactionState.Restored`](TransactionState.md#m-restored).

**Type:** `bool`

### <a id="m-productid"></a>ProductId

```csharp
public string ProductId { get; init; }
```

The product bought.

**Type:** `string`

### <a id="m-purchasetimeunixms"></a>PurchaseTimeUnixMs

```csharp
public long PurchaseTimeUnixMs { get; init; }
```

When the store says it happened, in milliseconds since 1970-01-01 UTC; 0 when unknown.

**Type:** `long`

### <a id="m-quantity"></a>Quantity

```csharp
public int Quantity { get; init; }
```

How many were bought (1 unless the store says otherwise).

**Type:** `int`

### <a id="m-serverproof"></a>ServerProof

```csharp
public string ServerProof { get; init; }
```

What a server needs to verify the purchase with the store: the base64 app receipt on Apple, the purchase token on Google Play. Empty for the fake store.

**Type:** `string`

### <a id="m-signature"></a>Signature

```csharp
public string Signature { get; init; }
```

Google Play: the base64 signature over [`PurchaseTransaction.SignedData`](PurchaseTransaction.md#m-signeddata). Empty elsewhere.

**Type:** `string`

### <a id="m-signeddata"></a>SignedData

```csharp
public string SignedData { get; init; }
```

Google Play: the purchase's JSON, which [`PurchaseTransaction.Signature`](PurchaseTransaction.md#m-signature) signs. Empty elsewhere.

**Type:** `string`

### <a id="m-state"></a>State

```csharp
public TransactionState State { get; init; }
```

Where the transaction stands.

**Type:** [`TransactionState`](TransactionState.md)

### <a id="m-transactionid"></a>TransactionId

```csharp
public string TransactionId { get; init; }
```

The store's id for this transaction. Stable across restores; empty for a purchase that never started.

**Type:** `string`
