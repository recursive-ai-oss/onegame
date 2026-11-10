[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# TypeBytes record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`TypeBytes`](TypeBytes.md)&gt;

```csharp
public sealed record TypeBytes : IEquatable<TypeBytes>
```

Bytes one entity type has cost on the wire in a scope (counted per connection it was sent to).

## Constructors

| Member | Description |
| --- | --- |
| [`TypeBytes(string, long, long, long, long)`](#m-ctor-string-long-long-long-long) | Bytes one entity type has cost on the wire in a scope (counted per connection it was sent to). |

### <a id="m-ctor-string-long-long-long-long"></a>TypeBytes(string, long, long, long, long)

```csharp
public TypeBytes(string Type, long Additions, long AddedBytes, long Deltas, long DeltaBytes);
```

Bytes one entity type has cost on the wire in a scope (counted per connection it was sent to).

**Parameters**

- `Type` (`string`): The entity type's name.
- `Additions` (`long`): Whole entities sent.
- `AddedBytes` (`long`): Bytes of those.
- `Deltas` (`long`): Deltas sent.
- `DeltaBytes` (`long`): Bytes of those.

## Properties

| Member | Description |
| --- | --- |
| [`AddedBytes`](#m-addedbytes) | Bytes of those. |
| [`Additions`](#m-additions) | Whole entities sent. |
| [`DeltaBytes`](#m-deltabytes) | Bytes of those. |
| [`Deltas`](#m-deltas) | Deltas sent. |
| [`Type`](#m-type) | The entity type's name. |

### <a id="m-addedbytes"></a>AddedBytes

```csharp
public long AddedBytes { get; init; }
```

Bytes of those.

**Type:** `long`

### <a id="m-additions"></a>Additions

```csharp
public long Additions { get; init; }
```

Whole entities sent.

**Type:** `long`

### <a id="m-deltabytes"></a>DeltaBytes

```csharp
public long DeltaBytes { get; init; }
```

Bytes of those.

**Type:** `long`

### <a id="m-deltas"></a>Deltas

```csharp
public long Deltas { get; init; }
```

Deltas sent.

**Type:** `long`

### <a id="m-type"></a>Type

```csharp
public string Type { get; init; }
```

The entity type's name.

**Type:** `string`
