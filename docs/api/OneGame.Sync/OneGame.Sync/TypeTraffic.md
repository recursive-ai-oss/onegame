[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# TypeTraffic class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class TypeTraffic
```

What one entity type has cost on the wire in a scope: bytes of state in additions (a whole entity entering a view) and in deltas, counted once per connection they were sent to.

## Properties

| Member | Description |
| --- | --- |
| [`AddedBytes`](#m-addedbytes) | Bytes of those additions. |
| [`Additions`](#m-additions) | Entities of this type sent whole to a connection (new, or entering its interest). |
| [`Bytes`](#m-bytes) | Additions plus deltas, in bytes. |
| [`DeltaBytes`](#m-deltabytes) | Bytes of those deltas. |
| [`Deltas`](#m-deltas) | Deltas sent for entities of this type. |
| [`TypeId`](#m-typeid) | The entity type's id. |
| [`TypeName`](#m-typename) | The entity type's name. |

### <a id="m-addedbytes"></a>AddedBytes

```csharp
public long AddedBytes { get; }
```

Bytes of those additions.

**Type:** `long`

### <a id="m-additions"></a>Additions

```csharp
public long Additions { get; }
```

Entities of this type sent whole to a connection (new, or entering its interest).

**Type:** `long`

### <a id="m-bytes"></a>Bytes

```csharp
public long Bytes { get; }
```

Additions plus deltas, in bytes.

**Type:** `long`

### <a id="m-deltabytes"></a>DeltaBytes

```csharp
public long DeltaBytes { get; }
```

Bytes of those deltas.

**Type:** `long`

### <a id="m-deltas"></a>Deltas

```csharp
public long Deltas { get; }
```

Deltas sent for entities of this type.

**Type:** `long`

### <a id="m-typeid"></a>TypeId

```csharp
public uint TypeId { get; }
```

The entity type's id.

**Type:** `uint`

### <a id="m-typename"></a>TypeName

```csharp
public string TypeName { get; }
```

The entity type's name.

**Type:** `string`
