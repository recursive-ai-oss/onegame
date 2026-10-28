[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Storage

# FolderByteStore class

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`IByteStore`](IByteStore.md)

```csharp
public sealed class FolderByteStore : IByteStore
```

Files kept in a folder on disk: the store for desktop, Android and iOS (where the folder is the app's own storage). A write goes to a temporary file in the same folder, is flushed to the device, and then replaces the real file in one rename, so a crash or a full disk never leaves half a file under the real name.

## Constructors

| Member | Description |
| --- | --- |
| [`FolderByteStore(string)`](#m-ctor-string) | Creates a store in `directory`; the folder is made on the first write. |

### <a id="m-ctor-string"></a>FolderByteStore(string)

```csharp
public FolderByteStore(string directory);
```

Creates a store in `directory`; the folder is made on the first write.

**Parameters**

- `directory` (`string`)

## Properties

| Member | Description |
| --- | --- |
| [`Description`](#m-description) | Where the files live, for settings screens and logs. |
| [`Folder`](#m-folder) | The folder holding the files. |

### <a id="m-description"></a>Description

```csharp
public string Description { get; }
```

Where the files live, for settings screens and logs.

**Type:** `string`

### <a id="m-folder"></a>Folder

```csharp
public string Folder { get; }
```

The folder holding the files.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`DefaultDirectory(string)`](#m-defaultdirectory-string) | The folder a game uses by default: `appName` under the user's local application data (`~/.local/share`, `%LocalAppData%`, `~/Library/Application Support`, or the app's sandbox on a phone). |
| [`Delete(string)`](#m-delete-string) | Deletes a file. |
| [`List(string)`](#m-list-string) | The names of the files that start with `prefix`, in ordinal order. |
| [`TryRead(string, int, out byte[])`](#m-tryread-string-int-byte) | Reads a file. |
| [`Write(string, ReadOnlySpan<byte>)`](#m-write-string-readonlyspan-byte) | Writes a file, replacing any earlier one. |

### <a id="m-defaultdirectory-string"></a>DefaultDirectory(string)

```csharp
public static string DefaultDirectory(string appName);
```

The folder a game uses by default: `appName` under the user's local application data (`~/.local/share`, `%LocalAppData%`, `~/Library/Application Support`, or the app's sandbox on a phone).

**Parameters**

- `appName` (`string`)

**Returns:** `string`

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
