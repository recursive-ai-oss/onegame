[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# BodyRef struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`ISyncValue`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md)&lt;[`BodyRef`](BodyRef.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`BodyRef`](BodyRef.md)&gt;

```csharp
public readonly struct BodyRef : ISyncValue<BodyRef>, IEquatable<BodyRef>
```

Names a body in a [`PhysicsSpace2D`](PhysicsSpace2D.md): a slot and a generation, so a reference to a removed body is detected instead of silently pointing at the body that took its slot. It is a plain synced value, so an entity keeps it in a property:

```csharp
[Synced] public partial BodyRef Body { get; private set; }
```

The default value is the null reference.

## Properties

| Member | Description |
| --- | --- |
| [`Generation`](#m-generation) | How many bodies have used the slot; 0 only in the null reference. |
| [`Index`](#m-index) | The body's slot in the space. |
| [`IsNull`](#m-isnull) | True for the default value, which names nothing. |

### <a id="m-generation"></a>Generation

```csharp
public int Generation { get; }
```

How many bodies have used the slot; 0 only in the null reference.

**Type:** `int`

### <a id="m-index"></a>Index

```csharp
public int Index { get; }
```

The body's slot in the space.

**Type:** `int`

### <a id="m-isnull"></a>IsNull

```csharp
public bool IsNull { get; }
```

True for the default value, which names nothing.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Equals(BodyRef)`](#m-equals-bodyref) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). |
| [`ToString()`](#m-tostring) |  |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-equals-bodyref"></a>Equals(BodyRef)

```csharp
public bool Equals(BodyRef other);
```

Implements [`IEquatable<BodyRef>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`BodyRef`](BodyRef.md))

**Returns:** `bool`

### <a id="m-equals-object"></a>Equals(object?)

```csharp
public override bool Equals(object? obj);
```

Overrides [`ValueType.Equals()`](https://learn.microsoft.com/dotnet/api/system.valuetype.equals).

**Parameters**

- `obj` (`object?`)

**Returns:** `bool`

### <a id="m-gethashcode"></a>GetHashCode()

```csharp
public override int GetHashCode();
```

Overrides [`ValueType.GetHashCode()`](https://learn.microsoft.com/dotnet/api/system.valuetype.gethashcode).

**Returns:** `int`

### <a id="m-read-syncreader"></a>Read(ref SyncReader)

```csharp
public static BodyRef Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

**Returns:** [`BodyRef`](BodyRef.md)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

Overrides [`ValueType.ToString()`](https://learn.microsoft.com/dotnet/api/system.valuetype.tostring).

**Returns:** `string`

### <a id="m-write-syncwriter"></a>Write(SyncWriter)

```csharp
public void Write(SyncWriter writer);
```

Writes the value in the sync format.

**Parameters**

- `writer` ([`SyncWriter`](../../OneGame.Core/OneGame.Core/SyncWriter.md))

### <a id="m-writejson-jsonout"></a>WriteJson(JsonOut)

```csharp
public void WriteJson(JsonOut json);
```

Writes the value as JSON for inspection.

**Parameters**

- `json` ([`JsonOut`](../../OneGame.Core/OneGame.Core/JsonOut.md))

## Operators

| Member | Description |
| --- | --- |
| [`operator !=(BodyRef, BodyRef)`](#m-operator-bodyref-bodyref) | Two references differ when they name different bodies. |
| [`operator ==(BodyRef, BodyRef)`](#m-operator-bodyref-bodyref-2) | Two references are equal when they name the same body. |

### <a id="m-operator-bodyref-bodyref"></a>operator !=(BodyRef, BodyRef)

```csharp
public static bool operator !=(BodyRef left, BodyRef right);
```

Two references differ when they name different bodies.

**Parameters**

- `left` ([`BodyRef`](BodyRef.md))
- `right` ([`BodyRef`](BodyRef.md))

**Returns:** `bool`

### <a id="m-operator-bodyref-bodyref-2"></a>operator ==(BodyRef, BodyRef)

```csharp
public static bool operator ==(BodyRef left, BodyRef right);
```

Two references are equal when they name the same body.

**Parameters**

- `left` ([`BodyRef`](BodyRef.md))
- `right` ([`BodyRef`](BodyRef.md))

**Returns:** `bool`
