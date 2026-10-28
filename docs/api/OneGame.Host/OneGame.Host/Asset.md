[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# Asset class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public abstract class Asset
```

A loaded file. Handles are shared: loading the same path twice returns the same handle and counts one more reference, and [`Asset.Release()`](Asset.md#m-release) gives it back. A development reload swaps the content under the handle, so hold the handle (or, for textures, the texture itself, which is updated in place) and read [`Asset<T>.Value`](Asset-1.md#m-value) when you use it.

## Properties

| Member | Description |
| --- | --- |
| [`CanReload`](#m-canreload) | True when an edit to the file can be swapped in while the game runs. |
| [`Digest`](#m-digest) | A 64-bit FNV-1a digest of the file's bytes in hex, so a replay or a test can tell which data it ran with. |
| [`Error`](#m-error) | The reason the last load or reload failed, or null. |
| [`FilePath`](#m-filepath) | The file on disk, or null when the content does not come from a file. |
| [`IsPending`](#m-ispending) | True when a changed data file was read and checked but not swapped in; [`Assets.ApplyPendingData()`](Assets.md#m-applypendingdata) applies it. |
| [`IsReady`](#m-isready) | True once the content is decoded and in use. |
| [`Kind`](#m-kind) | What the asset holds. |
| [`Path`](#m-path) | The path the game asked for, relative to the content folders. |
| [`Ready`](#m-ready) | Completes when the content is ready, or faults with an [`AssetException`](AssetException.md) when the load failed. |
| [`References`](#m-references) | How many loads have not been released yet. |
| [`StampLength`](#m-stamplength) | The length of [`Asset.FilePath`](Asset.md#m-filepath) when it was last read. |
| [`StampUtc`](#m-stamputc) | The write time of [`Asset.FilePath`](Asset.md#m-filepath) when it was last read. |
| [`Version`](#m-version) | 1 after the first load, plus one for every reload that was applied. |

### <a id="m-canreload"></a>CanReload

```csharp
public bool CanReload { get; }
```

True when an edit to the file can be swapped in while the game runs.

**Type:** `bool`

### <a id="m-digest"></a>Digest

```csharp
public string Digest { get; }
```

A 64-bit FNV-1a digest of the file's bytes in hex, so a replay or a test can tell which data it ran with.

**Type:** `string`

### <a id="m-error"></a>Error

```csharp
public string? Error { get; }
```

The reason the last load or reload failed, or null. After a failed reload the previous content is still in use.

**Type:** `string?`

### <a id="m-filepath"></a>FilePath

```csharp
public string? FilePath { get; }
```

The file on disk, or null when the content does not come from a file. Development reloads watch it.

**Type:** `string?`

### <a id="m-ispending"></a>IsPending

```csharp
public bool IsPending { get; }
```

True when a changed data file was read and checked but not swapped in; [`Assets.ApplyPendingData()`](Assets.md#m-applypendingdata) applies it.

**Type:** `bool`

### <a id="m-isready"></a>IsReady

```csharp
public bool IsReady { get; }
```

True once the content is decoded and in use. A background load is not ready until [`Assets.Update()`](Assets.md#m-update) or a read of [`Asset<T>.Value`](Asset-1.md#m-value) finishes it.

**Type:** `bool`

### <a id="m-kind"></a>Kind

```csharp
public AssetKind Kind { get; }
```

What the asset holds.

**Type:** [`AssetKind`](AssetKind.md)

### <a id="m-path"></a>Path

```csharp
public string Path { get; }
```

The path the game asked for, relative to the content folders.

**Type:** `string`

### <a id="m-ready"></a>Ready

```csharp
public Task Ready { get; }
```

Completes when the content is ready, or faults with an [`AssetException`](AssetException.md) when the load failed.

**Type:** [`Task`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task)

### <a id="m-references"></a>References

```csharp
public int References { get; }
```

How many loads have not been released yet.

**Type:** `int`

### <a id="m-stamplength"></a>StampLength

```csharp
public long StampLength { get; }
```

The length of [`Asset.FilePath`](Asset.md#m-filepath) when it was last read.

**Type:** `long`

### <a id="m-stamputc"></a>StampUtc

```csharp
public DateTime StampUtc { get; }
```

The write time of [`Asset.FilePath`](Asset.md#m-filepath) when it was last read.

**Type:** [`DateTime`](https://learn.microsoft.com/dotnet/api/system.datetime)

### <a id="m-version"></a>Version

```csharp
public int Version { get; }
```

1 after the first load, plus one for every reload that was applied.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Release()`](#m-release) | Gives one reference back. |

### <a id="m-release"></a>Release()

```csharp
public void Release();
```

Gives one reference back. At zero the asset leaves the cache and its GPU memory is freed; do not use it afterwards.
