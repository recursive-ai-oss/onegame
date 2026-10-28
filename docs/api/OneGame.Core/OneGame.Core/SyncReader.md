[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# SyncReader struct

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public ref struct SyncReader
```

> **Obsolete.** Types with embedded references are not supported in this version of your compiler.

Reads the generated binary format from untrusted bytes. Nothing here throws on bad input: the first problem is kept in [`SyncReader.Error`](SyncReader.md#m-error), every later read returns a default value, and the caller checks [`SyncReader.Ok`](SyncReader.md#m-ok) once at the end. Every length is checked against its limit and against the bytes that are left before anything is allocated.

## Constructors

| Member | Description |
| --- | --- |
| [`SyncReader(ReadOnlySpan<byte>)`](#m-ctor-readonlyspan-byte) | Creates a reader over `data`. |

### <a id="m-ctor-readonlyspan-byte"></a>SyncReader(ReadOnlySpan&lt;byte&gt;)

```csharp
public SyncReader(ReadOnlySpan<byte> data);
```

Creates a reader over `data`.

**Parameters**

- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

## Properties

| Member | Description |
| --- | --- |
| [`AtEnd`](#m-atend) | True when every byte has been consumed. |
| [`Error`](#m-error) | The first error met, or [`SyncError.None`](SyncError.md#m-none). |
| [`Ok`](#m-ok) | True while no error has been met. |
| [`Position`](#m-position) | Bytes consumed so far. |
| [`Remaining`](#m-remaining) | Bytes left to read. |

### <a id="m-atend"></a>AtEnd

```csharp
public bool AtEnd { get; }
```

True when every byte has been consumed.

**Type:** `bool`

### <a id="m-error"></a>Error

```csharp
public SyncError Error { get; }
```

The first error met, or [`SyncError.None`](SyncError.md#m-none).

**Type:** [`SyncError`](SyncError.md)

### <a id="m-ok"></a>Ok

```csharp
public bool Ok { get; }
```

True while no error has been met.

**Type:** `bool`

### <a id="m-position"></a>Position

```csharp
public int Position { get; }
```

Bytes consumed so far.

**Type:** `int`

### <a id="m-remaining"></a>Remaining

```csharp
public int Remaining { get; }
```

Bytes left to read.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`ExpectEnd()`](#m-expectend) | Fails with [`SyncError.TrailingBytes`](SyncError.md#m-trailingbytes) if any bytes are left. |
| [`Fail(SyncError)`](#m-fail-syncerror) | Records an error unless one is already recorded. |
| [`ReadBool()`](#m-readbool) | Reads a bool; only 0 and 1 are accepted. |
| [`ReadByte()`](#m-readbyte) | Reads one raw byte. |
| [`ReadBytes(int)`](#m-readbytes-int) | Reads a length-prefixed byte block and returns a view of it without copying. |
| [`ReadCount(int)`](#m-readcount-int) | Reads a count and checks it against `limit` and the bytes left (every element takes at least one byte). |
| [`ReadFixed32()`](#m-readfixed32) | Reads four little-endian bytes. |
| [`ReadFixed64()`](#m-readfixed64) | Reads eight little-endian bytes. |
| [`ReadInt16()`](#m-readint16) | Reads a 16-bit signed integer. |
| [`ReadInt32()`](#m-readint32) | Reads a 32-bit signed integer. |
| [`ReadInt64()`](#m-readint64) | Reads a 64-bit signed integer. |
| [`ReadSByte()`](#m-readsbyte) | Reads a raw signed byte. |
| [`ReadString()`](#m-readstring) | Reads a UTF-8 string written by [`SyncWriter.WriteString()`](SyncWriter.md#m-writestring-string). |
| [`ReadUInt16()`](#m-readuint16) | Reads a 16-bit unsigned integer. |
| [`ReadUInt32()`](#m-readuint32) | Reads a 32-bit unsigned integer. |
| [`ReadUInt64()`](#m-readuint64) | Reads a 64-bit unsigned integer. |
| [`ReadVarInt()`](#m-readvarint) | Reads a zigzag varint. |
| [`ReadVarUInt()`](#m-readvaruint) | Reads an unsigned LEB128 varint of at most ten bytes. |
| [`Slice(int)`](#m-slice-int) | Starts a sub-reader over the next `length` bytes and skips them here. |

### <a id="m-expectend"></a>ExpectEnd()

```csharp
public void ExpectEnd();
```

Fails with [`SyncError.TrailingBytes`](SyncError.md#m-trailingbytes) if any bytes are left.

### <a id="m-fail-syncerror"></a>Fail(SyncError)

```csharp
public void Fail(SyncError error);
```

Records an error unless one is already recorded.

**Parameters**

- `error` ([`SyncError`](SyncError.md))

### <a id="m-readbool"></a>ReadBool()

```csharp
public bool ReadBool();
```

Reads a bool; only 0 and 1 are accepted.

**Returns:** `bool`

### <a id="m-readbyte"></a>ReadByte()

```csharp
public byte ReadByte();
```

Reads one raw byte.

**Returns:** `byte`

### <a id="m-readbytes-int"></a>ReadBytes(int)

```csharp
public ReadOnlySpan<byte> ReadBytes(int limit);
```

Reads a length-prefixed byte block and returns a view of it without copying.

**Parameters**

- `limit` (`int`)

**Returns:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`

### <a id="m-readcount-int"></a>ReadCount(int)

```csharp
public int ReadCount(int limit);
```

Reads a count and checks it against `limit` and the bytes left (every element takes at least one byte).

**Parameters**

- `limit` (`int`)

**Returns:** `int` — The count, or 0 after a failure.

### <a id="m-readfixed32"></a>ReadFixed32()

```csharp
public uint ReadFixed32();
```

Reads four little-endian bytes.

**Returns:** `uint`

### <a id="m-readfixed64"></a>ReadFixed64()

```csharp
public ulong ReadFixed64();
```

Reads eight little-endian bytes.

**Returns:** `ulong`

### <a id="m-readint16"></a>ReadInt16()

```csharp
public short ReadInt16();
```

Reads a 16-bit signed integer.

**Returns:** `short`

### <a id="m-readint32"></a>ReadInt32()

```csharp
public int ReadInt32();
```

Reads a 32-bit signed integer.

**Returns:** `int`

### <a id="m-readint64"></a>ReadInt64()

```csharp
public long ReadInt64();
```

Reads a 64-bit signed integer.

**Returns:** `long`

### <a id="m-readsbyte"></a>ReadSByte()

```csharp
public sbyte ReadSByte();
```

Reads a raw signed byte.

**Returns:** `sbyte`

### <a id="m-readstring"></a>ReadString()

```csharp
public string ReadString();
```

Reads a UTF-8 string written by [`SyncWriter.WriteString()`](SyncWriter.md#m-writestring-string).

**Returns:** `string`

### <a id="m-readuint16"></a>ReadUInt16()

```csharp
public ushort ReadUInt16();
```

Reads a 16-bit unsigned integer.

**Returns:** `ushort`

### <a id="m-readuint32"></a>ReadUInt32()

```csharp
public uint ReadUInt32();
```

Reads a 32-bit unsigned integer.

**Returns:** `uint`

### <a id="m-readuint64"></a>ReadUInt64()

```csharp
public ulong ReadUInt64();
```

Reads a 64-bit unsigned integer.

**Returns:** `ulong`

### <a id="m-readvarint"></a>ReadVarInt()

```csharp
public long ReadVarInt();
```

Reads a zigzag varint.

**Returns:** `long`

### <a id="m-readvaruint"></a>ReadVarUInt()

```csharp
public ulong ReadVarUInt();
```

Reads an unsigned LEB128 varint of at most ten bytes.

**Returns:** `ulong`

### <a id="m-slice-int"></a>Slice(int)

```csharp
public SyncReader Slice(int length);
```

Starts a sub-reader over the next `length` bytes and skips them here.

**Parameters**

- `length` (`int`)

**Returns:** [`SyncReader`](SyncReader.md)
