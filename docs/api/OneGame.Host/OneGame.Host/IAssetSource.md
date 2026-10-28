[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# IAssetSource interface

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public interface IAssetSource
```

Where an [`Assets`](Assets.md) service reads content from. The default reads files; a platform whose content is not a folder supplies its own.

## Methods

| Member | Description |
| --- | --- |
| [`PhysicalPath(string)`](#m-physicalpath-string) | The file on disk behind a path, or null when there is none (or the content is not a file). |
| [`Read(string, long)`](#m-read-string-long) | Reads a whole file. |

### <a id="m-physicalpath-string"></a>PhysicalPath(string)

```csharp
string? PhysicalPath(string path);
```

The file on disk behind a path, or null when there is none (or the content is not a file). Development reloads watch these.

**Parameters**

- `path` (`string`): The path relative to the content folders.

**Returns:** `string?` — An absolute path or null.

### <a id="m-read-string-long"></a>Read(string, long)

```csharp
byte[] Read(string path, long maxBytes);
```

Reads a whole file.

**Parameters**

- `path` (`string`): The path relative to the content folders, with `/` separators.
- `maxBytes` (`long`): The most to read; a larger file must be refused before anything that size is allocated.

**Returns:** `byte[]` — The bytes.

**Exceptions**

- [`AssetException`](AssetException.md): `not_found`, `bad_path` or `too_large`.
