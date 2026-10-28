[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# FileAssetSource class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IAssetSource`](IAssetSource.md)

```csharp
public sealed class FileAssetSource : IAssetSource
```

Reads content from folders. The default roots are the program's folder (an export keeps `art/` and `data/` next to the program), `../Resources` (a macOS bundle) and the working directory (development, from the project folder), tried in that order. Paths are relative to a root and may not leave it.

## Constructors

| Member | Description |
| --- | --- |
| [`FileAssetSource()`](#m-ctor) | Creates a source over the default roots. |
| [`FileAssetSource(IEnumerable<string>)`](#m-ctor-ienumerable-string) | Creates a source over the given roots, tried in order. |

### <a id="m-ctor"></a>FileAssetSource()

```csharp
public FileAssetSource();
```

Creates a source over the default roots.

### <a id="m-ctor-ienumerable-string"></a>FileAssetSource(IEnumerable&lt;string&gt;)

```csharp
public FileAssetSource(IEnumerable<string> roots);
```

Creates a source over the given roots, tried in order.

**Parameters**

- `roots` ([`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)`<string>`): Folders that hold the content folders.

## Properties

| Member | Description |
| --- | --- |
| [`Roots`](#m-roots) | The roots, in the order tried. |

### <a id="m-roots"></a>Roots

```csharp
public IReadOnlyList<string> Roots { get; }
```

The roots, in the order tried.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

## Methods

| Member | Description |
| --- | --- |
| [`PhysicalPath(string)`](#m-physicalpath-string) | The file on disk behind a path, or null when there is none (or the content is not a file). |
| [`Read(string, long)`](#m-read-string-long) | Reads a whole file. |

### <a id="m-physicalpath-string"></a>PhysicalPath(string)

```csharp
public string? PhysicalPath(string path);
```

The file on disk behind a path, or null when there is none (or the content is not a file). Development reloads watch these.

**Parameters**

- `path` (`string`): The path relative to the content folders.

**Returns:** `string?` — An absolute path or null.

### <a id="m-read-string-long"></a>Read(string, long)

```csharp
public byte[] Read(string path, long maxBytes);
```

Reads a whole file.

**Parameters**

- `path` (`string`): The path relative to the content folders, with `/` separators.
- `maxBytes` (`long`): The most to read; a larger file must be refused before anything that size is allocated.

**Returns:** `byte[]` — The bytes.

**Exceptions**

- [`AssetException`](AssetException.md): `not_found`, `bad_path` or `too_large`.
