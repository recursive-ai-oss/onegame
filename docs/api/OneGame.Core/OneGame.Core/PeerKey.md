[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# PeerKey struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PeerKey`](PeerKey.md)&gt;

```csharp
public readonly struct PeerKey : IEquatable<PeerKey>
```

A 32-byte X25519 public key. It names a server (the keys a client trusts) or a client (who a server sees after the handshake). Public keys are not secret: print them, commit them, ship them in a client build or a server list. Text form is unpadded base64url, 43 characters.

## Constructors

| Member | Description |
| --- | --- |
| [`PeerKey(ReadOnlySpan<byte>)`](#m-ctor-readonlyspan-byte) | Wraps 32 bytes. |

### <a id="m-ctor-readonlyspan-byte"></a>PeerKey(ReadOnlySpan&lt;byte&gt;)

```csharp
public PeerKey(ReadOnlySpan<byte> bytes);
```

Wraps 32 bytes.

**Parameters**

- `bytes` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): The key bytes.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The span is not 32 bytes.

## Fields

| Member | Description |
| --- | --- |
| [`Size`](#m-size) | Size of a key in bytes. |

### <a id="m-size"></a>Size

```csharp
public const int Size = 32;
```

Size of a key in bytes.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`Bytes`](#m-bytes) | The key bytes (empty for [`PeerKey.IsEmpty`](PeerKey.md#m-isempty)). |
| [`IsEmpty`](#m-isempty) | True for the default value, which is no key. |

### <a id="m-bytes"></a>Bytes

```csharp
public ReadOnlySpan<byte> Bytes { get; }
```

The key bytes (empty for [`PeerKey.IsEmpty`](PeerKey.md#m-isempty)).

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`

### <a id="m-isempty"></a>IsEmpty

```csharp
public bool IsEmpty { get; }
```

True for the default value, which is no key.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Equals(PeerKey)`](#m-equals-peerkey) |  |
| [`Equals(object?)`](#m-equals-object) |  |
| [`GetHashCode()`](#m-gethashcode) |  |
| [`Parse(string)`](#m-parse-string) | Reads the text form written by [`PeerKey.ToString()`](PeerKey.md#m-tostring). |
| [`ToString()`](#m-tostring) | The unpadded base64url text of the key. |
| [`TryParse(string?, out PeerKey)`](#m-tryparse-string-peerkey) | Reads the text form, returning false instead of throwing. |

### <a id="m-equals-peerkey"></a>Equals(PeerKey)

```csharp
public bool Equals(PeerKey other);
```

Implements [`IEquatable<PeerKey>.Equals()`](https://learn.microsoft.com/dotnet/api/system.iequatable-1.equals).

**Parameters**

- `other` ([`PeerKey`](PeerKey.md))

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

### <a id="m-parse-string"></a>Parse(string)

```csharp
public static PeerKey Parse(string text);
```

Reads the text form written by [`PeerKey.ToString()`](PeerKey.md#m-tostring).

**Parameters**

- `text` (`string`): 43 characters of unpadded base64url (surrounding white space is ignored).

**Returns:** [`PeerKey`](PeerKey.md)

**Exceptions**

- [`KeyFormatException`](KeyFormatException.md): The text is not a key.

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

The unpadded base64url text of the key.

**Returns:** `string`

### <a id="m-tryparse-string-peerkey"></a>TryParse(string?, out PeerKey)

```csharp
public static bool TryParse(string? text, out PeerKey key);
```

Reads the text form, returning false instead of throwing.

**Parameters**

- `text` (`string?`): The key text.
- `key` ([`PeerKey`](PeerKey.md)): The key when this returns true.

**Returns:** `bool`

## Operators

| Member | Description |
| --- | --- |
| [`operator !=(PeerKey, PeerKey)`](#m-operator-peerkey-peerkey) | Compares two keys. |
| [`operator ==(PeerKey, PeerKey)`](#m-operator-peerkey-peerkey-2) | Compares two keys. |

### <a id="m-operator-peerkey-peerkey"></a>operator !=(PeerKey, PeerKey)

```csharp
public static bool operator !=(PeerKey left, PeerKey right);
```

Compares two keys.

**Parameters**

- `left` ([`PeerKey`](PeerKey.md))
- `right` ([`PeerKey`](PeerKey.md))

**Returns:** `bool`

### <a id="m-operator-peerkey-peerkey-2"></a>operator ==(PeerKey, PeerKey)

```csharp
public static bool operator ==(PeerKey left, PeerKey right);
```

Compares two keys.

**Parameters**

- `left` ([`PeerKey`](PeerKey.md))
- `right` ([`PeerKey`](PeerKey.md))

**Returns:** `bool`
