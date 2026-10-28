[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# EntityId struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`ISyncValue`](ISyncValue-1.md)&lt;[`EntityId`](EntityId.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`EntityId`](EntityId.md)&gt;, [`IComparable`](https://learn.microsoft.com/dotnet/api/system.icomparable-1)&lt;[`EntityId`](EntityId.md)&gt;

```csharp
public readonly struct EntityId : ISyncValue<EntityId>, IEquatable<EntityId>, IComparable<EntityId>
```

Identifies an entity in a world. Ids are assigned in increasing order and never reused, so an id that refers to a removed entity cannot later point at a different one. Zero means "no entity".

## Constructors

| Member | Description |
| --- | --- |
| [`EntityId(ulong)`](#m-ctor-ulong) | Wraps a raw id. |

### <a id="m-ctor-ulong"></a>EntityId(ulong)

```csharp
public EntityId(ulong value);
```

Wraps a raw id.

**Parameters**

- `value` (`ulong`)

## Properties

| Member | Description |
| --- | --- |
| [`IsValid`](#m-isvalid) | True for any id other than [`EntityId.None`](EntityId.md#m-none). |
| [`None`](#m-none) | The "no entity" id. |
| [`Value`](#m-value) | The raw number. |

### <a id="m-isvalid"></a>IsValid

```csharp
public bool IsValid { get; }
```

True for any id other than [`EntityId.None`](EntityId.md#m-none).

**Type:** `bool`

### <a id="m-none"></a>None

```csharp
public static EntityId None { get; }
```

The "no entity" id.

**Type:** [`EntityId`](EntityId.md)

### <a id="m-value"></a>Value

```csharp
public ulong Value { get; }
```

The raw number.

**Type:** `ulong`

## Methods

| Member | Description |
| --- | --- |
| [`CompareTo(EntityId)`](#m-compareto-entityid) |  |
| [`Equals(EntityId)`](#m-equals-entityid) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). |
| [`ToString()`](#m-tostring) |  |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-compareto-entityid"></a>CompareTo(EntityId)

```csharp
public int CompareTo(EntityId other);
```

**Parameters**

- `other` ([`EntityId`](EntityId.md))

**Returns:** `int`

### <a id="m-equals-entityid"></a>Equals(EntityId)

```csharp
public bool Equals(EntityId other);
```

**Parameters**

- `other` ([`EntityId`](EntityId.md))

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

**Returns:** `int`

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public static EntityId Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](SyncReader.md))

**Returns:** [`EntityId`](EntityId.md)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

**Returns:** `string`

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
public void Write(SyncWriter writer);
```

Writes the value in the sync format.

**Parameters**

- `writer` ([`SyncWriter`](SyncWriter.md))

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
public void WriteJson(JsonOut json);
```

Writes the value as JSON for inspection.

**Parameters**

- `json` ([`JsonOut`](JsonOut.md))

## Operators

| Member | Description |
| --- | --- |
| [`operator !=(EntityId, EntityId)`](#m-operator-entityid-entityid) | Inequality of the numbers. |
| [`operator <(EntityId, EntityId)`](#m-operator-entityid-entityid-2) | True if `a` is the smaller id. |
| [`operator <=(EntityId, EntityId)`](#m-operator-entityid-entityid-3) | True if `a` is not larger. |
| [`operator ==(EntityId, EntityId)`](#m-operator-entityid-entityid-4) | Equality of the numbers. |
| [`operator >(EntityId, EntityId)`](#m-operator-entityid-entityid-5) | True if `a` is the larger id. |
| [`operator >=(EntityId, EntityId)`](#m-operator-entityid-entityid-6) | True if `a` is not smaller. |

### <a id="m-operator-entityid-entityid"></a>operator !=(EntityId, EntityId)

```csharp
public static bool operator !=(EntityId a, EntityId b);
```

Inequality of the numbers.

**Parameters**

- `a` ([`EntityId`](EntityId.md))
- `b` ([`EntityId`](EntityId.md))

**Returns:** `bool`

### <a id="m-operator-entityid-entityid-2"></a>operator &lt;(EntityId, EntityId)

```csharp
public static bool operator <(EntityId a, EntityId b);
```

True if `a` is the smaller id.

**Parameters**

- `a` ([`EntityId`](EntityId.md))
- `b` ([`EntityId`](EntityId.md))

**Returns:** `bool`

### <a id="m-operator-entityid-entityid-3"></a>operator &lt;=(EntityId, EntityId)

```csharp
public static bool operator <=(EntityId a, EntityId b);
```

True if `a` is not larger.

**Parameters**

- `a` ([`EntityId`](EntityId.md))
- `b` ([`EntityId`](EntityId.md))

**Returns:** `bool`

### <a id="m-operator-entityid-entityid-4"></a>operator ==(EntityId, EntityId)

```csharp
public static bool operator ==(EntityId a, EntityId b);
```

Equality of the numbers.

**Parameters**

- `a` ([`EntityId`](EntityId.md))
- `b` ([`EntityId`](EntityId.md))

**Returns:** `bool`

### <a id="m-operator-entityid-entityid-5"></a>operator &gt;(EntityId, EntityId)

```csharp
public static bool operator >(EntityId a, EntityId b);
```

True if `a` is the larger id.

**Parameters**

- `a` ([`EntityId`](EntityId.md))
- `b` ([`EntityId`](EntityId.md))

**Returns:** `bool`

### <a id="m-operator-entityid-entityid-6"></a>operator &gt;=(EntityId, EntityId)

```csharp
public static bool operator >=(EntityId a, EntityId b);
```

True if `a` is not smaller.

**Parameters**

- `a` ([`EntityId`](EntityId.md))
- `b` ([`EntityId`](EntityId.md))

**Returns:** `bool`
