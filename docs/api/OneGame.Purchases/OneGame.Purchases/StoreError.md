[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# StoreError struct

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`StoreError`](StoreError.md)&gt;

```csharp
public readonly struct StoreError : IEquatable<StoreError>
```

A failure value: what went wrong and the store's own message for logs.

## Constructors

| Member | Description |
| --- | --- |
| [`StoreError(StoreErrorKind, string)`](#m-ctor-storeerrorkind-string) | A failure value: what went wrong and the store's own message for logs. |

### <a id="m-ctor-storeerrorkind-string"></a>StoreError(StoreErrorKind, string)

```csharp
public StoreError(StoreErrorKind Kind, string Message);
```

A failure value: what went wrong and the store's own message for logs.

**Parameters**

- `Kind` ([`StoreErrorKind`](StoreErrorKind.md)): The category.
- `Message` (`string`): The store's text, bounded in length. May be empty.

## Properties

| Member | Description |
| --- | --- |
| [`IsError`](#m-iserror) | True when something went wrong. |
| [`Kind`](#m-kind) | The category. |
| [`Message`](#m-message) | The store's text, bounded in length. |
| [`None`](#m-none) | No error. |

### <a id="m-iserror"></a>IsError

```csharp
public bool IsError { get; }
```

True when something went wrong.

**Type:** `bool`

### <a id="m-kind"></a>Kind

```csharp
public StoreErrorKind Kind { get; init; }
```

The category.

**Type:** [`StoreErrorKind`](StoreErrorKind.md)

### <a id="m-message"></a>Message

```csharp
public string Message { get; init; }
```

The store's text, bounded in length. May be empty.

**Type:** `string`

### <a id="m-none"></a>None

```csharp
public static StoreError None { get; }
```

No error.

**Type:** [`StoreError`](StoreError.md)
