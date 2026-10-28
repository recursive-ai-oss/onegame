[API reference](../../index.md) › [OneGame.Physics](../index.md) › OneGame.Physics

# CharacterRef struct

**Namespace:** `OneGame.Physics`  
**Package:** [`RecursiveAI.OneGame.Physics`](../index.md)  
**Implements:** [`ISyncValue`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md)&lt;[`CharacterRef`](CharacterRef.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CharacterRef`](CharacterRef.md)&gt;

```csharp
public readonly struct CharacterRef : ISyncValue<CharacterRef>, IEquatable<CharacterRef>
```

Names a character in a [`PhysicsSpace2D`](PhysicsSpace2D.md), like [`BodyRef`](BodyRef.md) names a body. Keep it in a synced property.

## Properties

| Member | Description |
| --- | --- |
| [`Generation`](#m-generation) | How many bodies have used the slot; 0 only in the null reference. |
| [`Index`](#m-index) | The slot of the character's body in the space (a character is a kinematic body with a capsule). |
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

The slot of the character's body in the space (a character is a kinematic body with a capsule).

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
| [`Equals(CharacterRef)`](#m-equals-characterref) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Read(ref SyncReader)`](#m-read-syncreader) | Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). |
| [`ToString()`](#m-tostring) |  |
| [`Write(SyncWriter)`](#m-write-syncwriter) | Writes the value in the sync format. |
| [`WriteJson(JsonOut)`](#m-writejson-jsonout) | Writes the value as JSON for inspection. |

### <a id="m-equals-characterref"></a>Equals(CharacterRef)

```csharp
public bool Equals(CharacterRef other);
```

Implements [`IEquatable<CharacterRef>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`CharacterRef`](CharacterRef.md))

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
public static CharacterRef Read(ref SyncReader reader);
```

Reads a value written by [`ISyncValue<TSelf>.Write()`](../../OneGame.Core/OneGame.Core/ISyncValue-1.md#m-write-syncwriter). Problems are recorded on the reader, and a default value is returned.

**Parameters**

- `reader` ([`SyncReader`](../../OneGame.Core/OneGame.Core/SyncReader.md))

**Returns:** [`CharacterRef`](CharacterRef.md)

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
| [`operator !=(CharacterRef, CharacterRef)`](#m-operator-characterref-characterref) | Two references differ when they name different characters. |
| [`operator ==(CharacterRef, CharacterRef)`](#m-operator-characterref-characterref-2) | Two references are equal when they name the same character. |

### <a id="m-operator-characterref-characterref"></a>operator !=(CharacterRef, CharacterRef)

```csharp
public static bool operator !=(CharacterRef left, CharacterRef right);
```

Two references differ when they name different characters.

**Parameters**

- `left` ([`CharacterRef`](CharacterRef.md))
- `right` ([`CharacterRef`](CharacterRef.md))

**Returns:** `bool`

### <a id="m-operator-characterref-characterref-2"></a>operator ==(CharacterRef, CharacterRef)

```csharp
public static bool operator ==(CharacterRef left, CharacterRef right);
```

Two references are equal when they name the same character.

**Parameters**

- `left` ([`CharacterRef`](CharacterRef.md))
- `right` ([`CharacterRef`](CharacterRef.md))

**Returns:** `bool`
