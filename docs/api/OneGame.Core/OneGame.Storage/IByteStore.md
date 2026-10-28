[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Storage

# IByteStore interface

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public interface IByteStore
```

A flat folder of named byte blobs: the low level under save slots and settings. A store is one place a game keeps its files for a player: a folder on disk, the browser's IndexedDB, an in-memory map for tests. Names are plain (see [`StoreNames`](StoreNames.md)). Stores never throw for a missing, unreadable or unwritable file; they answer false so the game can carry on and say so.

## Properties

| Member | Description |
| --- | --- |
| [`Description`](#m-description) | Where the files live, for settings screens and logs. |

### <a id="m-description"></a>Description

```csharp
string Description { get; }
```

Where the files live, for settings screens and logs.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`Delete(string)`](#m-delete-string) | Deletes a file. |
| [`List(string)`](#m-list-string) | The names of the files that start with `prefix`, in ordinal order. |
| [`TryRead(string, int, out byte[])`](#m-tryread-string-int-byte) | Reads a file. |
| [`Write(string, ReadOnlySpan<byte>)`](#m-write-string-readonlyspan-byte) | Writes a file, replacing any earlier one. |

### <a id="m-delete-string"></a>Delete(string)

```csharp
bool Delete(string name);
```

Deletes a file. Returns true when the file is gone afterwards (including when it never existed).

**Parameters**

- `name` (`string`)

**Returns:** `bool`

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): `name` is not a valid store name.

### <a id="m-list-string"></a>List(string)

```csharp
IReadOnlyList<string> List(string prefix = "");
```

The names of the files that start with `prefix`, in ordinal order. Half-written leftovers are not listed.

**Parameters**

- `prefix` (`string`)

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-tryread-string-int-byte"></a>TryRead(string, int, out byte\[\])

```csharp
bool TryRead(string name, int maxBytes, out byte[] data);
```

Reads a file. Returns false when it does not exist, is unreadable or is larger than `maxBytes`. The size is checked before the bytes are copied, so a huge or hostile file costs no memory.

**Parameters**

- `name` (`string`)
- `maxBytes` (`int`)
- `data` (`byte[]`)

**Returns:** `bool`

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): `name` is not a valid store name.

### <a id="m-write-string-readonlyspan-byte"></a>Write(string, ReadOnlySpan&lt;byte&gt;)

```csharp
bool Write(string name, ReadOnlySpan<byte> data);
```

Writes a file, replacing any earlier one. The replacement is atomic: after a crash or a failure the file holds either the whole old content or the whole new content, never a mix. Returns false when it could not be written (the old file stays).

**Parameters**

- `name` (`string`)
- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** `bool`

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): `name` is not a valid store name.
