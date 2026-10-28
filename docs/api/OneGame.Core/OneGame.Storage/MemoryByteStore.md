[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Storage

# MemoryByteStore class

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`IByteStore`](IByteStore.md)

```csharp
public sealed class MemoryByteStore : IByteStore
```

Files held in memory only. Headless runs and tests use it so a run never reads or changes a player's own files, and a game can hand one to a second [`SettingsFile<T>`](SettingsFile-1.md) or slot list to model "a fresh process" by sharing the same instance.

## Constructors

| Member | Description |
| --- | --- |
| [`MemoryByteStore()`](#m-ctor) | Creates an empty store. |

### <a id="m-ctor"></a>MemoryByteStore()

```csharp
public MemoryByteStore();
```

Creates an empty store.

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | How many files the store holds. |
| [`Description`](#m-description) | Where the files live, for settings screens and logs. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

How many files the store holds.

**Type:** `int`

### <a id="m-description"></a>Description

```csharp
public string Description { get; }
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
public bool Delete(string name);
```

Deletes a file. Returns true when the file is gone afterwards (including when it never existed).

**Parameters**

- `name` (`string`)

**Returns:** `bool`

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): `name` is not a valid store name.

### <a id="m-list-string"></a>List(string)

```csharp
public IReadOnlyList<string> List(string prefix = "");
```

The names of the files that start with `prefix`, in ordinal order. Half-written leftovers are not listed.

**Parameters**

- `prefix` (`string`)

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-tryread-string-int-byte"></a>TryRead(string, int, out byte\[\])

```csharp
public bool TryRead(string name, int maxBytes, out byte[] data);
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
public bool Write(string name, ReadOnlySpan<byte> data);
```

Writes a file, replacing any earlier one. The replacement is atomic: after a crash or a failure the file holds either the whole old content or the whole new content, never a mix. Returns false when it could not be written (the old file stays).

**Parameters**

- `name` (`string`)
- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** `bool`

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): `name` is not a valid store name.
