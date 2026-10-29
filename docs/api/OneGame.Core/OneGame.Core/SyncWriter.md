[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SyncWriter class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class SyncWriter
```

Growable byte buffer that the generated codecs write into. Integers are LEB128 varints (signed values zigzag-encoded), so small values stay small and the encoding is the same on every platform.

## Constructors

| Member | Description |
| --- | --- |
| [`SyncWriter(int)`](#m-ctor-int) | Creates a writer with the given starting capacity. |

### <a id="m-ctor-int"></a>SyncWriter(int)

```csharp
public SyncWriter(int capacity = 256);
```

Creates a writer with the given starting capacity.

**Parameters**

- `capacity` (`int`)

## Properties

| Member | Description |
| --- | --- |
| [`Length`](#m-length) | Number of bytes written so far. |
| [`Written`](#m-written) | The bytes written so far. |

### <a id="m-length"></a>Length

```csharp
public int Length { get; }
```

Number of bytes written so far.

**Type:** `int`

### <a id="m-written"></a>Written

```csharp
public ReadOnlySpan<byte> Written { get; }
```

The bytes written so far. Valid until the next write or [`SyncWriter.Reset()`](SyncWriter.md#m-reset).

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`

## Methods

| Member | Description |
| --- | --- |
| [`Reserve(int)`](#m-reserve-int) | Makes room for at least this many more bytes, so a large write that follows grows the buffer once, here, instead of in steps. |
| [`Reset()`](#m-reset) | Discards the written bytes and keeps the buffer, so a reused writer allocates nothing. |
| [`ToArray()`](#m-toarray) | Copies the written bytes into a new array. |
| [`WriteBool(bool)`](#m-writebool-bool) | Writes a bool as one byte, 0 or 1. |
| [`WriteByte(byte)`](#m-writebyte-byte) | Writes one raw byte. |
| [`WriteBytes(ReadOnlySpan<byte>)`](#m-writebytes-readonlyspan-byte) | Writes a byte count followed by the bytes. |
| [`WriteCount(int, int)`](#m-writecount-int-int) | Writes an element or row count and checks it against `limit`. |
| [`WriteFixed32(uint)`](#m-writefixed32-uint) | Writes a 32-bit value as exactly four little-endian bytes. |
| [`WriteFixed64(ulong)`](#m-writefixed64-ulong) | Writes a 64-bit value as exactly eight little-endian bytes. |
| [`WriteInt16(short)`](#m-writeint16-short) | Writes a 16-bit signed integer. |
| [`WriteInt32(int)`](#m-writeint32-int) | Writes a 32-bit signed integer. |
| [`WriteInt64(long)`](#m-writeint64-long) | Writes a 64-bit signed integer. |
| [`WriteRaw(ReadOnlySpan<byte>)`](#m-writeraw-readonlyspan-byte) | Writes raw bytes with no length prefix. |
| [`WriteSByte(sbyte)`](#m-writesbyte-sbyte) | Writes one raw signed byte. |
| [`WriteString(string?)`](#m-writestring-string) | Writes a UTF-8 string with a length prefix. |
| [`WriteUInt16(ushort)`](#m-writeuint16-ushort) | Writes a 16-bit unsigned integer. |
| [`WriteUInt32(uint)`](#m-writeuint32-uint) | Writes a 32-bit unsigned integer. |
| [`WriteUInt64(ulong)`](#m-writeuint64-ulong) | Writes a 64-bit unsigned integer. |
| [`WriteVarInt(long)`](#m-writevarint-long) | Writes a signed varint (zigzag, then LEB128). |
| [`WriteVarUInt(ulong)`](#m-writevaruint-ulong) | Writes an unsigned LEB128 varint. |

### <a id="m-reserve-int"></a>Reserve(int)

```csharp
public void Reserve(int extra);
```

Makes room for at least this many more bytes, so a large write that follows grows the buffer once, here, instead of in steps. Writes no bytes.

**Parameters**

- `extra` (`int`): Bytes about to be written.

### <a id="m-reset"></a>Reset()

```csharp
public void Reset();
```

Discards the written bytes and keeps the buffer, so a reused writer allocates nothing.

### <a id="m-toarray"></a>ToArray()

```csharp
public byte[] ToArray();
```

Copies the written bytes into a new array.

**Returns:** `byte[]`

### <a id="m-writebool-bool"></a>WriteBool(bool)

```csharp
public void WriteBool(bool value);
```

Writes a bool as one byte, 0 or 1.

**Parameters**

- `value` (`bool`)

### <a id="m-writebyte-byte"></a>WriteByte(byte)

```csharp
public void WriteByte(byte value);
```

Writes one raw byte.

**Parameters**

- `value` (`byte`)

### <a id="m-writebytes-readonlyspan-byte"></a>WriteBytes(ReadOnlySpan&lt;byte&gt;)

```csharp
public void WriteBytes(ReadOnlySpan<byte> bytes);
```

Writes a byte count followed by the bytes.

**Parameters**

- `bytes` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

### <a id="m-writecount-int-int"></a>WriteCount(int, int)

```csharp
public void WriteCount(int count, int limit);
```

Writes an element or row count and checks it against `limit`.

**Parameters**

- `count` (`int`)
- `limit` (`int`)

**Exceptions**

- [`SyncLimitException`](SyncLimitException.md): `count` is above `limit`.

### <a id="m-writefixed32-uint"></a>WriteFixed32(uint)

```csharp
public void WriteFixed32(uint value);
```

Writes a 32-bit value as exactly four little-endian bytes.

**Parameters**

- `value` (`uint`)

### <a id="m-writefixed64-ulong"></a>WriteFixed64(ulong)

```csharp
public void WriteFixed64(ulong value);
```

Writes a 64-bit value as exactly eight little-endian bytes.

**Parameters**

- `value` (`ulong`)

### <a id="m-writeint16-short"></a>WriteInt16(short)

```csharp
public void WriteInt16(short value);
```

Writes a 16-bit signed integer.

**Parameters**

- `value` (`short`)

### <a id="m-writeint32-int"></a>WriteInt32(int)

```csharp
public void WriteInt32(int value);
```

Writes a 32-bit signed integer.

**Parameters**

- `value` (`int`)

### <a id="m-writeint64-long"></a>WriteInt64(long)

```csharp
public void WriteInt64(long value);
```

Writes a 64-bit signed integer.

**Parameters**

- `value` (`long`)

### <a id="m-writeraw-readonlyspan-byte"></a>WriteRaw(ReadOnlySpan&lt;byte&gt;)

```csharp
public void WriteRaw(ReadOnlySpan<byte> bytes);
```

Writes raw bytes with no length prefix.

**Parameters**

- `bytes` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

### <a id="m-writesbyte-sbyte"></a>WriteSByte(sbyte)

```csharp
public void WriteSByte(sbyte value);
```

Writes one raw signed byte.

**Parameters**

- `value` (`sbyte`)

### <a id="m-writestring-string"></a>WriteString(string?)

```csharp
public void WriteString(string? value);
```

Writes a UTF-8 string with a length prefix. A null string is written as empty.

**Parameters**

- `value` (`string?`)

**Exceptions**

- [`SyncLimitException`](SyncLimitException.md): The string is longer than [`SyncLimits.MaxStringBytes`](SyncLimits.md#m-maxstringbytes) in UTF-8.

### <a id="m-writeuint16-ushort"></a>WriteUInt16(ushort)

```csharp
public void WriteUInt16(ushort value);
```

Writes a 16-bit unsigned integer.

**Parameters**

- `value` (`ushort`)

### <a id="m-writeuint32-uint"></a>WriteUInt32(uint)

```csharp
public void WriteUInt32(uint value);
```

Writes a 32-bit unsigned integer.

**Parameters**

- `value` (`uint`)

### <a id="m-writeuint64-ulong"></a>WriteUInt64(ulong)

```csharp
public void WriteUInt64(ulong value);
```

Writes a 64-bit unsigned integer.

**Parameters**

- `value` (`ulong`)

### <a id="m-writevarint-long"></a>WriteVarInt(long)

```csharp
public void WriteVarInt(long value);
```

Writes a signed varint (zigzag, then LEB128).

**Parameters**

- `value` (`long`)

### <a id="m-writevaruint-ulong"></a>WriteVarUInt(ulong)

```csharp
public void WriteVarUInt(ulong value);
```

Writes an unsigned LEB128 varint.

**Parameters**

- `value` (`ulong`)
