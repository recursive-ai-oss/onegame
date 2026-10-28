[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# BrowserFiles static class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public static class BrowserFiles
```

The browser's persistent file area, a flat map of names to bytes kept in IndexedDB. The page script loads it into memory before the program starts (`storageOpen`), so every call here is synchronous. A write is visible at once and reaches IndexedDB in one atomic transaction; [`BrowserFiles.Pending`](BrowserFiles.md#m-pending) counts the ones still on their way. Games use it through `OneGame.Storage.IByteStore`.

## Properties

| Member | Description |
| --- | --- |
| [`Failed`](#m-failed) | Writes IndexedDB refused (quota, a closed database). |
| [`Kind`](#m-kind) | "indexeddb" when files survive a reload, "memory" when the page could not open IndexedDB (private windows, blocked storage). |
| [`Pending`](#m-pending) | Writes that have not been committed to IndexedDB yet. |
| [`Why`](#m-why) | Why IndexedDB is not in use, or the last transaction failure; empty when nothing went wrong. |

### <a id="m-failed"></a>Failed

```csharp
public static int Failed { get; }
```

Writes IndexedDB refused (quota, a closed database). The files stay readable until the page reloads.

**Type:** `int`

### <a id="m-kind"></a>Kind

```csharp
public static string Kind { get; }
```

"indexeddb" when files survive a reload, "memory" when the page could not open IndexedDB (private windows, blocked storage).

**Type:** `string`

### <a id="m-pending"></a>Pending

```csharp
public static int Pending { get; }
```

Writes that have not been committed to IndexedDB yet. Wait for 0 before closing the page when a save must survive.

**Type:** `int`

### <a id="m-why"></a>Why

```csharp
public static string Why { get; }
```

Why IndexedDB is not in use, or the last transaction failure; empty when nothing went wrong.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`Delete(string)`](#m-delete-string) | Removes a file. |
| [`List(string)`](#m-list-string) | The keys that start with `prefix`, in ordinal order. |
| [`Read(string, Span<byte>)`](#m-read-string-span-byte) | Copies a file into `destination`, which must be exactly [`BrowserFiles.Size()`](BrowserFiles.md#m-size-string) bytes long. |
| [`Size(string)`](#m-size-string) | The size of a file, or -1 when there is none. |
| [`Write(string, ReadOnlySpan<byte>)`](#m-write-string-readonlyspan-byte) | Stores a file, replacing any earlier one. |

### <a id="m-delete-string"></a>Delete(string)

```csharp
public static void Delete(string key);
```

Removes a file.

**Parameters**

- `key` (`string`)

### <a id="m-list-string"></a>List(string)

```csharp
public static string[] List(string prefix);
```

The keys that start with `prefix`, in ordinal order.

**Parameters**

- `prefix` (`string`)

**Returns:** `string[]`

### <a id="m-read-string-span-byte"></a>Read(string, Span&lt;byte&gt;)

```csharp
public static bool Read(string key, Span<byte> destination);
```

Copies a file into `destination`, which must be exactly [`BrowserFiles.Size()`](BrowserFiles.md#m-size-string) bytes long.

**Parameters**

- `key` (`string`)
- `destination` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`)

**Returns:** `bool`

### <a id="m-size-string"></a>Size(string)

```csharp
public static int Size(string key);
```

The size of a file, or -1 when there is none.

**Parameters**

- `key` (`string`)

**Returns:** `int`

### <a id="m-write-string-readonlyspan-byte"></a>Write(string, ReadOnlySpan&lt;byte&gt;)

```csharp
public static void Write(string key, ReadOnlySpan<byte> data);
```

Stores a file, replacing any earlier one.

**Parameters**

- `key` (`string`)
- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)
