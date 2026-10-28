[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# AssetInfo record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AssetInfo`](AssetInfo.md)&gt;

```csharp
public sealed record AssetInfo : IEquatable<AssetInfo>
```

One loaded asset in the `assets` report.

## Constructors

| Member | Description |
| --- | --- |
| [`AssetInfo(string, string, int, int, string, string, string?, string?)`](#m-ctor-string-string-int-int-string-string-string-string) | One loaded asset in the `assets` report. |

### <a id="m-ctor-string-string-int-int-string-string-string-string"></a>AssetInfo(string, string, int, int, string, string, string?, string?)

```csharp
public AssetInfo(string Path, string Kind, int Version, int References, string State, string Digest, string? File, string? Error);
```

One loaded asset in the `assets` report.

**Parameters**

- `Path` (`string`): The path the game asked for.
- `Kind` (`string`): `texture`, `svg`, `data`, `sound`, `model` or `font`.
- `Version` (`int`): 1 after the first load, plus one per applied reload.
- `References` (`int`): Loads not yet released.
- `State` (`string`): `ready`, `loading` (background load), `pending` (changed data waiting for apply), `error` (the last reload or load failed; the old content is in use) or `failed` (never loaded).
- `Digest` (`string`): Digest of the file's bytes as last read.
- `File` (`string?`): The file on disk, when there is one.
- `Error` (`string?`): The reason of the last failure.

## Properties

| Member | Description |
| --- | --- |
| [`Digest`](#m-digest) | Digest of the file's bytes as last read. |
| [`Error`](#m-error) | The reason of the last failure. |
| [`File`](#m-file) | The file on disk, when there is one. |
| [`Kind`](#m-kind) | `texture`, `svg`, `data`, `sound`, `model` or `font`. |
| [`Path`](#m-path) | The path the game asked for. |
| [`References`](#m-references) | Loads not yet released. |
| [`State`](#m-state) | `ready`, `loading` (background load), `pending` (changed data waiting for apply), `error` (the last reload or load failed; the old content is in use) or `failed` (never loaded). |
| [`Version`](#m-version) | 1 after the first load, plus one per applied reload. |

### <a id="m-digest"></a>Digest

```csharp
public string Digest { get; init; }
```

Digest of the file's bytes as last read.

**Type:** `string`

### <a id="m-error"></a>Error

```csharp
public string? Error { get; init; }
```

The reason of the last failure.

**Type:** `string?`

### <a id="m-file"></a>File

```csharp
public string? File { get; init; }
```

The file on disk, when there is one.

**Type:** `string?`

### <a id="m-kind"></a>Kind

```csharp
public string Kind { get; init; }
```

`texture`, `svg`, `data`, `sound`, `model` or `font`.

**Type:** `string`

### <a id="m-path"></a>Path

```csharp
public string Path { get; init; }
```

The path the game asked for.

**Type:** `string`

### <a id="m-references"></a>References

```csharp
public int References { get; init; }
```

Loads not yet released.

**Type:** `int`

### <a id="m-state"></a>State

```csharp
public string State { get; init; }
```

`ready`, `loading` (background load), `pending` (changed data waiting for apply), `error` (the last reload or load failed; the old content is in use) or `failed` (never loaded).

**Type:** `string`

### <a id="m-version"></a>Version

```csharp
public int Version { get; init; }
```

1 after the first load, plus one per applied reload.

**Type:** `int`
